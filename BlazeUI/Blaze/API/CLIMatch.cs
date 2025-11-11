using System;

namespace BlazeUI.Blaze.API;
using Board_Representation;
using Move_Generation;
using Utils;
using static Utils.CLIUtils;

public enum Type
{
    Random,
    Analysis,
    Standard,
    Self,
    Autoplay,
}

public enum Side
{
    White,
    Black
}

public class CLIMatch(Board board, Type type, Side side, int depth = 2, bool debug = false, bool clear = true, int moveLimit = -1, bool dynamicDepth = true, bool useBook = true) : Match(board, depth, dynamicDepth, useBook)
{
    private static readonly Random random = new();
    private readonly int side = (int)side;

    public void Play()
    {
        ply = 0;

        while (true)
        {
            // game ended or reached move limit
            if (GameEnded() || (moveLimit != -1 && ply >= moveLimit))
            {
                FinishGame(GetOutcome());
                break;
            }
            
            PrintState();
            
            switch (type)
            {
                case Type.Analysis:
                    PlayerTurn();
                    break;
                case Type.Standard:
                    if (side == board.side)
                        PlayerTurn();
                    else
                        BotMove();
                    break;
                case Type.Autoplay:
                    BotMove();
                    break;
                case Type.Self:
                    BotMove();
                    Console.ReadKey();
                    break;
                case Type.Random:
                    Move[] moves = MoveGenerator.SearchBoard(board, false).ToArray();
                    TryMake(PickRandom(moves), 0);
                    break;
            }
        }
    }

    private void PlayerTurn()
    {
        General.Timer t = new();
        t.Start();
        while (true)
        {
            Console.Write("Enter your move: ");
            string? input = Console.ReadLine();
            if (input == null) continue;

            try
            {
                Move move = AlgebraicNotation.ParseMove(input, board);
                if (TryMake(move, t.Stop())) // successfully made move
                    break;
                
                PrintState("Illegal move");
            }
            catch
            {
                PrintState("Failed to parse notation");
            }
        }
    }

    private void FinishGame(Outcome outcome)
    {
        if (clear) 
            Console.Clear();
        
        Print(side);

        Console.WriteLine(outcome switch
        {
            Outcome.Draw => $"Game drawn on move {ply / 2}",
            Outcome.WhiteWin => $"White won on move {ply / 2}",
            Outcome.BlackWin => $"Black won on move {ply / 2}",
            Outcome.Ongoing => $"Game reached specified limit at ply {ply}",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        });
        
        Console.WriteLine("Full game:");
        Console.WriteLine(game.GetPGN());
    }

    private void PrintState(string? insert = null)
    {
        if (clear) 
            Console.Clear();
        if (insert is not null) 
            Console.WriteLine(insert);
        Console.WriteLine($"Move {ply / 2} - {(side == 0 ? "white" : "black")} to move");
        Console.WriteLine($"Last move: {game.LastMove?.Notate()}");
        if (debug)
            PrintDebugInfo();
        Print(side);
    }

    private void PrintDebugInfo()
    {
        Console.WriteLine(game[^1].time >= 0 ? $"Move made in {game[^1].time}ms" : "");
        Console.WriteLine($"Depth {depth}");
    }

    private Move PickRandom(Move[] moves)
    {
        return moves[random.Next(moves.Length)];
    }

    private void Print(int perspective)
    {
        PrintBoard(board, perspective, board.GetImbalance());
    }
}