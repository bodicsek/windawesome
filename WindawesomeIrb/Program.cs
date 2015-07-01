using System;
using System.IO;
using Microsoft.Scripting;

namespace WindawesomeIrb
{
  public static class Irb
  {
    private static Engine _engine;
    private static string _configPath = "Config/";

    public static void Main(string[] args)
    {
      Console.WriteLine("Windawesome irb...");
      _engine = new Engine(new Windawesome.Windawesome());
      _engine.InitializeEnironment();
      _engine.Repl();
    }

    public static string SetConfigPath(string configPath)
    {
      _configPath = configPath.EndsWith("\\") || configPath.EndsWith("/")
                    ? configPath
                    : configPath + "/";
      return _configPath;
    }

    public static void ExecuteConfig(string configFilename)
    {
      try
      {
        var fileInfo = new FileInfo(_configPath + configFilename);
        _engine.Execute(fileInfo.FullName);
      }
      catch(SyntaxErrorException e)
      {
        Console.WriteLine("Error starting at line {0} column {1}", e.Line, e.Column);
        Console.WriteLine(e.Message);
      }
      catch(Exception e)
      {
        Console.WriteLine(e.Message);
      }
    }
  }
}
