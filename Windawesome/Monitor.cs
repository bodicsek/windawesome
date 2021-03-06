﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Windawesome
{
	public class Monitor
	{
		public readonly int monitorIndex;
		public readonly IntPtr handle;
		public readonly Screen screen;
		public Workspace CurrentVisibleWorkspace { get; internal set; }
		public IEnumerable<Workspace> Workspaces { get { return workspaces.Keys; } }
		public Rectangle Bounds { get; private set; }
		public Rectangle WorkingArea { get; private set; }

		internal readonly HashSet<IntPtr> temporarilyShownWindows;
		private readonly Dictionary<Workspace, Tuple<int, AppBarNativeWindow, AppBarNativeWindow>> workspaces;

		private static bool isWindowsTaskbarShown;

		private static readonly NativeMethods.WinEventDelegate taskbarShownWinEventDelegate = TaskbarShownWinEventDelegate;
		private static readonly IntPtr taskbarShownWinEventHook;

		// TODO: when running under XP and a normal user account, but Windawesome is elevated, for example with SuRun,
		// the AppBars don't resize the desktop working area
		private sealed class AppBarNativeWindow : NativeWindow
		{
			public readonly int height;

			private Monitor monitor;
			private NativeMethods.RECT rect;
			private bool visible;
			private IEnumerable<IBar> bars;
			private readonly uint callbackMessageNum;
			private readonly NativeMethods.ABE edge;
			private bool isTopMost;

			private static uint count;

			public AppBarNativeWindow(int barHeight, bool topBar)
			{
				this.height = barHeight;
				visible = false;
				isTopMost = false;
				edge = topBar ? NativeMethods.ABE.ABE_TOP : NativeMethods.ABE.ABE_BOTTOM;

				this.CreateHandle(new CreateParams { Parent = NativeMethods.HWND_MESSAGE, ClassName = "Message" });

				callbackMessageNum = NativeMethods.WM_USER + count++;

				// register as AppBar
				var appBarData = new NativeMethods.APPBARDATA(this.Handle, callbackMessageNum);

				NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_NEW, ref appBarData);
			}

			public void Destroy()
			{
				// unregister as AppBar
				var appBarData = new NativeMethods.APPBARDATA(this.Handle);

				NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_REMOVE, ref appBarData);

				DestroyHandle();
			}

			public bool SetPosition(Monitor monitor)
			{
				this.monitor = monitor;

				var appBarData = new NativeMethods.APPBARDATA(this.Handle, uEdge: edge, rc: new NativeMethods.RECT { left = monitor.Bounds.Left, right = monitor.Bounds.Right });

				if (edge == NativeMethods.ABE.ABE_TOP)
				{
					appBarData.rc.top = monitor.Bounds.Top;
					appBarData.rc.bottom = appBarData.rc.top + this.height;
				}
				else
				{
					appBarData.rc.bottom = monitor.Bounds.Bottom;
					appBarData.rc.top = appBarData.rc.bottom - this.height;
				}

				NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_QUERYPOS, ref appBarData);

				if (edge == NativeMethods.ABE.ABE_TOP)
				{
					appBarData.rc.bottom = appBarData.rc.top + this.height;
				}
				else
				{
					appBarData.rc.top = appBarData.rc.bottom - this.height;
				}

				NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_SETPOS, ref appBarData);

				var changedPosition = appBarData.rc.bottom != rect.bottom || appBarData.rc.top != rect.top ||
					appBarData.rc.left != rect.left || appBarData.rc.right != rect.right;

				this.rect = appBarData.rc;

				this.visible = true;

				return changedPosition;
			}

			public void Hide()
			{
				var appBarData = new NativeMethods.APPBARDATA(this.Handle, uEdge: NativeMethods.ABE.ABE_TOP);

				NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_QUERYPOS, ref appBarData);
				NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_SETPOS, ref appBarData);

				this.visible = false;
			}

			// move the bars to their respective positions
			public IntPtr PositionBars(IntPtr winPosInfo, IEnumerable<IBar> bars)
			{
				this.bars = bars;

				var topBar = edge == NativeMethods.ABE.ABE_TOP;
				var currentY = topBar ? rect.top : rect.bottom;
				foreach (var bar in bars)
				{
					if (!topBar)
					{
						currentY -= bar.GetBarHeight();
					}
					var barRect = new NativeMethods.RECT
						{
							left = rect.left,
							top = currentY,
							right = rect.right,
							bottom = currentY + bar.GetBarHeight()
						};
					if (topBar)
					{
						currentY += bar.GetBarHeight();
					}

					bar.OnClientWidthChanging(barRect.right - barRect.left);

					NativeMethods.AdjustWindowRectEx(ref barRect, NativeMethods.GetWindowStyleLongPtr(bar.Handle),
						NativeMethods.GetMenu(bar.Handle) != IntPtr.Zero, NativeMethods.GetWindowExStyleLongPtr(bar.Handle));

					winPosInfo = NativeMethods.DeferWindowPos(winPosInfo, bar.Handle, NativeMethods.HWND_TOPMOST, barRect.left, barRect.top,
						barRect.right - barRect.left, barRect.bottom - barRect.top, NativeMethods.SWP.SWP_NOACTIVATE);
				}

				isTopMost = true;

				return winPosInfo;
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == callbackMessageNum)
				{
					if (visible)
					{
						switch ((NativeMethods.ABN) m.WParam)
						{
							case NativeMethods.ABN.ABN_FULLSCREENAPP:
								if (m.LParam == IntPtr.Zero)
								{
									// full-screen app is closing
									if (!isTopMost)
									{
										var winPosInfo = NativeMethods.BeginDeferWindowPos(bars.Count());
										winPosInfo = this.bars.Aggregate(winPosInfo, (current, bar) =>
											NativeMethods.DeferWindowPos(current, bar.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
												NativeMethods.SWP.SWP_NOACTIVATE | NativeMethods.SWP.SWP_NOMOVE | NativeMethods.SWP.SWP_NOSIZE));
										NativeMethods.EndDeferWindowPos(winPosInfo);

										isTopMost = true;
									}
								}
								else
								{
									// full-screen app is opening - check if that is the desktop window
									var foregroundWindow = NativeMethods.GetForegroundWindow();
									if (isTopMost && NativeMethods.GetWindowClassName(foregroundWindow) != "WorkerW")
									{
										var winPosInfo = NativeMethods.BeginDeferWindowPos(bars.Count());
										winPosInfo = this.bars.Aggregate(winPosInfo, (current, bar) =>
											NativeMethods.DeferWindowPos(current, bar.Handle, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0,
												NativeMethods.SWP.SWP_NOACTIVATE | NativeMethods.SWP.SWP_NOMOVE | NativeMethods.SWP.SWP_NOSIZE));
										NativeMethods.EndDeferWindowPos(winPosInfo);

										isTopMost = false;
									}
								}
								break;
							case NativeMethods.ABN.ABN_POSCHANGED:
								// ABN_POSCHANGED could be sent before the Monitor is notified of the change
								// of the working area in Windawesome::OnDisplaySettingsChanged
								monitor.SetBoundsAndWorkingArea();
								if (SetPosition(monitor))
								{
									var winPosInfo = NativeMethods.BeginDeferWindowPos(bars.Count());
									NativeMethods.EndDeferWindowPos(PositionBars(winPosInfo, bars));
								}
								break;
						}
					}
				}
				else
				{
					base.WndProc(ref m);
				}
			}
		}

		static Monitor()
		{
			// this is because Windows shows the taskbar at random points when it is made to autohide
			taskbarShownWinEventHook = NativeMethods.SetWinEventHook(NativeMethods.EVENT.EVENT_OBJECT_SHOW, NativeMethods.EVENT.EVENT_OBJECT_SHOW,
				IntPtr.Zero, taskbarShownWinEventDelegate, 0,
				NativeMethods.GetWindowThreadProcessId(SystemAndProcessInformation.taskbarHandle, IntPtr.Zero),
				NativeMethods.WINEVENT.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT.WINEVENT_SKIPOWNTHREAD);
		}

		internal Monitor(int monitorIndex)
		{
			this.workspaces = new Dictionary<Workspace, Tuple<int, AppBarNativeWindow, AppBarNativeWindow>>(2);
			this.temporarilyShownWindows = new HashSet<IntPtr>();

			this.monitorIndex = monitorIndex;
			this.screen = Screen.AllScreens[monitorIndex];
			var rect = NativeMethods.RECT.FromRectangle(this.screen.Bounds);
			this.handle = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MFRF.MONITOR_MONITOR_DEFAULTTONULL);
			SetBoundsAndWorkingArea();
		}

		internal void Dispose()
		{
			// this statement uses the laziness of Where
			workspaces.Values.Select(t => t.Item2).Concat(workspaces.Values.Select(t => t.Item3)).
				Where(ab => ab != null && ab.Handle != IntPtr.Zero).ForEach(ab => ab.Destroy());
		}

		internal static void StaticDispose()
		{
			NativeMethods.UnhookWinEvent(taskbarShownWinEventHook);

			if (!isWindowsTaskbarShown)
			{
				ShowHideWindowsTaskbar(true);
			}
		}

		internal void SetBoundsAndWorkingArea()
		{
			var monitorInfo = NativeMethods.MONITORINFO.Default;
			NativeMethods.GetMonitorInfo(this.handle, ref monitorInfo);
			Bounds = monitorInfo.rcMonitor.ToRectangle();
			WorkingArea = monitorInfo.rcWork.ToRectangle();
		}

		internal void SetStartingWorkspace(Workspace startingWorkspace)
		{
			CurrentVisibleWorkspace = startingWorkspace;
		}

		internal void Initialize()
		{
			ShowHideAppBars(null, CurrentVisibleWorkspace);

			ShowBars(CurrentVisibleWorkspace);

			CurrentVisibleWorkspace.SwitchTo();
		}

		public override bool Equals(object obj)
		{
			var other = obj as Monitor;
			return other != null && other.monitorIndex == this.monitorIndex;
		}

		public override int GetHashCode()
		{
			return this.monitorIndex;
		}

		private static void TaskbarShownWinEventDelegate(IntPtr hWinEventHook, NativeMethods.EVENT eventType,
			IntPtr hwnd, NativeMethods.OBJID idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			if (NativeMethods.IsWindowVisible(SystemAndProcessInformation.taskbarHandle) != isWindowsTaskbarShown)
			{
				ShowHideWindowsTaskbar(isWindowsTaskbarShown);
			}
		}

		internal void SwitchToWorkspace(Workspace workspace)
		{
			CurrentVisibleWorkspace.Unswitch();

			HideBars(workspace, CurrentVisibleWorkspace);

			// hides or shows the Windows taskbar
			if (screen.Primary && workspace.ShowWindowsTaskbar != isWindowsTaskbarShown)
			{
				ShowHideWindowsTaskbar(workspace.ShowWindowsTaskbar);
			}

			ShowHideAppBars(CurrentVisibleWorkspace, workspace);

			CurrentVisibleWorkspace = workspace;

			ShowBars(CurrentVisibleWorkspace);

			workspace.SwitchTo();
		}

		internal void HideBars(Workspace newWorkspace, Workspace oldWorkspace)
		{
			var oldBarsAtTop = oldWorkspace.AllBarsAtTop[monitorIndex];
			var oldBarsAtBottom = oldWorkspace.AllBarsAtBottom[monitorIndex];
			var newBarsAtTop = newWorkspace.AllBarsAtTop[monitorIndex];
			var newBarsAtBottom = newWorkspace.AllBarsAtBottom[monitorIndex];

			oldBarsAtTop.Concat(oldBarsAtBottom).Except(newBarsAtTop.Concat(newBarsAtBottom)).ForEach(b => b.Hide());
		}

		internal void ShowBars(Workspace workspace)
		{
			var newBarsAtTop = workspace.AllBarsAtTop[monitorIndex];
			var newBarsAtBottom = workspace.AllBarsAtBottom[monitorIndex];

			newBarsAtTop.Concat(newBarsAtBottom).ForEach(b => b.Show());
		}

		internal void ShowHideAppBars(Workspace oldWorkspace, Workspace newWorkspace)
		{
			var oldWorkspaceTuple = oldWorkspace == null ? null : workspaces[oldWorkspace];
			var newWorkspaceTuple = workspaces[newWorkspace];

			if (oldWorkspaceTuple == null || newWorkspaceTuple.Item1 != oldWorkspaceTuple.Item1)
			{
				ShowHideAppBarsAndRepositionBars(
					oldWorkspaceTuple == null ? null : oldWorkspaceTuple.Item2,
					oldWorkspaceTuple == null ? null : oldWorkspaceTuple.Item3,
					newWorkspaceTuple.Item2,
					newWorkspaceTuple.Item3,
					newWorkspace);
			}
		}

		internal static void ShowHideWindowsTaskbar(bool showWindowsTaskbar)
		{
			var appBarData = new NativeMethods.APPBARDATA(SystemAndProcessInformation.taskbarHandle);
			var state = (NativeMethods.ABS) (uint) NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_GETSTATE, ref appBarData);

			appBarData.lParam = (IntPtr) (showWindowsTaskbar ? state & ~NativeMethods.ABS.ABS_AUTOHIDE : state | NativeMethods.ABS.ABS_AUTOHIDE);
			NativeMethods.SHAppBarMessage(NativeMethods.ABM.ABM_SETSTATE, ref appBarData);

			var showHide = showWindowsTaskbar ? NativeMethods.SW.SW_SHOWNA : NativeMethods.SW.SW_HIDE;

			NativeMethods.ShowWindow(SystemAndProcessInformation.taskbarHandle, showHide);
			if (SystemAndProcessInformation.isAtLeastVista)
			{
				NativeMethods.ShowWindow(SystemAndProcessInformation.startButtonHandle, showHide);
			}

			isWindowsTaskbarShown = showWindowsTaskbar;
		}

		internal void AddWorkspace(Workspace workspace)
		{
			var workspaceBarsAtTop = workspace.AllBarsAtTop[monitorIndex];
			var workspaceBarsAtBottom = workspace.AllBarsAtBottom[monitorIndex];

			var matchingBar = workspaces.Keys.FirstOrDefault(ws =>
				workspaceBarsAtTop.SequenceEqual(ws.AllBarsAtTop[monitorIndex]) && workspaceBarsAtBottom.SequenceEqual(ws.AllBarsAtBottom[monitorIndex]));
			if (matchingBar != null)
			{
				var matchingWorkspace = workspaces[matchingBar];
				this.workspaces[workspace] = Tuple.Create(matchingWorkspace.Item1, matchingWorkspace.Item2, matchingWorkspace.Item3);

				return ;
			}

			var workspaceBarsEquivalentClass = (this.workspaces.Count == 0 ? 0 : this.workspaces.Values.Max(t => t.Item1)) + 1;

			AppBarNativeWindow appBarTopWindow;
			var topBarsHeight = workspaceBarsAtTop.Sum(bar => bar.GetBarHeight());
			var matchingAppBar = workspaces.Values.Select(t => t.Item2).FirstOrDefault(ab =>
				(ab == null && topBarsHeight == 0) || (ab != null && topBarsHeight == ab.height));
			if (matchingAppBar != null || topBarsHeight == 0)
			{
				appBarTopWindow = matchingAppBar;
			}
			else
			{
				appBarTopWindow = new AppBarNativeWindow(topBarsHeight, true);
			}

			AppBarNativeWindow appBarBottomWindow;
			var bottomBarsHeight = workspaceBarsAtBottom.Sum(bar => bar.GetBarHeight());
			matchingAppBar = workspaces.Values.Select(t => t.Item3).FirstOrDefault(uniqueAppBar =>
				(uniqueAppBar == null && bottomBarsHeight == 0) || (uniqueAppBar != null && bottomBarsHeight == uniqueAppBar.height));
			if (matchingAppBar != null || bottomBarsHeight == 0)
			{
				appBarBottomWindow = matchingAppBar;
			}
			else
			{
				appBarBottomWindow = new AppBarNativeWindow(bottomBarsHeight, false);
			}

			this.workspaces[workspace] = Tuple.Create(workspaceBarsEquivalentClass, appBarTopWindow, appBarBottomWindow);
		}

		internal void RemoveWorkspace(Workspace workspace)
		{
			var workspaceTuple = workspaces[workspace];
			workspaces.Remove(workspace);
			if (workspaceTuple.Item2 != null && workspaces.All(kv => kv.Value.Item2 != workspaceTuple.Item2))
			{
				workspaceTuple.Item2.Destroy();
			}
			if (workspaceTuple.Item3 != null && workspaces.All(kv => kv.Value.Item3 != workspaceTuple.Item3))
			{
				workspaceTuple.Item3.Destroy();
			}
		}

		private void ShowHideAppBarsAndRepositionBars(AppBarNativeWindow previousAppBarTopWindow, AppBarNativeWindow previousAppBarBottomWindow,
			AppBarNativeWindow newAppBarTopWindow, AppBarNativeWindow newAppBarBottomWindow,
			Workspace newWorkspace)
		{
			ShowHideAppBarForms(previousAppBarTopWindow, newAppBarTopWindow);
			ShowHideAppBarForms(previousAppBarBottomWindow, newAppBarBottomWindow);

			var newBarsAtTop = newWorkspace.AllBarsAtTop[monitorIndex];
			var newBarsAtBottom = newWorkspace.AllBarsAtBottom[monitorIndex];

			var winPosInfo = NativeMethods.BeginDeferWindowPos(newBarsAtTop.Count + newBarsAtBottom.Count);
			if (newAppBarTopWindow != null)
			{
				winPosInfo = newAppBarTopWindow.PositionBars(winPosInfo, newBarsAtTop);
			}
			if (newAppBarBottomWindow != null)
			{
				winPosInfo = newAppBarBottomWindow.PositionBars(winPosInfo, newBarsAtBottom);
			}
			NativeMethods.EndDeferWindowPos(winPosInfo);
		}

		private void ShowHideAppBarForms(AppBarNativeWindow hideForm, AppBarNativeWindow showForm)
		{
			// this whole thing is so complicated as to avoid changing of the working area if the bars in the new workspace
			// take the same space as the one in the previous one

			// set the working area to a new one if needed
			if (hideForm != null)
			{
				if (showForm == null || hideForm != showForm)
				{
					hideForm.Hide();
					if (showForm != null)
					{
						showForm.SetPosition(this);
					}
				}
			}
			else if (showForm != null)
			{
				showForm.SetPosition(this);
			}
		}
	}
}
