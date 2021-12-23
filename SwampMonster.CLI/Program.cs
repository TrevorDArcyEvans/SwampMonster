namespace SwampMonster.CLI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using CommandLine;
using Manoli.Utils.CSharpFormat;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Scriban;
using Core;

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
    var anal = opt.UseEventAggregator
      ? await AnalyserFactory.CreateEventAggregatorAnalyser(opt.SolutionFilePath, new ProgressBarProjectLoadStatus())
      : await AnalyserFactory.CreateEventAnalyser(opt.SolutionFilePath, new ProgressBarProjectLoadStatus());
    var refMap = await anal.Analyse();
    var docMap = anal.GetDocumentMap();
    var evtSrcMap = anal.GetEventSourceFileMap(refMap);

    Directory.CreateDirectory(opt.OutputDirectory);

    CopySupportFiles(opt.OutputDirectory);
    UpdateEvents(anal, opt.OutputDirectory, refMap, docMap, evtSrcMap);
    GenerateSourceFiles(anal, opt.OutputDirectory, refMap, docMap);
    GenerateIndexFile(anal, opt.OutputDirectory, docMap, evtSrcMap);

    DumpReferencesMap(anal, refMap);
  }

  private static void GenerateIndexFile(
    IAnalyser anal,
    string optOutputDirectory,
    IReadOnlyDictionary<string, string> docMap,
    IReadOnlyDictionary<string, string> evtSrcMap)
  {
    var evtLinksMap = evtSrcMap.Keys
      .ToDictionary(evt => HttpUtility.HtmlEncode(evt), evt => docMap.ContainsKey(evtSrcMap[evt]) ? docMap[evtSrcMap[evt]] : string.Empty);
    var solnDir = $"{Path.GetDirectoryName(anal.Solution.FilePath)}\\";
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
    IAnalyser anal,
    string optOutputDirectory,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap,
    IReadOnlyDictionary<string, string> evtSrcMap)
  {
    var events = refMap.Keys.Select(evt => $"{HttpUtility.HtmlEncode(anal.GetFullyQualifiedEventName(evt))}");
    var eventFileMap = evtSrcMap.Keys
      .ToDictionary(evt => evt, evt => docMap.ContainsKey(evtSrcMap[evt]) ? docMap[evtSrcMap[evt]] : string.Empty);

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
    IAnalyser anal,
    string optOutputDirectory,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap)
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

      var solnAbsFilePath = anal.Solution.FilePath;
      var srcLinksMap = anal.GetSourceLinks(csFilePath, refMap, docMap);
      var sinkLinksMap = anal.GetSinkLinks(csFilePath, refMap, docMap);

      var docFileStr = temp.Render(
        new
        {
          file_name = Path.GetFileName(csFilePath),
          csharp_source_file = htmlSrc,
          sources_links_map = srcLinksMap,
          sinks_links_map = sinkLinksMap
        });
      var docFilePath = Path.Combine(optOutputDirectory, docMap[csFilePath]);
      File.WriteAllText(docFilePath, docFileStr);
    }
  }

  private static void DumpReferencesMap(
    IAnalyser anal,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
  {
    Console.WriteLine($"{anal.Solution.FilePath}");
    foreach (var evt in refMap.Keys)
    {
      var evtLoc = evt.Locations.Single();
      var evtFilePath = evtLoc.SourceTree?.FilePath;
      Console.WriteLine($"  {anal.GetFullyQualifiedEventName(evt)} --> {evtLoc.SourceTree?.FilePath ?? string.Empty}[{evtLoc.SourceSpan.Start}..{evtLoc.SourceSpan.End}]");
      foreach (var refSym in refMap[evt])
      {
        foreach (var loc in refSym.Locations)
        {
          var locFilePath = loc.Location.SourceTree?.FilePath;
          var srcSink = evtFilePath == locFilePath ? "*" : "X";
          Console.WriteLine($"    [{srcSink}] {loc.Location}");
        }
      }
    }

    Console.WriteLine("Key:");
    Console.WriteLine("  [x] = sink   event");
    Console.WriteLine("  [*] = source event");
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
