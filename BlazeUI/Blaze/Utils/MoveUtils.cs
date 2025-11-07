using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazeUI.Blaze.Utils;

public static class MoveUtils
{
    public static readonly char[] Files = ['a','b','c','d','e','f','g','h'];
    public static string GetSquare((int file, int rank) square)
    {
        return Files[square.file] + (square.rank + 1).ToString();
    }
    
    public readonly struct Finder(ulong mask, uint wPiece, uint bPiece)
    {
        public readonly ulong mask = mask;

        public uint GetPiece(int side)
        {
            return side == 0 ? wPiece : bPiece;
        }
    }
    
    public static Finder GetFinderMask(char c, int file, int rank, Board board)
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
    
    public enum Disambiguation
    {
        None,
        File,
        Rank,
        Complete
    }
    
    public static Finder GetFinderMask(uint piece, int file, int rank, Board board)
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

    public static Disambiguation FindLowestDisambiguation(Board board, Finder finder, (int file, int rank) src, (int file, int rank) dest)
    {
        int count = 0;
        List<(int file, int rank)> sources = new();
        Move[] moves = MoveGenerator.SearchBoard(board, false).ToArray();

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
    
    public static (int File, int rank) FindMovingPiece(Board board, Finder finder, Disambiguation disambiguation, (int File, int rank) dest, int d=8)
    {
        int found = 0;
        (int File, int rank) last = (8,8);
        Move[] moves = MoveGenerator.SearchBoard(board, false).ToArray();
        
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
    
    public static readonly string[] PromotionStr = ["?", "r", "n", "b", "q","?","?",String.Empty];
    public static readonly char[] AlgPieces = ['?','R','N','B','Q','K'];
    
    public static readonly char[] ValidPieces = ['R','N','B','Q','K'];
    public static readonly char[] ValidFiles = ['a','b','c','d','e','f','g','h'];
    public static readonly char[] ValidRanks = ['1','2','3','4','5','6','7','8'];
    public static readonly char[] validPromotions = ['Q','R','B','N'];
    
    public static readonly Dictionary<char, int> Indices = new()
    {
        { 'a', 0 },
        { 'b', 1 },
        { 'c', 2 },
        { 'd', 3 },
        { 'e', 4 },
        { 'f', 5 },
        { 'g', 6 },
        { 'h', 7 },
    };
    
    public static readonly Dictionary<char, uint> Promotions = new()
    {
        { 'q', 0b100 },
        { 'r', 0b001 },
        { 'b', 0b011 },
        { 'n', 0b010 },
    };
    
    public class NotationParsingException(string message): Exception(message) { }
}