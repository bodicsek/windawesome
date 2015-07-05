using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Windawesome.Widgets
{
	public sealed class LayoutWidget : IFixedWidthWidget
	{
		private Label _layoutLabel;
		private Bar _bar;
		private bool _isLeft;


    public Color BackgroundColor { get; set; }

    public Color ForegroundColor { get; set; }

    public Action OnClick { get; set; }


    public LayoutWidget()
    {
    }

		public LayoutWidget(Color? backgroundColor = null, Color? foregroundColor = null, Action onClick = null)
		{
			BackgroundColor = backgroundColor ?? Color.FromArgb(0x99, 0xB4, 0xD1);
			ForegroundColor = foregroundColor ?? Color.Black;
			OnClick = onClick;
		}


		private void OnWorkspaceLayoutChanged(Workspace workspace)
		{
			if (workspace.Monitor == _bar.Monitor && workspace.IsWorkspaceVisible)
			{
				var oldLeft = _layoutLabel.Left;
				var oldRight = _layoutLabel.Right;
				var oldWidth = _layoutLabel.Width;
				_layoutLabel.Text = workspace.Layout.LayoutSymbol();
				_layoutLabel.Width = TextRenderer.MeasureText(_layoutLabel.Text, _layoutLabel.Font).Width;
				if (_layoutLabel.Width != oldWidth)
				{
					this.RepositionControls(oldLeft, oldRight);
					_bar.DoFixedWidthWidgetWidthChanged(this);
				}
			}
		}

		#region IWidget Members

		void IWidget.StaticInitializeWidget(Windawesome windawesome)
		{
		}

		void IWidget.InitializeWidget(Bar bar)
		{
			this._bar = bar;

			bar.BarShown += () => OnWorkspaceLayoutChanged(bar.Monitor.CurrentVisibleWorkspace);

			Workspace.LayoutUpdated += () => OnWorkspaceLayoutChanged(bar.Monitor.CurrentVisibleWorkspace);
			Workspace.WorkspaceShown += OnWorkspaceLayoutChanged;
			Workspace.WorkspaceLayoutChanged += (ws, _) => OnWorkspaceLayoutChanged(ws);

			_layoutLabel = bar.CreateLabel("", 0);
			_layoutLabel.TextAlign = ContentAlignment.MiddleCenter;
			_layoutLabel.BackColor = BackgroundColor;
			_layoutLabel.ForeColor = ForegroundColor;
			if (OnClick != null)
			{
				_layoutLabel.Click += (unused1, unused2) => OnClick();
			}
		}

		IEnumerable<Control> IFixedWidthWidget.GetInitialControls(bool isLeft)
		{
			this._isLeft = isLeft;

			return new Control[] { _layoutLabel };
		}

		public void RepositionControls(int left, int right)
		{
			this._layoutLabel.Location = this._isLeft ? new Point(left, 0) : new Point(right - this._layoutLabel.Width, 0);
		}

		int IWidget.GetLeft()
		{
			return _layoutLabel.Left;
		}

		int IWidget.GetRight()
		{
			return _layoutLabel.Right;
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
