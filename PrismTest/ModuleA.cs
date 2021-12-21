namespace PrismTest
{
  using Prism.Events;

  public sealed class ModuleA : ModuleBase
  {
    public ModuleA(IEventAggregator evtAgg) :
      base(evtAgg)
    {
    }

    public override string Name => "ModuleA";
  }
}
