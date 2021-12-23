namespace SwampMonster.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;

public static class AnalyserFactory
{
  public static async Task<IAnalyser> CreateEventAnalyser(
    string solnFilePath,
    IProgress<ProjectLoadProgress> progress = null,
    CancellationToken cancellationToken = default)
  {
    return await EventAnalyser.Create(solnFilePath, progress, cancellationToken);
  }

  public static async Task<IAnalyser> CreateEventAggregatorAnalyser(
    string solnFilePath,
    IProgress<ProjectLoadProgress> progress = null,
    CancellationToken cancellationToken = default)
  {
    return await EventAggregatorAnalyser.Create(solnFilePath, progress, cancellationToken);
  }
}
