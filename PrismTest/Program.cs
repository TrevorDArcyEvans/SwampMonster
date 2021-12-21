namespace PrismTest
{
  using Prism.Events;

  public static class Program
  {
    public static void Main(string[] args)
    {
      var evtAgg = new EventAggregator();
      using var modA = new ModuleA(evtAgg);
      using var modB = new ModuleB(evtAgg);
      var msg = new Message
      {
        Text = "Test"
      };

      evtAgg.GetEvent<MessageEvent>().Publish(msg);
    }
  }
}
