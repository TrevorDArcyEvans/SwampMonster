namespace CSharpFormat.CLI
{
  using Manoli.Utils.CSharpFormat;

  public static class Program
  {
    public static void Main(string[] args)
    {
      var csFilePath = args[0];
      var srcFmt = (Manoli.Utils.CSharpFormat.CSharpFormat)CodeFormatFactory.Create(SourceLanguages.CSharp);
      using var strm = File.Open(csFilePath, FileMode.Open);
      var htmlSrc = srcFmt.FormatCode(strm);
      
      Console.WriteLine(csFilePath);
      Console.WriteLine();
      Console.WriteLine(htmlSrc);
    }
  }
}
