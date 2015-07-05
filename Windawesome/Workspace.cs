using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Windawesome
{
  public sealed class Workspace
  {
    private static int s_Count;

    private readonly LinkedList<Window> _windows; // all windows, sorted in tab-order
    private bool _hasChanges;

    private ILayout _layout;
    private bool _isCurrentWorkspace;


    internal uint HideFromAltTabWhenOnInactiveWorkspaceCount { get; set; }

    internal uint SharedWindowsCount { get; set; }
    
    public int Id { get; private set; }

    public Monitor Monitor { get; set; }

    public ILayout Layout
    {
      get { return _layout; }
      set { _layout = value; _layout.Initialize(this); }
    }

    public LinkedList<IBar>[] AllBarsAtTop { get; private set; }

    public IBar[] BarsAtTop
    {
      get { return AllBarsAtTop.SelectMany(barList => barList).ToArray(); }
      set
      {
        if (value != null)
        {
          value.ForEach(bar => AllBarsAtTop[bar.Monitor.monitorIndex].AddLast(bar));
        }
      }
    }

    public LinkedList<IBar>[] AllBarsAtBottom { get; private set; }

    public IBar[] BarsAtBottom
    {
      get { return AllBarsAtBottom.SelectMany(barList => barList).ToArray(); }
      set
      {
        if (value != null)
        {
          value.ForEach(bar => AllBarsAtBottom[bar.Monitor.monitorIndex].AddLast(bar));
        }
      }
    }

    public string Name { get; set; }

    public bool ShowWindowsTaskbar { get; set; }

    public bool IsWorkspaceVisible { get; set; }

    public bool RepositionOnSwitchedTo { get; set; }

    public bool IsCurrentWorkspace
    {
      get { return _isCurrentWorkspace; }

      internal set
      {
        _isCurrentWorkspace = value;
        if (_isCurrentWorkspace)
        {
          DoWorkspaceActivated(this);
        }
        else
        {
          DoWorkspaceDeactivated(this);
        }
      }
    }

    #region Events

    public delegate void WorkspaceWindowAddedEventHandler(Workspace workspace, Window window);
    public static event WorkspaceWindowAddedEventHandler WorkspaceWindowAdded;

    public delegate void WorkspaceWindowRemovedEventHandler(Workspace workspace, Window window);
    public static event WorkspaceWindowRemovedEventHandler WorkspaceWindowRemoved;

    public delegate void WorkspaceWindowMinimizedEventHandler(Workspace workspace, Window window);
    public static event WorkspaceWindowMinimizedEventHandler WorkspaceWindowMinimized;

    public delegate void WorkspaceWindowRestoredEventHandler(Workspace workspace, Window window);
    public static event WorkspaceWindowRestoredEventHandler WorkspaceWindowRestored;

    public delegate void WorkspaceWindowOrderChangedEventHandler(Workspace workspace, Window window, int positions, bool backwards);
    public static event WorkspaceWindowOrderChangedEventHandler WorkspaceWindowOrderChanged;

    public delegate void WorkspaceHiddenEventHandler(Workspace workspace);
    public static event WorkspaceHiddenEventHandler WorkspaceHidden;

    public delegate void WorkspaceShownEventHandler(Workspace workspace);
    public static event WorkspaceShownEventHandler WorkspaceShown;

    public delegate void WorkspaceActivatedEventHandler(Workspace workspace);
    public static event WorkspaceActivatedEventHandler WorkspaceActivated;

    public delegate void WorkspaceDeactivatedEventHandler(Workspace workspace);
    public static event WorkspaceDeactivatedEventHandler WorkspaceDeactivated;

    public delegate void WorkspaceMonitorChangedEventHandler(Workspace workspace, Monitor oldMonitor, Monitor newMonitor);
    public static event WorkspaceMonitorChangedEventHandler WorkspaceMonitorChanged;

    public delegate void WorkspaceLayoutChangedEventHandler(Workspace workspace, ILayout oldLayout);
    public static event WorkspaceLayoutChangedEventHandler WorkspaceLayoutChanged;

    public delegate void WindowActivatedEventHandler(IntPtr hWnd);
    public static event WindowActivatedEventHandler WindowActivatedEvent;

    public delegate void WindowTitlebarToggledEventHandler(Window window);
    public event WindowTitlebarToggledEventHandler WindowTitlebarToggled;

    public delegate void WindowBorderToggledEventHandler(Window window);
    public event WindowBorderToggledEventHandler WindowBorderToggled;

    public delegate void LayoutUpdatedEventHandler();
    public static event LayoutUpdatedEventHandler LayoutUpdated; // TODO: this should be for a specific workspace. But how to call from Widgets then?

    private static void DoWorkspaceWindowAdded(Workspace workspace, Window window)
    {
      if (WorkspaceWindowAdded != null)
      {
        WorkspaceWindowAdded(workspace, window);
      }
    }

    private static void DoWorkspaceWindowRemoved(Workspace workspace, Window window)
    {
      if (WorkspaceWindowRemoved != null)
      {
        WorkspaceWindowRemoved(workspace, window);
      }
    }

    private static void DoWorkspaceWindowMinimized(Workspace workspace, Window window)
    {
      if (WorkspaceWindowMinimized != null)
      {
        WorkspaceWindowMinimized(workspace, window);
      }
    }

    private static void DoWorkspaceWindowRestored(Workspace workspace, Window window)
    {
      if (WorkspaceWindowRestored != null)
      {
        WorkspaceWindowRestored(workspace, window);
      }
    }

    private static void DoWorkspaceWindowOrderChanged(Workspace workspace, Window window, int positions, bool backwards)
    {
      if (WorkspaceWindowOrderChanged != null)
      {
        WorkspaceWindowOrderChanged(workspace, window, positions, backwards);
      }
    }

    private static void DoWorkspaceHidden(Workspace workspace)
    {
      if (WorkspaceHidden != null)
      {
        WorkspaceHidden(workspace);
      }
    }

    private static void DoWorkspaceShown(Workspace workspace)
    {
      if (WorkspaceShown != null)
      {
        WorkspaceShown(workspace);
      }
    }

    private static void DoWorkspaceActivated(Workspace workspace)
    {
      if (WorkspaceActivated != null)
      {
        WorkspaceActivated(workspace);
      }
    }

    private static void DoWorkspaceDeactivated(Workspace workspace)
    {
      if (WorkspaceDeactivated != null)
      {
        WorkspaceDeactivated(workspace);
      }
    }

    internal static void DoWorkspaceMonitorChanged(Workspace workspace, Monitor oldMonitor, Monitor newMonitor)
    {
      if (WorkspaceMonitorChanged != null)
      {
        WorkspaceMonitorChanged(workspace, oldMonitor, newMonitor);
      }
    }

    private static void DoWorkspaceLayoutChanged(Workspace workspace, ILayout oldLayout)
    {
      if (WorkspaceLayoutChanged != null)
      {
        WorkspaceLayoutChanged(workspace, oldLayout);
      }
    }

    private void DoWindowTitlebarToggled(Window window)
    {
      if (WindowTitlebarToggled != null)
      {
        WindowTitlebarToggled(window);
      }
    }

    private void DoWindowBorderToggled(Window window)
    {
      if (WindowBorderToggled != null)
      {
        WindowBorderToggled(window);
      }
    }

    public static void DoLayoutUpdated()
    {
      if (LayoutUpdated != null)
      {
        LayoutUpdated();
      }
    }

    #endregion

    public Workspace()
    {
      _windows = new LinkedList<Window>();
      Id = ++s_Count;
      AllBarsAtTop = Screen.AllScreens.Select(_ => new LinkedList<IBar>()).ToArray();
      AllBarsAtBottom = Screen.AllScreens.Select(_ => new LinkedList<IBar>()).ToArray();
    }

    public Workspace(Monitor monitor, ILayout layout, IEnumerable<IBar> barsAtTop = null, IEnumerable<IBar> barsAtBottom = null,
      string name = null, bool showWindowsTaskbar = false, bool repositionOnSwitchedTo = false)
      : this()
    {
      Monitor = monitor;
      Layout = layout;
      BarsAtTop = barsAtTop.ToArray();
      BarsAtBottom = barsAtBottom.ToArray();
      Name = name;
      ShowWindowsTaskbar = showWindowsTaskbar;
      RepositionOnSwitchedTo = repositionOnSwitchedTo;
    }

    public override int GetHashCode()
    {
      return this.Id;
    }

    public override bool Equals(object obj)
    {
      var workspace = obj as Workspace;
      return workspace != null && workspace.Id == this.Id;
    }

    internal void SwitchTo()
    {
      if (SharedWindowsCount > 0)
      {
        // sets the layout- and workspace-specific changes to the windows
        _windows.Where(w => w.WorkspacesCount > 1).ForEach(w => RestoreSharedWindowState(w, false));
      }

      IsWorkspaceVisible = true;

      if (NeedsToReposition())
      {
        // Repositions if there is/are new/deleted windows
        Reposition();
      }

      DoWorkspaceShown(this);
    }

    internal void Unswitch()
    {
      if (SharedWindowsCount > 0)
      {
        _windows.Where(w => w.WorkspacesCount > 1 && ShouldSaveAndRestoreSharedWindowsPosition(w)).
          ForEach(w => w.SavePosition());
      }

      IsWorkspaceVisible = false;
      DoWorkspaceHidden(this);
    }

    private void RestoreSharedWindowState(Window window, bool doNotShow)
    {
      // TODO: when a shared window is removed from its next to last workspace,
      // if it was on a full-screen or the last one is on a full-screen layout,
      // it is not repositioned correctly
      window.Initialize();
      if (ShouldSaveAndRestoreSharedWindowsPosition(window))
      {
        window.RestorePosition(doNotShow);
      }
    }

    private bool ShouldSaveAndRestoreSharedWindowsPosition(Window window)
    {
      return !NeedsToReposition() || window.IsFloating || Layout.ShouldSaveAndRestoreSharedWindowsPosition();
    }

    public bool NeedsToReposition()
    {
      return _hasChanges || RepositionOnSwitchedTo;
    }

    public void Reposition()
    {
      _hasChanges = !IsWorkspaceVisible;
      if (IsWorkspaceVisible)
      {
        Layout.Reposition();
      }
    }

    public void ChangeLayout(ILayout layout)
    {
      if (layout.LayoutName() != Layout.LayoutName())
      {
        Layout.Dispose();
        layout.Initialize(this);
        var oldLayout = Layout;
        Layout = layout;
        Reposition();
        DoWorkspaceLayoutChanged(this, oldLayout);
      }
    }

    internal void WindowMinimized(IntPtr hWnd)
    {
      var window = GetWindow(hWnd);
      if (window != null)
      {
        if (!window.IsFloating)
        {
          Layout.WindowMinimized(window);
        }

        DoWorkspaceWindowMinimized(this, window);
      }
    }

    internal void WindowRestored(IntPtr hWnd)
    {
      var window = GetWindow(hWnd);
      if (window != null)
      {
        if (!window.IsFloating)
        {
          Layout.WindowRestored(window);
        }

        DoWorkspaceWindowRestored(this, window);
      }
    }

    internal void WindowActivated(IntPtr hWnd)
    {
      WindowActivatedEvent(hWnd);
    }

    internal void WindowCreated(Window window)
    {
      _windows.AddFirst(window);
      if (window.hideFromAltTabAndTaskbarWhenOnInactiveWorkspace)
      {
        HideFromAltTabWhenOnInactiveWorkspaceCount++;
      }
      if (window.WorkspacesCount > 1)
      {
        SharedWindowsCount++;
      }
      if (IsWorkspaceVisible || window.WorkspacesCount == 1)
      {
        window.Initialize();
      }

      if (!NativeMethods.IsIconic(window.hWnd) && !window.IsFloating)
      {
        Layout.WindowCreated(window);

        _hasChanges |= !IsWorkspaceVisible;
      }

      DoWorkspaceWindowAdded(this, window);
    }

    internal void WindowDestroyed(Window window)
    {
      _windows.Remove(window);
      if (window.hideFromAltTabAndTaskbarWhenOnInactiveWorkspace)
      {
        HideFromAltTabWhenOnInactiveWorkspaceCount--;
      }
      if (window.WorkspacesCount > 1)
      {
        SharedWindowsCount--;
      }

      if (!NativeMethods.IsIconic(window.hWnd) && !window.IsFloating)
      {
        Layout.WindowDestroyed(window);

        _hasChanges |= !IsWorkspaceVisible;
      }

      DoWorkspaceWindowRemoved(this, window);
    }

    public int GetWindowsCount()
    {
      return _windows.Count;
    }

    public bool ContainsWindow(IntPtr hWnd)
    {
      return _windows.Any(w => w.hWnd == hWnd);
    }

    public Window GetWindow(IntPtr hWnd)
    {
      return _windows.FirstOrDefault(w => w.hWnd == hWnd);
    }

    public IEnumerable<Window> GetLayoutManagedWindows()
    {
      return _windows.Where(w => !w.IsFloating && !NativeMethods.IsIconic(w.hWnd));
    }

    public IEnumerable<Window> GetWindows()
    {
      return _windows;
    }

    internal void ToggleWindowFloating(Window window)
    {
      window.IsFloating = !window.IsFloating;
      if (!NativeMethods.IsIconic(window.hWnd))
      {
        if (window.IsFloating)
        {
          Layout.WindowDestroyed(window);
        }
        else
        {
          Layout.WindowCreated(window);
        }
      }
    }

    internal void ToggleShowHideWindowTitlebar(Window window)
    {
      window.ToggleShowHideTitlebar();
      DoWindowTitlebarToggled(window);
    }

    internal void ToggleShowHideWindowBorder(Window window)
    {
      window.ToggleShowHideWindowBorder();
      DoWindowBorderToggled(window);
    }

    internal void ToggleWindowsTaskbarVisibility()
    {
      if (Monitor.screen.Primary)
      {
        ShowWindowsTaskbar = !ShowWindowsTaskbar;
        Monitor.ShowHideWindowsTaskbar(ShowWindowsTaskbar);
        Reposition();
      }
    }

    internal void Initialize()
    {
      // I'm adding to the front of the list in WindowCreated, however EnumWindows enums
      // from the top of the Z-order to the bottom, so I need to reverse the list
      if (_windows.Count > 0)
      {
        _windows.ToArray().ForEach(this.ShiftWindowToMainPosition); // n ^ 2!
      }
    }

    internal void RemoveFromSharedWindows(Window window)
    {
      RestoreSharedWindowState(window, !IsWorkspaceVisible);
      SharedWindowsCount--;
    }

    #region Window Position

    public Window GetNextWindow(Window window)
    {
      var node = _windows.Find(window);
      if (node != null)
      {
        return node.Next != null ? node.Next.Value : _windows.First.Value;
      }
      return null;
    }

    public Window GetPreviousWindow(Window window)
    {
      var node = _windows.Find(window);
      if (node != null)
      {
        return node.Previous != null ? node.Previous.Value : _windows.Last.Value;
      }
      return null;
    }

    public void ShiftWindowForward(Window window, int positions = 1)
    {
      if (_windows.Count > 1 && _windows.Last.Value != window)
      {
        var node = _windows.Find(window);
        if (node != null)
        {
          var nextNode = node.Next;
          _windows.Remove(node);
          var i = 0;
          while (++i < positions && nextNode != null)
          {
            nextNode = nextNode.Next;
          }
          if (nextNode != null)
          {
            _windows.AddAfter(nextNode, node);
          }
          else
          {
            _windows.AddLast(node);
          }

          this.Reposition();
          DoWorkspaceWindowOrderChanged(this, window, i, false);
        }
      }
    }

    public void ShiftWindowBackwards(Window window, int positions = 1)
    {
      if (_windows.Count > 1 && _windows.First.Value != window)
      {
        var node = _windows.Find(window);
        if (node != null)
        {
          var previousNode = node.Previous;
          _windows.Remove(node);
          var i = 0;
          while (++i < positions && previousNode != null)
          {
            previousNode = previousNode.Previous;
          }
          if (previousNode != null)
          {
            _windows.AddBefore(previousNode, node);
          }
          else
          {
            _windows.AddFirst(node);
          }

          this.Reposition();
          DoWorkspaceWindowOrderChanged(this, window, i, true);
        }
      }
    }

    public void ShiftWindowToMainPosition(Window window)
    {
      if (_windows.Count > 1 && _windows.First.Value != window)
      {
        var node = _windows.First;
        var i = 0;
        for (; node != null && node.Value != window; node = node.Next, i++)
        {
        }
        if (node != null)
        {
          _windows.Remove(node);
          _windows.AddFirst(node);

          this.Reposition();
          DoWorkspaceWindowOrderChanged(this, window, i, true);
        }
      }
    }

    #endregion

  }
}
