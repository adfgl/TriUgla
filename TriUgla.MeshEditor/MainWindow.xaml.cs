using System.Windows;
using System.Windows.Input;

namespace TriUgla.MeshEditor;

public partial class MainWindow : Window
{
    public static readonly RoutedCommand ResetViewCommand = new(nameof(ResetViewCommand), typeof(MainWindow));
    public static readonly RoutedCommand FitViewCommand = new(nameof(FitViewCommand), typeof(MainWindow));
    public static readonly RoutedCommand ZoomInCommand = new(nameof(ZoomInCommand), typeof(MainWindow));
    public static readonly RoutedCommand ZoomOutCommand = new(nameof(ZoomOutCommand), typeof(MainWindow));

    public MainWindow()
    {
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(ResetViewCommand, (_, _) => Viewport.ResetView()));
        CommandBindings.Add(new CommandBinding(FitViewCommand, (_, _) => Viewport.FitToMesh()));
        CommandBindings.Add(new CommandBinding(ZoomInCommand, (_, _) => Viewport.ZoomAtCenter(1.2)));
        CommandBindings.Add(new CommandBinding(ZoomOutCommand, (_, _) => Viewport.ZoomAtCenter(1 / 1.2)));
        Loaded += (_, _) => Viewport.FitToMesh();
    }

    void Viewport_OnViewChanged(object? sender, EventArgs e)
        => ZoomText.Text = $"{Viewport.Zoom * 100:0}%";

    void Viewport_OnMouseWorldPositionChanged(object? sender, Point world)
        => CoordinatesText.Text = $"X {world.X,7:0.00}   Y {world.Y,7:0.00}";
}
