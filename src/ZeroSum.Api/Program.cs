using ZeroSum.Engine;
using System.Text.Json.Serialization;
using ZeroSum.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();
var processor = new OrderBookProcessor(new BitmapOrderBook());
long nextId = 1;

app.MapGet("/book", async () => await processor.ExecuteAsync(book => new {bestBid = book.BestBid(), bestAsk = book.BestAsk()}));
app.MapPost("/orders", async (OrderRequest request) =>
{
    try
    {
        var result = await processor.ExecuteAsync(book =>
        {
            var order = new Order
            {
                Id = nextId++,
                TraderId = "api",
                OrderSide = request.Side,
                Type = request.Type,
                Price = request.Price,
                Quantity = request.Quantity,
                Timestamp = DateTimeOffset.UtcNow
            };
            var trades = book.Add(order);
            return new { orderId = order.Id, trades };
        });
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/orders/{id}", async (long id) =>
{
    var cancelled = await processor.ExecuteAsync(book => book.Cancel(id));
    if (cancelled)
    {
        return Results.Ok();
    }
    return Results.NotFound();
});
app.Run();
public partial class Program
{
}