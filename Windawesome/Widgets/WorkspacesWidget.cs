using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Windawesome.Widgets
{
  public sealed class WorkspacesWidget : IFixedWidthWidget
  {
    private static Windawesome _windawesome;
    private static Timer _flashTimer;
    private static Dictionary<IntPtr, Workspace> _flashingWindows;
    private static HashSet<Workspace> _flashingWorkspaces;

    private bool _isLeft;
    private Label[] _workspaceLabels;

    private delegate void WorkFlashingStopped(Workspace workspace);
    private static event WorkFlashingStopped OnWorkspaceFlashingStopped;


    public Color NormalForegroundColor { get; set; }

    public Color NormalBackgroundColor { get; set; }

    public Color HighlightedForegroundColor { get; set; }

    public Color HighlightedBackgroundColor { get; set; }

    public Color HighlightedInactiveForegroundColor { get; set; }

    public Color HighlightedInactiveBackgroundColor { get; set; }

    public Color FlashingForegroundColor { get; set; }

    public Color FlashingBackgroundColor { get; set; }

    public bool FlashWorkspaces { get; set; }
    


    public WorkspacesWidget()
    {
      _flashTimer = new Timer { Interval = 500 };
      _flashingWindows = new Dictionary<IntPtr, Workspace>(3);
      _flashingWorkspaces = new HashSet<Workspace>();

      Workspace.WorkspaceWindowRemoved += (_, w) => StopFlashingApplication(w.hWnd);
      Workspace.WindowActivatedEvent += StopFlashingApplication;
      Workspace.WorkspaceWindowRestored += (_, w) => StopFlashingApplication(w.hWnd);
      Windawesome.WindowFlashing += OnWindowFlashing;
    }

    public WorkspacesWidget(Color? normalForegroundColor = null, Color? normalBackgroundColor = null,
      Color? highlightedForegroundColor = null, Color? highlightedBackgroundColor = null,
      Color? highlightedInactiveForegroundColor = null, Color? highlightedInactiveBackgroundColor = null,
      Color? flashingForegroundColor = null, Color? flashingBackgroundColor = null, bool flashWorkspaces = true)
      : this()
    {
      NormalForegroundColor = normalForegroundColor ?? Color.White;
      NormalBackgroundColor = normalBackgroundColor ?? Color.Black;
      HighlightedForegroundColor = highlightedForegroundColor ?? Color.White;
      HighlightedBackgroundColor = highlightedBackgroundColor ?? Color.FromArgb(0x33, 0x99, 0xFF);
      HighlightedInactiveForegroundColor = highlightedInactiveForegroundColor ?? Color.White;
      HighlightedInactiveBackgroundColor = highlightedInactiveBackgroundColor ?? Color.Green;
      FlashingForegroundColor = flashingForegroundColor ?? Color.White;
      FlashingBackgroundColor = flashingBackgroundColor ?? Color.Red;
      FlashWorkspaces = flashWorkspaces;
    }


    private void OnWorkspaceLabelClick(object sender, EventArgs e)
    {
      _windawesome.SwitchToWorkspace(Array.IndexOf(_workspaceLabels, sender as Label) + 1);
    }

    private void SetWorkspaceLabelColor(Workspace workspace)
    {
      var workspaceLabel = _workspaceLabels[workspace.Id - 1];
      if (workspace.IsCurrentWorkspace)
      {
        workspaceLabel.BackColor = HighlightedBackgroundColor;
        workspaceLabel.ForeColor = HighlightedForegroundColor;
      }
      else if (workspace.IsWorkspaceVisible)
      {
        workspaceLabel.BackColor = HighlightedInactiveBackgroundColor;
        workspaceLabel.ForeColor = HighlightedInactiveForegroundColor;
      }
      else
      {
        workspaceLabel.BackColor = NormalBackgroundColor;
        workspaceLabel.ForeColor = NormalForegroundColor;
      }
    }

    private void OnWorkspaceChangedFromTo(Workspace workspace)
    {
      SetWorkspaceLabelColor(workspace);
    }

    private static void OnWindowFlashing(IntPtr hWnd, LinkedList<Tuple<Workspace, Window>> list)
    {
      if (list != null)
      {
        if (NativeMethods.IsWindow(hWnd) && !_flashingWindows.ContainsKey(hWnd))
        {
          var foregroundWindow = NativeMethods.GetForegroundWindow();

          if (Utilities.DoForSelfAndOwnersWhile(foregroundWindow, h => h != hWnd) == IntPtr.Zero)
          {
            var workspace = list.First.Value.Item1;

            _flashingWindows[hWnd] = workspace;
            _flashingWorkspaces.Add(workspace);
            if (_flashingWorkspaces.Count == 1)
            {
              _flashTimer.Start();
            }
          }
        }
      }
    }

    private static void StopFlashingApplication(IntPtr hWnd)
    {
      Workspace workspace;
      if (_flashingWindows.TryGetValue(hWnd, out workspace))
      {
        _flashingWindows.Remove(hWnd);
        if (_flashingWindows.Values.All(w => w != workspace))
        {
          OnWorkspaceFlashingStopped(workspace);
          _flashingWorkspaces.Remove(workspace);
          if (_flashingWorkspaces.Count == 0)
          {
            _flashTimer.Stop();
          }
        }
      }
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
      foreach (var flashingWorkspace in _flashingWorkspaces)
      {
        if (_workspaceLabels[flashingWorkspace.Id - 1].BackColor == FlashingBackgroundColor)
        {
          SetWorkspaceLabelColor(flashingWorkspace);
        }
        else
        {
          _workspaceLabels[flashingWorkspace.Id - 1].BackColor = FlashingBackgroundColor;
          _workspaceLabels[flashingWorkspace.Id - 1].ForeColor = FlashingForegroundColor;
        }
      }
    }

    #region IWidget Members

    void IWidget.StaticInitializeWidget(Windawesome windawesome)
    {
      WorkspacesWidget._windawesome = windawesome;
    }

    void IWidget.InitializeWidget(Bar bar)
    {
      if (FlashWorkspaces)
      {
        _flashTimer.Tick += OnTimerTick;
        OnWorkspaceFlashingStopped += SetWorkspaceLabelColor;
      }

      bar.BarShown += () => _flashTimer.Start();
      bar.BarHidden += () => _flashTimer.Stop();

      _workspaceLabels = new Label[_windawesome.config.Workspaces.Length];

      Workspace.WorkspaceWindowAdded += (ws, _) => SetWorkspaceLabelColor(ws);
      Workspace.WorkspaceWindowRemoved += (ws, _) => SetWorkspaceLabelColor(ws);

      Workspace.WorkspaceDeactivated += OnWorkspaceChangedFromTo;
      Workspace.WorkspaceActivated += OnWorkspaceChangedFromTo;
      Workspace.WorkspaceShown += OnWorkspaceChangedFromTo;
      Workspace.WorkspaceHidden += OnWorkspaceChangedFromTo;

      for (var i = 0; i < _windawesome.config.Workspaces.Length; i++)
      {
        var workspace = _windawesome.config.Workspaces[i];
        var name = workspace.Name ?? (i + 1).ToString();

        var label = bar.CreateLabel(" " + name + " ", 0);
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.Click += OnWorkspaceLabelClick;
        _workspaceLabels[i] = label;
        SetWorkspaceLabelColor(workspace);
      }
    }

    IEnumerable<Control> IFixedWidthWidget.GetInitialControls(bool isLeft)
    {
      this._isLeft = isLeft;

      return _workspaceLabels;
    }

    public void RepositionControls(int left, int right)
    {
      if (_isLeft)
      {
        foreach (var label in _workspaceLabels)
        {
          label.Location = new Point(left, 0);
          left += label.Width;
        }
      }
      else
      {
        foreach (var label in NativeMethods.Reverse(_workspaceLabels))
        {
          right -= label.Width;
          label.Location = new Point(right, 0);
        }
      }
    }

    int IWidget.GetLeft()
    {
      return _workspaceLabels.First().Left;
    }

    int IWidget.GetRight()
    {
      return _workspaceLabels.Last().Right;
    }

    void IWidget.StaticDispose()
    {
    }

    void IWidget.Dispose()
    {
    }

    void IWidget.Refresh()
    {
      // remove all flashing windows
      _flashingWindows.Keys.ToArray().ForEach(StopFlashingApplication);
    }

    #endregion
  }
}
