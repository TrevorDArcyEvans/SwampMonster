namespace SwampMonster.CLI
{
  using System.Threading.Tasks;
  using SwampMonster.Core;

  public static class Program
  {
    public static async Task Main(string[] args)
    {
      var solnPath = args[0];
      var anal = new Analyser(solnPath);
      await anal.Analyse();
    }
  }
}
