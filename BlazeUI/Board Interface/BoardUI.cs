using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;
using BlazeUI.Utils;
using BlazeUI.BotAPI;

namespace BlazeUI.Board_Interface;

public class BoardUI : Grid
{
    public PromotionHandler? PromotionHandler;
    public EmbeddedMatch? Match;
    public Side PlayerSide;
    private bool IsPlayerTurn => Match is not null && Match.GetSide() == PlayerSide;
    
    public readonly PieceGrid GridBoard = new()
    {
        ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*"),
        RowDefinitions = new RowDefinitions("*,*,*,*,*,*,*,*"),
    };

    public readonly HighlightGrid Highlights = new()
    {
        ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*"),
        RowDefinitions = new RowDefinitions("*,*,*,*,*,*,*,*")
    };

    private readonly Grid BackgroundBoard = new()
    {
        ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*"),
        RowDefinitions = new RowDefinitions("*,*,*,*,*,*,*,*")
    };

    public void SetMatch(EmbeddedMatch? match, Side side)
    {
        Match = match;
        PlayerSide = side;
        
        if (Match is null)
            GridBoard.LoadBoard(new(Presets.StartingBoard), side, false);
        else
        {
            GridBoard.LoadBoard(Match.board, side, true);
            if (IsPlayerTurn)
                GridBoard.locked.Unlock(PlayerSide);
            else
                this.PlayBotMove();
        }
    }
    
    public void Initialize(PromotionHandler promotionHandler)
    {
        GridBoard.Base = this;
        Highlights.Base = this;
        
        PromotionHandler = promotionHandler;
        
        Children.Add(BackgroundBoard);
        Children.Add(Highlights);
        Children.Add(GridBoard);
        
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
        
        SetMatch(null, Side.White);
    }

    public void PieceRaised((int x, int y) pos)
    {
        Highlights.HighlightLegalMoves(Match!.board, PlayerSide, pos);
    }

    public void PieceReleased()
    {
        Highlights.Clear("moves");
    }
}