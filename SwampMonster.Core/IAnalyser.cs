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
}
