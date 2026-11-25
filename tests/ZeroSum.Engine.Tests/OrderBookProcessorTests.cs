namespace ZeroSum.Engine.Tests;

public class OrderBookProcessorTests
{
    [Fact]
    public async Task ExecuteAsync_EmptyBook_ReturnsNullBestAsk()
    {
        var processor = new OrderBookProcessor(new BitmapOrderBook());
        var bestAsk = await processor.ExecuteAsync(book => book.BestAsk());
        Assert.Null(bestAsk);
    }

    [Fact]
    public async Task ExecuteAsync_ThousandCallsAtOnce_RunsOneAtATime()
    {
        var processor = new OrderBookProcessor(new BitmapOrderBook());
        int counter = 0;
        var tasks = new List<Task>();
        for(int i = 0; i < 1000; i++)
        {
            var executeTask = processor.ExecuteAsync(book => ++counter);
            tasks.Add(executeTask);
        }
        await Task.WhenAll(tasks);
        Assert.Equal(1000,counter);
    }
}