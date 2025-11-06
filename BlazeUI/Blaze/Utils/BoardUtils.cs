using System;
using BlazeUI.Blaze.Board_Representation;

namespace BlazeUI.Blaze.Utils;

public static class BoardUtils
{
    public static class General
    {
        [System.Runtime.CompilerServices.InlineArray(14)]
        public struct BitwiseBoard
        {
            public BitwiseBoard(Board source)
            {
                for (int file = 0; file < 8; file++)
                for (int rank = 0; rank < 8; rank++)
                {
                    if (source.GetPiece(file, rank) != Pieces.Empty)
                        this[source.GetPiece(file, rank)] |= BitboardUtils.GetSquare(file, rank);
                }
            }
            
            private ulong bitboard;

            public ulong this[uint i]
            {
                get => this[(int)i];
                set => this[(int)i] = value;
            }
        }
        
        [System.Runtime.CompilerServices.InlineArray(8)]
        public struct PiecewiseBoard : IEquatable<PiecewiseBoard>
        {
            private uint rank;

            public PiecewiseBoard(uint[] board)
            {
                for(int i = 0; i < 8; i++)
                    this[i] = board[i];
            }

            public bool Equals(PiecewiseBoard other)
            {
                for(int i = 0; i < 8; i++)
                    if (this[i] != other[i])
                        return false;
                return true;
            }
        }
        
        public struct ValuePair(int white, int black)
        {
            public int  white = white;
            public int  black = black;
        
            public int this[int side]
            {
                get => side == 0 ? white : black;
                set { if (side == 0) white = value;else black = value; }
            }
        
            public int this[uint side]
            {
                get => side == 0 ? white : black;
                set { if (side == 0) white = value;else black = value; }
            }

            public int Sum()
            {
                return white + black;
            }
        }
        
        public struct CoordinatePair((int file, int rank) white, (int file, int rank)  black) : IEquatable<CoordinatePair>
        {
            private (int file, int rank)  white = white;
            private (int file, int rank)  black = black;
        
            public (int file, int rank)  this[int side]
            {
                get => side == 0 ? white : black;
                set {
                    if (side == 0) white = value;
                    else black = value;
                }
            }

            bool IEquatable<CoordinatePair>.Equals(CoordinatePair other)
            {
                return white == other.white && black == other.black;
            }

            public override string ToString()
            {
                return $"white: {white}, black: {black}";
            }
        }
    }
    
    public static class BoardSetup
    {
        public static int CountPawns(Board board)
        {
            return (int)(ulong.PopCount(board.AllPawns()));
        }

        public static General.CoordinatePair FindKings(Board board)
        {
            General.CoordinatePair result = new();
            
            bool whiteFound = false;
            bool blackFound = false;
            
            for (int file = 0; file < 8; file++)
            for (int rank = 0; rank < 8; rank++)
            {
                if (board.GetPiece(file, rank) == Pieces.WhiteKing)
                {
                    if (whiteFound)
                        throw new BoardSetupException("Two or more white kings found");
                    
                    result[0] = (file, rank);
                    whiteFound = true;
                }
                else if (board.GetPiece(file, rank) == Pieces.BlackKing)
                {
                    if (blackFound)
                        throw new BoardSetupException("Two or more black kings found");
                    
                    result[1] = (file, rank);
                    blackFound = true;
                }
            }
            
            return result;
        }

        public static General.ValuePair CountMaterial(Board board)
        {
            General.ValuePair material = new();
            
            for (int file = 0; file < 8; file++)
            for (int rank = 0; rank < 8; rank++)
            {
                if (board.GetPiece(file, rank) == Pieces.Empty)
                    continue;
                int side = (int)board.GetPiece(file, rank) >> 3;
                
                material[side] = Pieces.Value[board.GetPiece(file, rank)];
            }
            
            return material;
        }
        
        private class BoardSetupException(string message) : Exception(message) { }
    }
    
    public static class Parsing
    {
        // TODO: add parsing a board from a FEN string
    }
}