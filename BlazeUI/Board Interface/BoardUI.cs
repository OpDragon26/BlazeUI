using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Board_Representation;

namespace BlazeUI.Board_Interface;

public class BoardUI : Grid
{
    private PromotionHandler? PromotionHandler;
    private EmbeddedMatch? Match;
    public Side PlayerSide;
    
    public readonly PieceGrid GridBoard = new PieceGrid
    {
        ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*"),
        RowDefinitions = new RowDefinitions("*,*,*,*,*,*,*,*"),
    };

    private readonly Grid Highlights = new Grid
    {
        ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*"),
        RowDefinitions = new RowDefinitions("*,*,*,*,*,*,*,*")
    };

    private readonly Grid BackgroundBoard = new Grid
    {
        ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*"),
        RowDefinitions = new RowDefinitions("*,*,*,*,*,*,*,*")
    };

    public void CallPieceMove((int x, int y) from, (int x, int y) to)
    {
        
    }

    public void SetMatch(EmbeddedMatch? match, Side side)
    {
        Match = match;
        PlayerSide = side;
        
        if (match is null)
            GridBoard.LoadBoard(new(Presets.StartingBoard), side, false);
        else
            GridBoard.LoadBoard(match.board, side, true);
    }
    
    public void Initialize(PromotionHandler promotionHandler)
    {
        GridBoard.Base = this;
        
        PromotionHandler = promotionHandler;
        
        Children.Add(GridBoard);
        Children.Add(Highlights);
        Children.Add(BackgroundBoard);
        
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                Rectangle rect = new Rectangle { [Shape.FillProperty] = (file + rank) % 2 == 0 ? Colors.LightSquare : Colors.DarkSquare };
                SetRow(rect, file);
                SetColumn(rect, rank);
                BackgroundBoard.Children.Add(rect);
            }
        }
    }

    public void PieceRaised((int x, int y) pos)
    {
        
    }

    public void PieceReleased()
    {
        
    }
}