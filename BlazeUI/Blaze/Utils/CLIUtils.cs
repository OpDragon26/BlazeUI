using System;
using System.Runtime.InteropServices;

namespace BlazeUI.Blaze.Utils;
using Board_Representation;

public static class CLIUtils
{
    private static readonly bool WindowsMode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
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