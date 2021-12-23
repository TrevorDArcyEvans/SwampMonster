namespace SwampMonster.CLI;

using CommandLine;

internal sealed class Options
{
  [Value(index: 0, Required = true, HelpText = "Path to solution file")]
  public string SolutionFilePath { get; set; }

  [Option('o', "output", Required = true, HelpText = "Output directory")]
  public string OutputDirectory { get; set; }

  [Option('a', "agg", Required = false, Default = false, HelpText = "Use EventAggregatorAnalyser")]
  public bool UseEventAggregator { get; set; }
}