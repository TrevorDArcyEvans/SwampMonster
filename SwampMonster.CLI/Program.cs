namespace SwampMonster.CLI
{
  using System;
  using System.Linq;
  using System.Threading.Tasks;
  using Core;

  public static class Program
  {
    public static async Task Main(string[] args)
    {
      var solnPath = args[0];
      var anal = new Analyser(solnPath);
      var refMap = await anal.Analyse();

      Console.WriteLine($"{solnPath}");
      foreach (var evt in refMap.Keys)
      {
        var evtLoc = evt.Locations.Single();
        var evtFilePath = evtLoc.SourceTree.FilePath;
        Console.WriteLine($"  {evt.ContainingNamespace}.{evt.ContainingSymbol.Name}.{evt.Name} --> {evtLoc}");
        foreach (var refSym in refMap[evt])
        {
          foreach (var loc in refSym.Locations)
          {
            var locFilePath = loc.Location.SourceTree.FilePath;
            var srcSink = evtFilePath == locFilePath ? "*" : "X";
            Console.WriteLine($"    [{srcSink}] {loc.Location}");
          }
        }
      }
    }
  }
}
