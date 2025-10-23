using System;
using Avalonia.Controls;

namespace BlazeUI;

public class PromotionHandler(Grid displayGrid)
{
    private readonly Border _border = (displayGrid.Children[0] as Border)!;
    private readonly Image _queenPromotion = GetPanel(displayGrid).FindControl<Image>("QueenPromotion")!;
    private readonly Image _rookPromotion = GetPanel(displayGrid).FindControl<Image>("RookPromotion")!;
    private readonly Image _knightPromotion = GetPanel(displayGrid).FindControl<Image>("KnightPromotion")!;
    private readonly Image _bishopPromotion = GetPanel(displayGrid).FindControl<Image>("BishopPromotion")!;

    public uint _selected = 0b111;
    
    public void RequestPromotion(int file)
    {
        Grid.SetColumn(_border, file);
        displayGrid.ZIndex = 5;
    }

    public void SendBack()
    {
        displayGrid.ZIndex = -5;
        _selected = 0b111;
    }

    public void InitImages(Blaze.Side side)
    {
        _queenPromotion.Source = GridBoard.GetPieceBitmap(0b100 | ((uint)side << 3));
        _rookPromotion.Source = GridBoard.GetPieceBitmap(0b001 | ((uint)side << 3));
        _knightPromotion.Source = GridBoard.GetPieceBitmap(0b010 | ((uint)side << 3));
        _bishopPromotion.Source = GridBoard.GetPieceBitmap(0b011 | ((uint)side << 3));
    }

    private static StackPanel GetPanel(Grid grid)
    {
        return ((grid.Children[0] as Border)!.Child as StackPanel)!;
    }
}