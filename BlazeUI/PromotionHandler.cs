using Avalonia.Controls;
using BlazeUI.Blaze.API;
using BlazeUI.Utils;

namespace BlazeUI;

public class PromotionHandler(Grid displayGrid)
{
    public bool Active;
    
    private readonly Border _border = (displayGrid.Children[0] as Border)!;
    private readonly Image _queenPromotion = GetPanel(displayGrid).FindControl<Image>("QueenPromotion")!;
    private readonly Image _rookPromotion = GetPanel(displayGrid).FindControl<Image>("RookPromotion")!;
    private readonly Image _knightPromotion = GetPanel(displayGrid).FindControl<Image>("KnightPromotion")!;
    private readonly Image _bishopPromotion = GetPanel(displayGrid).FindControl<Image>("BishopPromotion")!;

    public uint Selected = 0b111;
    
    public void Request(int file)
    {
        Active = true;
        Grid.SetColumn(_border, file);
        displayGrid.ZIndex = 5;
    }

    public void Cancel()
    {
        Active = false;
        displayGrid.ZIndex = -5;
    }
    
    public void InitImages(Side side)
    {
        _queenPromotion.Source = BoardUIUtils.GetPieceBitmap(0b100 | ((uint)side << 3));
        _rookPromotion.Source = BoardUIUtils.GetPieceBitmap(0b001 | ((uint)side << 3));
        _knightPromotion.Source = BoardUIUtils.GetPieceBitmap(0b010 | ((uint)side << 3));
        _bishopPromotion.Source = BoardUIUtils.GetPieceBitmap(0b011 | ((uint)side << 3));
    }

    private static StackPanel GetPanel(Grid grid)
    {
        return ((grid.Children[0] as Border)!.Child as StackPanel)!;
    }
}