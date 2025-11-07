using System;

namespace BlazeUI.Blaze.Board_Representation;

public static class ZobristHash
{
    private static readonly int[,,] PieceNumbers = new int[14,8,8];
    private static readonly int[] CastlingNumbers = new int[16];
    private static readonly int[] EnPassantFiles = new int[9];
    private static readonly Random random = new();
    private static int BlackToMove;
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

    public static Zobrist HashBoard(Board board)
    {
        Zobrist hash = new();
        
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                if ((board.AllPieces() & BitboardUtils.GetSquare(file, rank)) != 0) // if there is a piece on the square
                {
                    hash.Modify(PieceNumbers[board.GetPiece(file, rank), file, rank]);
                }
            }
        }
        
        if (board.side == 1)
            hash.Modify(BlackToMove);
        
        hash.Modify(CastlingNumbers[board.castling]);
        
        // if the en passant file is 8, so there is no en passant available, the hash will be XOR-ed with 0, so nothing changes
        hash.Modify(EnPassantFiles[board.enPassant.file]);
        
        return hash;
    }

    public struct Zobrist : IEquatable<Zobrist>
    {
        public int key;

        public override int GetHashCode()
        {
            return key;
        }

        public bool Equals(Zobrist other)
        {
            return key == other.key;
        }

        public void Modify(int component)
        {
            key ^= component;
        }

        private void UpdatePiece(uint piece, int file, int rank)
        {
            Modify(PieceNumbers[piece, file, rank]);
        }

        private void UpdatePiece(uint piece, (int file, int rank) square)
        {
            UpdatePiece(piece, square.file, square.rank);
        }

        public void Update(Move move, uint source, uint target, int side, byte castling, int enPassantFile)
        {
            if (side == 1)
                Modify(BlackToMove);
            
            // update pieces
            UpdatePiece(source, move.Source);
            if (target != Pieces.Empty) // if capture, remove captured piece
                UpdatePiece(target, move.Destination);
            UpdatePiece(move.IsPromotion() ? move.Promotion : source, move.Destination);
            
            // remove en passant and castling
            // the EP file only needs to be re-added if the next move changes the file, since if there isn't an EP file
            // the hash will remain unchanged
            Modify(CastlingNumbers[castling]);
            Modify(EnPassantFiles[enPassantFile]);
            
            Modify(CastlingNumbers[castling & move.CastlingBan]);

            switch (move.Type & 0b0111) // independent of side
            {
                case 0b0001: // double pawn move
                    Modify(EnPassantFiles[move.Source.file]);
                    break;
                
                case 0b0010: // short castle
                    UpdatePiece(side == 0 ? Pieces.WhiteRook : Pieces.BlackRook, 7, side * 7);
                    UpdatePiece(side == 0 ? Pieces.WhiteRook : Pieces.BlackRook, 5, side * 7);
                    break;
                
                case 0b0011: // long castle
                    UpdatePiece(side == 0 ? Pieces.WhiteRook : Pieces.BlackRook, 0, side * 7);
                    UpdatePiece(side == 0 ? Pieces.WhiteRook : Pieces.BlackRook, 3, side * 7);
                    break;
                
                case 0b0100: // en passant
                    // remove the pawn
                    UpdatePiece(side == 0 ? Pieces.BlackPawn : Pieces.WhitePawn, move.Destination.file, 4 - side);
                    break;
            }
        }
    }
}