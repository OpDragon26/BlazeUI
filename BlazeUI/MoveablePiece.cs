using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace BlazeUI;

public class MoveablePiece : Image
{
    public required GridBoard PieceGrid;
    private bool _pressed;
    private Point _position;
    private TranslateTransform? _translate;
    private (int X, int Y) _start;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _pressed = true;
        _position = e.GetPosition(this.GetVisualParent());
        Point _relPosition = e.GetPosition(this);

        _position = new(_position.X - _relPosition.X + Bounds.Width / 2, _position.Y - _relPosition.Y + Bounds.Height / 2);
        _start = GetPositionOnGrid(_position);

        ZIndex = 10;
        
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ZIndex = 0;
        
        _pressed = false;
        SnapToGrid(e.GetPosition(this.GetVisualParent()));

        base.OnPointerReleased(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_pressed)
        {
            Point pos = e.GetPosition(this.GetVisualParent());

            double offsetX = pos.X - _position.X;
            double offsetY = pos.Y - _position.Y;

            _translate = new TranslateTransform(offsetX, offsetY);
            RenderTransform = _translate;
        }

        base.OnPointerMoved(e);
    }

    private (int X, int Y) GetPositionOnGrid(Point position)
    {
        double squareSize = PieceGrid.InnerGrid.Bounds.Width / 8;
        int x = (int)(position.X / squareSize);
        int y = (int)(position.Y / squareSize);
        return (x, y);
    }

    private void SnapToGrid(Point position)
    {
        (int X, int Y) pos = GetPositionOnGrid(position);
        
        _translate = null;
        RenderTransform = null;

        if (!InvalidSquare(pos))
        {
            //Console.WriteLine($"Moving from {_start} to {pos}");
            PieceGrid.MovePiece(_start, pos);
        }
    }

    private bool InvalidSquare((int x, int y) pos)
    {
        return !(pos.x is >= 0 and < 8 && pos.y is >= 0 and < 8);
    }

}