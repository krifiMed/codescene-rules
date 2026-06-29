namespace ECommerce.Catalog.Api.Endpoints;

public static class UnhealthyCatalogEndpoints
{
    public static RouteGroupBuilder MapUnhealthyCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v2/products").WithTags("Catalog-v2");
        group.MapProductSearchEndpoints();
        group.MapProductBulkUpdateEndpoints();
        group.MapProductReportEndpoints();
        group.MapProductLogEndpoints();
        return group;
    }
}
