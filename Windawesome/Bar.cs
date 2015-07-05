using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Windawesome
{
  public sealed class Bar : IBar
  {

    public class NonActivatableForm : Form
    {
      protected override CreateParams CreateParams
      {
        get
        {
          var createParams = base.CreateParams;
          // make the form not activatable
          createParams.ExStyle |= (int)
            (NativeMethods.WS_EX.WS_EX_NOACTIVATE | NativeMethods.WS_EX.WS_EX_TOOLWINDOW | NativeMethods.WS_EX.WS_EX_TOPMOST);
          return createParams;
        }
      }

      protected override bool ShowWithoutActivation { get { return true; } }

      protected override void WndProc(ref Message m)
      {
        if (m.Msg == NativeMethods.WM_MOUSEACTIVATE)
        {
          m.Result = NativeMethods.MA_NOACTIVATE;
        }
        else if (m.Msg == NativeMethods.WM_SYSCOMMAND &&
          (m.WParam == NativeMethods.SC_MINIMIZESigned || m.WParam == NativeMethods.SC_MAXIMIZESigned))
        {
          m.Result = IntPtr.Zero;
        }
        else
        {
          base.WndProc(ref m);
        }
      }
    }

    private static readonly HashSet<Type> s_WidgetTypes = new HashSet<Type>();

    private readonly NonActivatableForm _form;
    private int _rightmostLeftAlign;
    private int _leftmostRightAlign;


    public Monitor Monitor { get; set; }

    public IFixedWidthWidget[] LeftAlignedWidgets { get; set; }

    public IFixedWidthWidget[] RightAlignedWidgets { get; set; }

    public ISpanWidget[] MiddleAlignedWidgets { get; set; }

    public int BarHeight { get; set; }

    public Font Font { get; set; }

    public Color BackgroundColor
    {
      get { return _form.BackColor; }
      set { _form.BackColor = value; }
    }


    #region Events

    private delegate void SpanWidgetControlsAddedEventHandler(ISpanWidget widget, IEnumerable<Control> controls);
    private event SpanWidgetControlsAddedEventHandler SpanWidgetControlsAdded;

    private delegate void SpanWidgetControlsRemovedEventHandler(ISpanWidget widget, IEnumerable<Control> controls);
    private event SpanWidgetControlsRemovedEventHandler SpanWidgetControlsRemoved;

    private delegate void FixedWidthWidgetWidthChangedEventHandler(IFixedWidthWidget widget);
    private event FixedWidthWidgetWidthChangedEventHandler FixedWidthWidgetWidthChanged;

    private delegate void WidgetControlsChangedEventHandler(IWidget widget, IEnumerable<Control> oldControls, IEnumerable<Control> newControls);
    private event WidgetControlsChangedEventHandler WidgetControlsChanged;

    public delegate void BarShownEventHandler();
    public event BarShownEventHandler BarShown;

    public delegate void BarHiddenEventHandler();
    public event BarHiddenEventHandler BarHidden;

    public void DoWidgetControlsChanged(IWidget widget, IEnumerable<Control> controlsRemoved, IEnumerable<Control> controlsAdded)
    {
      WidgetControlsChanged(widget, controlsRemoved, controlsAdded);
    }

    public void DoSpanWidgetControlsAdded(ISpanWidget widget, IEnumerable<Control> controls)
    {
      SpanWidgetControlsAdded(widget, controls);
    }

    public void DoSpanWidgetControlsRemoved(ISpanWidget widget, IEnumerable<Control> controls)
    {
      SpanWidgetControlsRemoved(widget, controls);
    }

    public void DoFixedWidthWidgetWidthChanged(IFixedWidthWidget widget)
    {
      FixedWidthWidgetWidthChanged(widget);
    }

    private void DoBarShown()
    {
      if (BarShown != null)
      {
        BarShown();
      }
    }

    private void DoBarHidden()
    {
      if (BarHidden != null)
      {
        BarHidden();
      }
    }

    #endregion

    public Bar()
    {
      BarHeight = 20;
      Font = new Font("Lucida Console", 8);
      _form = CreateForm();
    }

    public Bar(Monitor monitor, IEnumerable<IFixedWidthWidget> leftAlignedWidgets, IEnumerable<IFixedWidthWidget> rightAlignedWidgets,
      IEnumerable<ISpanWidget> middleAlignedWidgets, int barHeight = 20, Font font = null, Color? backgroundColor = null)
      : this()
    {
      Monitor = monitor;
      LeftAlignedWidgets = leftAlignedWidgets.ToArray();
      RightAlignedWidgets = rightAlignedWidgets.ToArray();
      MiddleAlignedWidgets = middleAlignedWidgets.ToArray();
      BarHeight = barHeight;
      Font = font ?? new Font("Lucida Console", 8);
      BackgroundColor = backgroundColor ?? Color.Black;
    }

    public override int GetHashCode()
    {
      return this._form.Handle.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      var bar = obj as Bar;
      return bar != null && this._form.Handle == bar._form.Handle;
    }

    #region IBar Members

    void IBar.InitializeBar(Windawesome windawesome)
    {
      // statically initialize all widgets
      // this statement uses the laziness of Where
      this.LeftAlignedWidgets.Cast<IWidget>().Concat(this.RightAlignedWidgets).Concat(this.MiddleAlignedWidgets).
        Where(w => !s_WidgetTypes.Contains(w.GetType())).
        ForEach(w => { w.StaticInitializeWidget(windawesome); s_WidgetTypes.Add(w.GetType()); });

      WidgetControlsChanged = OnWidgetControlsChanged;
      SpanWidgetControlsAdded = OnSpanWidgetControlsAdded;
      SpanWidgetControlsRemoved = OnSpanWidgetControlsRemoved;
      FixedWidthWidgetWidthChanged = OnFixedWidthWidgetWidthChanged;

      LeftAlignedWidgets.ForEach(w => w.InitializeWidget(this));
      RightAlignedWidgets.ForEach(w => w.InitializeWidget(this));
      MiddleAlignedWidgets.ForEach(w => w.InitializeWidget(this));

      // get initial controls
      this._form.SuspendLayout();

      this.LeftAlignedWidgets.SelectMany(widget => widget.GetInitialControls(true)).ForEach(this._form.Controls.Add);
      this.RightAlignedWidgets.SelectMany(widget => widget.GetInitialControls(false)).ForEach(this._form.Controls.Add);
      this.MiddleAlignedWidgets.SelectMany(widget => widget.GetInitialControls()).ForEach(this._form.Controls.Add);

      this._form.ResumeLayout();
    }

    void IBar.Dispose()
    {
      LeftAlignedWidgets.ForEach(w => w.Dispose());
      RightAlignedWidgets.ForEach(w => w.Dispose());
      MiddleAlignedWidgets.ForEach(w => w.Dispose());

      // statically dispose of all widgets
      // this statement uses the laziness of Where
      this.LeftAlignedWidgets.Cast<IWidget>().Concat(this.RightAlignedWidgets).Concat(this.MiddleAlignedWidgets).
        Where(w => s_WidgetTypes.Contains(w.GetType())).
        ForEach(w => { w.StaticDispose(); s_WidgetTypes.Remove(w.GetType()); });

      this._form.Dispose();
    }

    public int GetBarHeight()
    {
      return BarHeight;
    }

    IntPtr IBar.Handle { get { return this._form.Handle; } }


    void IBar.OnClientWidthChanging(int newWidth)
    {
      if (this._form.ClientSize.Width != newWidth)
      {
        ResizeWidgets(newWidth);
      }
    }

    void IBar.Show()
    {
      if (!this._form.Visible)
      {
        this._form.Show();
        DoBarShown();
      }
    }

    void IBar.Hide()
    {
      this._form.Hide();
      DoBarHidden();
    }

    void IBar.Refresh()
    {
      this.LeftAlignedWidgets.Cast<IWidget>().Concat(this.RightAlignedWidgets).Concat(this.MiddleAlignedWidgets).
        ForEach(w => w.Refresh());
    }

    #endregion

    #region Event Handlers

    private void OnWidgetControlsChanged(IWidget widget, IEnumerable<Control> controlsRemoved, IEnumerable<Control> controlsAdded)
    {
      this._form.SuspendLayout();

      controlsRemoved.ForEach(this._form.Controls.Remove);
      controlsAdded.ForEach(this._form.Controls.Add);

      if (widget is IFixedWidthWidget)
      {
        ResizeWidgets(widget as IFixedWidthWidget);
      }

      this._form.ResumeLayout();
    }

    private void OnSpanWidgetControlsAdded(ISpanWidget widget, IEnumerable<Control> controls)
    {
      this._form.SuspendLayout();

      controls.ForEach(this._form.Controls.Add);

      this._form.ResumeLayout();
    }

    private void OnSpanWidgetControlsRemoved(ISpanWidget widget, IEnumerable<Control> controls)
    {
      this._form.SuspendLayout();

      controls.ForEach(this._form.Controls.Remove);

      this._form.ResumeLayout();
    }

    private void OnFixedWidthWidgetWidthChanged(IFixedWidthWidget widget)
    {
      this._form.SuspendLayout();

      ResizeWidgets(widget);

      this._form.ResumeLayout();
    }

    #endregion

    private static NonActivatableForm CreateForm()
    {
      var newForm = new NonActivatableForm
        {
          StartPosition = FormStartPosition.Manual,
          FormBorderStyle = FormBorderStyle.FixedToolWindow,
          AutoValidate = AutoValidate.Disable,
          CausesValidation = false,
          ControlBox = false,
          MaximizeBox = false,
          MinimizeBox = false,
          ShowIcon = false,
          ShowInTaskbar = false,
          SizeGripStyle = SizeGripStyle.Hide,
          AutoScaleMode = AutoScaleMode.Font,
          AutoScroll = false,
          AutoSize = false,
          HelpButton = false,
          TopLevel = true,
          WindowState = FormWindowState.Normal,
          ClientSize = new Size(0, 0)
        };

      return newForm;
    }

    private void ResizeWidgets(int newWidth)
    {
      RepositionLeftAlignedWidgets(0, 0);
      RepositionRightAlignedWidgets(RightAlignedWidgets.Length - 1, newWidth);
      RepositionMiddleAlignedWidgets();
    }

    private void ResizeWidgets(IFixedWidthWidget widget)
    {
      int index;
      if ((index = Array.IndexOf(LeftAlignedWidgets, widget)) != -1)
      {
        RepositionLeftAlignedWidgets(index + 1, widget.GetRight());
      }
      else
      {
        RepositionRightAlignedWidgets(Array.IndexOf(RightAlignedWidgets, widget) - 1, widget.GetLeft());
      }

      RepositionMiddleAlignedWidgets();
    }

    private void RepositionLeftAlignedWidgets(int fromIndex, int fromX)
    {
      for (var i = fromIndex; i < LeftAlignedWidgets.Length; i++)
      {
        var w = LeftAlignedWidgets[i];
        w.RepositionControls(fromX, -1);
        fromX = w.GetRight();
      }

      _rightmostLeftAlign = fromX;
    }

    private void RepositionRightAlignedWidgets(int fromIndex, int toX)
    {
      for (var i = fromIndex; i >= 0; i--)
      {
        var w = RightAlignedWidgets[i];
        w.RepositionControls(-1, toX);
        toX = w.GetLeft();
      }

      _leftmostRightAlign = toX;
    }

    private void RepositionMiddleAlignedWidgets()
    {
      if (MiddleAlignedWidgets.Length > 0)
      {
        var eachWidth = (_leftmostRightAlign - _rightmostLeftAlign) / MiddleAlignedWidgets.Length;
        var x = _rightmostLeftAlign;
        foreach (var w in MiddleAlignedWidgets)
        {
          w.RepositionControls(x, x + eachWidth);
          x += eachWidth;
        }
      }
    }

    public Label CreateLabel(string text, int xLocation, int width = -1)
    {
      var label = new Label();
      label.SuspendLayout();
      label.AutoSize = false;
      label.AutoEllipsis = true;
      label.Text = text;
      label.Font = Font;
      label.Size = new Size(width == -1 ? TextRenderer.MeasureText(label.Text, label.Font).Width : width, this.BarHeight);
      label.Location = new Point(xLocation, 0);
      label.TextAlign = ContentAlignment.MiddleLeft; // TODO: this doesn't work when there are ellipsis for certain fonts/font-sizes
      label.ResumeLayout();

      return label;
    }
  }
}
