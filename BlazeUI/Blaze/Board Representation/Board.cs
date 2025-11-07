using System.Collections.Generic;
using static BlazeUI.Blaze.Utils.BoardUtils;
using static BlazeUI.Blaze.Utils.BoardUtils.General;

namespace BlazeUI.Blaze.Board_Representation;

public class Board
{

    /*
    13 pieces -> 4 bits per piece -> 8 uints, each corresponding to one row
    
    Black's perspective
      7 6 5 4 3 2 1 0
    0 0 0 0 0 0 0 0 0
    1 1 1 1 1 1 1 1 1
    2 2 2 2 2 2 2 2 2 
    3 3 3 3 3 3 3 3 3 
    4 4 4 4 4 4 4 4 4 
    5 5 5 5 5 5 5 5 5 
    6 6 6 6 6 6 6 6 6 
    7 7 7 7 7 7 7 7 7 
    */
    
    // basic values
    private PiecewiseBoard board;
    public int side;
    public (int file, int rank) enPassant = (8, 8);
    
    // bitboards
    public BitwiseBoard bitboards;
    // white pieces
    // black pieces
    // white pawns only
    // black pawns only
    
    // castling
    public byte castling = 0b1111; // white short, white long, black short, black long
    
    public CoordinatePair KingPositions;

    private int halfMoveClock;
    private int pawns;
    private ValuePair values;
    private readonly Dictionary<int, int> repeat = new();
    public int hashKey;

    public byte castled;
    // only the last two bits can be on
    // 0b10 - white castled
    // 0b01 - black castled

    public bool considerRepetition;
    
    public Board(PiecewiseBoard board, bool considerRepetition = true)
    {
        this.board = board;
        
        InitBoard();

        hashKey = Hasher.ZobristHash(this);
        this.considerRepetition = considerRepetition;
    }
    
    public Board(Board board, bool permChange = false) // clone board
    {
        this.board = board.board;
        side = board.side;
        bitboards = board.bitboards;
        enPassant = board.enPassant;
        castling = board.castling;
        KingPositions = board.KingPositions;
        halfMoveClock = board.halfMoveClock;
        pawns = board.pawns;
        values = board.values;
        if (considerRepetition)
            repeat = permChange ? new() : new(board.repeat);
        hashKey = board.hashKey;
        castled = board.castled;
        considerRepetition = board.considerRepetition;
    }

    public Board(string FEN, bool considerRepetition = true)
    {
        string[] fields = FEN.Split(' ');
        
        board = Parsing.LoadFENPiecewise(fields[0]);
        side = Parsing.GetActiveSide(fields[1]);
        castling = Parsing.ParseCastling(fields[2]);
        enPassant = fields[3] == "-" ? (8, 8) : Move.ParseSquare(fields[3]);
        halfMoveClock = int.Parse(fields[4]);
        hashKey = Hasher.ZobristHash(this);

        InitBoard();
        
        this.considerRepetition = considerRepetition;
    }
    
    public void MakeMove(Move move)
    {
        halfMoveClock++;
        if (side == 1 && considerRepetition)
            hashKey ^= Hasher.BlackToMove;
        
        // update bitboards
        bitboards[GetPiece(move.Source)] ^= BitboardUtils.GetSquare(move.Source);

        if (GetPiece(move.Destination) != Pieces.Empty) // if the move is a capture
        {
            values[1-side] -= Pieces.Value[GetPiece(move.Destination)]; // subtract the value of the piece from the opponent
            bitboards[GetPiece(move.Destination)] ^= BitboardUtils.GetSquare(move.Destination); // switch the square on the other side's bitboard
            halfMoveClock = 0;
            if ((GetPiece(move.Destination) & Pieces.TypeMask) == Pieces.WhitePawn) // if the taken piece was a pawn
                pawns--;
        }
        
        if (move.Promotion == 0b111) // move is not a promotion
        {
            bitboards[GetPiece(move.Source)] ^= BitboardUtils.GetSquare(move.Destination);
            SetPiece(move.Destination, GetPiece(move.Source));
            
            if ((GetPiece(move.Destination) & Pieces.TypeMask) == Pieces.WhitePawn) // if the moved piece is a pawn
                halfMoveClock = 0;
            else if ((GetPiece(move.Destination) & Pieces.TypeMask) == Pieces.WhiteKing) // if the moved piece is a king
                KingPositions[side] = move.Destination;
        }
        else // move is a promotion
        {
            bitboards[((uint)side << 3) | move.Promotion] ^= BitboardUtils.GetSquare(move.Destination);
            SetPiece(move.Destination, ((uint)side << 3) | move.Promotion);
            halfMoveClock = 0; // the piece moved is definitely a pawn
            pawns--;
            values[side] += Pieces.Value[GetPiece(move.Destination)]; // add the value of the promoted piece to the moving side
            values[side] -= Pieces.Value[Pieces.WhitePawn | (side << 3)]; // subtract the value of the pawn from the moving side
        }
        
        // update the hash key
        if (considerRepetition)
        {
            hashKey ^= Hasher.PieceNumbers[GetPiece(move.Source), move.Source.file, move.Source.rank]; // remove the moving piece
            hashKey ^= Hasher.PieceNumbers[GetPiece(move.Destination), move.Destination.file, move.Destination.rank]; // add the moved piece, including promoted pieces
            hashKey ^= Hasher.CastlingNumbers[castling]; // remove the castling rights number
            // remove the en passant file if there was any, if it was 8, no need to change anything
            hashKey ^= Hasher.EnPassantFiles[enPassant.file];
        }
        
        Clear(move.Source);
        enPassant = (8, 8);
        byte saveCastling = castling;
        castling &= move.CastlingBan;
        if (saveCastling != castling || move.Pawn || move.Capture)
            repeat.Clear();
        
        if (considerRepetition) hashKey ^= Hasher.CastlingNumbers[castling]; // add the new castling rights number

        switch (move.Type)
        {
            case 0b0000: break;
            case 0b0001: // white double move
                enPassant = (move.Source.file, 2);
                if (considerRepetition) hashKey ^= Hasher.EnPassantFiles[move.Source.file]; // add the en passant file
            break;
            
            case 0b1001: // black double move
                enPassant = (move.Source.file, 5);
                if (considerRepetition) hashKey ^= Hasher.EnPassantFiles[move.Source.file]; // add the en passant file
            break;
            
            case 0b0010: // white short castle
                Clear(7, 0);
                SetPiece(5,0, Pieces.WhiteRook);
                bitboards[Pieces.WhiteRook] ^= BitboardUtils.GetSquare(7,0);
                bitboards[Pieces.WhiteRook] ^= BitboardUtils.GetSquare(5,0);
                // update the hash key
                if (considerRepetition)
                { 
                    hashKey ^= Hasher.PieceNumbers[Pieces.WhiteRook, 7, 0];
                    hashKey ^= Hasher.PieceNumbers[Pieces.WhiteRook, 5, 0];
                }

                castled |= 0b10;
            break;
            
            case 0b0011: // white long castle
                Clear(0, 0);
                SetPiece(3,0, Pieces.WhiteRook);
                bitboards[Pieces.WhiteRook] ^= BitboardUtils.GetSquare(0,0);
                bitboards[Pieces.WhiteRook] ^= BitboardUtils.GetSquare(3,0);
                // update the hash key
                if (considerRepetition)
                {
                    hashKey ^= Hasher.PieceNumbers[Pieces.WhiteRook, 0, 0];
                    hashKey ^= Hasher.PieceNumbers[Pieces.WhiteRook, 3, 0];
                }

                castled |= 0b10;
            break;
            
            case 0b1010: // black short castle
                Clear(7, 7);
                SetPiece(5,7, Pieces.BlackRook);
                bitboards[Pieces.BlackRook] ^= BitboardUtils.GetSquare(7,7);
                bitboards[Pieces.BlackRook] ^= BitboardUtils.GetSquare(5,7);
                // update the hash key
                if (considerRepetition)
                {
                    hashKey ^= Hasher.PieceNumbers[Pieces.BlackRook, 7, 7];
                    hashKey ^= Hasher.PieceNumbers[Pieces.BlackRook, 5, 7]; 
                }
                
                castled |= 0b01;
            break;
            
            case 0b1011: // black long castle
                Clear(0, 7);
                SetPiece(3,7, Pieces.BlackRook);
                bitboards[Pieces.BlackRook] ^= BitboardUtils.GetSquare(0,7);
                bitboards[Pieces.BlackRook] ^= BitboardUtils.GetSquare(3,7);
                // update the hash key
                if (considerRepetition)
                {
                    hashKey ^= Hasher.PieceNumbers[Pieces.BlackRook, 0, 7];
                    hashKey ^= Hasher.PieceNumbers[Pieces.BlackRook, 3, 7];
                }

                castled |= 0b01;
            break;
            
            case 0b0100: // white en passant
                Clear(move.Destination.file, 4);
                bitboards[Pieces.BlackPawn] ^= BitboardUtils.GetSquare(move.Destination.file,4);
                values.black += 100;
                // update the hash key
                if (considerRepetition) hashKey ^= Hasher.PieceNumbers[Pieces.BlackPawn, move.Destination.file, 4];
            break;
            
            case 0b1100: // black en passant
                Clear(move.Destination.file, 3);
                bitboards[Pieces.WhitePawn] ^= BitboardUtils.GetSquare(move.Destination.file,3);
                values.white -= 100;
                // update the hash key
                if (considerRepetition) hashKey ^= Hasher.PieceNumbers[Pieces.WhitePawn, move.Destination.file, 3];
            break;
        }
        
        if (considerRepetition)
            Add(); // adds the hash of the board to the dictionary

        side = 1 - side;
    }

    public bool IsDraw()
    {
        // threefold repetition or 50 move rule or each side has a minor piece or less and there are no pawns left (insufficient material)
        // stalemate requires searching for legal moves, so it's checked elsewhere
        return repeat.ContainsValue(3) || halfMoveClock > 100 || (pawns == 0 && values.white <= 1300 && values.black >= -1300);
    }
    
    public Outcome GetOutcome()
    {
        // gets the actual outcome of the match
        // requires searching for legal moves, shouldn't be used during a search
        if (IsDraw())
            return Outcome.Draw;
        
        Move[] moves = Search.FilterChecks(Search.SearchBoard(this), this);
        if (moves.Length == 0) // if there are no legal moves
            // if the king is attacked, the game ended in a checkmate, if it isn't the game is a draw by stalemate
            return Search.Attacked(KingPositions[side], this, 1-side) ? side == 0 ? Outcome.BlackWin : Outcome.WhiteWin : Outcome.Draw;

        return Outcome.Ongoing;
    }

    private void InitBoard()
    {
        bitboards = new BitwiseBoard(board);
        pawns = BoardSetup.CountPawns(bitboards);
        KingPositions = BoardSetup.FindKings(board);
        values = BoardSetup.CountMaterial(board);
    }

    public int GetImbalance()
    {
        return values.Sum();
    }
    
    public bool IsEndgame()
    {
        return values.white + int.Abs(values.black) < 5300;
    }

    // adds the hash of the board 
    private void Add()
    {
        if (repeat.TryGetValue(hashKey, out int v)) // if the hash of the board is in already in the dictionary
        {
            // if it is found, v is at least 1, if it's more, this is the third time the position appears, so the game is a draw by threefold repetition
            repeat[hashKey] = v + 1;
        }
        else // the board position is entirely new
            repeat.Add(hashKey, 1);
    }

    public override bool Equals(object? obj)
    {
        var item = obj as Board;
        if (item == null)
            return false;
        return board.Equals(item.board) && enPassant == item.enPassant && side == item.side && castling == item.castling;
    }

    public override int GetHashCode()
    {
        return Hasher.ZobristHash(this);
    }
    
    public ulong AllPieces()
    {
        return bitboards[Pieces.WhitePawn] | bitboards[Pieces.BlackPawn] | bitboards[Pieces.WhiteRook] | bitboards[Pieces.BlackRook]
            | bitboards[Pieces.WhiteKnight] | bitboards[Pieces.BlackKnight] | bitboards[Pieces.WhiteBishop] | bitboards[Pieces.BlackBishop]
            | bitboards[Pieces.WhiteQueen] | bitboards[Pieces.BlackQueen] | bitboards[Pieces.WhiteKing] | bitboards[Pieces.BlackKing];
    }

    public ulong AllPawns()
    {
        return bitboards[Pieces.WhitePawn] | bitboards[Pieces.BlackPawn];
    }

    public ulong WhitePieces()
    {
        return bitboards[Pieces.WhitePawn] | bitboards[Pieces.WhiteRook] | bitboards[Pieces.WhiteKnight] | bitboards[Pieces.WhiteBishop] | bitboards[Pieces.WhiteQueen] | bitboards[Pieces.WhiteKing];
    }

    public ulong BlackPieces()
    {
        return bitboards[Pieces.BlackPawn] | bitboards[Pieces.BlackRook] | bitboards[Pieces.BlackKnight] | bitboards[Pieces.BlackBishop] | bitboards[Pieces.BlackQueen] | bitboards[Pieces.BlackKing];
    }

    public ulong GetBitboard(int color)
    {
        return color == 0 ? WhitePieces() : BlackPieces();
    }

    public ulong GetBitboard(int color, uint piece)
    {
        return bitboards[piece | ((uint)color << 3)];
    }
    
    public uint GetPiece((int file, int rank) square) // overload that takes a tuple
    {
        return (board[square.rank] >> (square.file * 4)) & PieceMask;
    }
    
    public uint GetPiece(int file, int rank) // overload that takes individual values
    {
        return (board[rank] >> (file * 4)) & PieceMask;
    }

    private void Clear((int file, int rank) square) // overload that takes a tuple
    {
        board[square.rank] |= (PieceMask << (square.file * 4)); // set the given square to 1111
    }
    
    private void Clear(int file, int rank) // overload that takes individual values
    {
        board[rank] |= (PieceMask << (file * 4)); // set the given square to 1111
    }
    
    private void SetPiece((int file, int rank) square, uint piece) // overload that takes a tuple
    {
        board[square.rank] &= ~(PieceMask << (square.file * 4)); // set the given square to 0000
        board[square.rank] |= (piece << (square.file * 4)); // set the square to the given piece
    }
    
    private void SetPiece(int file, int rank, uint piece) // overload that takes individual values
    {
        board[rank] &= ~(PieceMask << (file * 4)); // set the given square to 0000
        board[rank] |= (piece << (file * 4)); // set the square to the given piece
    }
}

public enum Outcome
{
    Ongoing,
    WhiteWin,
    BlackWin,
    Draw
}
    
public static class Presets
{
    public static readonly string StartingBoard = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
}