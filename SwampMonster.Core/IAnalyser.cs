namespace SwampMonster.Core;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

public interface IAnalyser
{
  Task<Dictionary<ISymbol, IEnumerable<ReferencedSymbol>>> Analyse();
  Solution Solution { get; }
  Dictionary<string, string> GetDocumentMap();

  // [fully-qualified-event-name] --> [source-file-path]
  Dictionary<string, string> GetEventSourceFileMap(IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap);

  string GetFullyQualifiedEventName(ISymbol evt);

  // [fully-qualified-event-name] --> [Guid-file-path where event is received]
  IEnumerable<KeyValuePair<string, string>> GetSourceLinks(
    string csFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap);

  // [fully-qualified-event-name] --> [Guid-file-path where event is generated]
  IEnumerable<KeyValuePair<string, string>> GetSinkLinks(
    string csFilePath,
    IReadOnlyDictionary<ISymbol, IEnumerable<ReferencedSymbol>> refMap,
    IReadOnlyDictionary<string, string> docMap);

  // true if location is a source of event
  bool IsSource(ISymbol evt, ReferenceLocation loc);
}
