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

  protected override async Task<HashSet<ISymbol>> GetAllEvents()
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

    return allEvents.Distinct().ToHashSet();
  }

  public override string GetFullyQualifiedEventName(ISymbol evt) => $"{evt.ContainingNamespace}.{evt.ContainingSymbol.Name}.{evt.Name}";

  public override IEnumerable<KeyValuePair<string, string>> GetSourceLinks(
    string csFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap)
  {
    foreach (var evt in refMap.Keys)
    {
      // only looking for events raised/received in src file
      var evtLoc = evt.Locations.Single();
      var evtFilePath = evtLoc.SourceTree?.FilePath;
      if (csFilePath != evtFilePath)
      {
        // event raised in another file, so skip
        continue;
      }

      // we now have an event which is raised in src file
      foreach (var refSym in refMap[evt])
      {
        foreach (var loc in refSym.Locations)
        {
          var locFilePath = loc.Location.SourceTree?.FilePath;
          if (csFilePath == locFilePath)
          {
            // event is received in src file, so skip
            continue;
          }

          // event raised in src file but received in another file 
          yield return new KeyValuePair<string, string>($"{GetFullyQualifiedEventName(evt)} --> {Path.GetRelativePath(Solution.FilePath, locFilePath)}[{loc.Location.SourceSpan.Start}..{loc.Location.SourceSpan.End}]", docMap[locFilePath]);
        }
      }
    }
  }

  public override IEnumerable<KeyValuePair<string, string>> GetSinkLinks(
    string csFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap)
  {
    foreach (var evt in refMap.Keys)
    {
      var evtLoc = evt.Locations.Single();
      var evtFilePath = evtLoc.SourceTree?.FilePath;
      if (csFilePath == evtFilePath)
      {
        continue;
      }

      foreach (var refSym in refMap[evt])
      {
        foreach (var loc in refSym.Locations)
        {
          var locFilePath = loc.Location.SourceTree?.FilePath;
          if (csFilePath != locFilePath)
          {
            continue;
          }

          yield return new KeyValuePair<string, string>($"{GetFullyQualifiedEventName(evt)} --> {Path.GetRelativePath(Solution.FilePath, evtFilePath)}[{loc.Location.SourceSpan.Start}..{loc.Location.SourceSpan.End}]", docMap[evtFilePath]);
        }
      }
    }
  }

  public override bool IsSource(ISymbol evt, ReferenceLocation loc)
  {
    var evtLoc = evt.Locations.Single();
    var evtFilePath = evtLoc.SourceTree?.FilePath;
    var locFilePath = loc.Location.SourceTree?.FilePath;
    var isSource = evtFilePath == locFilePath;

    return isSource;
  }
}
