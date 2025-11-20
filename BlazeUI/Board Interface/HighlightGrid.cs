using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace BlazeUI.Board_Interface;
using Blaze.API;
using Blaze.Board_Representation;
using Blaze.Move_Generation;
using Blaze.Utils;
using Utils;

public class HighlightGrid : Grid
{
    public BoardUI? Base;

    public void HighlightCheck(Board board, Side side)
    {
        Clear("check");
        
        if (board.KingInCheck(0))
            HighlightSingle(Invert.Switch(board.KingPositions[0], side), "check", Colors.HighLightCheck);
        else if (board.KingInCheck(1))
            HighlightSingle(Invert.Switch(board.KingPositions[1], side), "check", Colors.HighLightCheck);
    }
    
    public void HighlightLegalMoves(Board board, Side side, (int x, int y) pos)
    {
        Clear("moves");
        if (board.GetOutcome() != Outcome.Ongoing)
            return;
        
        Highlight(
            MoveGenerator.SearchBoard(board, false)
            .ToArray()
            .Where(move => move.Source == Invert.Switch(pos, side))
            .Select(move => BitboardUtils.GetSquare(move.Destination))
            .Aggregate(0UL, (u1, u2) => u1 | u2),
            side, "moves",Colors.HighLight);
    }
    
    private void Highlight(ulong bitboard, Side perspective, string id, SolidColorBrush color)
    {
        for (int file = 0; file < 8; file++)
        for (int rank = 0; rank < 8; rank++)
        {
            (int file, int rank) square = (file, rank);
            (int x, int y) pos = Invert.Switch(square, perspective);

            if ((bitboard & BitboardUtils.GetSquare(square)) != 0)
                HighlightSingle(pos, id, color);
        }
    }
    
    public void HighlightMove(Move move)
    {
        Clear("last-move");

        Side side = Base?.PlayerSide ?? Side.White;
        HighlightSingle(Invert.Switch(move.Source, side), "last-move", Colors.HighLightMove);
        HighlightSingle(Invert.Switch(move.Destination, side), "last-move", Colors.HighLightMove);
    }
    
    private void HighlightSingle((int x, int y) pos, string id, SolidColorBrush color)
    {
        HighlightRect rect = new HighlightRect { [Shape.FillProperty] = color , ID = id };
        Children.Add(rect);
        SetColumn(rect, pos.x);
        SetRow(rect, pos.y);
    }

    public void Clear(string id)
    {
        Children.RemoveAll(Children.Where(child => (child as HighlightRect)!.ID.Equals(id)));
    }
}

file class HighlightRect : Rectangle
{
    public required string ID;
}