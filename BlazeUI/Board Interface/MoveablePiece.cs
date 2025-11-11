using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using BlazeUI.Utils;

namespace BlazeUI.Board_Interface;

public class MoveablePiece : Image
{
    public required BoardUI Base;
    private bool Pressed;
    private Point Position;
    private TranslateTransform? Translate;
    private (int X, int Y) Start;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Position = e.GetPosition(this.GetVisualParent());
        Point _relPosition = e.GetPosition(this);
        Position = new(Position.X - _relPosition.X + Bounds.Width / 2, Position.Y - _relPosition.Y + Bounds.Height / 2);
        Start = GetPositionOnGrid(Position);

        Console.WriteLine(Base.IsLocked(Start));
        if (Base.IsLocked(Start) || !e.Properties.IsLeftButtonPressed)
            return;
        
        Pressed = true;
        Base.PieceRaised(Start);
        
        ZIndex = 10;
        
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (Base.IsLocked(Start) || !e.Properties.IsLeftButtonPressed)
            return;
        
        ZIndex = 0;
        
        Pressed = false;
        SnapToGrid(e.GetPosition(this.GetVisualParent()));
        Base.PieceReleased();
        
        base.OnPointerReleased(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (Base.IsLocked(Start) || !e.Properties.IsLeftButtonPressed)
            return;
        
        if (Pressed)
        {
            Point pos = e.GetPosition(this.GetVisualParent());

            double offsetX = pos.X - Position.X;
            double offsetY = pos.Y - Position.Y;

            Translate = new TranslateTransform(offsetX, offsetY);
            RenderTransform = Translate;
        }

        base.OnPointerMoved(e);
    }

    private (int X, int Y) GetPositionOnGrid(Point position)
    {
        double squareSize = Base.GridBoard.Bounds.Width / 8;
        int x = (int)(position.X / squareSize);
        int y = (int)(position.Y / squareSize);
        return (x, y);
    }

    private void SnapToGrid(Point position)
    {
        (int X, int Y) pos = GetPositionOnGrid(position);
        
        Translate = null;
        RenderTransform = null;

        if (!InvalidSquare(pos))
        {
            //Console.WriteLine($"Moving from {_start} to {pos}");
            Base.CallPieceMove(Start, pos);
        }
    }

    private bool InvalidSquare((int x, int y) pos)
    {
        return !(pos.x is >= 0 and < 8 && pos.y is >= 0 and < 8);
    }

}