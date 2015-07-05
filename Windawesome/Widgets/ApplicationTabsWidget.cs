using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Windawesome.Widgets
{
  public sealed class ApplicationTabsWidget : ISpanWidget
  {
    private static Windawesome _windawesome;

    private LinkedList<Tuple<IntPtr, Panel>>[] _applicationPanels; // [(hWnd, Panel)]
    private Panel _currentlyHighlightedPanel;
    private bool[] _mustResize;
    private int _left, _right;
    private Bar _bar;
    private bool _isShown;


    public bool ShowSingleApplicationTab { get; set; }

    public Color NormalForegroundColor { get; set; }

    public Color NormalBackgroundColor { get; set; }

    public Color HighlightedForegroundColor { get; set; }

    public Color HighlightedBackgroundColor { get; set; }


    public ApplicationTabsWidget()
    {
      _isShown = false;
    }

    public ApplicationTabsWidget(bool showSingleApplicationTab = false,
      Color? normalForegroundColor = null, Color? normalBackgroundColor = null,
      Color? highlightedForegroundColor = null, Color? highlightedBackgroundColor = null)
      : this()
    {
      ShowSingleApplicationTab = showSingleApplicationTab;
      NormalForegroundColor = normalForegroundColor ?? Color.Black;
      NormalBackgroundColor = normalBackgroundColor ?? Color.FromArgb(0xF0, 0xF0, 0xF0);
      HighlightedForegroundColor = highlightedForegroundColor ?? Color.White;
      HighlightedBackgroundColor = highlightedBackgroundColor ?? Color.FromArgb(0x33, 0x99, 0xFF);
    }


    private void ResizeApplicationPanels(int left, int right, int workspaceId)
    {
      _mustResize[workspaceId] = false;

      if (_applicationPanels[workspaceId].Count > 0)
      {
        var eachWidth = (right - left) / (ShowSingleApplicationTab ? 1 : _applicationPanels[workspaceId].Count);

        foreach (var panel in this._applicationPanels[workspaceId].Select(tuple => tuple.Item2))
        {
          panel.Location = new Point(left, 0);
          panel.Size = new Size(eachWidth, this._bar.GetBarHeight());
          panel.Controls[0].Size = new Size(this._bar.GetBarHeight(), this._bar.GetBarHeight());
          panel.Controls[1].Size = new Size(eachWidth - this._bar.GetBarHeight(), this._bar.GetBarHeight());
          if (!this.ShowSingleApplicationTab)
          {
            left += eachWidth;
          }
        }

        if (!ShowSingleApplicationTab && _currentlyHighlightedPanel != null)
        {
          if (_applicationPanels[workspaceId].Count == 1)
          {
            _currentlyHighlightedPanel.ForeColor = NormalForegroundColor;
            _currentlyHighlightedPanel.BackColor = NormalBackgroundColor;
          }
          else
          {
            _currentlyHighlightedPanel.ForeColor = HighlightedForegroundColor;
            _currentlyHighlightedPanel.BackColor = HighlightedBackgroundColor;
          }
        }
      }
    }

    private Panel CreatePanel(Window window)
    {
      var panel = new Panel();
      panel.SuspendLayout();
      panel.AutoSize = false;
      panel.Location = new Point(_left, 0);
      panel.ForeColor = NormalForegroundColor;
      panel.BackColor = NormalBackgroundColor;

      var pictureBox = new PictureBox
        {
          Size = new Size(this._bar.GetBarHeight(), this._bar.GetBarHeight()),
          SizeMode = PictureBoxSizeMode.CenterImage
        };
      pictureBox.Click += this.OnApplicationTabClick;
      panel.Controls.Add(pictureBox);

      Utilities.GetWindowSmallIconAsBitmap(window.hWnd, bitmap => pictureBox.Image = bitmap);

      var label = _bar.CreateLabel(window.DisplayName, _bar.GetBarHeight(), 0);
      label.Click += this.OnApplicationTabClick;
      panel.Controls.Add(label);

      panel.ResumeLayout();
      return panel;
    }

    private void OnWindowActivated(IntPtr hWnd)
    {
      if (_isShown && (!ShowSingleApplicationTab || _bar.Monitor.CurrentVisibleWorkspace.IsCurrentWorkspace))
      {
        Panel panel = null;
        var applications = _applicationPanels[_bar.Monitor.CurrentVisibleWorkspace.Id - 1];

        if (applications.Count > 0 &&
          (hWnd = Utilities.DoForSelfAndOwnersWhile(hWnd, h => applications.All(t => t.Item1 != h))) != IntPtr.Zero)
        {
          panel = applications.First(t => t.Item1 == hWnd).Item2;
          if (panel == _currentlyHighlightedPanel)
          {
            return;
          }
        }

        // removes the current highlight
        if (_currentlyHighlightedPanel != null)
        {
          if (ShowSingleApplicationTab)
          {
            _currentlyHighlightedPanel.Hide();
          }
          else
          {
            _currentlyHighlightedPanel.ForeColor = NormalForegroundColor;
            _currentlyHighlightedPanel.BackColor = NormalBackgroundColor;
          }
        }

        // highlights the new app
        if (panel != null)
        {
          if (ShowSingleApplicationTab)
          {
            panel.Show();
          }
          else if (applications.Count > 1)
          {
            panel.ForeColor = HighlightedForegroundColor;
            panel.BackColor = HighlightedBackgroundColor;
          }

          _currentlyHighlightedPanel = panel;
        }
        else
        {
          _currentlyHighlightedPanel = null;
        }
      }
    }

    private void OnApplicationTabClick(object sender, EventArgs e)
    {
      _windawesome.SwitchToApplication(
        _applicationPanels[_bar.Monitor.CurrentVisibleWorkspace.Id - 1].
          First(tuple => tuple.Item2 == (((Control)sender).Parent as Panel)).Item1);
    }

    private void OnWindowTitleOrIconChanged(Workspace workspace, Window window, string newText, Bitmap newIcon)
    {
      if (newText != null) // text changed
      {
        Tuple<IntPtr, Panel> tuple;
        var applications = _applicationPanels[workspace.Id - 1];
        if ((tuple = applications.FirstOrDefault(t => t.Item1 == window.hWnd)) != null)
        {
          tuple.Item2.Controls[1].Text = newText;
        }
      }
      else // icon changed
      {
        Tuple<IntPtr, Panel> tuple;
        var applications = _applicationPanels[workspace.Id - 1];
        if ((tuple = applications.FirstOrDefault(t => t.Item1 == window.hWnd)) != null)
        {
          ((PictureBox)tuple.Item2.Controls[0]).Image = newIcon;
        }
      }
    }

    private void OnWorkspaceWindowAdded(Workspace workspace, Window window)
    {
      var workspaceId = workspace.Id - 1;
      var newPanel = CreatePanel(window);

      _applicationPanels[workspaceId].AddFirst(Tuple.Create(window.hWnd, newPanel));

      if (_isShown && _bar.Monitor == workspace.Monitor && workspace.IsWorkspaceVisible)
      {
        ResizeApplicationPanels(_left, _right, workspaceId);
      }
      else
      {
        newPanel.Hide();
        _mustResize[workspaceId] = true;
      }
      _bar.DoSpanWidgetControlsAdded(this, new[] { newPanel });
    }

    private void OnWorkspaceWindowRemoved(Workspace workspace, Window window)
    {
      var workspaceId = workspace.Id - 1;
      var tuple = _applicationPanels[workspaceId].FirstOrDefault(t => t.Item1 == window.hWnd);
      if (tuple != null)
      {
        _applicationPanels[workspaceId].Remove(tuple);
        if (_isShown && _bar.Monitor == workspace.Monitor && workspace.IsWorkspaceVisible)
        {
          ResizeApplicationPanels(_left, _right, workspaceId);
        }
        else
        {
          _mustResize[workspaceId] = true;
        }
        _bar.DoSpanWidgetControlsRemoved(this, new[] { tuple.Item2 });
      }
    }

    private void OnWorkspaceWindowOrderChanged(Workspace workspace, Window window, int positions, bool backwards)
    {
      var applications = _applicationPanels[workspace.Id - 1];
      for (var node = applications.First; node != null; node = node.Next)
      {
        if (node.Value.Item1 == window.hWnd)
        {
          var otherNode = backwards ? node.Previous : node.Next;
          applications.Remove(node);
          for (var i = 1; i < positions; i++)
          {
            otherNode = backwards ? otherNode.Previous : otherNode.Next;
          }
          if (backwards)
          {
            applications.AddBefore(otherNode, node);
          }
          else
          {
            applications.AddAfter(otherNode, node);
          }
          break;
        }
      }

      if (_isShown && _bar.Monitor == workspace.Monitor && workspace.IsWorkspaceVisible)
      {
        ResizeApplicationPanels(_left, _right, workspace.Id - 1);
      }
      else
      {
        _mustResize[workspace.Id - 1] = true;
      }
    }

    private void OnWorkspaceShown(Workspace workspace)
    {
      if (_isShown && _bar.Monitor == workspace.Monitor)
      {
        var workspaceId = workspace.Id - 1;
        if (_applicationPanels[workspaceId].Count > 0)
        {
          if (_mustResize[workspaceId])
          {
            ResizeApplicationPanels(_left, _right, workspaceId);
          }
          if (!ShowSingleApplicationTab)
          {
            _applicationPanels[workspaceId].ForEach(t => t.Item2.Show());
          }
          _currentlyHighlightedPanel = null;
        }
      }
    }

    private void OnWorkspaceHidden(Workspace workspace)
    {
      if (_isShown && _bar.Monitor == workspace.Monitor)
      {
        var workspaceId = workspace.Id - 1;
        if (_applicationPanels[workspaceId].Count > 0)
        {
          if (!ShowSingleApplicationTab)
          {
            _applicationPanels[workspaceId].ForEach(t => t.Item2.Hide());
          }
          else if (_currentlyHighlightedPanel != null)
          {
            _currentlyHighlightedPanel.Hide();
          }
        }
      }
    }

    private void OnWorkspaceMonitorChanged(Workspace workspace, Monitor oldMonitor, Monitor newMonitor)
    {
      if (_bar.Monitor == oldMonitor || _bar.Monitor == newMonitor)
      {
        _applicationPanels.ForEach(p => p.ForEach(t => t.Item2.Hide()));

        if (_mustResize[_bar.Monitor.CurrentVisibleWorkspace.Id - 1])
        {
          ResizeApplicationPanels(_left, _right, _bar.Monitor.CurrentVisibleWorkspace.Id - 1);
        }
        if (!ShowSingleApplicationTab)
        {
          _applicationPanels[_bar.Monitor.CurrentVisibleWorkspace.Id - 1].ForEach(t => t.Item2.Show());
        }
      }
    }

    private void OnBarShown()
    {
      _isShown = true;
    }

    private void OnBarHidden()
    {
      _isShown = false;
    }

    #region IWidget Members

    void IWidget.StaticInitializeWidget(Windawesome windawesome)
    {
      ApplicationTabsWidget._windawesome = windawesome;
    }

    void IWidget.InitializeWidget(Bar bar)
    {
      this._bar = bar;

      bar.BarShown += OnBarShown;
      bar.BarHidden += OnBarHidden;

      Windawesome.WindowTitleOrIconChanged += OnWindowTitleOrIconChanged;
      Workspace.WorkspaceWindowAdded += OnWorkspaceWindowAdded;
      Workspace.WorkspaceWindowRemoved += OnWorkspaceWindowRemoved;
      Workspace.WorkspaceWindowRestored += (_, w) => OnWindowActivated(w.hWnd);
      Workspace.WindowActivatedEvent += OnWindowActivated;
      Workspace.WorkspaceHidden += OnWorkspaceHidden;
      Workspace.WorkspaceShown += OnWorkspaceShown;
      Workspace.WorkspaceDeactivated += _ => OnWindowActivated(IntPtr.Zero);
      Workspace.WorkspaceWindowOrderChanged += OnWorkspaceWindowOrderChanged;
      Workspace.WorkspaceMonitorChanged += OnWorkspaceMonitorChanged;

      _currentlyHighlightedPanel = null;

      _mustResize = new bool[_windawesome.config.Workspaces.Length];
      _applicationPanels = new LinkedList<Tuple<IntPtr, Panel>>[_windawesome.config.Workspaces.Length];
      for (var i = 0; i < _windawesome.config.Workspaces.Length; i++)
      {
        _applicationPanels[i] = new LinkedList<Tuple<IntPtr, Panel>>();
      }
    }

    IEnumerable<Control> ISpanWidget.GetInitialControls()
    {
      return Enumerable.Empty<Control>();
    }

    void IWidget.RepositionControls(int left, int right)
    {
      this._left = left;
      this._right = right;

      for (var i = 0; i < _windawesome.config.Workspaces.Length; i++)
      {
        _mustResize[i] = true;
      }

      ResizeApplicationPanels(left, right, _bar.Monitor.CurrentVisibleWorkspace.Id - 1);
    }

    int IWidget.GetLeft()
    {
      return _left;
    }

    int IWidget.GetRight()
    {
      return _right;
    }

    void IWidget.StaticDispose()
    {
    }

    void IWidget.Dispose()
    {
    }

    void IWidget.Refresh()
    {
    }

    #endregion
  }
}
