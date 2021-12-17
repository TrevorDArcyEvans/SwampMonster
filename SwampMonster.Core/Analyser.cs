namespace SwampMonster.Core
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using Microsoft.Build.Locator;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp.Syntax;
  using Microsoft.CodeAnalysis.FindSymbols;
  using Microsoft.CodeAnalysis.MSBuild;

  public class Analyser
  {
    private readonly string _solnPath;

    public Analyser(string solnPath)
    {
      if (!File.Exists(solnPath))
      {
        throw new FileNotFoundException($"Could not find {solnPath}");
      }

      _solnPath = solnPath;

      if (!MSBuildLocator.IsRegistered)
      {
        var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
        MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
      }
    }

    public async Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> Analyse()
    {
      var solution = await LoadSolution(_solnPath);
      var allEvents = await GetAllEvents(solution);
      var refMap = await GetAllEventReferences(allEvents, solution);

      return refMap;
    }

    // [event] --> [locations]
    // Note:  [locations] includes source+sink
    //        sink includes subscribe+unsubscribe
    private static async Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> GetAllEventReferences(List<ISymbol> allEvents, Solution solution)
    {
      var refMap = new Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>();
      foreach (var thisEvent in allEvents)
      {
        var refsToEvents = await SymbolFinder.FindReferencesAsync(thisEvent, solution);
        refMap.Add(thisEvent, refsToEvents);
      }

      return refMap;
    }

    private static async Task<List<ISymbol>> GetAllEvents(Solution solution)
    {
      var allEvents = new List<ISymbol>();
      foreach (var project in solution.Projects)
      {
        var compilation = await project.GetCompilationAsync();
        var docs = project.Documents;
        foreach (var doc in docs)
        {
          var synTree = await doc.GetSyntaxTreeAsync();
          var model = compilation.GetSemanticModel(synTree);
          var root = await synTree.GetRootAsync();
          var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
          foreach (var classNode in classNodes)
          {
            var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(classNode);
            var events = symbol.GetMembers().OfType<IEventSymbol>();
            allEvents.AddRange(events);
            var fieldEvents = symbol.GetMembers().OfType<IFieldSymbol>().Where(ev => ev.Type.Name == "EventHandler");
            allEvents.AddRange(fieldEvents);
          }
        }
      }

      return allEvents;
    }

    private static async Task<Solution> LoadSolution(string solnPath)
    {
      var workspace = MSBuildWorkspace.Create();
      workspace.SkipUnrecognizedProjects = true;
      workspace.WorkspaceFailed += (sender, args) =>
      {
        if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
        {
          Console.Error.WriteLine(args.Diagnostic.Message);
        }
      };

      var solution = await workspace.OpenSolutionAsync(solnPath);

      return solution;
    }
  }
}
