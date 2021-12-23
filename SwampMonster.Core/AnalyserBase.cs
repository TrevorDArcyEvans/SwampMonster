namespace SwampMonster.Core;

using System;
using System.Collections.Generic;
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

  public Solution Solution { get; protected set; }

  protected abstract Task<IEnumerable<ISymbol>> GetAllEvents();
  protected abstract Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> GetAllEventReferences(IEnumerable<ISymbol> allEvents);

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
}
