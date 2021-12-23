namespace SwampMonster.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

public sealed class EventAggregatorAnalyser : AnalyserBase
{
  const string EventAggregatorSourceDefinition = "Prism.Events.PubSubEvent<TPayload>.Publish(TPayload)";
  const string EventAggregatorSinkDefinition = "Prism.Events.PubSubEvent<TPayload>.Subscribe(System.Action<TPayload>)";

  string[] EventAggregatorDefinitions =
  {
    "Prism.Events.IEventAggregator.GetEvent<TEventType>()",
    "Prism.Events.EventAggregator.GetEvent<TEventType>()",
    EventAggregatorSinkDefinition,
    "Prism.Events.PubSubEvent<TPayload>.Unsubscribe(System.Action<TPayload>)",
    EventAggregatorSourceDefinition
  };

  private EventAggregatorAnalyser(string solnFilePath) :
    base(solnFilePath)
  {
  }

  public static async Task<EventAggregatorAnalyser> Create(
    string solnFilePath,
    IProgress<ProjectLoadProgress> progress = null,
    CancellationToken cancellationToken = default)
  {
    if (!File.Exists(solnFilePath))
    {
      throw new FileNotFoundException($"Could not find {solnFilePath}");
    }

    var retval = new EventAggregatorAnalyser(solnFilePath);
    await retval.LoadSolution(progress, cancellationToken);

    return retval;
  }

  protected override async Task<IEnumerable<ISymbol>> GetAllEvents()
  {
    var allEvents = new List<ISymbol>();
    foreach (var project in Solution.Projects)
    {
      foreach (var doc in project.Documents)
      {
        var synRoot = await doc.GetSyntaxRootAsync();
        var model = await doc.GetSemanticModelAsync();
        var methInvocs = synRoot.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var methInvoc in methInvocs)
        {
          var methSym = model.GetSymbolInfo(methInvoc).Symbol;
          if (methSym is null)
          {
            continue;
          }

          var origDef = ((IMethodSymbol)methSym).OriginalDefinition.ToString();
          if (origDef is not (EventAggregatorSinkDefinition or EventAggregatorSourceDefinition))
          {
            continue;
          }

          allEvents.Add(methSym);
        }
      }
    }

    return allEvents;
  }

  public override string GetFullyQualifiedEventName(ISymbol evt) => evt.ToString();

  public override IEnumerable<KeyValuePair<string, string>> GetSourceLinks(
    string csFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap)
  {
    foreach (var evt in refMap.Keys)
    {
      var origDef = ((IMethodSymbol)evt).OriginalDefinition.ToString();
      if (origDef is not EventAggregatorSourceDefinition)
      {
        continue;
      }

      // we now have a src event
      foreach (var refSym in refMap[evt])
      {
        foreach (var loc in refSym.Locations)
        {
          var locFilePath = loc.Location.SourceTree?.FilePath;
          if (csFilePath != locFilePath)
          {
            // src event not raised in csFilePath, so skip
            continue;
          }

          yield return new KeyValuePair<string, string>($"{HttpUtility.HtmlEncode(GetFullyQualifiedEventName(evt))} --> {Path.GetRelativePath(Solution.FilePath, locFilePath)}", docMap[locFilePath]);
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
      var origDef = ((IMethodSymbol)evt).OriginalDefinition.ToString();
      if (origDef is not EventAggregatorSinkDefinition)
      {
        continue;
      }

      // we now have a sink event
      foreach (var refSym in refMap[evt])
      {
        foreach (var loc in refSym.Locations)
        {
          var locFilePath = loc.Location.SourceTree?.FilePath;
          if (csFilePath != locFilePath)
          {
            // sink event not received in csFilePath, so skip
            continue;
          }

          yield return new KeyValuePair<string, string>($"{HttpUtility.HtmlEncode(GetFullyQualifiedEventName(evt))} --> {Path.GetRelativePath(Solution.FilePath, locFilePath)}", string.Empty);
        }
      }
    }
  }

  public override bool IsSource(ISymbol evt, ReferenceLocation loc)
  {
    var origDef = ((IMethodSymbol)evt).OriginalDefinition.ToString();
    var isSource = origDef is EventAggregatorSourceDefinition;

    return isSource;
  }
}
