namespace PrismTest;

using Prism.Events;

public sealed class ModuleB : ModuleBase
{
  public ModuleB(IEventAggregator evtAgg) :
    base(evtAgg)
  {
  }

  public override string Name => "ModuleB";
}