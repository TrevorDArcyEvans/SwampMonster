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

  public sealed class Analyser
  {
    private readonly string _solnFilePath;

    public static async Task<Analyser> Create(string solnFilePath)
    {
      if (!File.Exists(solnFilePath))
      {
        throw new FileNotFoundException($"Could not find {solnFilePath}");
      }

      var retval = new Analyser(solnFilePath);
      await retval.LoadSolution();

      return retval;
    }

    private Analyser(string solnFilePath)
    {
      if (!MSBuildLocator.IsRegistered)
      {
        var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
        MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
      }

      _solnFilePath = solnFilePath;
    }

    public Solution Solution { get; private set; }

    public async Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> Analyse()
    {
      var allEvents = await GetAllEvents();
      var refMap = await GetAllEventReferences(allEvents);

      return refMap;
    }

    // [event] --> [locations]
    // Note:  [locations] includes source+sink
    //        sink includes subscribe+unsubscribe
    private async Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> GetAllEventReferences(List<ISymbol> allEvents)
    {
      var refMap = new Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>();
      foreach (var thisEvent in allEvents)
      {
        var refsToEvents = await SymbolFinder.FindReferencesAsync(thisEvent, Solution);
        refMap.Add(thisEvent, refsToEvents);
      }

      return refMap;
    }

    private async Task<List<ISymbol>> GetAllEvents()
    {
      var allEvents = new List<ISymbol>();
      foreach (var project in Solution.Projects)
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

    private async Task LoadSolution()
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

      Solution = await workspace.OpenSolutionAsync(_solnFilePath);
    }
  }
}
