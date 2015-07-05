using System;
using System.IO;
using System.Linq;
using Microsoft.Scripting;

namespace WindawesomeIrb
{
  public static class Irb
  {
    private static IronRubyEngine _engine;
    private static DirectoryInfo _configPath = new DirectoryInfo("Config");

    public static void Main(string[] args)
    {
      Console.WriteLine("Windawesome irb...");
      _engine = new IronRubyEngine();
      _engine.InitializeWindawesomeEnironment();
      _engine.Repl();
    }

    public static string SetConfigPath(string configPath)
    {
      var dirInfo = new DirectoryInfo(configPath);
      if (dirInfo.Exists)
      {
        _configPath = dirInfo;
        return dirInfo.FullName; 
      }
      throw new FileNotFoundException("The config path was not found", dirInfo.FullName);
    }

    public static string LoadConfig(string configFilenameWildcardPattern)
    {
      var configFileInfo = _configPath.GetFiles(configFilenameWildcardPattern).FirstOrDefault();
      if (configFileInfo != null && configFileInfo.Exists)
      {        
        return LoadFile(configFileInfo.FullName);
      }
      throw new FileNotFoundException(string.Format("There is no matching config file in directory {0}", _configPath.FullName));
    }

    public static string LoadFile(string fullPath)
    {
      return LoadFileWithErrorHandling(() =>
        {
          _engine.Execute(fullPath);
          return fullPath;
        });
    }

    private static string LoadFileWithErrorHandling(Func<string> action)
    {
      try
      {
        return action();
      }
      catch (SyntaxErrorException e)
      {
        Console.WriteLine("Error starting at line {0} column {1}", e.Line, e.Column);
        Console.WriteLine(e.Message);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
      return string.Empty;
    }
  }
}
