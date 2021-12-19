namespace SwampMonster.CLI
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using CommandLine;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.FindSymbols;
  using Core;

  public static class Program
  {
    public static async Task Main(string[] args)
    {
      var result = await Parser.Default.ParseArguments<Options>(args)
        .WithParsedAsync(Run);
      await result.WithNotParsedAsync(HandleParseError);
    }

    private static async Task Run(Options opt)
    {
      var anal = await Analyser.Create(opt.SolutionFilePath);
      var refMap = await anal.Analyse();

      DumpReferencesMap(opt.SolutionFilePath, refMap);
      DumpSourceFiles(anal);
    }

    private static void DumpSourceFiles(Analyser anal)
    {
      var docs = anal.Solution.Projects.SelectMany(proj => proj.Documents);
      foreach (var doc in docs)
      {
        Console.WriteLine($"  {doc.FilePath}");
      }
    }

    private static void DumpReferencesMap(string solnFilePath, Dictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
    {
      Console.WriteLine($"{solnFilePath}");
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

    private static Task HandleParseError(IEnumerable<Error> errs)
    {
      if (errs.IsVersion())
      {
        Console.WriteLine("Version Request");
        return Task.CompletedTask;
      }

      if (errs.IsHelp())
      {
        Console.WriteLine("Help Request");
        return Task.CompletedTask;
        ;
      }

      Console.WriteLine("Parser Fail");
      return Task.CompletedTask;
    }
  }
}
