﻿namespace Windawesome.Layouts
{
	public sealed class FullScreenLayout : ILayout
	{
		private Workspace workspace;

		private void MaximizeWindow(Window window)
		{
			if (Utilities.WindowIsNotHung(window))
			{
				var newMonitorBounds = workspace.Monitor.Bounds;
				var newMonitorWorkingArea = workspace.Monitor.WorkingArea;

				var hWindowsMonitor = NativeMethods.MonitorFromWindow(window.hWnd, NativeMethods.MFRF.MONITOR_MONITOR_DEFAULTTONULL);
				var windowsMonitorInfo = NativeMethods.MONITORINFO.Default;
				NativeMethods.GetMonitorInfo(hWindowsMonitor, ref windowsMonitorInfo);
				var windowsMonitorBounds = windowsMonitorInfo.rcMonitor.ToRectangle();

				var winPlacement = NativeMethods.WINDOWPLACEMENT.Default;
				NativeMethods.GetWindowPlacement(window.hWnd, ref winPlacement);

				winPlacement.MaxPosition.X = newMonitorBounds.Left;
				winPlacement.MaxPosition.Y = newMonitorBounds.Top;

				if (NativeMethods.IsZoomed(window.hWnd) && windowsMonitorBounds != newMonitorBounds)
				{
					// restore if program is maximized and should be on a different monitor
					NativeMethods.ShowWindow(window.hWnd, NativeMethods.SW.SW_SHOWNOACTIVATE); // should not use SW_RESTORE as it activates the window
					System.Threading.Thread.Sleep(NativeMethods.minimizeRestoreDelay);
				}

				var ws = NativeMethods.GetWindowStyleLongPtr(window.hWnd);
				if (ws.HasFlag(NativeMethods.WS.WS_CAPTION | NativeMethods.WS.WS_MAXIMIZEBOX))
				{
					if (windowsMonitorBounds != newMonitorBounds)
					{
						winPlacement.NormalPosition.left += newMonitorBounds.Left - windowsMonitorBounds.Left; // these are in working area coordinates
						winPlacement.NormalPosition.right += newMonitorBounds.Right - windowsMonitorBounds.Right;
						winPlacement.NormalPosition.top += newMonitorBounds.Top - windowsMonitorBounds.Top;
						winPlacement.NormalPosition.bottom += newMonitorBounds.Bottom - windowsMonitorBounds.Bottom;
					}

					winPlacement.ShowCmd = NativeMethods.SW.SW_SHOWMAXIMIZED;
				}
				else
				{
					winPlacement.NormalPosition.left = newMonitorBounds.Left; // these are in working area coordinates
					winPlacement.NormalPosition.right = newMonitorBounds.Left + newMonitorWorkingArea.Width;
					winPlacement.NormalPosition.top = newMonitorBounds.Top;
					winPlacement.NormalPosition.bottom = newMonitorBounds.Top + newMonitorWorkingArea.Height;

					winPlacement.ShowCmd = NativeMethods.SW.SW_SHOWNOACTIVATE;
				}

				NativeMethods.SetWindowPlacement(window.hWnd, ref winPlacement);
			}
		}

		private void OnWorkspaceWindowAddedOrRemoved(Workspace workspace, Window window)
		{
			if (workspace == this.workspace && workspace.IsWorkspaceVisible)
			{
				Workspace.DoLayoutUpdated();
			}
		}

		#region ILayout Members

		string ILayout.LayoutSymbol()
		{
			return workspace.GetWindowsCount() == 0 ? "[M]" : "[" + workspace.GetWindowsCount() + "]";
		}

		public string LayoutName()
		{
			return "Full Screen";
		}

		void ILayout.Initialize(Workspace workspace)
		{
			this.workspace = workspace;

			workspace.WindowTitlebarToggled += MaximizeWindow;
			workspace.WindowBorderToggled += MaximizeWindow;

			Workspace.WorkspaceWindowAdded += OnWorkspaceWindowAddedOrRemoved;
			Workspace.WorkspaceWindowRemoved += OnWorkspaceWindowAddedOrRemoved;
		}

		void ILayout.Dispose()
		{
			workspace.WindowTitlebarToggled -= MaximizeWindow;
			workspace.WindowBorderToggled -= MaximizeWindow;

			Workspace.WorkspaceWindowAdded -= OnWorkspaceWindowAddedOrRemoved;
			Workspace.WorkspaceWindowRemoved -= OnWorkspaceWindowAddedOrRemoved;
		}

		bool ILayout.ShouldSaveAndRestoreSharedWindowsPosition()
		{
			return false;
		}

		void ILayout.Reposition()
		{
			workspace.GetLayoutManagedWindows().ForEach(MaximizeWindow);
			Workspace.DoLayoutUpdated();
		}

		void ILayout.WindowMinimized(Window window)
		{
		}

		void ILayout.WindowRestored(Window window)
		{
			(this as ILayout).WindowCreated(window);
		}

		void ILayout.WindowCreated(Window window)
		{
			if (workspace.IsWorkspaceVisible)
			{
				MaximizeWindow(window);
			}
		}

		void ILayout.WindowDestroyed(Window window)
		{
		}

		#endregion
	}
}
