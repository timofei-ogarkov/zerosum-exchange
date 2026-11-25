using System.Threading.Channels;
namespace ZeroSum.Engine;

public class OrderBookProcessor
{
    private readonly IOrderBook _book;
    private readonly Channel<Action> _queue;
    public OrderBookProcessor(IOrderBook book)
    {
       _book = book; 
       _queue = Channel.CreateUnbounded<Action>(new UnboundedChannelOptions { SingleReader = true});
       Task.Run(ProcessQueueAsync);
    }
    public Task<T> ExecuteAsync<T>(Func<IOrderBook, T> work)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Writer.TryWrite(() =>
        {
            try
            {
                completion.SetResult(work(_book));
            }
            catch(Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }
    private async Task ProcessQueueAsync()
    {
        await foreach (var job in _queue.Reader.ReadAllAsync())
        {
            job();
        }
    }
    
}