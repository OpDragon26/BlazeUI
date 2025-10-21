using System;

namespace BlazeUI.Blaze;

public static class Hasher
{
    public static readonly int[,,] PieceNumbers = new int[14,8,8];
    public static readonly int[] CastlingNumbers = new int[16];
    public static readonly int[] EnPassantFiles = new int[9];
    private static readonly Random random = new();
    public static int BlackToMove;
    private static bool init;
    
    public static void Init()
    {
        if (init) return;
        init = true;
        
        // for every piece
        for (int i = 0; i < 14; i++)
        {
            if (i is 6 or 7) continue; // no piece at these values
            
            // for every square
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 7; file >= 0; file--)
                {
                    PieceNumbers[i, file, rank] = random.Next();
                }
            }
        }
        
        // indicates that the side to move is black
        BlackToMove = random.Next();
        
        // for every combination of white and black castling
        for (int i = 0; i < 16; i++)
        {
            CastlingNumbers[i] = random.Next();
        }
        
        // for every file 
        for (int i = 0; i < 8; i++)
        {
            EnPassantFiles[i] = random.Next();
        }
        EnPassantFiles[8] = 0;
    }

    public static int ZobristHash(Board board)
    {
        int hash = 0;
        
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                if ((board.AllPieces() & BitboardUtils.GetSquare(file, rank)) != 0) // if there is a piece on the square
                {
                    hash ^= PieceNumbers[board.GetPiece(file, rank), file, rank];
                }
            }
        }
        
        if (board.side == 1)
            hash ^= BlackToMove;
        
        hash ^= CastlingNumbers[board.castling];
        
        // if the en passant file is 8, so there is no en passant available, the hash will be XOR-ed with 0, so nothing changes
        hash ^= EnPassantFiles[board.enPassant.file];
        
        return hash;
    }
}