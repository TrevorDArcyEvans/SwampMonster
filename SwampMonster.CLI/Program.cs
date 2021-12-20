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
      var evtSrcMap = GetEventSourceFileMap(refMap);

      Directory.CreateDirectory(opt.OutputDirectory);

      CopySupportFiles(opt.OutputDirectory);
      UpdateEvents(opt.OutputDirectory, refMap, docMap, evtSrcMap);
      GenerateSourceFiles(opt.OutputDirectory, docMap);
      GenerateIndexFile(opt.OutputDirectory, anal.Solution.FilePath, docMap, evtSrcMap);

      DumpReferencesMap(opt.SolutionFilePath, refMap);
    }

    private static void GenerateIndexFile(
      string optOutputDirectory,
      string solnAbsFilePath,
      Dictionary<string, string> docMap,
      Dictionary<string, string> evtSrcMap)
    {
      var evtLinksMap = evtSrcMap.Keys.ToDictionary(evtName => evtName, evtName => docMap[evtSrcMap[evtName]]);
      var solnDir = $"{Path.GetDirectoryName(solnAbsFilePath)}\\";
      var unsortedSrcFilesMap = docMap.Keys.ToDictionary(docFilePath => docFilePath.Replace(solnDir, string.Empty), docFilePath => docMap[docFilePath]);
      var srcFilesMap = new SortedDictionary<string, string>(unsortedSrcFilesMap);

      var exeAssy = Assembly.GetExecutingAssembly().Location;
      var assyDir = Path.GetDirectoryName(exeAssy);
      var tempFilePath = Path.Combine(assyDir, "wwwroot", "index.html");
      var tempText = File.ReadAllText(tempFilePath);
      var temp = Template.Parse(tempText, tempFilePath);
      var indexText = temp.Render(
        new
        {
          events_links_map = evtLinksMap,
          src_files_map = srcFilesMap
        });
      var indexFilePath = Path.Combine(optOutputDirectory, "index.html");
      File.WriteAllText(indexFilePath, indexText);
    }

    private static void UpdateEvents(
      string optOutputDirectory,
      Dictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
      Dictionary<string, string> docMap,
      Dictionary<string, string> evtSrcMap)
    {
      var events = refMap.Keys.Select(evt => $"{evt.ContainingNamespace}.{evt.ContainingSymbol.Name}.{evt.Name}");
      var eventFileMap = evtSrcMap.Keys.ToDictionary(evt => evt, evt => docMap[evtSrcMap[evt]]);

      var exeAssy = Assembly.GetExecutingAssembly().Location;
      var assyDir = Path.GetDirectoryName(exeAssy);
      var tempFilePath = Path.Combine(assyDir, "wwwroot", "autocomplete.js");
      var tempText = File.ReadAllText(tempFilePath);
      var temp = Template.Parse(tempText, tempFilePath);
      var autocompleteText = temp.Render(
        new
        {
          events,
          event_file_map = eventFileMap
        });
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

    private static void GenerateSourceFiles(
      string optOutputDirectory,
      Dictionary<string, string> docMap)
    {
      var exeAssy = Assembly.GetExecutingAssembly().Location;
      var assyDir = Path.GetDirectoryName(exeAssy);
      var tempFilePath = Path.Combine(assyDir, "wwwroot", "csharp.html");
      var tempText = File.ReadAllText(tempFilePath);
      var temp = Template.Parse(tempText, tempFilePath);
      foreach (var csFilePath in docMap.Keys)
      {
        var srcFmt = (CSharpFormat)CodeFormatFactory.Create(SourceLanguages.CSharp);
        using var strm = File.Open(csFilePath, FileMode.Open);
        var htmlSrc = srcFmt.FormatCode(strm);
        var docFileStr = temp.Render(
          new
          {
            file_name = Path.GetFileName(csFilePath),
            csharp_source_file = htmlSrc,
            sinks_links = "aaa", // TODO   sinks_links
            sources_links = "bbb" // TODO   sources_links
          });
        var docFilePath = Path.Combine(optOutputDirectory, docMap[csFilePath]);
        File.WriteAllText(docFilePath, docFileStr);
      }
    }

    // [original-source-file-path] --> [Guid-file-path]
    private static Dictionary<string, string> GetDocumentMap(Analyser anal)
    {
      var docMap = anal.Solution.Projects
        .SelectMany(proj => proj.Documents)
        .ToDictionary(doc => doc.FilePath, doc => Path.ChangeExtension(doc.Id.Id.ToString(), ".html"));
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

    // [fully-qualified-event-name] --> [source-file-path]
    private static Dictionary<string, string> GetEventSourceFileMap(Dictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
    {
      var retval = new Dictionary<string, string>();
      foreach (var evt in refMap.Keys)
      {
        var evtLoc = evt.Locations.Single();
        retval.Add($"{evt.ContainingNamespace}.{evt.ContainingSymbol.Name}.{evt.Name}", evtLoc.SourceTree.FilePath);
      }

      return retval;
    }

    private static void DumpReferencesMap(
      string solnFilePath,
      Dictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
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
