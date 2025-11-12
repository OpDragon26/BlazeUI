using System.Linq;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Board_Interface;

namespace BlazeUI.Utils;

public static class ComponentCommunicationHandler
{
    public static Side GetSide(this EmbeddedMatch match)
    {
        return (Side)match.board.side;
    }
    
    public static bool IsLocked(this BoardUI board, (int x, int y) objPos)
    {
        return board.GridBoard.IsLocked(objPos);
    }
    
    private static bool IsLocked(this PieceGrid board, (int x, int y) objPos)
    {
        if (board.PieceList.Any(piece => piece.pos == objPos))
            return board.locked.IsLocked(board.PieceList.Find(piece => piece.pos == objPos)!.side);
        
        return true;
    }
}