namespace SwampMonster.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

public sealed class EventAnalyser : AnalyserBase
{
  public static async Task<EventAnalyser> Create(
    string solnFilePath,
    IProgress<ProjectLoadProgress> progress = null,
    CancellationToken cancellationToken = default)
  {
    if (!File.Exists(solnFilePath))
    {
      throw new FileNotFoundException($"Could not find {solnFilePath}");
    }

    var retval = new EventAnalyser(solnFilePath);
    await retval.LoadSolution(progress, cancellationToken);

    return retval;
  }

  private EventAnalyser(string solnFilePath) :
    base(solnFilePath)
  {
  }

  // [event] --> [locations]
  // Note:  [locations] includes source+sink
  //        sink includes subscribe+unsubscribe
  protected override async Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> GetAllEventReferences(IEnumerable<ISymbol> allEvents)
  {
    var refMap = new Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>();
    foreach (var thisEvent in allEvents)
    {
      var refsToEvents = await SymbolFinder.FindReferencesAsync(thisEvent, Solution);
      refMap.Add(thisEvent, refsToEvents);
    }

    return refMap;
  }

  protected override async Task<IEnumerable<ISymbol>> GetAllEvents()
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
}