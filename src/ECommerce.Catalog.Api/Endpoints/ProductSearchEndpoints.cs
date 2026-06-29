using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ECommerce.Catalog.Api.Endpoints;

public static class ProductSearchEndpoints
{
    public static RouteGroupBuilder MapProductSearchEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/search", HandleSearch).WithName("SearchProductsV2");
        return group;
    }

    private static async Task<IResult> HandleSearch(
        string? name, string? category, int? minPrice, int? maxPrice,
        string? sortBy, string? sortOrder, int? page, int? pageSize,
        bool? includeOutOfStock, string? format, CatalogDbContext db)
    {
        var count = RequestLog.IncrementRequestCount();
        RequestLog.Add($"Search request #{count}");

        var products = await db.Products.AsNoTracking().ToListAsync();
        var filtered = FilterProducts(products, name, minPrice, maxPrice, includeOutOfStock);
        var results = filtered.Select(BuildProductResult).ToList();
        var sorted = SortResults(results, sortBy, sortOrder);
        var (paged, actualPage, actualPageSize) = Paginate(sorted, page, pageSize);

        if (IsCsvFormat(format))
            return FormatAsCsv(paged);

        var outOfStockCount = products.Count(p => p.AvailableStock <= 0);
        LogSearchCompletion(results);
        return BuildSearchResponse(paged, results, actualPage, actualPageSize, outOfStockCount);
    }

    private static List<Product> FilterProducts(
        List<Product> products, string? name, int? minPrice, int? maxPrice, bool? includeOutOfStock)
    {
        return products
            .Where(p => MatchesNameFilter(p, name))
            .Where(p => MatchesPriceFilter(p, minPrice, maxPrice))
            .Where(p => !IsExcludedOutOfStock(p, includeOutOfStock))
            .ToList();
    }

    private static bool MatchesNameFilter(Product p, string? name)
    {
        if (string.IsNullOrEmpty(name))
            return true;
        return p.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private static bool MatchesPriceFilter(Product p, int? minPrice, int? maxPrice)
    {
        if (minPrice.HasValue && maxPrice.HasValue)
            return p.Price >= minPrice.Value && p.Price <= maxPrice.Value;
        if (minPrice.HasValue)
            return p.Price >= minPrice.Value;
        if (maxPrice.HasValue)
            return p.Price <= maxPrice.Value;
        return true;
    }

    private static bool IsExcludedOutOfStock(Product p, bool? includeOutOfStock)
    {
        if (p.AvailableStock > 0)
            return false;
        return includeOutOfStock != true;
    }

    private static ProductSearchResult BuildProductResult(Product p)
    {
        var discount = DiscountResult.Calculate(p.Price, p.AvailableStock);
        RequestLog.LogDiscount(p.Id, p.Price, discount);
        return new ProductSearchResult(
            p.Id, p.Name, p.Description, p.Price, p.AvailableStock,
            (int)discount.Type, discount.Percent, discount.Status, discount.DiscountedPrice);
    }

    private static List<ProductSearchResult> SortResults(
        List<ProductSearchResult> results, string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortBy))
            return results;
        var descending = sortOrder == "desc";
        return sortBy switch
        {
            "price" => ApplySort(results, r => r.Price, descending),
            "name" => ApplySort(results, r => r.Name, descending),
            "stock" => ApplySort(results, r => r.AvailableStock, descending),
            _ => results
        };
    }

    private static List<ProductSearchResult> ApplySort<TKey>(
        List<ProductSearchResult> items, Func<ProductSearchResult, TKey> selector, bool descending)
    {
        return descending
            ? items.OrderByDescending(selector).ToList()
            : items.OrderBy(selector).ToList();
    }

    private static (List<ProductSearchResult> Items, int Page, int PageSize) Paginate(
        List<ProductSearchResult> results, int? page, int? pageSize)
    {
        var actualPage = Math.Max(page ?? 1, 1);
        var actualSize = Math.Clamp(pageSize ?? 10, 1, 100);
        var skip = (actualPage - 1) * actualSize;
        return (results.Skip(skip).Take(actualSize).ToList(), actualPage, actualSize);
    }

    private static bool IsCsvFormat(string? format) =>
        format?.Equals("csv", StringComparison.OrdinalIgnoreCase) ?? false;

    private static IResult FormatAsCsv(List<ProductSearchResult> results)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Id,Name,Description,Price,Stock,DiscountType,DiscountPercent,Status,FinalPrice");
        foreach (var r in results)
        {
            csv.AppendLine($"{r.Id},{r.Name},{r.Description},{r.Price},{r.AvailableStock}," +
                $"{r.DiscountType},{r.DiscountPercent},{r.Status},{r.FinalPrice}");
        }
        return Results.Text(csv.ToString(), "text/csv");
    }

    private static void LogSearchCompletion(List<ProductSearchResult> results)
    {
        var avgPrice = results.Count > 0 ? results.Average(r => r.Price) : 0m;
        RequestLog.Add($"Search completed: {results.Count} results, avg price: {avgPrice}");
    }

    private static IResult BuildSearchResponse(
        List<ProductSearchResult> paged, List<ProductSearchResult> allResults,
        int page, int pageSize, int outOfStockCount)
    {
        var totalCount = allResults.Count;
        var avgPrice = allResults.Count > 0 ? allResults.Average(r => r.Price) : 0m;
        return Results.Ok(new
        {
            Data = paged,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Stats = new
            {
                AveragePrice = avgPrice,
                TotalStock = allResults.Sum(r => r.AvailableStock),
                OutOfStockCount = outOfStockCount
            }
        });
    }

    private record ProductSearchResult(
        int Id, string? Name, string? Description, decimal Price, int AvailableStock,
        int DiscountType, double DiscountPercent, string Status, decimal FinalPrice);
}
