using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using IronRuby;
using Microsoft.Scripting.Hosting;

namespace Windawesome
{
  public class Config
  {
    public IEnumerable<IPlugin> Plugins { get; set; }
    public IBar[] Bars { get; set; }
    public Workspace[] Workspaces { get; set; }
    public IEnumerable<Workspace> StartingWorkspaces { get; set; }
    public IEnumerable<ProgramRule> ProgramRules { get; set; }

    public int WindowBorderWidth { get; set; }
    public int WindowPaddedBorderWidth { get; set; }
    public bool ShowMinimizeMaximizeRestoreAnimations { get; set; }
    public bool HideMouseWhenTyping { get; set; }
    public bool FocusFollowsMouse { get; set; }
    public bool FocusFollowsMouseSetOnTop { get; set; }
    public bool MoveMouseOverMonitorsOnSwitch { get; set; }
    public bool CheckForUpdates { get; set; }

    internal Config()
    {
      this.WindowBorderWidth = -1;
      this.WindowPaddedBorderWidth = -1;
      this.CheckForUpdates = true;
    }

    internal void LoadConfiguration(Windawesome windawesome)
    {
      const string layoutsDirName = "Layouts";
      const string widgetsDirName = "Widgets";
      const string pluginsDirName = "Plugins";
      const string configDirName = "Config";

      if (!Directory.Exists(configDirName) || Directory.EnumerateFiles(configDirName).FirstOrDefault() == null)
      {
        throw new Exception("You HAVE to have a " + configDirName + " directory in the folder and it must " +
          "contain at least one Python or Ruby file that initializes all instance variables in 'config' " +
          "that don't have default values!");
      }
      if (!Directory.Exists(layoutsDirName))
      {
        Directory.CreateDirectory(layoutsDirName);
      }
      if (!Directory.Exists(widgetsDirName))
      {
        Directory.CreateDirectory(widgetsDirName);
      }
      if (!Directory.Exists(pluginsDirName))
      {
        Directory.CreateDirectory(pluginsDirName);
      }
      var files =
        Directory.EnumerateFiles(layoutsDirName).Select(fileName => new FileInfo(fileName)).Concat(
        Directory.EnumerateFiles(widgetsDirName).Select(fileName => new FileInfo(fileName))).Concat(
        Directory.EnumerateFiles(pluginsDirName).Select(fileName => new FileInfo(fileName))).Concat(
        Directory.EnumerateFiles(configDirName).Select(fileName => new FileInfo(fileName)));

      PluginLoader.LoadAll(windawesome, this, files);
    }

    private static class PluginLoader
    {
      private static ScriptEngine rubyEngine;

      private static ScriptEngine RubyEngine
      {
        get
        {
          if (rubyEngine == null)
          {
            rubyEngine = Ruby.CreateEngine();
            InitializeScriptEngine(rubyEngine);
          }
          return rubyEngine;
        }
      }

      private static void InitializeScriptEngine(ScriptEngine engine)
      {
        var searchPaths = engine.GetSearchPaths().ToList();
        searchPaths.Add(Environment.CurrentDirectory);
        engine.SetSearchPaths(searchPaths);

        AppDomain.CurrentDomain.GetAssemblies().ForEach(engine.Runtime.LoadAssembly);
      }

      private static ScriptEngine GetEngineForFile(FileSystemInfo file)
      {
        switch (file.Extension)
        {
          case ".ir":
          case ".rb":
            return RubyEngine;
          case ".dll":
            var assembly = Assembly.LoadFrom(file.FullName);
            if (RubyEngine != null)
            {
              RubyEngine.Runtime.LoadAssembly(assembly);
            }
            break;
        }

        return null;
      }

      public static void LoadAll(Windawesome windawesome, Config config, IEnumerable<FileInfo> files)
      {
        ScriptScope scope = null;
        ScriptEngine previousLanguage = null;
        foreach (var file in files)
        {
          var engine = GetEngineForFile(file);
          if (engine != null)
          {
            if (scope == null)
            {
              scope = engine.CreateScope();
              scope.SetVariable("windawesome", windawesome);
              scope.SetVariable("config", config);
            }
            else if (previousLanguage != engine)
            {
              var oldScope = scope;
              scope = engine.CreateScope();
              oldScope.GetItems().
                Where(variable => variable.Value != null).
                ForEach(variable => scope.SetVariable(variable.Key, variable.Value));
              previousLanguage.Runtime.Globals.GetItems().
                Where(variable => variable.Value != null).
                ForEach(variable => scope.SetVariable(variable.Key, variable.Value));
            }

            scope = engine.ExecuteFile(file.FullName, scope);
            previousLanguage = engine;
          }
        }
      }
    }
  }

  public enum State
  {
    SHOWN = 0,
    HIDDEN = 1,
    AS_IS = 2
  }

  public enum OnWindowCreatedOnWorkspaceAction
  {
    MoveToTop,
    PreserveTopmostWindow
  }

  public enum OnWindowCreatedOrShownAction
  {
    SwitchToWindowsWorkspace,
    MoveWindowToCurrentWorkspace,
    TemporarilyShowWindowOnCurrentWorkspace,
    HideWindow
  }

  public class ProgramRule
  {
    public class Rule
    {
      public int Workspace { get; set; }
      public bool IsFloating { get; set; }
      public State Titlebar { get; set; }
      public State InAltTabAndTaskbar { get; set; }
      public State WindowBorders { get; set; }
      public bool RedrawOnShow { get; set; }
      public bool HideFromAltTabAndTaskbarWhenOnInactiveWorkspace { get; set; }

      public Rule()
      {
        Workspace = 0;
        IsFloating = false;
        Titlebar = State.AS_IS;
        InAltTabAndTaskbar = State.AS_IS;
        WindowBorders = State.AS_IS;
        RedrawOnShow = false;
        HideFromAltTabAndTaskbarWhenOnInactiveWorkspace = false;
      }

      public Rule(int workspace = 0,
        bool isFloating = false,
        State titlebar = State.AS_IS,
        State inAltTabAndTaskbar = State.AS_IS,
        State windowBorders = State.AS_IS,
        bool redrawOnShow = false,
        bool hideFromAltTabAndTaskbarWhenOnInactiveWorkspace = false)
      {
        Workspace = workspace;
        IsFloating = isFloating;
        Titlebar = titlebar;
        InAltTabAndTaskbar = inAltTabAndTaskbar;
        WindowBorders = windowBorders;
        RedrawOnShow = redrawOnShow;
        HideFromAltTabAndTaskbarWhenOnInactiveWorkspace = hideFromAltTabAndTaskbarWhenOnInactiveWorkspace;
      }
    }

    private Regex _className;
    private Regex _displayName;
    private Regex _processName;

    public string ClassName
    {
      get { return _className.ToString(); }
      set { _className = new Regex(value, RegexOptions.Compiled); }
    }

    public string DisplayName
    {
      get { return _displayName.ToString(); }
      set { _displayName = new Regex(value, RegexOptions.Compiled); }
    }

    public string ProcessName
    {
      get { return _processName.ToString(); }
      set { _processName = new Regex(value, RegexOptions.Compiled); }
    }

    public NativeMethods.WS StyleContains { get; set; }

    public NativeMethods.WS StyleNotContains { get; set; }

    public NativeMethods.WS_EX ExStyleContains { get; set; }

    public NativeMethods.WS_EX ExStyleNotContains { get; set; }

    public CustomMatchingFunctionDelegate CustomMatchingFunction { get; set; }

    public CustomMatchingFunctionDelegate CustomOwnedWindowMatchingFunction { get; set; }

    public bool IsManaged { get; set; }

    public int TryAgainAfter { get; set; }

    public int WindowCreatedDelay { get; set; }

    public bool RedrawDesktopOnWindowCreated { get; set; }

    public bool ShowMenu { get; set; }

    public bool UpdateIcon { get; set; }

    public OnWindowCreatedOrShownAction OnWindowCreatedAction { get; set; }

    public OnWindowCreatedOrShownAction OnHiddenWindowShownAction { get; set; }

    public OnWindowCreatedOnWorkspaceAction OnWindowCreatedOnCurrentWorkspaceAction { get; set; }

    public OnWindowCreatedOnWorkspaceAction OnWindowCreatedOnInactiveWorkspaceAction { get; set; }

    public Rule[] Rules { get; set; }

    public delegate bool CustomMatchingFunctionDelegate(IntPtr hWnd);

    public ProgramRule()
    {
      ClassName = ".*";
      DisplayName = ".*";
      ProcessName = ".*";
      StyleContains = (NativeMethods.WS)0;
      StyleNotContains = (NativeMethods.WS)0;
      ExStyleContains = (NativeMethods.WS_EX)0;
      ExStyleNotContains = (NativeMethods.WS_EX)0;
      CustomMatchingFunction = Utilities.IsAltTabWindow;
      CustomOwnedWindowMatchingFunction = DefaultOwnedWindowMatchingFunction;
      IsManaged = true;
      TryAgainAfter = -1;
      WindowCreatedDelay = -1;
      RedrawDesktopOnWindowCreated = false;
      ShowMenu = true;
      UpdateIcon = false;
      OnWindowCreatedAction = OnWindowCreatedOrShownAction.SwitchToWindowsWorkspace;
      OnHiddenWindowShownAction = OnWindowCreatedOrShownAction.SwitchToWindowsWorkspace;
      OnWindowCreatedOnCurrentWorkspaceAction = OnWindowCreatedOnWorkspaceAction.MoveToTop;
      OnWindowCreatedOnInactiveWorkspaceAction = OnWindowCreatedOnWorkspaceAction.MoveToTop;
      Rules = new[] { new Rule() };
    }

    public ProgramRule(string className = ".*",
      string displayName = ".*",
      string processName = ".*",
      NativeMethods.WS styleContains = (NativeMethods.WS) 0,
      NativeMethods.WS styleNotContains = (NativeMethods.WS) 0,
      NativeMethods.WS_EX exStyleContains = (NativeMethods.WS_EX) 0,
      NativeMethods.WS_EX exStyleNotContains = (NativeMethods.WS_EX) 0,
      CustomMatchingFunctionDelegate customMatchingFunction = null,
      CustomMatchingFunctionDelegate customOwnedWindowMatchingFunction = null,
      bool isManaged = true,
      int tryAgainAfter = -1,
      int windowCreatedDelay = -1,
      bool redrawDesktopOnWindowCreated = false,
      bool showMenu = true,
      bool updateIcon = false,
      OnWindowCreatedOrShownAction onWindowCreatedAction = OnWindowCreatedOrShownAction.SwitchToWindowsWorkspace,
      OnWindowCreatedOrShownAction onHiddenWindowShownAction = OnWindowCreatedOrShownAction.SwitchToWindowsWorkspace,
      OnWindowCreatedOnWorkspaceAction onWindowCreatedOnCurrentWorkspaceAction = OnWindowCreatedOnWorkspaceAction.MoveToTop,
      OnWindowCreatedOnWorkspaceAction onWindowCreatedOnInactiveWorkspaceAction = OnWindowCreatedOnWorkspaceAction.MoveToTop,
      int showOnWorkspacesCount = 0,
      IEnumerable<Rule> rules = null)
    {
      ClassName = className;
      DisplayName = displayName;
      ProcessName = processName;
      StyleContains = styleContains;
      StyleNotContains = styleNotContains;
      ExStyleContains = exStyleContains;
      ExStyleNotContains = exStyleNotContains;
      CustomMatchingFunction = customMatchingFunction ?? Utilities.IsAltTabWindow;
      CustomOwnedWindowMatchingFunction = customOwnedWindowMatchingFunction ?? DefaultOwnedWindowMatchingFunction;

      IsManaged = isManaged;
      if (IsManaged)
      {
        TryAgainAfter = tryAgainAfter;
        WindowCreatedDelay = windowCreatedDelay;
        RedrawDesktopOnWindowCreated = redrawDesktopOnWindowCreated;
        ShowMenu = showMenu;
        UpdateIcon = updateIcon;
        OnWindowCreatedAction = onWindowCreatedAction;
        OnHiddenWindowShownAction = onHiddenWindowShownAction;
        OnWindowCreatedOnCurrentWorkspaceAction = onWindowCreatedOnCurrentWorkspaceAction;
        OnWindowCreatedOnInactiveWorkspaceAction = onWindowCreatedOnInactiveWorkspaceAction;

        if (showOnWorkspacesCount > 0)
        {
          if (rules == null)
          {
            rules = new Rule[] { };
          }

          // This is slow (n ^ 2), but it doesn't matter in this case
          Rules = rules.Concat(
            Enumerable.Range(1, showOnWorkspacesCount).Where(i => rules.All(r => r.Workspace != i)).Select(i => new Rule(i))).
            ToArray();
        }
        else
        {
          Rules = rules == null ? new[] { new Rule() } : rules.ToArray();
        }
      }
    }

    internal bool IsMatch(IntPtr hWnd, string cName, string dName, string pName, NativeMethods.WS style, NativeMethods.WS_EX exStyle)
    {
      return _className.IsMatch(cName) && _displayName.IsMatch(dName) && _processName.IsMatch(pName) &&
        (style & StyleContains) == StyleContains && (style & StyleNotContains) == 0 &&
        (exStyle & ExStyleContains) == ExStyleContains && (exStyle & ExStyleNotContains) == 0 &&
        CustomMatchingFunction(hWnd);
    }

    private static bool DefaultOwnedWindowMatchingFunction(IntPtr hWnd)
    {
      var exStyle = NativeMethods.GetWindowExStyleLongPtr(hWnd);
      return !exStyle.HasFlag(NativeMethods.WS_EX.WS_EX_NOACTIVATE) &&
        !exStyle.HasFlag(NativeMethods.WS_EX.WS_EX_TOOLWINDOW);
    }
  }
}
