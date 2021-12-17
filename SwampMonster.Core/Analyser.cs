using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SwampMonster.Core
{
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

    public async Task Analyse()
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
      var solution = await workspace.OpenSolutionAsync(_solnPath);
      foreach (var project in solution.Projects)
      {
        var compilation = await project.GetCompilationAsync();
        var docs = project.Documents;
        foreach (var doc in docs)
        {
          var synTree = await doc.GetSyntaxTreeAsync();
          var model = compilation.GetSemanticModel(synTree);
        }
      }
    }
  }
}
