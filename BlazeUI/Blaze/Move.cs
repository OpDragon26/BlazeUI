using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazeUI.Blaze;

public class Move
{
    public readonly (int file, int rank) Source;
    public readonly (int file, int rank) Destination;
    public readonly uint Promotion;
    public readonly int Type;
    public readonly int Priority;
    public readonly byte CastlingBan;
    public readonly bool Pawn;
    public readonly bool Capture;
    
    /*
    Special moves
    0000 - regular move
    0001 - white double move
    1001 - black double move
    0010 - white short castle
    0011 - white long castle
    1010 - black short castle
    1011 - black long castle
    0100 - white en passant
    1100 - black en passant
    */

    // the castling mask has up to 4 bits. When the move is made, the mask is then AND-ed with the castling rights in the board, removing the bit that is 0
    
    public Move((int file, int rank) source, (int file, int rank) destination, uint promotion = 0b111, int type = 0b0000, int priority = 0, byte castlingBan = 0b1111, bool pawn = false, bool capture = false)
    {
        Source = source;
        Destination = destination;
        Promotion = promotion;
        Type = type;
        Priority = priority;
        CastlingBan = castlingBan;
        Pawn = pawn;
        Capture = capture;

        if (destination == (7, 0) || source == (7, 0)) CastlingBan &= 0b0111; // if a move is made from or to h1, remove white's short castle rights
        if (destination == (0, 0) || source == (0, 0)) CastlingBan &= 0b1011; // if a move is made from or to a1, remove white's long castle rights
        if (destination == (7, 7) || source == (7, 7)) CastlingBan &= 0b1101; // if a move is made from or to h8, remove black's short castle rights
        if (destination == (0, 7) || source == (0, 7)) CastlingBan &= 0b1110; // if a move is made from or to a8, remove black's long castle rights
        if (source == (4, 0)) CastlingBan = 0b0011; // if the origin of the move is the white king's starting position, remove white's castling rights
        if (source == (4, 7)) CastlingBan = 0b1100; // if the origin of the move is the black king's starting position, remove black's castling rights
    }

    public override bool Equals(object? obj)
    {
        var item = obj as Move;
        if (item == null) return false;
        return Source == item.Source && Destination == item.Destination && Promotion == item.Promotion && Type == item.Type && Pawn == item.Pawn;
    }

    public override int GetHashCode()
    {
        int hash = 0;
        hash |= Source.file << 29;
        hash |= Source.rank << 26;
        hash |= Destination.file << 23;
        hash |= Destination.rank << 20;
        hash |= (int)Promotion << 17;
        hash |= Type << 13;
        return hash;
    }

    private static readonly Dictionary<char, int> Indices = new()
    {
        { "a"[0], 0 },
        { "b"[0], 1 },
        { "c"[0], 2 },
        { "d"[0], 3 },
        { "e"[0], 4 },
        { "f"[0], 5 },
        { "g"[0], 6 },
        { "h"[0], 7 },
    };

    private static readonly Dictionary<char, uint> Promotions = new()
    {
        { "q"[0], 0b100 },
        { "r"[0], 0b001 },
        { "b"[0], 0b011 },
        { "n"[0], 0b010 },
    };
    public Move(string move, Board board)
    {
        Source = (Indices[move[0]], Convert.ToInt32(Convert.ToString(move[1])) - 1);
        Destination = (Indices[move[2]], Convert.ToInt32(Convert.ToString(move[3])) - 1);
        Promotion = move.Length == 5 ? Promotions[move[4]] : 0b111;
        Pawn = false;
        // implicit special moves

        if ((board.GetPiece(Source) & Pieces.TypeMask) == Pieces.WhitePawn) // if the piece is a pawn
        {
            Pawn = true;
            if (Destination == board.enPassant) // if the target is enPassantSquare
                Type = 0b0100 | (board.side << 3);
            else if ((Source.rank == 1 && Destination.rank == 3) || (Source.rank == 6 && Destination.rank == 4)) // if the move is a double move
                Type = 0b0001 | (board.side << 3);
        }
        else if ((board.GetPiece(Source) & Pieces.TypeMask) == Pieces.WhiteKing) // if the piece is a king
        {
            if (Source.file == 4 && (Source.rank == 0 || Source.rank == 7) && (Destination.rank == 0 || Destination.rank == 7)) // if the move is from the king starting square and on the 1st/7th ranks
            {
                if (Destination.file == 2) // long castle
                    Type = 0b0011 | (board.side << 3);
                else if (Destination.file == 6) // short castle
                    Type = 0b0010 | (board.side << 3);
            }
        }

        CastlingBan = 0b1111;
        if (Destination == (7, 0) || Source == (7, 0)) CastlingBan &= 0b0111; // if a move is made from or to h1, remove white's short castle rights
        if (Destination == (0, 0) || Source == (0, 0)) CastlingBan &= 0b1011; // if a move is made from or to a1, remove white's long castle rights
        if (Destination == (7, 7) || Source == (7, 7)) CastlingBan &= 0b1101; // if a move is made from or to h8, remove black's short castle rights
        if (Destination == (0, 7) || Source == (0, 7)) CastlingBan &= 0b1110; // if a move is made from or to a8, remove black's long castle rights
        if (Source == (4, 0)) CastlingBan = 0b0011; // if the origin of the move is the white king's starting position, remove white's castling rights
        if (Source == (4, 7)) CastlingBan = 0b1100; // if the origin of the move is the black king's starting position, remove black's castling rights
    }

    public static (int file, int rank) ParseSquare(string square)
    {
        if (Indices.TryGetValue(square[0], out var file))
        {
            if (Convert.ToInt32(Convert.ToString(square[1])) - 1 is >= 0 and <= 7)
                return (file, Convert.ToInt32(Convert.ToString(square[1])) - 1);
            throw new IndexOutOfRangeException($"Failed to parse square: '{square}' rank not within the confines of the board: {Convert.ToInt32(Convert.ToString(square[1])) - 1}");
        }

        throw new ArgumentException($"Failed to parse square: '{square}' Invalid file: '{square[0]}'");
    }

    public static string GetSquare((int file, int rank) square)
    {
        return Files[square.file] + (square.rank + 1).ToString();
    }
    private static string GetSquare(int file, int rank)
    {
        return Files[file] + (rank + 1).ToString();
    }

    private static readonly char[] Files = ['a','b','c','d','e','f','g','h'];
    public static readonly string[] PromotionStr = ["?", "r", "n", "b", "q","?","?",String.Empty];
    private static readonly char[] AlgPieces = ['?','R','N','B','Q','K'];
    
    public string GetUCI()
    {
        return GetSquare(Source) + GetSquare(Destination) + PromotionStr[Promotion & Pieces.TypeMask];
    }

    public string Notate(Board board)
    {
        if ((board.GetPiece(Source) & Pieces.TypeMask) == Pieces.WhiteKing && Source.file == 4)
        {
            if (Destination.file == 6)
                return "O-O";
            if (Destination.file == 2)
                return "O-O-O";
        }

        string notation = "";

        if ((board.GetPiece(Source) & Pieces.TypeMask) == Pieces.WhitePawn)
        {
            // pawn move

            if (Source.file == Destination.file) // move forward
            {
                if (board.GetPiece(Destination) == Pieces.Empty)
                    notation = GetSquare(Destination);
                else
                    throw new NotationParsingException(
                        $"Failed to convert to Algebraic notation: {GetSquare(Destination)} is not empty: {GetUCI()}");
            }
            else // capture
            {
                if (board.GetPiece(Destination) != Pieces.Empty || Destination == board.enPassant)
                    notation = Files[Source.file] + "x" + GetSquare(Destination);
                else
                    throw new NotationParsingException(
                        $"Failed to convert to Algebraic notation: cannot capture empty: {GetSquare(Destination)}: {GetUCI()}");
            }

            if ((Promotion & Pieces.TypeMask) != 0b111)
                notation += '=' + char.ToUpper(PromotionStr[Promotion & Pieces.TypeMask][0]).ToString();
        }
        else
        {
            // piece move
            notation += AlgPieces[board.GetPiece(Source) & Pieces.TypeMask];
            
            Disambiguation disambiguation = FindLowestDisambiguation(board, GetFinderMask(board.GetPiece(Source) & Pieces.TypeMask, Destination.file, Destination.rank, board), Source, Destination);

            notation += disambiguation switch
            {
                Disambiguation.None => string.Empty,
                Disambiguation.Complete => GetSquare(Source),
                Disambiguation.File => Files[Source.file],
                Disambiguation.Rank => Source.rank + 1,
                _ => throw new NotationParsingException($"What? {disambiguation}")
            };

            if (board.GetPiece(Destination) != Pieces.Empty) // capture
                notation += 'x';
            
            notation += GetSquare(Destination);
        }
        
        Board tempBoard = new Board(board);
        tempBoard.MakeMove(this);
        Outcome outcome = tempBoard.GetOutcome();
        if (outcome is Outcome.BlackWin or Outcome.WhiteWin)
        {
            // checkmate
            notation += '#';
        }

        else if (Search.Attacked(tempBoard.KingPositions[tempBoard.side], tempBoard, 1 - tempBoard.side))
        {
            // check
            notation += '+';
        }
        
        return notation;
    }

    private readonly struct Finder(ulong mask, uint wPiece, uint bPiece)
    {
        public readonly ulong mask = mask;

        public uint GetPiece(int side)
        {
            return side == 0 ? wPiece : bPiece;
        }
    }

    private static Finder GetFinderMask(char c, int file, int rank, Board board)
    {
        return c switch
        {
            'N' => new Finder(Bitboards.KnightMasks[file, rank], Pieces.WhiteKnight, Pieces.BlackKnight),
            'B' => new Finder(MagicLookup.BishopLookupMoves((file, rank), board.AllPieces()).captures, Pieces.WhiteBishop, Pieces.BlackBishop),
            'Q' => new Finder(MagicLookup.RookLookupMoves((file, rank), board.AllPieces()).captures | MagicLookup.BishopLookupMoves((file, rank), board.AllPieces()).captures, Pieces.WhiteQueen, Pieces.BlackQueen),
            'R' => new Finder(MagicLookup.RookLookupMoves((file, rank), board.AllPieces()).captures, Pieces.WhiteRook, Pieces.BlackRook),
            'K' => new Finder(Bitboards.KingMasks[file, rank], Pieces.WhiteKing, Pieces.BlackKing),
            _ => throw new NotationParsingException($"Unknown piece: {c}")
        };
    }
    
    private static Finder GetFinderMask(uint piece, int file, int rank, Board board)
    {
        return piece switch
        {
            Pieces.WhiteKnight => new Finder(Bitboards.KnightMasks[file, rank], Pieces.WhiteKnight, Pieces.BlackKnight),
            Pieces.WhiteBishop => new Finder(MagicLookup.BishopLookupMoves((file, rank), board.AllPieces()).captures, Pieces.WhiteBishop, Pieces.BlackBishop),
            Pieces.WhiteQueen => new Finder(MagicLookup.RookLookupMoves((file, rank), board.AllPieces()).captures | MagicLookup.BishopLookupMoves((file, rank), board.AllPieces()).captures, Pieces.WhiteQueen, Pieces.BlackQueen),
            Pieces.WhiteRook => new Finder(MagicLookup.RookLookupMoves((file, rank), board.AllPieces()).captures, Pieces.WhiteRook, Pieces.BlackRook),
            Pieces.WhiteKing => new Finder(Bitboards.KingMasks[file, rank], Pieces.WhiteKing, Pieces.BlackKing),
            _ => throw new NotationParsingException($"Unknown piece: {piece}")
        };
    }
    
    private static readonly char[] ValidPieces = ['R','N','B','Q','K'];
    private static readonly char[] ValidFiles = ['a','b','c','d','e','f','g','h'];
    private static readonly char[] ValidRanks = ['1','2','3','4','5','6','7','8'];
    private static readonly char[] validPromotions = ['Q','R','B','N'];
    public static Move Parse(string alg, Board board)
    {
        if (alg[^1] == '#' || alg[^1] == '+') // if the move is a check or a checkmate, remove the notation
            alg = alg[..^1]; // removes the last character
        
        if (alg.Equals("O-O"))
            return board.side == 0 ? Bitboards.WhiteShortCastle : Bitboards.BlackShortCastle;
        if (alg.Equals("O-O-O"))
            return board.side == 0 ? Bitboards.WhiteLongCastle : Bitboards.BlackLongCastle;

        (int file, int rank) dest;
        (int file, int rank) src;
        int flag = 0;
        
        switch (alg.Length)
        {
            case 2: // pawn move forward
                dest = ParseSquare(alg);
                int offset = board.side * 2 - 1;
                if (board.GetPiece(dest.file, dest.rank + offset) == (board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn))
                    src = (dest.file, dest.rank + offset);
                else if (board.GetPiece(dest.file, dest.rank + 2 * offset) == (board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn))
                {
                    src = (dest.file, dest.rank + 2 * offset);
                    flag = board.side == 0 ? 0b0001 : 0b1001;
                }
                else
                    throw new NotationParsingException($"No pawn can move to the given square: {alg}");

                return new Move(src, dest, type: flag, pawn: true);
            
            case 3:
                // regular non-disambiguated piece move: Nf3
                dest = ParseSquare($"{alg[1]}{alg[2]}");
                return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.None, dest), dest);
            
            case 4:
                
                // pawn capture
                // piece capture
                // non-capture promotion
                // disambiguated piece move
                if (alg[1] == 'x')
                {
                    // capture
                    if (ValidFiles.Contains(alg[0]))
                    {
                        // pawn capture
                        dest = ParseSquare($"{alg[2]}{alg[3]}");
                        
                        if (board.GetPiece(dest) == Pieces.Empty && dest != board.enPassant)
                            throw new  NotationParsingException("The targeted square is empty and isn't an en passant target");
                        if (dest == board.enPassant)
                            flag = board.side == 0 ? 0b0100 : 0b1100;
                        
                        if (board.side == 0 && board.GetPiece(Indices[alg[0]], dest.rank - 1) == Pieces.WhitePawn)
                            return new Move((Indices[alg[0]], dest.rank - 1), dest, pawn: true, type: flag);
                        if (board.GetPiece(Indices[alg[0]], dest.rank + 1) == Pieces.BlackPawn)
                            return new Move((Indices[alg[0]], dest.rank + 1), dest, pawn: true, type: flag);
                    }
                    else if (ValidPieces.Contains(alg[0]))
                        // piece capture: Nxe4
                        // treated as it wasn't a capture
                        return Parse($"{alg[0]}{alg[2]}{alg[3]}", board);
                    else
                        throw new  NotationParsingException($"Invalid file or piece: {alg[0]}");
                }
                else if (alg[2] == '=')
                {
                    // promotion: e8=Q
                    if (validPromotions.Contains(alg[3]))
                    {
                        dest = ParseSquare($"{alg[0]}{alg[1]}");
                        src = board.GetPiece(dest.file, dest.rank + (board.side * 2 - 1)) == (board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn) 
                            ? (dest.file, dest.rank - (board.side * 2 - 1)) : throw new NotationParsingException("No pawn can move to the given square");
                        return new Move(src, dest, pawn: true, promotion: Promotions[char.ToLower(alg[3])]);
                    }
                    throw new NotationParsingException($"Unknown or incorrect promotion: {alg[3]}");
                }
                else if (ValidFiles.Contains(alg[1]))
                {
                    // file disambiguation: Nfd2
                    dest = ParseSquare($"{alg[2]}{alg[3]}");
                    return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.File, dest, Indices[alg[1]]), dest);
                }
                else if (ValidRanks.Contains(alg[1]))
                {
                    // rank disambiguation
                    dest = ParseSquare($"{alg[2]}{alg[3]}");
                    return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.Rank, dest,int.Parse(alg[1].ToString())), dest);
                }
                throw new NotationParsingException($"Unknown notation: {alg}");
            case 5:
                // doubly disambiguated move
                // single disambiguated capture
                
                // disambiguated capture: Nfxe5
                if (ValidPieces.Contains(alg[0]) && alg[2] == 'x')
                {
                    dest = ParseSquare($"{alg[3]}{alg[4]}");
                    if (ValidFiles.Contains(alg[1])) // file disambiguation
                        return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.File, dest, Indices[alg[1]]), dest);
                    if (ValidRanks.Contains(alg[1])) // rank disambiguation
                       return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.Rank, dest, int.Parse(alg[1].ToString())), dest);
                    throw new NotationParsingException($"Unknown notation: {alg}");
                }
                
                // doubly disambiguated move: Nd3e5
                if (ValidPieces.Contains(alg[0]))
                    return new Move(ParseSquare($"{alg[1]}{alg[2]}"), ParseSquare($"{alg[3]}{alg[4]}"));
                throw new NotationParsingException($"Unknown notation: {alg}");
            
            case 6:
                // doubly disambiguated capture: Nd3xe5
                if (ValidPieces.Contains(alg[0]) && alg[3] == 'x')
                    return new Move(ParseSquare($"{alg[1]}{alg[2]}"), ParseSquare($"{alg[4]}{alg[5]}"));
                // capture promotion: fxe8=Q
                if (alg[1] == 'x' && alg[4] == '=' && ValidFiles.Contains(alg[0]) && validPromotions.Contains(alg[5]))
                {
                    // capture promotion
                    dest = ParseSquare($"{alg[2]}{alg[3]}");
                        
                    if (board.side == 0 && board.GetPiece(Indices[alg[0]], dest.rank - 1) == Pieces.WhitePawn)
                        return new Move((Indices[alg[0]], dest.rank - 1), dest, pawn: true, promotion: Promotions[char.ToLower(alg[5])]);
                    if (board.GetPiece(Indices[alg[0]], dest.rank + 1) == Pieces.BlackPawn)
                        return new Move((Indices[alg[0]], dest.rank + 1), dest, pawn: true, promotion: Promotions[char.ToLower(alg[5])]);
                }
                
                throw new NotationParsingException($"Unknown notation: {alg}");
            
            case 7:
                if (alg[6] == '#' || alg[6] == '+') // move is a check or checkmate
                    return Parse($"{alg[0]}{alg[1]}{alg[2]}{alg[3]}{alg[4]}{alg[5]}", board);
                throw new NotationParsingException($"Unknown notation: {alg}");
            default:
                throw new NotationParsingException($"Unknown notation: {alg}");
        }
    }

    private enum Disambiguation
    {
        None,
        File,
        Rank,
        Complete
    }

    private static (int File, int rank) FindMovingPiece(Board board, Finder finder, Disambiguation disambiguation, (int File, int rank) dest, int d=8)
    {
        int found = 0;
        (int File, int rank) last = (8,8);
        Move[] moves = Search.FilterChecks(Search.SearchBoard(board, false), board);
        
        for (int rank = 7; rank >= 0; rank--)
        {
            if (disambiguation == Disambiguation.Rank && rank != d - 1) continue;
            for (int file = 0; file < 8; file++)
            {
                if (disambiguation == Disambiguation.File && file != d) continue;
                if ((finder.mask & BitboardUtils.GetSquare(file, rank)) != 0 && board.GetPiece(file, rank) == finder.GetPiece(board.side)) 
                {
                    if (moves.Contains(new Move((file, rank), dest)))
                    {
                        last = (file, rank);
                        found++;
                    }
                }
            }
        }
        if (found == 0)
            throw (new NotationParsingException(d != 8 ? $"Unnecessary disambiguation: None found on {d - 1}" : "No piece found that could move to the given square"));
        if (found != 1)
            throw new NotationParsingException($"Inadequate disambiguation: found {found}");
        return last;
    }

    private static Disambiguation FindLowestDisambiguation(Board board, Finder finder, (int file, int rank) src, (int file, int rank) dest)
    {
        int count = 0;
        List<(int file, int rank)> sources = new();
        Move[] moves = Search.FilterChecks(Search.SearchBoard(board, false), board);

        foreach (Move move in moves)
        {
            // if the moves is made by the given piece to the given square
            if (move.Destination == dest && board.GetPiece(move.Source) == finder.GetPiece(board.side))
            {
                sources.Add(move.Source);
                count++;
            }
        }


        if (count == 0)
            throw new NotationParsingException(
                $"No piece found that could move to the given square: {GetSquare(dest)}");
        if (count == 1)
            return Disambiguation.None;
        // count > 1

        
        int file = 0;
        int rank = 0;
        
        // counts how many squares have a given file or rank
        foreach ((int file, int rank) square in sources)
        {
            // if any of the found moves start from the same file as the disambiguated move
            if (square.file == src.file)
                file++;
            // if any of the found moves start from the same rank as the disambiguated move
            if (square.rank == src.rank)
                rank++;
        }

        return (file > 1, rank > 1) switch
        {
            (false, false) => Disambiguation.File,
            (true, false) => Disambiguation.Rank,
            (false, true) => Disambiguation.File,
            (true, true) => Disambiguation.Complete,
        };
    }
}

[Serializable]
public class NotationParsingException: Exception
{
    public NotationParsingException () {}
    public NotationParsingException (string message) : base(message) {}
    public NotationParsingException (string message, Exception innerException) : base (message, innerException) {}    
}