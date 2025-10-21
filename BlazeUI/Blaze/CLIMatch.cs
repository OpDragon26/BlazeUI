using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BlazeUI.Blaze;

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

public class CLIMatch : Match
{
    private static readonly Random random = new();
    private static readonly bool WindowsMode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private readonly bool debug;
    private readonly Type type;
    private readonly int side;
    private readonly int moveLimit;
    private readonly bool clear;

    public CLIMatch(Board board, Type type, Side side, int depth = 2, bool debug = false, bool clear = true, int moveLimit = -1, bool dynamicDepth = true, bool useBook = true) : base(board, depth, dynamicDepth, useBook)
    {
        game = new();
    
        this.debug = debug;
        this.type = type;
        this.side = (int)side;
        this.moveLimit = moveLimit;
        this.clear = clear;
    }

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
                    Move[] moves = Search.SearchBoard(board, false).ToArray();
                    TryMake(PickRandom(moves), out var node, 0);
                    break;
            }
        }
    }

    private void PlayerTurn()
    {
        Timer t = new();
        t.Start();
        while (true)
        {
            Console.Write("Enter your move: ");
            string? input = Console.ReadLine();
            if (input == null) continue;

            try
            {
                Move move = Move.Parse(input, board);
                if (TryMake(move, out var node, t.Stop())) // successfully made move
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
        Console.WriteLine(GetPGN());
    }

    private void PrintState(string? insert = null)
    {
        if (clear) 
            Console.Clear();
        if (insert is not null) 
            Console.WriteLine(insert);
        Console.WriteLine($"Move {ply / 2} - {(side == 0 ? "white" : "black")} to move");
        Console.WriteLine($"Last move: {LastMove()}");
        if (debug)
            PrintDebugInfo();
        Print(side);
    }

    private string LastMove()
    {
        if (game.Count == 0)
            return "";
        if (game.Count == 1)
            return game[0].move.Notate(new Board(Presets.StartingBoard));
        return game[^1].move.Notate(game[^2].board);
    }

    private void PrintDebugInfo()
    {
        Console.WriteLine(game.Count > 0 ? $"Move made in {game[^1].time}ms" : "");
        Console.WriteLine($"Depth {depth}");
    }

    private Move PickRandom(Move[] moves)
    {
        return moves[random.Next(moves.Length)];
    }

    public string GetUCI()
    {
        string[] pgn = new string[game.Count];

        for (int i = 0; i < pgn.Length; i++)
        {
            pgn[i] = game[i].move.GetUCI();
        }
        
        return string.Join(' ', pgn);
    }

    private string GetPGN()
    {
        return GetPGN(game);
    }

    public static string GetPGN(List<PGNNode> game)
    {
        string[] pgn = new string[game.Count];

        pgn[0] = "1. " + game[0].move.Notate(new Board(Presets.StartingBoard));
        for (int i = 1; i < pgn.Length; i++)
        {
            string num = i % 2 == 0 ? $"{i / 2 + 1}. " : "";
            
            pgn[i] = num + game[i].move.Notate(game[i-1].board);
        }
        
        return string.Join(' ', pgn);
    }

    public PGNNode[] GetNodes()
    {
        return game.ToArray();
    }

    public static void PrintBoard(Board board, int perspective = 0, int imbalance = 0)
    {
        PrintBoard(board, perspective, WindowsMode ? IHateWindows : PieceStrings, imbalance);
    }
    
    private static void PrintBoard(Board board, int perspective, string[] pieceStrings, int imbalance = 0)
    {
        if (perspective == 1)
        {
            // black's perspective
            Console.WriteLine(imbalance > 0 ? $"# h g f e d c b a  +{imbalance}" : "# h g f e d c b a");
            
            for (int rank = 0; rank < 8; rank++)
            {
                string rankStr = $"{rank + 1} ";
                
                for (int file = 7; file >= 0; file--)
                    rankStr += pieceStrings[board.GetPiece(file, rank)] + " ";
                
                if (imbalance < 0 && rank == 7) // black advantage
                    rankStr += $" +{-imbalance}";
                
                Console.WriteLine(rankStr);
            }
        }
        else
        {
            // white's perspective
            Console.WriteLine(imbalance < 0 ? $"# a b c d e f g h  +{-imbalance}" : "# a b c d e f g h");
            
            for (int rank = 7; rank >= 0; rank--)
            {
                string rankStr = $"{rank + 1} ";
                
                for (int file = 0; file < 8; file++)
                    rankStr += pieceStrings[board.GetPiece((file, rank))] + " ";
                
                if (imbalance > 0 && rank == 0)
                    rankStr += $" +{imbalance}";
                
                Console.WriteLine(rankStr);
            }
        }
    }

    private void Print(int perspective)
    {
        PrintBoard(board, perspective, board.GetImbalance());
    }

    public static void PrintBitboard(ulong bitboard, int perspective, string on = "#", string off = " ")
    {
        string bitboardStr = "";

        if (perspective == 1)
        {
            Console.Write("# h g f e d c b a");
            
            for (int i = 63; i >= 0; i--)
            {
                if ((i + 1) % 8 == 0)
                    bitboardStr += $"\n{9 - ((i + 1) / 8)} ";
            
                if (((bitboard << 63 - i) >> 63) != 0)
                    bitboardStr += on + " ";
                else
                    bitboardStr += off + " ";
            }
        }
        else
        {
            Console.Write("# a b c d e f g h");
            
            for (int i = 0; i < 64; i++)
            {
                if (i % 8 == 0)
                    bitboardStr += $"\n{8 - (i / 8)} ";
            
                if (((bitboard << 63 - i) >> 63) != 0)
                    bitboardStr += on + " ";
                else
                    bitboardStr += off + " ";
            }
        }
        
        Console.WriteLine(bitboardStr);
    }

    private static readonly string[] PieceStrings =
    [
        "\u265f",
        "\u265c",
        "\u265e",
        "\u265d",
        "\u265b",
        "\u265a", // 5
        "?",
        "?",
        "\u2659", // 8
        "\u2656",
        "\u2658",
        "\u2657",
        "\u2655",
        "\u2654", // 13
        "?",
        " "
    ];

    private static readonly string[] IHateWindows =
    [
        "P",
        "R",
        "N",
        "B",
        "Q",
        "K", // 5
        "?",
        "?",
        "p", // 8
        "r",
        "n",
        "b",
        "q",
        "k", // 13
        "?",
        " "
    ];
}