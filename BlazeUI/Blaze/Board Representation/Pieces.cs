using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze.Board_Representation;

public static class Pieces
{
    // 4 bits per piece
    // white and black pieces only differ in the first bit
    public const uint WhitePawn = 0b0000; // 0
    public const uint WhiteRook = 0b0001; // 1
    public const uint WhiteKnight = 0b0010; // 2
    public const uint WhiteBishop = 0b0011; // 3
    public const uint WhiteQueen = 0b0100; // 4
    public const uint WhiteKing = 0b0101; // 5

    public const uint BlackPawn = 0b1000; // 8
    public const uint BlackRook = 0b1001; // 9
    public const uint BlackKnight = 0b1010; // 10
    public const uint BlackBishop = 0b1011; // 11
    public const uint BlackQueen = 0b1100; // 12
    public const uint BlackKing = 0b1101; // 13

    public const uint Empty = 0b1111; // 15

    public const uint TypeMask = 0b111;
    public const uint ColorMask = 0b1000;

    public static uint Flip(uint piece)
    {
        return 0b1000 ^ piece;
    }

    public static int ColorOf(uint piece)
    {
        return (int)(piece >> 3);
    }

    public static uint TypeOf(uint piece)
    {
        return piece & TypeMask;
    }
    
    public static readonly int[] Value =
    [
        100, // 0
        500,
        300,
        300,
        900,
        1000, // 5
        0,
        0,
        -100, // 8
        -500,
        -300,
        -300,
        -900,
        -1000, // 13
        0,
        0
    ];

    private static readonly Dictionary<char, uint> PieceStrings = new()
    {
        {'P', WhitePawn },
        {'R', WhiteRook },
        {'N', WhiteKnight },
        {'B', WhiteBishop },
        {'Q', WhiteQueen },
        {'K', WhiteKing },
        {'p', BlackPawn},
        {'r', BlackRook },
        {'n', BlackKnight },
        {'b', BlackBishop },
        {'q', BlackQueen },
        {'k', BlackKing },
    };

    public static uint Parse(char s)
    {
        if (PieceStrings.TryGetValue(s, out uint piece))
            return piece;
        
        throw new FormatException($"Unable to parse FEN: Unknown piece: '{s}'");
    }
}