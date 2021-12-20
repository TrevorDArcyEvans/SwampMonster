using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Manoli.Utils.CSharpFormat;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Scriban;
using SwampMonster.Core;

namespace SwampMonster.CLI
{
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
      var docMap = GetDocumentMap(anal);

      CopySupportFiles(opt.OutputDirectory);
      UpdateEvents(opt.OutputDirectory, refMap);
      GenerateSourceFiles(opt.OutputDirectory, docMap);

      DumpReferencesMap(opt.SolutionFilePath, refMap);
    }

    private static void UpdateEvents(string optOutputDirectory, Dictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
    {
      var exeAssy = Assembly.GetExecutingAssembly().Location;
      var assyDir = Path.GetDirectoryName(exeAssy);
      var tempFilePath = Path.Combine(assyDir, "wwwroot", "autocomplete.js");
      var events = refMap.Keys.Select(sym => $"\"{sym.Name}\"");
      var eventsStr = string.Join(',', events);
      var tempText = File.ReadAllText(tempFilePath);
      var temp = Template.Parse(tempText, tempFilePath);
      var autocompleteText = temp.Render(new { Events = eventsStr });
      var autocompleteFilePath = Path.Combine(optOutputDirectory, "autocomplete.js");
      File.WriteAllText(autocompleteFilePath, autocompleteText);
    }

    private static void CopySupportFiles(string optOutputDirectory)
    {
      var exeAssy = Assembly.GetExecutingAssembly().Location;
      var assyDir = Path.GetDirectoryName(exeAssy);
      var supportDir = Path.Combine(assyDir, "wwwroot");
      CopyDir.Copy(supportDir, optOutputDirectory);
    }

    private static void GenerateSourceFiles(string optOutputDirectory, Dictionary<string, DocumentId> docMap)
    {
      Directory.CreateDirectory(optOutputDirectory);
      var exeAssy = Assembly.GetExecutingAssembly().Location;
      var assyDir = Path.GetDirectoryName(exeAssy);
      var tempFilePath = Path.Combine(assyDir, "wwwroot", "csharp.html");
      var tempText = File.ReadAllText(tempFilePath);
      var temp = Template.Parse(tempText, tempFilePath);
      foreach (var csFilePath in docMap.Keys)
      {
        var docId = docMap[csFilePath];
        var docFilePath = Path.Combine(optOutputDirectory, docId.Id.ToString());
        var docFilePathExtn = Path.ChangeExtension(docFilePath, ".html");
        var srcFmt = (CSharpFormat)CodeFormatFactory.Create(SourceLanguages.CSharp);
        using var strm = File.Open(csFilePath, FileMode.Open);
        var htmlSrc = srcFmt.FormatCode(strm);
        var docFileStr = temp.Render(
          new
          {
            file_name = Path.GetFileName(csFilePath), 
            csharp_source_file = htmlSrc,
            sinks_links = "aaa",
            sources_links = "bbb"
          });
        File.WriteAllText(docFilePathExtn, docFileStr);
      }
    }

    // [file path] --> [DocumentId]
    private static Dictionary<string, DocumentId> GetDocumentMap(Analyser anal)
    {
      var docMap = anal.Solution.Projects
        .SelectMany(proj => proj.Documents)
        .ToDictionary(doc => doc.FilePath, doc => doc.Id);
      return docMap;
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
        Console.WriteLine($"  {evt.ContainingNamespace}.{evt.ContainingSymbol.Name}.{evt.Name} --> {evtLoc.SourceTree.FilePath}[{evtLoc.SourceSpan.Start}..{evtLoc.SourceSpan.End}]");
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
