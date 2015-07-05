using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Windawesome.Widgets
{
  public sealed class SeparatorWidget : IFixedWidthWidget
  {
    private Label _label;
    private bool _isLeft;


    public string Separator { get; set; }

    public Color BackgroundColor { get; set; }

    public Color ForegroundColor { get; set; }


    public SeparatorWidget()
    {
      Separator = "|";
    }

    public SeparatorWidget(string separator = "|", Color? backgroundColor = null, Color? foregroundColor = null)
      : this()
    {
      Separator = separator;

      BackgroundColor = backgroundColor ?? Color.White;
      ForegroundColor = foregroundColor ?? Color.Black;
    }


    #region IWidget Members

    void IWidget.StaticInitializeWidget(Windawesome windawesome)
    {
    }

    void IWidget.InitializeWidget(Bar bar)
    {
      _label = bar.CreateLabel(Separator, 0);
      _label.BackColor = BackgroundColor;
      _label.ForeColor = ForegroundColor;
      _label.TextAlign = ContentAlignment.MiddleCenter;
    }

    IEnumerable<Control> IFixedWidthWidget.GetInitialControls(bool isLeft)
    {
      this._isLeft = isLeft;

      return new[] { _label };
    }

    public void RepositionControls(int left, int right)
    {
      this._label.Location = this._isLeft ? new Point(left, 0) : new Point(right - this._label.Width, 0);
    }

    int IWidget.GetLeft()
    {
      return _label.Left;
    }

    int IWidget.GetRight()
    {
      return _label.Right;
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
