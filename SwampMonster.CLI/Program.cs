namespace SwampMonster.CLI;

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
    var anal = await AnalyserFactory.CreateEventAnalyser(opt.SolutionFilePath, new ProgressBarProjectLoadStatus());
    var refMap = await anal.Analyse();
    var docMap = anal.GetDocumentMap();
    var evtSrcMap = GetEventSourceFileMap(refMap);

    Directory.CreateDirectory(opt.OutputDirectory);

    CopySupportFiles(opt.OutputDirectory);
    UpdateEvents(opt.OutputDirectory, refMap, docMap, evtSrcMap);
    GenerateSourceFiles(opt.OutputDirectory, anal.Solution.FilePath, refMap, docMap);
    GenerateIndexFile(opt.OutputDirectory, anal.Solution.FilePath, docMap, evtSrcMap);

    DumpReferencesMap(opt.SolutionFilePath, refMap);
  }

  private static void GenerateIndexFile(
    string optOutputDirectory,
    string solnAbsFilePath,
    IReadOnlyDictionary<string, string> docMap,
    IReadOnlyDictionary<string, string> evtSrcMap)
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
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap,
    IReadOnlyDictionary<string, string> evtSrcMap)
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
    string solnAbsFilePath,
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

      var srcLinksMap = GetSourceLinks(solnAbsFilePath, csFilePath, refMap, docMap);
      var sinkLinksMap = GetSinkLinks(solnAbsFilePath, csFilePath, refMap, docMap);

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

  // [fully-qualified-event-name] --> [Guid-file-path where event is received]
  private static IEnumerable<KeyValuePair<string, string>> GetSourceLinks(
    string solnAbsFilePath,
    string csFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap)
  {
    foreach (var evt in refMap.Keys)
    {
      // only looking for events raised/received in src file
      var evtLoc = evt.Locations.Single();
      var evtFilePath = evtLoc.SourceTree.FilePath;
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
          var locFilePath = loc.Location.SourceTree.FilePath;
          if (csFilePath == locFilePath)
          {
            // event is received in src file, so skip
            continue;
          }

          // event raised in src file but received in another file 
          yield return new KeyValuePair<string, string>($"{GetFullyQualifiedEventName(evt)} --> {Path.GetRelativePath(solnAbsFilePath, locFilePath)}", docMap[locFilePath]);
        }
      }
    }
  }

  // [fully-qualified-event-name] --> [Guid-file-path where event is generated]
  private static IEnumerable<KeyValuePair<string, string>> GetSinkLinks(
    string solnAbsFilePath,
    string csFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap)
  {
    foreach (var evt in refMap.Keys)
    {
      var evtLoc = evt.Locations.Single();
      var evtFilePath = evtLoc.SourceTree.FilePath;
      if (csFilePath == evtFilePath)
      {
        continue;
      }

      foreach (var refSym in refMap[evt])
      {
        foreach (var loc in refSym.Locations)
        {
          var locFilePath = loc.Location.SourceTree.FilePath;
          if (csFilePath != locFilePath)
          {
            continue;
          }

          yield return new KeyValuePair<string, string>($"{GetFullyQualifiedEventName(evt)} --> {Path.GetRelativePath(solnAbsFilePath, evtFilePath)}", docMap[evtFilePath]);
        }
      }
    }
  }

  // [fully-qualified-event-name] --> [source-file-path]
  private static Dictionary<string, string> GetEventSourceFileMap(IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
  {
    var retval = new Dictionary<string, string>();
    foreach (var evt in refMap.Keys)
    {
      var evtLoc = evt.Locations.Single();
      retval.Add($"{GetFullyQualifiedEventName(evt)}", evtLoc.SourceTree.FilePath);
    }

    return retval;
  }

  private static void DumpReferencesMap(
    string solnFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap)
  {
    Console.WriteLine($"{solnFilePath}");
    foreach (var evt in refMap.Keys)
    {
      var evtLoc = evt.Locations.Single();
      var evtFilePath = evtLoc.SourceTree.FilePath;
      Console.WriteLine($"  {GetFullyQualifiedEventName(evt)} --> {evtLoc.SourceTree.FilePath}[{evtLoc.SourceSpan.Start}..{evtLoc.SourceSpan.End}]");
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
      
    Console.WriteLine("Key:");
    Console.WriteLine("  [x] = sink   event");
    Console.WriteLine("  [*] = source event");
  }

  private static string GetFullyQualifiedEventName(ISymbol evt) => $"{evt.ContainingNamespace}.{evt.ContainingSymbol.Name}.{evt.Name}";

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