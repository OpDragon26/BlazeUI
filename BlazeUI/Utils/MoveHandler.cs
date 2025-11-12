using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;
using BlazeUI.Blaze.Utils;
using BlazeUI.Board_Interface;
using BlazeUI.BotAPI;

namespace BlazeUI.Utils;

public static class MoveHandler
{
    public static void CallPieceMove(this BoardUI ui, (int x, int y) from, (int x, int y) to)
    {
        MoveHandlingResult result = GenerateMove(ui, from, to);
        
        if (result.Move is null)
            return;
        
        ui.HandlePlayerMove(result.Move);
    }
    
    private static bool HandleMove(this BoardUI ui, Move move)
    {
        if (ui.Match is null)
            return false;

        if (ui.Match.TryMake(move))
        {
            ui.GridBoard.LoadBoard();
            return true;
        }
        return false;
    }

    private static void HandlePlayerMove(this BoardUI ui, Move move)
    {
        if (ui.HandleMove(move))
            ui.PlayBotMove();
    }
    
    public static void HandleBotMove(this BoardUI ui, Move move)
    {
        ui.GridBoard.LoadBoard();
        ui.GridBoard.locked.Unlock(ui.PlayerSide);
    }

    private static MoveHandlingResult GenerateMove(this BoardUI ui, (int x, int y) from, (int x, int y) to)
    {
        (int file, int rank) source = Invert.Switch(from, ui.PlayerSide);
        (int file, int rank) destination = Invert.Switch(to, ui.PlayerSide);

        if (ui.Match is null)
            return new(false);

        if (Pieces.TypeOf(ui.Match.board.GetPiece(source)) == Pieces.WhitePawn && (destination.rank is 0 or 7))
            return new(true);

        //Console.WriteLine($"{MoveUtils.GetSquare(source)}{MoveUtils.GetSquare(destination)}");
        Move move = new Move($"{MoveUtils.GetSquare(source)}{MoveUtils.GetSquare(destination)}", ui.Match.board);
        return new(false, move);
    }

    private class MoveHandlingResult(bool promotion, Move? move = null)
    {
        public bool RequiresPromotion = promotion;
        public Move? Move = move;
    }
}