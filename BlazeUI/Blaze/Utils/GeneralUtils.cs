using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazeUI.Blaze.Utils;
using Board_Representation;
using Interface;

public static class GeneralUtils
{
    public static bool SideWon(Side side, Outcome outcome)
    {
        return (side == Side.White && outcome == Outcome.WhiteWin) || (side == Side.Black && outcome == Outcome.BlackWin);
    }
    
    public static MaterialComparison CompareMaterial(Board board)
    {
        MaterialComparison comparison = new(board.GetImbalance() / 100);

        // get the pieces of each side
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                if ((board.GetBitboard(0) & BitboardUtils.GetSquare(file, rank)) != 0)
                    comparison.WhitePieces.Add(board.GetPiece(file, rank));
                else if ((board.GetBitboard(1) & BitboardUtils.GetSquare(file, rank)) != 0)
                    comparison.BlackPieces.Add(board.GetPiece(file, rank) & Pieces.TypeMask);
            }
        }
        
        RemoveIntersection(comparison.WhitePieces, comparison.BlackPieces);
        
        return comparison;
    }

    private static void RemoveIntersection<T>(List<T> list1, List<T> list2)
    {
        var tempList2 = new List<T>(list2);

        var intersection = new List<T>();

        foreach (T item in list1)
        {
            if (tempList2.Contains(item))
            {
                intersection.Add(item);
                tempList2.Remove(item); // remove one occurrence
            }
        }

        // Remove the intersection elements from both lists
        foreach (T item in intersection)
        {
            list1.Remove(item);
            list2.Remove(item);
        }
    }
    
    public class MaterialComparison(int balance)
    {
        public readonly List<uint> WhitePieces = new();
        public readonly List<uint> BlackPieces = new();

        private string GetWhiteString(Dictionary<uint, char> converter)
        {
            return $"{String.Concat(WhitePieces.Select(p => converter[p]))} {(balance > 0 ? $"+{balance}" : "")}";
        }

        public string GetWhiteString()
        {
            return GetWhiteString(UnicodeConverter);
        }

        private string GetBlackString(Dictionary<uint, char> converter)
        {
            return $"{String.Concat(BlackPieces.Select(p => converter[p | Pieces.ColorMask]))} {(balance < 0 ? $"+{-balance}" : "")}";
        }

        public string GetBlackString()
        {
            return GetBlackString(UnicodeConverter);
        }
    }

    private static readonly Dictionary<uint, char> UnicodeConverter = new()
    {
        {0b0000,'♟'},
        {0b0001,'♜'},
        {0b0010,'♞'},
        {0b0011,'♝'},
        {0b0100,'♛'},
        {0b0101,'♚'},
        
        {0b1000,'♙'},
        {0b1001,'♖'},
        {0b1010,'♘'},
        {0b1011,'♗'},
        {0b1100,'♕'},
        {0b1101,'♔'},
    };
    
    public class Timer
    {
        private DateTime StartTime = DateTime.Now;

        public void Start()
        {
            StartTime = DateTime.Now;
        }

        public long Stop()
        {
            TimeSpan elapsed = StartTime - DateTime.Now;
            return elapsed.Milliseconds;
        }
    }
}