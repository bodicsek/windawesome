using System.Linq;
using Microsoft.Scripting.Hosting;
using IronRuby;
using System;
using System.Reflection;
using System.IO;

namespace WindawesomeIrb
{
  class IronRubyEngine
  {
    private ScriptEngine _engine;
    private ScriptScope _scope;

    public IronRubyEngine()
    {
      _engine = Ruby.CreateEngine();
      _scope = _engine.CreateScope();
    }

    public void InitializeWindawesomeEnironment()
    {
      var windawesome = new Windawesome.Windawesome();

      _scope.SetVariable("config", windawesome.config);
      _scope.SetVariable("windawesome", windawesome);

      var searchPaths = _engine.GetSearchPaths().ToList();
      searchPaths.Add(Environment.CurrentDirectory);
      _engine.SetSearchPaths(searchPaths);

      AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(asm =>
      {
        LoadAssemblyIntoEngine(asm);
      });
      LoadAssemblyIntoEngine(Assembly.Load("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));

      var files = Directory.EnumerateFiles("Layouts").Concat(
                  Directory.EnumerateFiles("Widgets").Concat(
                  Directory.EnumerateFiles("Plugins")))
                  .Select(fileName => new FileInfo(fileName));

      files.Where(fi => fi.Extension == ".dll")
            .Select(fi => Assembly.LoadFrom(fi.FullName))
            .ToList()
            .ForEach(asm =>
            {
              LoadAssemblyIntoEngine(asm);
            });

      files.Where(fi => fi.Extension == ".rb")
            .ToList()
            .ForEach(fi =>
            {
              Console.WriteLine(fi.FullName);
              _scope = _engine.ExecuteFile(fi.FullName, _scope);
            });
    }

    private void LoadAssemblyIntoEngine(Assembly assembly)
    {
      Console.WriteLine(assembly.FullName);
      _engine.Runtime.LoadAssembly(assembly);
    }

    public void Repl()
    {
      _engine.Execute(@"require 'repl'
                        include WindawesomeIrb
                        repl(binding)", _scope);
    }

    public void Execute(string filename)
    {      
      _scope = _engine.ExecuteFile(filename, _scope);
    }
  }
}
