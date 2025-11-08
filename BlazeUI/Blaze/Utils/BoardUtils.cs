using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze.Utils;
using Board_Representation;

public static class BoardUtils
{
    public static class General
    {
        public const uint PieceMask = 0xF; // covers the last 4 bits
        
        [System.Runtime.CompilerServices.InlineArray(14)]
        public struct BitwiseBoard
        {
            public BitwiseBoard(PiecewiseBoard source)
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
            private uint row;

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
            
            // not meant to be used during search
            public void SetPiece(int file, int rank, uint piece)
            {
                this[rank] &= ~(PieceMask << (file * 4)); // set the given square to 0000
                this[rank] |= (piece << (file * 4)); // set the square to the given piece
            }

            public uint GetPiece(int file, int rank)
            {
                return (this[rank] >> (file * 4)) & PieceMask;
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
        private class BoardSetupException(string message) : Exception(message) { }
        
        public static int CountPawns(General.BitwiseBoard board)
        {
            ulong allPawns = board[Pieces.WhitePawn] | board[Pieces.BlackPawn];
            return (int)(ulong.PopCount(allPawns));
        }

        public static General.CoordinatePair FindKings(General.PiecewiseBoard board)
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

        public static General.ValuePair CountMaterial(General.PiecewiseBoard board)
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
    }
    
    public static class Parsing
    {
        private class BoardParsingException(string message) : Exception(message) { }
        
        public static General.PiecewiseBoard LoadFENPiecewise(string FENBoard)
        {
            General.PiecewiseBoard board = new();
            
            string[] ranks = FENBoard.Split('/');
            
            for (int r = 0; r < 8; r++) // for each rank
            {
                // current file
                int indexer = 0;

                for (int c = 0; c < ranks[r].Length; c++) // for each character
                {
                    if (int.TryParse(ranks[r][c].ToString(), out int v)) // if the character is a number
                    {
                        // fill that many empty squares
                        for (int i = 0; i < v; i++)
                        {
                            board.SetPiece(indexer++, 7-r, Pieces.Empty);
                        }
                    }
                    else
                    {
                        board.SetPiece(indexer++, 7-r, Pieces.Parse(ranks[r][c]));
                    }
                }
            }
            
            return board;
        }

        public static int GetActiveSide(string side)
        {
            if (side == "w")
                return 0;
            if (side == "b")
                return 1;
            throw new BoardParsingException($"'{side}' is not a valid side");
        }
        
        private static readonly Dictionary<char, byte> CastlingAvailability = new()
        {
            {'K', 0b1000},
            {'Q', 0b0100},
            {'k', 0b0010},
            {'q', 0b0001}
        };
    
        public static byte ParseCastling(string s)
        {
            if (s == "-")
                return 0;

            byte c = 0;

            foreach (char cc in s)
            {
                if (CastlingAvailability.TryGetValue(cc, out byte ca))
                    c |= ca;
                else
                    throw new BoardParsingException($"Unable to parse FEN: Unknown castling availability char: {cc}");
            }

            return c;
        }
    }
}