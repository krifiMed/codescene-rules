using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Ordering.Api.Models;
using ECommerce.Ordering.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Ordering.Tests;

public class OrderingApiTests : IClassFixture<OrderingApiTests.OrderingApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public OrderingApiTests(OrderingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOrders_ReturnsListOfOrders()
    {
        var orders = await _client.GetFromJsonAsync<List<Order>>("/api/orders", JsonOptions);

        Assert.NotNull(orders);
        Assert.IsType<List<Order>>(orders);
    }

    [Fact]
    public async Task GetOrderById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/orders/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_EmptyItems_Returns400()
    {
        var request = new { Customer = "Alice", Items = Array.Empty<object>() };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("at least one item", body);
    }

    [Fact]
    public async Task CreateOrder_InvalidProductId_Returns400()
    {
        var request = new
        {
            Customer = "Bob",
            Items = new[] { new { ProductId = 999, Quantity = 1 } }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("999", body);
        Assert.Contains("does not exist", body);
    }

    [Fact]
    public async Task CreateOrder_ValidProduct_Returns201WithOrder()
    {
        var request = new
        {
            Customer = "Charlie",
            Items = new[] { new { ProductId = 1, Quantity = 2 } }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal("Charlie", order.Customer);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Single(order.Items);
        Assert.Equal(1, order.Items[0].ProductId);
        Assert.Equal("Mechanical Keyboard", order.Items[0].ProductName);
        Assert.Equal(2, order.Items[0].Quantity);
    }

    [Fact]
    public async Task CreateOrder_MultipleItems_CalculatesTotal()
    {
        var request = new
        {
            Customer = "Diana",
            Items = new[]
            {
                new { ProductId = 1, Quantity = 1 },
                new { ProductId = 2, Quantity = 3 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(119.99m + 49.50m * 3, order.Total);
    }

    [Fact]
    public async Task CreateOrder_ThenGetById_RoundTrips()
    {
        var request = new
        {
            Customer = "Eve",
            Items = new[] { new { ProductId = 3, Quantity = 1 } }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var created = await createResponse.Content.ReadFromJsonAsync<Order>(JsonOptions);

        var fetched = await _client.GetFromJsonAsync<Order>($"/api/orders/{created!.Id}", JsonOptions);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Eve", fetched.Customer);
        Assert.Single(fetched.Items);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Custom factory that replaces the CatalogServiceClient's HttpClient
    /// with a mock handler returning known products (IDs 1-4).
    /// </summary>
    public class OrderingApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real CatalogServiceClient registration and replace with a mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(CatalogServiceClient));
                if (descriptor is not null)
                    services.Remove(descriptor);

                // Remove any HttpClient registrations for CatalogServiceClient
                var httpClientDescriptors = services
                    .Where(d => d.ServiceType.FullName?.Contains("CatalogServiceClient") == true)
                    .ToList();
                foreach (var d in httpClientDescriptors)
                    services.Remove(d);

                services.AddSingleton(new MockCatalogHandler());
                services.AddHttpClient<CatalogServiceClient>()
                    .ConfigurePrimaryHttpMessageHandler<MockCatalogHandler>();
            });
        }
    }
}

internal class MockCatalogHandler : HttpMessageHandler
{
    private static readonly Dictionary<int, CatalogProduct> Products = new()
    {
        [1] = new(1, "Mechanical Keyboard", "Hot-swappable RGB keyboard", 119.99m, 50),
        [2] = new(2, "Wireless Mouse", "Ergonomic 8k DPI mouse", 49.50m, 120),
        [3] = new(3, "4K Monitor", "27-inch IPS display", 329.00m, 25),
        [4] = new(4, "USB-C Hub", "7-in-1 docking hub", 39.99m, 200),
    };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        // Parse /api/products/{id}
        if (path.StartsWith("/api/products/") && int.TryParse(path["/api/products/".Length..], out var id))
        {
            if (Products.TryGetValue(id, out var product))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(product)
                });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
