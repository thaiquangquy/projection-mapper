using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using TeamSketch.Models;
using TeamSketch.Views;

namespace TeamSketch.Services;

public class RedererDrawAction
{
    public List<Ellipse> pointList;
    public List<Path> pathList;

    public RedererDrawAction()
    {
        pointList = new List<Ellipse>();
        pathList = new List<Path>();
    }
}

public interface IRenderer
{
    void DrawPoint(double x, double y);
    void RemovePoint(double x, double y);
    void EnqueueLineSegment(Point point1, Point point2);

    /// <summary>
    /// Render line from locally batched points.
    /// </summary>
    /// <returns>The points making up the line that was rendered.</returns>
    List<Point> RenderLine();

    /// <summary>
    /// Render line from the input points.
    /// </summary>
    /// <param name="linePointsQueue"></param>
    /// <param name="thickness"></param>
    /// <param name="colorBrush"></param>
    void RenderLine(Queue<Point> linePointsQueue, double thickness, SolidColorBrush colorBrush);

    /// <summary>
    /// Remove line from the input points.
    /// </summary>
    /// <param name="linePointsQueue"></param>
    /// <param name="thickness"></param>
    /// <param name="colorBrush"></param>
    void RemoveLine(Queue<Point> linePointsQueue, double thickness, SolidColorBrush colorBrush);

    Point RestrictPointToCanvas(double x, double y);

    void Undo(int startIndex, int endIndex);
    RangeAction Redo();
    bool RedoEnabled();
    void ClearRedoStack();
    void ResetCanvas();

    int GetLastItemIndex();
}

public class Renderer : IRenderer
{
    private readonly BrushSettings _brushSettings;
    private readonly Canvas _canvas;
    private readonly Queue<Point> _linePointsQueue = new();
    private readonly Stack<RedererDrawAction> _undoStack = new();
    private readonly Stack<IEnumerable<IControl>> _redoStack = new();
    private readonly DropOutStack<IEnumerable<IControl>> _redoDropOutStack = new(50);

    public Renderer(BrushSettings brushSettings, Canvas canvas)
    {
        _brushSettings = brushSettings;
        _canvas = canvas;
    }

    public void ResetCanvas()
    {
        if (_canvas != null)
        {
            _canvas.Children.Clear();
            _canvas.Background = Brushes.White;
        }
        _undoStack.Clear();
    }

    public void Undo(int startIndex, int endIndex)
    {
        if (_canvas != null)
        {
            var redoAction = _canvas.Children.GetRange(startIndex, endIndex - startIndex);
            _redoDropOutStack.Push(redoAction);
            _canvas.Children.RemoveRange(startIndex, endIndex - startIndex);
        }
    }

    public RangeAction Redo()
    {
        RangeAction rangeAction = new();
        rangeAction.startIndex = rangeAction.endIndex = 0;

        if (_redoDropOutStack.Count > 0)
        {
            rangeAction.startIndex = GetLastItemIndex();
            var redoAction = _redoDropOutStack.Pop();
            _canvas.Children.AddRange(redoAction);

            rangeAction.endIndex = rangeAction.startIndex + redoAction.Count();
        }
        return rangeAction;
    }

    public bool RedoEnabled()
    {
        return _redoDropOutStack.Count > 0;
    }

    public void ClearRedoStack()
    {
        _redoDropOutStack.Clear();
    }

    public int GetLastItemIndex()
    {
        if (_canvas != null)
        {
            return _canvas.Children.Count;
        }

        return -1;
    }

    public void DrawPoint(double x, double y)
    {
        var ellipse = new Ellipse
        {
            Margin = new Thickness(x - _brushSettings.HalfThickness, y - _brushSettings.HalfThickness, 0, 0),
            Fill = _brushSettings.ColorBrush,
            Width = _brushSettings.Thickness,
            Height = _brushSettings.Thickness
        };
        _canvas.Children.Add(ellipse);
    }

    public void RemovePoint(double x, double y)
    {
        var ellipse = new Ellipse
        {
            Margin = new Thickness(x - _brushSettings.HalfThickness, y - _brushSettings.HalfThickness, 0, 0),
            Fill = _brushSettings.ColorBrush,
            Width = _brushSettings.Thickness,
            Height = _brushSettings.Thickness
        };
        _canvas.Children.Remove(ellipse);
    }

    public void EnqueueLineSegment(Point point1, Point point2)
    {
        _linePointsQueue.Enqueue(point1);
        _linePointsQueue.Enqueue(point2);
    }
    
    public List<Point> RenderLine()
    {
        if (_linePointsQueue.Count == 0)
        {
            return new List<Point>();
        }

        var myPointCollection = new Points();

        var result = _linePointsQueue.ToList();
        var firstPoint = _linePointsQueue.Dequeue();

        while (_linePointsQueue.Count > 0)
        {
            var point = _linePointsQueue.Dequeue();
            myPointCollection.Add(point);
        }

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure
        {
            Segments = new PathSegments
            {
                new PolyLineSegment
                {
                    Points = myPointCollection
                }
            },
            StartPoint = firstPoint,
            IsClosed = false
        };
        pathGeometry.Figures.Add(pathFigure);

        var path = new Path
        {
            Stroke = _brushSettings.ColorBrush,
            StrokeThickness = _brushSettings.Thickness,
            Data = pathGeometry
        };
        _canvas.Children.Add(path);

        var ellipse = new Ellipse
        {
            Margin = new Thickness(firstPoint.X - _brushSettings.HalfThickness, firstPoint.Y - _brushSettings.HalfThickness, 0, 0),
            Fill = _brushSettings.ColorBrush,
            Width = _brushSettings.Thickness,
            Height = _brushSettings.Thickness
        };
        _canvas.Children.Add(ellipse);

        return result;
    }

    public void RenderLine(Queue<Point> linePointsQueue, double thickness, SolidColorBrush colorBrush)
    {
        if (linePointsQueue.Count == 0)
        {
            return;
        }

        var myPointCollection = new Points();

        var firstPoint = linePointsQueue.Dequeue();

        while (linePointsQueue.Count > 0)
        {
            var point = linePointsQueue.Dequeue();
            myPointCollection.Add(point);
        }

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure
        {
            Segments = new PathSegments
            {
                new PolyLineSegment
                {
                    Points = myPointCollection
                }
            },
            StartPoint = firstPoint,
            IsClosed = false
        };
        pathGeometry.Figures.Add(pathFigure);

        var path = new Path
        {
            Stroke = colorBrush,
            StrokeThickness = thickness,
            Data = pathGeometry
        };
        _canvas.Children.Add(path);

        var ellipse = new Ellipse
        {
            Margin = new Thickness(firstPoint.X - thickness / 2, firstPoint.Y - thickness / 2, 0, 0),
            Fill = colorBrush,
            Width = thickness,
            Height = thickness
        };
        _canvas.Children.Add(ellipse);
    }

    public void RemoveLine(Queue<Point> linePointsQueue, double thickness, SolidColorBrush colorBrush)
    {
        if (linePointsQueue.Count == 0)
        {
            return;
        }

        var myPointCollection = new Points();

        var firstPoint = linePointsQueue.Dequeue();

        while (linePointsQueue.Count > 0)
        {
            var point = linePointsQueue.Dequeue();
            myPointCollection.Add(point);
        }

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure
        {
            Segments = new PathSegments
            {
                new PolyLineSegment
                {
                    Points = myPointCollection
                }
            },
            StartPoint = firstPoint,
            IsClosed = false
        };
        pathGeometry.Figures.Add(pathFigure);

        var path = new Path
        {
            Stroke = colorBrush,
            StrokeThickness = thickness,
            Data = pathGeometry
        };
        _canvas.Children.Remove(path);

        var ellipse = new Ellipse
        {
            Margin = new Thickness(firstPoint.X - thickness / 2, firstPoint.Y - thickness / 2, 0, 0),
            Fill = colorBrush,
            Width = thickness,
            Height = thickness
        };
        _canvas.Children.Remove(ellipse);
    }

    public Point RestrictPointToCanvas(double x, double y)
    {
        if (x > _brushSettings.MaxBrushPointX)
        {
            x = _brushSettings.MaxBrushPointX;
        }
        else if (x < _brushSettings.MinBrushPoint)
        {
            x = _brushSettings.MinBrushPoint;
        }

        if (y > _brushSettings.MaxBrushPointY)
        {
            y = _brushSettings.MaxBrushPointY;
        }
        else if (y < _brushSettings.MinBrushPoint)
        {
            y = _brushSettings.MinBrushPoint;
        }

        return new Point(x, y);
    }
}
