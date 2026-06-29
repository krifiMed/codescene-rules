namespace ECommerce.Catalog.Api.Endpoints;

public static class ProductLogEndpoints
{
    public static RouteGroupBuilder MapProductLogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/logs", () => Results.Ok(RequestLog.GetSnapshot()))
            .WithName("GetLogs");

        group.MapDelete("/logs", () =>
        {
            RequestLog.Clear();
            return Results.Ok("Logs cleared");
        }).WithName("ClearLogs");

        return group;
    }
}
