using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamSketch;
using TeamSketch.Models;
using TeamSketch.Services;
using TeamSketch.Views;

namespace ProjectionMapper.Services
{
    public class DrawLineAction
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public DrawLineAction(Point startPoint, Point endPoint)
        {
            StartPoint= startPoint;
            EndPoint= endPoint;
        }
    }

    public class LineRenderer
    {
        private readonly Canvas _canvas;
        private readonly DropOutStack<DrawLineAction> _undoStack = new(100);
        private readonly DropOutStack<DrawLineAction> _redoStack = new(100);

        public double Thickness { get; private set; }
        public double HalfThickness { get; private set; }

        public double MaxBrushPointX { get; private set; }
        public double MaxBrushPointY { get; private set; }
        public double MinBrushPoint { get; private set; }

        private const double lineThickness = 4;
        private Point StartPoint { get; set; }

        public LineRenderer(Canvas canvas)
        {
            _canvas = canvas;

            Thickness = 4;
            HalfThickness = Thickness / 2;

            MaxBrushPointX = Globals.CanvasWidth - HalfThickness;
            MaxBrushPointY = Globals.CanvasHeight - HalfThickness;
            MinBrushPoint = HalfThickness;
        }
        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var lastAction = _undoStack.Pop();
                _redoStack.Push(lastAction);
                _canvas.Children.RemoveAt(_canvas.Children.Count - 1);
            }
        }
        public RangeAction Redo()
        {
            if (_redoStack.Count > 0 )
            {
                RemoveTempLine();

                var lastUndoAction = _redoStack.Pop();
                DrawLine(lastUndoAction.StartPoint, lastUndoAction.EndPoint);
            }

            return null;
        }
        public bool UndoEnabled()
        {
            return _undoStack.Count > 0;
        }
        public bool RedoEnabled()
        {
            return _redoStack.Count > 0;
        }
        public void ClearRedoStack()
        {

        }
        public void ResetCanvas()
        {
            if (_canvas != null)
            {
                _canvas.Children.Clear();
                _canvas.Background = Brushes.White;
            }
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public int GetLastItemIndex()
        {
            return _canvas.Children.Count - 1;
        }

        internal void SetStartPoint(Point startPoint)
        {
            StartPoint = startPoint;
        }

        internal void SetEndPoint(Point endPoint)
        {
            if (endPoint != StartPoint)
            {
                RemoveTempLine();

                DrawLine(StartPoint, endPoint);
                _redoStack.Clear();
            }
        }

        private void DrawLine(Point startPoint, Point endPoint)
        {
            Line line = new()
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Stroke = Brushes.Black,
                StrokeThickness = lineThickness
            };
            _canvas.Children.Add(line);
            _undoStack.Push(new DrawLineAction(startPoint, endPoint));
        }

        internal void SetTempEndPoint(Point tempEndPoint)
        {
            RemoveTempLine();

            Line line = new()
            {
                Tag = "tempLine",
                StartPoint = StartPoint,
                EndPoint = tempEndPoint,
                Stroke = Brushes.Black,
                StrokeThickness = lineThickness,
            };
            _canvas.Children.Add(line);
        }

        private void RemoveTempLine()
        {
            if (_canvas.Children.Count > 0)
            {
                var child = (from c in _canvas.Children.OfType<Control>()
                             where "tempLine".Equals(c.Tag)
                             select c).FirstOrDefault();
                if (child != null)
                {
                    _canvas.Children.Remove(child);
                }
            }
        }

        public Point RestrictPointToCanvas(double x, double y)
        {
            if (x > MaxBrushPointX)
            {
                x = MaxBrushPointX;
            }
            else if (x < MinBrushPoint)
            {
                x = MinBrushPoint;
            }

            if (y > MaxBrushPointY)
            {
                y = MaxBrushPointY;
            }
            else if (y < MinBrushPoint)
            {
                y = MinBrushPoint;
            }

            return new Point(x, y);
        }
    }
}
