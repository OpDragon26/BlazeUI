using System;
using System.Collections.Generic;
using BlazeUI.Blaze.Move_Generation;

namespace BlazeUI.Blaze.Utils;
using Magic_Lookup;
using Board_Representation;

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
        ulong potentialCapturers = finder.mask & board.bitboards[finder.GetPiece(board.side)];
        List<(int Files, int rank)> sources = new();
        
        (ulong pinned, Dictionary<ulong, ulong> pinStates) pinState = MoveGenerator.GetPinStates(board, board.side);

        for (int file = 0; file < 8; file++)
        for (int rank = 0; rank < 8; rank++)
            if ((potentialCapturers & BitboardUtils.GetSquare(file, rank)) != 0)
            {
                // if pinned
                if ((pinState.pinned & BitboardUtils.GetSquare(file, rank)) != 0)
                {
                    ulong possibleMoves = pinState.pinStates[BitboardUtils.GetSquare(file, rank)];
                    if ((BitboardUtils.GetSquare(dest) & possibleMoves) == 0)
                        continue;
                }
                
                sources.Add((file, rank));
            }
        
        if (sources.Count == 0)
            throw new NotationParsingException($"No moves found to square: {GetSquare(dest)}");
        
        if (sources.Count == 1)
            return Disambiguation.None;
        
        int files = 0;
        int ranks = 0;
        
        // counts how many squares have a given file or rank
        foreach ((int file, int rank) square in sources)
        {
            // if any of the found moves start from the same file as the disambiguated move
            if (square.file == src.file)
                files++;
            // if any of the found moves start from the same rank as the disambiguated move
            if (square.rank == src.rank)
                ranks++;
        }
        
        return (files > 1, ranks > 1) switch
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
        ulong potentialCapturers = finder.mask & board.bitboards[finder.GetPiece(board.side)];
        
        if (disambiguation == Disambiguation.File)
            potentialCapturers &= BitboardUtils.GetFile(d);
        else if (disambiguation == Disambiguation.Rank)
            potentialCapturers &= BitboardUtils.GetRank(d);

        if (ulong.PopCount(potentialCapturers) == 1)
            return BitboardUtils.FindSquare(potentialCapturers);
        string message = (ulong.PopCount(potentialCapturers) < 1 ? "Multiple pieces" : "No piece") +
                         $" could move to the square {GetSquare(dest)} with disambiguation {disambiguation} {d}";
        throw new NotationParsingException(message);
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