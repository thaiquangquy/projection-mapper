using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using ProjectionMapper.Services;
using SkiaSharp;
using Splat;
using TeamSketch.DependencyInjection;
using TeamSketch.Models;
using TeamSketch.Services;
using TeamSketch.Utils;
using TeamSketch.ViewModels;

namespace TeamSketch.Views;
public class RangeAction
{
    public int startIndex = 0;
    public int endIndex = 0;
}

public partial class MainWindow : Window
{
    private readonly IAppState _appState;
    private readonly IRenderer _renderer;
    private readonly LineRenderer _lineRenderer;
    private readonly DispatcherTimer _lineRenderingTimer = new();
    private Point currentPoint = new();
    private bool pressed;

    private Stack<RangeAction> undoRangeActionsStack;
    private Stack<RangeAction> redoRangeActionsStack;
    private RangeAction currentRangeDrawAction;
    private string ImageFileName;

    public bool IsFirstPoint = true;
    public Point StartPoint;

    public MainWindow()
    {
        InitializeComponent();

        canvas.Width = this.Width;
        canvas.Height = this.Height;
        Globals.CanvasHeight = canvas.Height;
        Globals.CanvasWidth = canvas.Width;

        _appState = Locator.Current.GetRequiredService<IAppState>();
        _renderer = new Renderer(_appState.BrushSettings, canvas);
        _lineRenderer = new LineRenderer(canvas);

        _lineRenderingTimer.Tick += LineRenderingTimer_Tick;
        _lineRenderingTimer.Interval = TimeSpan.FromMilliseconds(Globals.RenderingIntervalMs);
        _lineRenderingTimer.Start();

        _appState.BrushSettings.BrushChanged += BrushSettings_BrushChanged;
        this.Opened += MainWindow_Opened;

        canvas.PointerMoved += Canvas_PointerMoved;

        undoRangeActionsStack = new Stack<RangeAction> { };
        redoRangeActionsStack = new Stack<RangeAction> { };
    }

    private void MainWindow_Opened(object sender, EventArgs e)
    {
        PropertiesSaveContextMenu.Open(null);
        PropertiesSaveContextMenu.Close();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        var vm = DataContext as MainWindowViewModel;
        vm.RequestClose += Vm_RequestClose;
        vm.RequestSave += Vm_RequestSave;
        vm.RequestNewFile += Vm_RequestNewFile;
        vm.RequestOpenFile += Vm_RequestOpenFile;
        vm.RequestRedo += Vm_RequestRedo;
        vm.RequestUndo += Vm_RequestUndo;

        base.OnDataContextChanged(e);
    }

    private void Vm_RequestUndo()
    {
        _lineRenderer.Undo();

        var vm = DataContext as MainWindowViewModel;
        vm.RedoEnabled = _lineRenderer.RedoEnabled();
        vm.UndoEnabled = vm.SaveEnabled = vm.SaveAsEnabled = _lineRenderer.UndoEnabled();
        return;

        if (undoRangeActionsStack.Count > 0)
        {
            RangeAction rangeAction= undoRangeActionsStack.Pop();
            _renderer.Undo(rangeAction.startIndex, rangeAction.endIndex);

            vm.RedoEnabled = true;
            if (undoRangeActionsStack.Count == 0)
            {
                vm.UndoEnabled = false;
                vm.SaveEnabled = false;
                vm.SaveAsEnabled = false;
            }
        }
    }

    private void Vm_RequestRedo()
    {
        _lineRenderer.Redo();
        var vm = DataContext as MainWindowViewModel;
        vm.RedoEnabled = _lineRenderer.RedoEnabled();
        vm.UndoEnabled = vm.SaveEnabled = vm.SaveAsEnabled = _lineRenderer.UndoEnabled();
        return;

        RangeAction rangeAction = _renderer.Redo();
        if (rangeAction.endIndex != 0 && rangeAction.startIndex != 0)
        {
            undoRangeActionsStack.Push(rangeAction);
        }
        vm.RedoEnabled = _renderer.RedoEnabled();
    }

    private async void Vm_RequestOpenFile()
    {
        if (_lineRenderer.GetLastItemIndex() > 0 && DataContext != null && (DataContext as MainWindowViewModel).SaveEnabled)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                "Open file",
                "Do you want to save the current file?",
                MessageBox.Avalonia.Enums.ButtonEnum.YesNo);
            var result = await messageBoxStandardWindow.Show();
            if (result == MessageBox.Avalonia.Enums.ButtonResult.Yes)
            {
                await SaveImage(false);
            }
        }

        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filters.Add(new FileDialogFilter());

        openFileDialog.Title = "Open Image As...";
        openFileDialog.Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        openFileDialog.AllowMultiple = false;
        List<FileDialogFilter> Filters = new List<FileDialogFilter>();
        FileDialogFilter filter = new FileDialogFilter();
        List<string> extension = new List<string>();
        extension.Add("png");
        filter.Extensions = extension;
        filter.Name = "Image Files";
        Filters.Add(filter);
        openFileDialog.Filters = Filters;

        string[] imageFileNames = await openFileDialog.ShowAsync(this);
        if (imageFileNames != null)
        {
            Reset();

            string imageFileName = imageFileNames[0];
            Bitmap bitmap = new(imageFileName);
            var imgBrush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill
            };
            canvas.Background = imgBrush;
        }

        if (PropertiesSaveContextMenu.IsOpen)
        {
            PropertiesSaveContextMenu.Close();
        }
    }

    private async void Vm_RequestNewFile()
    {
        if (_lineRenderer.GetLastItemIndex() > 0 && DataContext != null && (DataContext as MainWindowViewModel).SaveEnabled)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                "Creating new file",
                "Do you want to save the current file?",
                MessageBox.Avalonia.Enums.ButtonEnum.YesNo);

            var result = await messageBoxStandardWindow.Show();
            if (result == MessageBox.Avalonia.Enums.ButtonResult.Yes)
            {
                await SaveImage(false);
            }
        }
        Reset();
    }

    private void Reset()
    {
        _lineRenderer.ResetCanvas();
        ImageFileName = string.Empty;
    }

    private async void Vm_RequestSave(bool saveAs)
    {
        await SaveImage(saveAs);
    }

    private async Task SaveImage(bool saveAs)
    {
        if (string.IsNullOrEmpty(ImageFileName) || saveAs)
        {
            SaveFileDialog SaveFileBox = new()
            {
                Title = "Save Image As...",
                Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                InitialFileName = ""
            };
            List<FileDialogFilter> Filters = new();
            FileDialogFilter filter = new FileDialogFilter();
            List<string> extension = new();
            extension.Add("png");
            filter.Extensions = extension;
            filter.Name = "Image Files";
            Filters.Add(filter);
            SaveFileBox.Filters = Filters;

            SaveFileBox.DefaultExtension = "png";
            ImageFileName = await SaveFileBox.ShowAsync(this);
        }

        if (ImageFileName != null)
        {
            //if (File.Exists(ImageFileName))
            //{
            //    File.Delete(ImageFileName);
            //}

            RenderTargetBitmap rtb = new(PixelSize.FromSizeWithDpi(new Size(canvas.Width, canvas.Height), 96));
            rtb.Render(canvas);
            rtb.Save(ImageFileName);

            var vm = DataContext as MainWindowViewModel;
            vm.SaveEnabled = false;
        }
    }

    private async void Vm_RequestClose()
    {
        if (_lineRenderer.GetLastItemIndex() > 0 && DataContext != null && (DataContext as MainWindowViewModel).SaveEnabled)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                "Creating new file",
                "Do you want to save the current file?",
                MessageBox.Avalonia.Enums.ButtonEnum.YesNo);

            var result = await messageBoxStandardWindow.Show();
            if (result == MessageBox.Avalonia.Enums.ButtonResult.Yes)
            {
                await SaveImage(false);
            }
        }
        Close();
    }

    private void LineRenderingTimer_Tick(object sender, EventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            //var points = _renderer.RenderLine();
            //if (!points.Any())
            //{
            //    if (pressed == false && currentRangeDrawAction != null)
            //    {
            //        undoRangeActionsStack.Push(currentRangeDrawAction);
            //        currentRangeDrawAction = null;
            //        _renderer.ClearRedoStack();
            //        var vm = DataContext as MainWindowViewModel;
            //        vm.UndoEnabled = true;
            //        vm.SaveEnabled= true;
            //        vm.SaveAsEnabled= true;
            //        vm.RedoEnabled = false;
            //    }
            //    return;
            //}

            //if (currentRangeDrawAction!= null)
            //{
            //    currentRangeDrawAction.endIndex = Math.Max(currentRangeDrawAction.endIndex, _renderer.GetLastItemIndex());
            //}
        });
    }

    private void BrushSettings_BrushChanged(object sender, BrushChangedEventArgs e)
    {
        canvas.Cursor = e.Cursor;
    }

    private void Canvas_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            //currentPoint = e.GetPosition(canvas);
            //pressed = true;

            //currentRangeDrawAction = new RangeAction();
            //currentRangeDrawAction.startIndex = _renderer.GetLastItemIndex();

            if (IsFirstPoint)
            {
                StartPoint = e.GetPosition(canvas);
                IsFirstPoint = false;
                _lineRenderer.SetStartPoint(StartPoint);
            }
            //else
            //{
            //    Line line = new Line() { StartPoint = StartPoint, EndPoint = e.GetPosition(canvas), Stroke = Brushes.Black, StrokeThickness = 2 };
            //    canvas.Children.Add(line);
            //    IsFirstPoint = true;
            //}
        }
        else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {

        }
    }

    private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            Point newPosition = e.GetPosition(canvas);
            var endPoint = _lineRenderer.RestrictPointToCanvas(newPosition.X, newPosition.Y);
            _lineRenderer.SetEndPoint(endPoint);
            IsFirstPoint = true;

            var vm = DataContext as MainWindowViewModel;
            vm.UndoEnabled = true;
            vm.SaveEnabled = true;
            vm.SaveAsEnabled = true;
            vm.RedoEnabled = false;
        }

        //pressed = false;

        //var newPoint = _renderer.RestrictPointToCanvas(currentPoint.X, currentPoint.Y);
        //_renderer.DrawPoint(newPoint.X, newPoint.Y);

        //if (currentRangeDrawAction != null)
        //{
        //    currentRangeDrawAction.endIndex = _renderer.GetLastItemIndex();
        //}
    }

    private void Canvas_PointerMoved(object sender, PointerEventArgs e)
    {
        //Point cusorPointOnCanvas = _renderer.RestrictPointToCanvas(e.GetPosition(canvas).X, e.GetPosition(canvas).Y);

        Point cusorPointOnCanvas = e.GetPosition(canvasGrid);
        VerticalLine.StartPoint = new Point(cusorPointOnCanvas.X, 0);
        VerticalLine.EndPoint = new Point(cusorPointOnCanvas.X, Height);
        HorizontalLine.StartPoint = new Point(0, cusorPointOnCanvas.Y);
        HorizontalLine.EndPoint = new Point(Width, cusorPointOnCanvas.Y);

        if (!IsFirstPoint)
        {
            Point newPosition = e.GetPosition(canvas);
            var tempEndPoint = _lineRenderer.RestrictPointToCanvas(newPosition.X, newPosition.Y);

            _lineRenderer.SetTempEndPoint(tempEndPoint);
        }

        return;
    }

    protected override void OnInitialized()
    {
        var screen = this.Screens.ScreenFromPoint(this.Position);
        this.Width = screen.Bounds.Width;
        this.Height = screen.Bounds.Height;
        this.Position = new PixelPoint(0, 0);
        base.OnInitialized();
    }
}
