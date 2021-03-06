namespace SwampMonster.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

public abstract class AnalyserBase : IAnalyser
{
  private readonly string _solnFilePath;

  protected AnalyserBase(string solnFilePath)
  {
    if (!MSBuildLocator.IsRegistered)
    {
      var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
      MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
    }

    _solnFilePath = solnFilePath;
  }

  public async Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> Analyse()
  {
    var allEvents = await GetAllEvents();
    var refMap = await GetAllEventReferences(allEvents);

    return refMap;
  }

  public Solution Solution { get; private set; }

  // [original-source-file-path] --> [Guid-file-path]
  public Dictionary<string, string> GetDocumentMap()
  {
    var docMap = Solution.Projects
      .SelectMany(proj => proj.Documents)
      .ToDictionary(doc => doc.FilePath, doc => Path.ChangeExtension(doc.Id.Id.ToString(), ".html"));
    return docMap;
  }

  // [fully-qualified-event-name] --> [source-file-path]
  public Dictionary<string, string> GetEventSourceFileMap(IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
  {
    var retval = new Dictionary<string, string>();
    foreach (var evt in refMap.Keys)
    {
      var evtLoc = evt.Locations.Single();
      retval.Add($"{GetFullyQualifiedEventName(evt)}", evtLoc.SourceTree?.FilePath ?? string.Empty);
    }

    return retval;
  }

  protected abstract Task<HashSet<ISymbol>> GetAllEvents();

  protected async Task LoadSolution(
    IProgress<ProjectLoadProgress> progress = null,
    CancellationToken cancellationToken = default)
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

    Solution = await workspace.OpenSolutionAsync(_solnFilePath, progress, cancellationToken);
  }

  public abstract string GetFullyQualifiedEventName(ISymbol evt);
  public abstract IEnumerable<KeyValuePair<string, string>> GetSourceLinks(string csFilePath, IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap, IReadOnlyDictionary<string, string> docMap);
  public abstract IEnumerable<KeyValuePair<string, string>> GetSinkLinks(string csFilePath, IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap, IReadOnlyDictionary<string, string> docMap);
  public abstract bool IsSource(ISymbol evt, ReferenceLocation loc);

  // [event] --> [locations]
  // Note:  [locations] includes source+sink
  //        sink includes subscribe+unsubscribe
  private async Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> GetAllEventReferences(IEnumerable<ISymbol> allEvents)
  {
    var refMap = new Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>();
    foreach (var thisEvent in allEvents)
    {
      var refsToEvents = await SymbolFinder.FindReferencesAsync(thisEvent, Solution);
      refMap.Add(thisEvent, refsToEvents);
    }

    return refMap;
  }
}
