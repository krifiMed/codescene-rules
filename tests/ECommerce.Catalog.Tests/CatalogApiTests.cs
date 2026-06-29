using System.Net;
using System.Net.Http.Json;
using ECommerce.Catalog.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Catalog.Tests;

public class CatalogApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CatalogApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededProducts()
    {
        var products = await _client.GetFromJsonAsync<List<Product>>("/api/products");

        Assert.NotNull(products);
        Assert.Equal(4, products.Count);
    }

    [Theory]
    [InlineData(1, "Mechanical Keyboard")]
    [InlineData(2, "Wireless Mouse")]
    [InlineData(3, "4K Monitor")]
    [InlineData(4, "USB-C Hub")]
    public async Task GetProductById_ExistingId_ReturnsProduct(int id, string expectedName)
    {
        var product = await _client.GetFromJsonAsync<Product>($"/api/products/{id}");

        Assert.NotNull(product);
        Assert.Equal(id, product.Id);
        Assert.Equal(expectedName, product.Name);
    }

    [Fact]
    public async Task GetProductById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_Returns201WithProduct()
    {
        var request = new { Name = "Test Widget", Description = "A test product", Price = 9.99m, AvailableStock = 10 };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        Assert.Equal("Test Widget", product.Name);
        Assert.Equal(9.99m, product.Price);
        Assert.True(product.Id > 0);
    }

    [Fact]
    public async Task CreateProduct_ThenGetById_RoundTrips()
    {
        var request = new { Name = "Round Trip Item", Description = (string?)null, Price = 1.00m, AvailableStock = 5 };
        var createResponse = await _client.PostAsJsonAsync("/api/products", request);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

        var fetched = await _client.GetFromJsonAsync<Product>($"/api/products/{created!.Id}");

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Round Trip Item", fetched.Name);
        Assert.Null(fetched.Description);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AliveEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
