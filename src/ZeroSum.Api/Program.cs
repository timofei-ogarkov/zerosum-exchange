using ZeroSum.Engine;
using System.Text.Json.Serialization;
using ZeroSum.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();
var book = new BitmapOrderBook();
long nextId = 1;

app.MapGet("/book", () => new { bestBid = book.BestBid(), bestAsk = book.BestAsk() });
app.MapPost("/orders", (OrderRequest request) =>
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
    return new {orderId = order.Id, trades};
});

app.Run();
public partial class Program
{
}