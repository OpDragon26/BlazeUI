using System;
using Avalonia.Threading;

namespace BlazeUI.Utils;
using Blaze.Board_Representation;
using Blaze.Move_Generation;
using Blaze.Utils;
using Board_Interface;
using BotAPI;

public static class MoveHandler
{
    public static void CallPieceMove(this BoardUI ui, (int x, int y) from, (int x, int y) to)
    {
        MoveHandlingResult result = GenerateMove(ui, from, to);
        
        if (result.Move is null)
            return;
        if (!result.RequiresPromotion)
            ui.HandlePlayerMove(result.Move);
        else
            ui.AskPromotion(result.Move.Destination.file, result.Move, () => ui.HandlePlayerMove(result.Move));
    }
    
    private static bool HandleMove(this BoardUI ui, Move move)
    {
        if (ui.Match is null)
            return false;

        if (ui.Match.TryMake(move))
        {
            ui.GridBoard.LoadBoard(move);
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
        ui.GridBoard.LoadBoard(move);
        ui.GridBoard.locked.Unlock(ui.PlayerSide);
    }

    private static void AskPromotion(this BoardUI ui, int file, Move move, Action finished)
    {
        if (ui.PromotionHandler is null)
            return;
        
        ui.PromotionHandler.Request(file);

        DispatcherTimer timer = new();
        timer.Tick += (_, _) =>
        {
            if (!ui.PromotionHandler.Active)
            {
                move.Promotion = ui.PromotionHandler.Selected;
                finished();
                timer.Stop();
            }
        };
        timer.Start();
    }
    
    private static MoveHandlingResult GenerateMove(this BoardUI ui, (int x, int y) from, (int x, int y) to)
    {
        (int file, int rank) source = Invert.Switch(from, ui.PlayerSide);
        (int file, int rank) destination = Invert.Switch(to, ui.PlayerSide);

        if (ui.Match is null)
            return new(false);

        Move move = new Move($"{MoveUtils.GetSquare(source)}{MoveUtils.GetSquare(destination)}", ui.Match.board);
        if (Pieces.TypeOf(ui.Match.board.GetPiece(source)) == Pieces.WhitePawn && (destination.rank is 0 or 7))
            return new(true, move);

        //Console.WriteLine($"{MoveUtils.GetSquare(source)}{MoveUtils.GetSquare(destination)}");
        return new(false, move);
    }

    private class MoveHandlingResult(bool promotion, Move? move = null)
    {
        public readonly bool RequiresPromotion = promotion;
        public readonly Move? Move = move;
    }
}