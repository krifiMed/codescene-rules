using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Models;

namespace ECommerce.Catalog.Api.Endpoints;

public static class ProductBulkUpdateEndpoints
{
    public static RouteGroupBuilder MapProductBulkUpdateEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/bulk-update", HandleBulkUpdate).WithName("BulkUpdateProducts");
        return group;
    }

    private static async Task<IResult> HandleBulkUpdate(
        List<BulkProductUpdate> updates, CatalogDbContext db)
    {
        var count = RequestLog.IncrementRequestCount();
        RequestLog.Add($"Bulk update request #{count}, {updates.Count} items");

        var errors = new List<string>();
        var updated = new List<int>();

        for (int i = 0; i < updates.Count; i++)
        {
            var error = await ProcessSingleUpdate(updates[i], i, db, updated);
            if (error != null)
                errors.Add(error);
        }

        await db.SaveChangesAsync();
        RequestLog.Add($"Bulk update completed: {updated.Count} updated, {errors.Count} errors");
        return Results.Ok(new { Updated = updated, Errors = errors });
    }

    private static async Task<string?> ProcessSingleUpdate(
        BulkProductUpdate u, int index, CatalogDbContext db, List<int> updated)
    {
        if (u.Id <= 0)
            return $"Item at index {index}: invalid id";
        if (string.IsNullOrEmpty(u.Name))
            return $"Product {u.Id}: name is required";
        if (u.Price < 0)
            return $"Product {u.Id}: price must be >= 0";
        if (u.Stock < 0)
            return $"Product {u.Id}: stock must be >= 0";

        var product = await db.Products.FindAsync(u.Id);
        if (product == null)
            return $"Product {u.Id} not found";

        ApplyUpdate(product, u);
        var discount = DiscountResult.Calculate(product.Price, product.AvailableStock);
        RequestLog.LogDiscount(product.Id, product.Price, discount);
        updated.Add(u.Id);
        return null;
    }

    private static void ApplyUpdate(Product product, BulkProductUpdate u)
    {
        product.Name = u.Name!;
        product.Price = u.Price;
        product.AvailableStock = u.Stock;
        if (u.Description != null)
            product.Description = u.Description;
    }
}

public record BulkProductUpdate(int Id, string? Name, string? Description, decimal Price, int Stock);
