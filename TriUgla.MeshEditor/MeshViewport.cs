using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TriUgla.MeshEditor;

public sealed class MeshViewport : FrameworkElement
{
    const double MinZoom = 0.05;
    const double MaxZoom = 100;
    const double BasePixelsPerUnit = 48;

    static readonly Pen MinorGridPen = FrozenPen("#142039", 1);
    static readonly Pen MajorGridPen = FrozenPen("#22314D", 1);
    static readonly Pen AxisXPen = FrozenPen("#F87171", 1.4);
    static readonly Pen AxisYPen = FrozenPen("#4ADE80", 1.4);
    static readonly Pen EdgePen = FrozenPen("#67E8F9", 1.35);
    static readonly Pen BoundaryPen = FrozenPen("#E2E8F0", 2.1);
    static readonly Brush FaceBrush = FrozenBrush("#142C3C50");
    static readonly Brush NodeBrush = FrozenBrush("#F8FAFC");
    static readonly Brush LabelBrush = FrozenBrush("#71819A");

    readonly Point[] _vertices =
    [
        new(-5, -2.6), new(-3.2, 2.1), new(-0.7, 3.3), new(2.3, 2.5),
        new(5.1, 0.7), new(3.8, -2.8), new(0.6, -3.6), new(-2.1, -3.2),
        new(-1.8, -0.4), new(0.3, 1.2), new(2.2, -0.8)
    ];

    readonly (int A, int B, int C)[] _triangles =
    [
        (0, 1, 8), (1, 2, 9), (1, 9, 8), (2, 3, 9), (3, 4, 10),
        (3, 10, 9), (4, 5, 10), (5, 6, 10), (6, 7, 8), (6, 8, 10),
        (7, 0, 8), (8, 9, 10)
    ];

    Point _origin;
    Point _lastPointer;
    bool _isPanning;

    public event EventHandler? ViewChanged;
    public event EventHandler<Point>? MouseWorldPositionChanged;

    public double Zoom { get; private set; } = 1;

    public MeshViewport()
    {
        Cursor = Cursors.Cross;
        ClipToBounds = true;
        Focusable = true;
        SnapsToDevicePixels = true;
        Loaded += (_, _) => ResetView();
        SizeChanged += (_, _) => InvalidateVisual();
    }

    public void ResetView()
    {
        Zoom = 1;
        _origin = new Point(ActualWidth / 2, ActualHeight / 2);
        NotifyViewChanged();
    }

    public void FitToMesh()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        double minX = _vertices.Min(p => p.X);
        double maxX = _vertices.Max(p => p.X);
        double minY = _vertices.Min(p => p.Y);
        double maxY = _vertices.Max(p => p.Y);
        const double padding = 90;
        double availableWidth = Math.Max(1, ActualWidth - padding * 2);
        double availableHeight = Math.Max(1, ActualHeight - padding * 2);
        Zoom = Math.Clamp(Math.Min(availableWidth / ((maxX - minX) * BasePixelsPerUnit),
                                   availableHeight / ((maxY - minY) * BasePixelsPerUnit)), MinZoom, MaxZoom);

        var center = new Point((minX + maxX) / 2, (minY + maxY) / 2);
        _origin = new Point(ActualWidth / 2 - center.X * Scale, ActualHeight / 2 + center.Y * Scale);
        NotifyViewChanged();
    }

    public void ZoomAtCenter(double factor)
        => ZoomAround(new Point(ActualWidth / 2, ActualHeight / 2), factor);

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(FrozenBrush("#080D18"), null, new Rect(RenderSize));
        DrawGrid(dc);
        DrawMesh(dc);
        DrawOrigin(dc);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        ZoomAround(e.GetPosition(this), Math.Pow(1.0015, e.Delta));
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        if (e.ClickCount == 2)
        {
            FitToMesh();
            e.Handled = true;
            return;
        }

        _isPanning = true;
        _lastPointer = e.GetPosition(this);
        CaptureMouse();
        Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Point current = e.GetPosition(this);
        MouseWorldPositionChanged?.Invoke(this, ScreenToWorld(current));

        if (_isPanning)
        {
            Vector movement = current - _lastPointer;
            _origin += movement;
            _lastPointer = current;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!_isPanning) return;
        _isPanning = false;
        ReleaseMouseCapture();
        Cursor = Cursors.Cross;
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        _isPanning = false;
        Cursor = Cursors.Cross;
        base.OnLostMouseCapture(e);
    }

    double Scale => BasePixelsPerUnit * Zoom;

    void ZoomAround(Point anchor, double factor)
    {
        Point world = ScreenToWorld(anchor);
        double next = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        if (Math.Abs(next - Zoom) < double.Epsilon) return;
        Zoom = next;
        _origin = new Point(anchor.X - world.X * Scale, anchor.Y + world.Y * Scale);
        NotifyViewChanged();
    }

    Point WorldToScreen(Point point) => new(_origin.X + point.X * Scale, _origin.Y - point.Y * Scale);
    Point ScreenToWorld(Point point) => new((point.X - _origin.X) / Scale, (_origin.Y - point.Y) / Scale);

    void DrawGrid(DrawingContext dc)
    {
        double step = NiceGridStep(70 / Scale);
        double minorPixels = step * Scale;
        Point topLeft = ScreenToWorld(new Point(0, 0));
        Point bottomRight = ScreenToWorld(new Point(ActualWidth, ActualHeight));
        int firstX = (int)Math.Floor(topLeft.X / step);
        int lastX = (int)Math.Ceiling(bottomRight.X / step);
        int firstY = (int)Math.Floor(bottomRight.Y / step);
        int lastY = (int)Math.Ceiling(topLeft.Y / step);

        for (int i = firstX; i <= lastX; i++)
        {
            double x = _origin.X + i * minorPixels;
            Pen pen = i == 0 ? AxisYPen : (i % 5 == 0 ? MajorGridPen : MinorGridPen);
            dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
            if (i != 0 && i % 5 == 0) DrawLabel(dc, (i * step).ToString("0.##", CultureInfo.InvariantCulture), new Point(x + 5, _origin.Y + 5));
        }

        for (int i = firstY; i <= lastY; i++)
        {
            double y = _origin.Y - i * minorPixels;
            Pen pen = i == 0 ? AxisXPen : (i % 5 == 0 ? MajorGridPen : MinorGridPen);
            dc.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
            if (i != 0 && i % 5 == 0) DrawLabel(dc, (i * step).ToString("0.##", CultureInfo.InvariantCulture), new Point(_origin.X + 5, y + 3));
        }
    }

    void DrawMesh(DrawingContext dc)
    {
        foreach (var triangle in _triangles)
        {
            Point a = WorldToScreen(_vertices[triangle.A]);
            Point b = WorldToScreen(_vertices[triangle.B]);
            Point c = WorldToScreen(_vertices[triangle.C]);
            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(a, true, true);
                context.PolyLineTo([b, c], true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(FaceBrush, EdgePen, geometry);
        }

        int[] boundary = [0, 1, 2, 3, 4, 5, 6, 7, 0];
        for (int i = 0; i < boundary.Length - 1; i++)
            dc.DrawLine(BoundaryPen, WorldToScreen(_vertices[boundary[i]]), WorldToScreen(_vertices[boundary[i + 1]]));

        foreach (Point vertex in _vertices)
            dc.DrawEllipse(NodeBrush, null, WorldToScreen(vertex), 3.2, 3.2);
    }

    void DrawOrigin(DrawingContext dc)
    {
        dc.DrawEllipse(FrozenBrush("#F8FAFC"), null, _origin, 3, 3);
        DrawLabel(dc, "0", _origin + new Vector(7, 6));
    }

    void DrawLabel(DrawingContext dc, string text, Point location)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 10, LabelBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, location);
    }

    void NotifyViewChanged()
    {
        InvalidateVisual();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    static double NiceGridStep(double target)
    {
        double power = Math.Pow(10, Math.Floor(Math.Log10(target)));
        double normalized = target / power;
        double nice = normalized < 2 ? 1 : normalized < 5 ? 2 : 5;
        return nice * power;
    }

    static Pen FrozenPen(string color, double thickness)
    {
        var pen = new Pen(FrozenBrush(color), thickness);
        pen.Freeze();
        return pen;
    }

    static Brush FrozenBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
