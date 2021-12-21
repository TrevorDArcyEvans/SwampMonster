namespace PrismTest
{
  using System;
  using Prism.Events;

  public abstract class ModuleBase : IDisposable
  {
    private readonly IEventAggregator _evtAgg;

    public ModuleBase(IEventAggregator evtAgg)
    {
      _evtAgg = evtAgg;
      var evt = _evtAgg.GetEvent<MessageEvent>();
      evt.Subscribe(HandleMessage);
    }

    public abstract string Name { get; }

    private void HandleMessage(Message msg)
    {
      Console.WriteLine($"{Name}  {msg.Sent:HH:mm:ss} {msg.Text}");
    }

    public void Dispose()
    {
      _evtAgg.GetEvent<MessageEvent>().Unsubscribe(HandleMessage);
    }
  }
}
