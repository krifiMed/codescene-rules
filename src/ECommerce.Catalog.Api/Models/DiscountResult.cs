namespace ECommerce.Catalog.Api.Models;

public enum DiscountType
{
    None = 0,
    BulkDiscount = 1,
    MediumDiscount = 2,
    LowStockWarning = 3
}

public record DiscountResult(DiscountType Type, double Percent, string Status, decimal DiscountedPrice)
{
    public static DiscountResult Calculate(decimal price, int availableStock)
    {
        if (price > 100 && availableStock > 50)
            return Create(DiscountType.BulkDiscount, 10, "bulk-discount", price);
        if (price > 50 && availableStock > 20)
            return Create(DiscountType.MediumDiscount, 5, "medium-discount", price);
        if (availableStock < 5 && availableStock > 0)
            return Create(DiscountType.LowStockWarning, 0, "low-stock-warning", price);
        return new DiscountResult(DiscountType.None, 0, "normal", price);
    }

    private static DiscountResult Create(DiscountType type, double percent, string status, decimal price)
    {
        var discountedPrice = price - (price * (decimal)percent / 100);
        return new DiscountResult(type, percent, status, discountedPrice);
    }
}
