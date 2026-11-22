using ZeroSum.Engine;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var book = new BitmapOrderBook();

app.MapGet("/book", () => new {bestBid = book.BestBid(), bestAsk = book.BestAsk()});

app.Run();
public partial class Program
{
}