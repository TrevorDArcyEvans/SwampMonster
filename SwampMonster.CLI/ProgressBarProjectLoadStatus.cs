namespace SwampMonster.CLI
{
  using System;
  using Microsoft.CodeAnalysis.MSBuild;

  public sealed class ProgressBarProjectLoadStatus : IProgress<ProjectLoadProgress>
  {
    public void Report(ProjectLoadProgress value)
    {
      Console.Out.WriteLine($"{value.Operation} {value.FilePath}");
    }
  }
}
