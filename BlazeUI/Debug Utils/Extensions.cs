using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;

namespace BlazeUI.Debug_Utils;

public static class Extensions
{
    public static void PlayMoves(this Board board, string[] moves)
    {
        foreach (var m in moves)
        {
            Move move = new Move(m, board);
            board.MakeMove(move);
        }
    }
}