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
    private LineRenderer _lineRenderer;
    private string ImageFileName;

    public bool IsFirstPoint = true;
    public Point StartPoint;

    public MainWindow()
    {
        InitializeComponent();


        canvasViewbox.Width = canvas.Width = this.Width;
        canvasViewbox.Height = canvas.Height = this.Height;
        Globals.CanvasHeight = canvas.Height;
        Globals.CanvasWidth = canvas.Width;

        _lineRenderer = new LineRenderer(canvas);

        this.Opened += MainWindow_Opened;

        canvas.PointerMoved += Canvas_PointerMoved;
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
    }

    private void Vm_RequestRedo()
    {
        _lineRenderer.Redo();
        var vm = DataContext as MainWindowViewModel;
        vm.RedoEnabled = _lineRenderer.RedoEnabled();
        vm.UndoEnabled = vm.SaveEnabled = vm.SaveAsEnabled = _lineRenderer.UndoEnabled();
    }

    private async void Vm_RequestOpenFile()
    {
        if (_lineRenderer.GetLastItemIndex() > 0 && DataContext != null && (DataContext as MainWindowViewModel).SaveEnabled)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                "Open file",
                "Do you want to save the current file?",
                MessageBox.Avalonia.Enums.ButtonEnum.YesNo);
            var result = await messageBoxStandardWindow.ShowDialog(this);
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
                Stretch = Stretch.Uniform
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

            var result = await messageBoxStandardWindow.ShowDialog(this);
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

            var result = await messageBoxStandardWindow.ShowDialog(this);
            if (result == MessageBox.Avalonia.Enums.ButtonResult.Yes)
            {
                await SaveImage(false);
            }
        }
        Close();
    }
    private void Canvas_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (IsFirstPoint)
            {
                StartPoint = e.GetPosition(canvas);
                IsFirstPoint = false;
                _lineRenderer.SetStartPoint(StartPoint);
            }
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
    }

    private void Canvas_PointerMoved(object sender, PointerEventArgs e)
    {
        Point cusorPointOnCanvas = e.GetPosition(mainGrid);
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
