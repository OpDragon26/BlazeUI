using System;
using System.Collections.Generic;

namespace BlazeUI.Blaze;

public static class BitboardUtils
{
    public static List<PinSearchResult> GeneratePinResult((int file, int rank) pos, ulong pieces, uint piece)
    {
        List<PinSearchResult> results = new();
        (int file, int rank)[] pattern = piece == Pieces.WhiteRook ? RookPattern : BishopPattern;
        
        for (int i = 0; i < 4; i++) // for each direction
        {
            int found = 0;
            (int, int) pinPos = (0,0);
            (int, int) pinnedPos = (0, 0);
            ulong path = 0;
            
            for (int j = 1; j < 8; j++) // in each direction
            {
                (int file, int rank) target = (pos.file + pattern[i].file * j, pos.rank + pattern[i].rank * j);
                
                if (!ValidSquare(target.file, target.rank)) // if the square is outside the bounds of the board
                    break;
                if ((pieces & GetSquare(target)) != 0) // if the targeted square is not empty
                {
                    if (++found > 2) break;
                    if (found == 1) // pinned piece
                        pinnedPos = target;
                    if (found == 2) // pinning piece
                    {
                        pinPos = target;
                        path |= GetSquare(target);
                    }
                }
                else
                    path |= GetSquare(target);
            }
            
            if (found == 2)
                results.Add(new PinSearchResult(pinPos, GetSquare(pinnedPos), path));
        }
        
        return results;
    }

    public readonly struct PinSearchResult((int file, int rank) pinningPos, ulong pinnedPiece, ulong path)
    {
        public readonly (int file, int rank) pinningPos = pinningPos;
        public readonly ulong pinnedPiece = pinnedPiece;
        public readonly ulong path = path;
    }
    
    public static (Move[] moves, ulong captures) GetMoves(ulong blockers, (int file, int rank) pos, ulong piece)
    {
        ulong captures = 0;
        List<Move> moves = new List<Move>();

        (int file, int rank)[] pattern = piece == Pieces.WhiteRook ? RookPattern : BishopPattern;

        for (int i = 0; i < 4; i++) // for each pattern
        {
            for (int j = 1; j < 8; j++) // in each direction
            {
                (int file, int rank) target = (pos.file + pattern[i].file * j, pos.rank + pattern[i].rank * j);
                
                if (!ValidSquare(target.file, target.rank)) // if the square is outside the bounds of the board
                    break;
                if ((blockers & GetSquare(target)) == 0) // if the targeted square is empty
                    moves.Add(new Move(pos, target, priority: 5 + Bitboards.PriorityWeights[target.file, target.rank] * Weights.PriorityWeightMultiplier));
                else
                {
                    captures |= GetSquare(target);
                    break;
                }
            }
        }
        
        moves.Sort((a, c) => c.Priority.CompareTo(a.Priority));
        return (moves.ToArray(), captures);
    }
    
    public static Move[] GetBitboardMoves(ulong bitboard, (int file, int rank) pos, int priority, bool pawn = false, bool capture = false)
    {
        List<Move> moves = new List<Move>();
        
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                if ((bitboard & GetSquare(file, rank)) != 0) // if the given square is on
                {
                    if (!pawn)
                        moves.Add(new Move(pos, (file,rank), priority: priority + Bitboards.PriorityWeights[file, rank], capture: capture));
                    else if (pawn)
                    {
                        if (rank == 0 || rank == 7) // promotion
                        {
                            moves.Add(new Move(pos, (file,rank), promotion: Pieces.WhiteQueen, priority: priority + Bitboards.PriorityWeights[file, rank] + 50, pawn: pawn, capture: capture));
                            moves.Add(new Move(pos, (file,rank), promotion: Pieces.WhiteRook, priority: priority + Bitboards.PriorityWeights[file, rank] + 5, pawn: pawn, capture: capture));
                            moves.Add(new Move(pos, (file,rank), promotion: Pieces.WhiteBishop, priority: priority + Bitboards.PriorityWeights[file, rank], pawn: pawn, capture: capture));
                            moves.Add(new Move(pos, (file,rank), promotion: Pieces.WhiteKnight, priority: priority + Bitboards.PriorityWeights[file, rank], pawn: pawn, capture: capture));
                        }
                        else
                            moves.Add(new Move(pos, (file,rank), priority: priority + Bitboards.PriorityWeights[file, rank] + 20, pawn: pawn, capture: capture));

                    }
                }
            }
        }
        
        moves.Sort((a, c) => c.Priority.CompareTo(a.Priority));
        return moves.ToArray();
    }
    
    public static Move GetEnPassantMoves(ulong bitboard)
    {
        (int file, int rank) source = (0, 0);
        (int file, int rank) target = (0, 0);
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                if ((bitboard & GetSquare(file, rank)) != 0) // if the given square is on
                {
                    if (rank == 4 || rank == 3) 
                        source = (file, rank);
                    else if (rank == 5 || rank == 2)
                        target = (file, rank);
                }
            }
        }

        Move move = new Move(source, target, type: source.rank == 4 ? 0b0100 : 0b1100, priority: 3, pawn: true); // source.rank == 4 => white

        return move;
    }

    public static Move[] GetPawnMoves(ulong combination, (int file, int rank) pos, int color)
    {
        List<Move> moves = new List<Move>();
        
        if (color == 0)
        {
            if (pos.rank == 6) // white promotion rank
            {
                if ((combination & GetSquare(pos.file, 7)) == 0) // if the square in front is empty
                {
                    moves.Add(new Move(pos, (pos.file, 7), Pieces.WhiteQueen, priority: 30, pawn: true));
                    moves.Add(new Move(pos, (pos.file, 7), Pieces.WhiteRook, priority: 2, pawn: true));
                    moves.Add(new Move(pos, (pos.file, 7), Pieces.WhiteBishop, priority: 2, pawn: true));
                    moves.Add(new Move(pos, (pos.file, 7), Pieces.WhiteKnight, priority: 2, pawn: true));
                }
            }
            else // not a promotion
            {
                if ((combination & GetSquare(pos.file, pos.rank + 1)) == 0) // if the square in front is empty
                {
                    moves.Add(new Move(pos, (pos.file, pos.rank + 1), priority: 5 + Bitboards.PriorityWeights[pos.file, pos.rank + 2] + pos.rank, pawn: true));
                    
                    if (pos.rank == 1 && (combination & GetSquare(pos.file, pos.rank + 2)) == 0) // check if the double move square is empty
                        moves.Add(new Move(pos, (pos.file, pos.rank + 2), priority: 6 + Bitboards.PriorityWeights[pos.file, pos.rank + 2] + pos.rank, type: 0b0001, pawn: true));
                }
            }
        }
        else
        {
            if (pos.rank == 1) // black promotion rank
            {
                if ((combination & GetSquare(pos.file, 0)) == 0) // if the square behind is empty
                {
                    moves.Add(new Move(pos, (pos.file, 0), Pieces.BlackQueen, priority: 30, pawn: true));
                    moves.Add(new Move(pos, (pos.file, 0), Pieces.BlackRook, priority: 2, pawn: true));
                    moves.Add(new Move(pos, (pos.file, 0), Pieces.BlackBishop, priority: 2, pawn: true));
                    moves.Add(new Move(pos, (pos.file, 0), Pieces.BlackKnight, priority: 2, pawn: true));
                }
            }
            else // not a promotion
            {
                if ((combination & GetSquare(pos.file, pos.rank - 1)) == 0) // if the square behind is empty
                {
                    moves.Add(new Move(pos, (pos.file, pos.rank - 1), priority: 12 + Bitboards.PriorityWeights[pos.file, pos.rank - 1] - pos.rank, pawn: true));
                    
                    if (pos.rank == 6 && (combination & GetSquare(pos.file, pos.rank - 2)) == 0) // check if the double move square is empty
                        moves.Add(new Move(pos, (pos.file, pos.rank - 2), priority: 13 + Bitboards.PriorityWeights[pos.file, pos.rank - 2] - pos.rank, type: 0b1001, pawn: true));
                }
            }
        }
        
        moves.Sort((a, c) => c.Priority.CompareTo(a.Priority));
        return moves.ToArray();
    }

    public static Move[] GetPawnCaptures(ulong combination, (int file, int rank) pos, int color)
    {
        List<Move> moves = new List<Move>();
        
        if (color == 0)
        {
            if (pos.rank == 6) // white promotion rank
            {
                // check if the capture squares are occupied
                if ((combination & GetSquare(pos.file + 1, 7)) != 0)
                {
                    moves.Add(new Move(pos, (pos.file + 1, 7), Pieces.WhiteQueen, priority: 65, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file + 1, 7), Pieces.WhiteRook, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file + 1, 7), Pieces.WhiteBishop, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file + 1, 7), Pieces.WhiteKnight, priority: 2, pawn: true, capture: true));
                }
                if ((combination & GetSquare(pos.file - 1, 7)) != 0)
                {
                    moves.Add(new Move(pos, (pos.file - 1, 7), Pieces.WhiteQueen, priority: 65, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file - 1, 7), Pieces.WhiteRook, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file - 1, 7), Pieces.WhiteBishop, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file - 1, 7), Pieces.WhiteKnight, priority: 2, pawn: true, capture: true));
                }
            }
            else // not a promotion
            {
                // check if the capture squares are occupied
                if ((combination & GetSquare(pos.file + 1, pos.rank + 1)) != 0)
                    moves.Add(new Move(pos, (pos.file + 1, pos.rank + 1), priority: 60, pawn: true, capture: true));
                if ((combination & GetSquare(pos.file - 1, pos.rank + 1)) != 0)
                    moves.Add(new Move(pos, (pos.file - 1, pos.rank + 1), priority: 60, pawn: true, capture: true));
            }
        }
        else
        {
            if (pos.rank == 1) // black promotion rank
            {
                // check if the capture squares are occupied
                if ((combination & GetSquare(pos.file + 1, 0)) != 0)
                {
                    moves.Add(new Move(pos, (pos.file + 1, 0), Pieces.BlackQueen, priority: 65, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file + 1, 0), Pieces.BlackRook, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file + 1, 0), Pieces.BlackBishop, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file + 1, 0), Pieces.BlackKnight, priority: 2, pawn: true, capture: true));
                }
                if ((combination & GetSquare(pos.file - 1, 0)) != 0)
                {
                    moves.Add(new Move(pos, (pos.file - 1, 0), Pieces.BlackQueen, priority: 65, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file - 1, 0), Pieces.BlackRook, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file - 1, 0), Pieces.BlackBishop, priority: 2, pawn: true, capture: true));
                    moves.Add(new Move(pos, (pos.file - 1, 0), Pieces.BlackKnight, priority: 2, pawn: true, capture: true));
                }
            }
            else // not a promotion
            {
                // check if the capture squares are occupied
                if ((combination & GetSquare(pos.file + 1, pos.rank - 1)) != 0)
                    moves.Add(new Move(pos, (pos.file + 1, pos.rank - 1), priority: 60, pawn: true, capture: true));
                if ((combination & GetSquare(pos.file - 1, pos.rank - 1)) != 0)
                    moves.Add(new Move(pos, (pos.file - 1, pos.rank - 1), priority: 60, pawn: true, capture: true));
            }
        }
        
        return moves.ToArray();
    }
    
    // reused code from my previous attempt
    // generates every bit combination using a mask
    public static ulong[] Combinations(ulong blockerMask)
    {
        // count how many on bits are there in the mask, that's going to give us the amount of combinations
        List<int> indices = new List<int>();
        int l = 0;
        for (int i = 0; i < 64; i++)
        {
            if (((blockerMask << 63 - i) >> 63) != 0)
            {
                l++;
                indices.Add(i);
            }
        }
        ulong[] combinations = new ulong[(int)Math.Pow(2, l)];

        // for each combination
        for (ulong i = 0; i < (ulong)combinations.Length; i++)
        {
            ulong combination = 0;

            // for each index in the mask, push the bits of the combination to the right indices 
            for (int j = 0; j < l; j++)
            {
                combination ^= ((i << 63 - j) >> 63) << indices[j];
            }

            combinations[i] = combination;
        }

        return combinations;
    }
    
    public static List<ulong> Combinations(ulong blockerMask, int limit)
    {
        List<int> allIndices = new List<int>();
        int l = 0;
        for (int i = 0; i < 64; i++)
        {
            if (((blockerMask << 63 - i) >> 63) != 0)
            {
                l++;
                allIndices.Add(i);
            }
        }
        
        List<ulong> combinations = new();

        foreach (ulong i in GetValidCombinations(l, Math.Min(l, limit)))
        {
            ulong combination = 0;

            // for each index in the mask, push the bits of the combination to the right indices 
            for (int j = 0; j < l; j++)
            {
                combination ^= ((i << 63 - j) >> 63) << allIndices[j];
            }
            combinations.Add(combination);
        }
        
        return combinations;
    }
    
    public static IEnumerable<ulong> GetValidCombinations(int max, int limit)
    {
        if (max < 0 || max > 64)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be between 0 and 64 inclusive.");

        if (limit < 0 || limit > max)
            throw new ArgumentOutOfRangeException(nameof(limit), "limit must be between 0 and max inclusive.");
        
        for (int ones = 0; ones <= limit; ones++)
        {
            foreach (ulong combination in GenerateBitCombinations(max, ones))
            {
                yield return combination;
            }
        }
    }

    // Generates all ulong numbers with exactly 'ones' bits set within 'max' bit positions
    private static IEnumerable<ulong> GenerateBitCombinations(int max, int ones)
    {
        if (ones == 0)
        {
            yield return 0;
            yield break;
        }

        int[] indices = new int[ones];
        for (int i = 0; i < ones; i++)
            indices[i] = i;

        while (indices[0] <= max - ones)
        {
            // Build ulong from bit indices
            ulong value = 0;
            foreach (int index in indices)
            {
                value |= 1UL << index;
            }

            yield return value;

            // Generate next combination
            int pos = ones - 1;
            while (pos >= 0 && indices[pos] == max - ones + pos)
                pos--;

            if (pos < 0)
                break;

            indices[pos]++;
            for (int i = pos + 1; i < ones; i++)
                indices[i] = indices[i - 1] + 1;
        }
    }
    
    public static ulong GetMoveBitboards(ulong blockers, (int file, int rank) pos, ulong piece)
    {
        ulong moves = 0;

        (int file, int rank)[] pattern = piece == Pieces.WhiteRook ? RookPattern : BishopPattern;

        for (int i = 0; i < 4; i++) // for each pattern
        {
            for (int j = 1; j < 8; j++) // in each direction
            {
                (int file, int rank) target = (pos.file + pattern[i].file * j, pos.rank + pattern[i].rank * j);
                
                if (!ValidSquare(target.file, target.rank)) // if the square is outside the bounds of the board
                    break;
                if ((blockers & GetSquare(target)) == 0) // if the targeted square is empty
                    moves |= GetSquare(target);
                else
                {
                    moves |= GetSquare(target);
                    break;
                }
            }
        }
        
        return moves;
    }

    public static ulong GetAttackLines((int file, int rank) pos, ulong squares)
    {
        ulong result = 0;
        
        for (int file = 0; file < 8; file++)
        for (int rank = 0; rank < 8; rank++)
            if ((squares & GetSquare(file, rank)) != 0)
                result |= Bitboards.PathLookup[pos.file, pos.rank, file, rank] & ~GetSquare(pos);
        
        return result;
    }
    
    public static ulong GetPinLine(ulong blockers, (int file, int rank) pos, ulong piece)
    {
        ulong final = 0;
        
        (int file, int rank)[] pattern = piece == Pieces.WhiteRook ? RookPattern : BishopPattern;
        
        for (int i = 0; i < 4; i++) // for each pattern
        {
            bool blockerFound = false;
            for (int j = 1; j < 8; j++) // in each direction
            {
                (int file, int rank) target = (pos.file + pattern[i].file * j, pos.rank + pattern[i].rank * j);
                
                if (!ValidSquare(target.file, target.rank)) // if the square is outside the bounds of the board
                    break;
                if ((blockers & GetSquare(target)) != 0) // if the targeted square is occupied
                {
                    blockerFound = true;
                    break;
                }
            }
            
            if (!blockerFound)
                continue;
            
            for (int j = 1; j < 8; j++) // in each direction
            {
                (int file, int rank) target = (pos.file + pattern[i].file * j, pos.rank + pattern[i].rank * j);
                
                if (!ValidSquare(target.file, target.rank)) // if the square is outside the bounds of the board
                    break;
                if ((blockers & GetSquare(target)) == 0) // if the targeted square is empty
                    final |= GetSquare(target);
                else
                {
                    final |= GetSquare(target);
                    break;
                }
            }
        }
        
        return final;
    }
    
    public static ulong[] GetSingleBits(ulong mask)
    {
        List<ulong> bits = new();
        for (int rank = 0; rank < 8; rank++)
        for (int file = 0; file < 8; file++)
            if ((mask & GetSquare(file, rank)) != 0) // square occupied on the mask
                bits.Add(GetSquare(file, rank));
        return bits.ToArray();
    }
    
    private static readonly (int file, int rank)[] RookPattern =
    [
        (0, 1),
        (0, -1),
        (1, 0),
        (-1, 0),
    ];
    private static readonly (int file, int rank)[] BishopPattern =
    [
        (1, 1),
        (1, -1),
        (-1, 1),
        (-1, -1),
    ];

    public static readonly (int file, int rank)[] KnightPattern =
    [
        (2, 1),
        (2, -1),
        (-2, 1),
        (-2, -1),
        (1, 2),
        (1, -2),
        (-1, 2),
        (-1, -2),
    ];
    
    public static ulong GetMask((int file, int rank) pos, (int file, int rank)[] pattern)
    {
        ulong mask = 0;
        
        for (int i = 0; i < pattern.Length; i++) // for each pattern
        {
            (int file, int rank) target = (pos.file + pattern[i].file, pos.rank + pattern[i].rank);

            if (ValidSquare(target.file, target.rank))
                mask |= GetSquare(target);
        }
        
        return mask;
    }

    public static ulong GetMoveBitboard(Move[] moveList)
    {
        ulong bitboard = 0;

        foreach (Move move in moveList)
        {
            bitboard |= GetSquare(move.Destination);
        }
        
        return bitboard;
    }

    public static ulong GetPossibleEnPassantSquare(int file, int side)
    {
        return side == 0 ? Bitboards.WhitePossibleEnPassant << file : Bitboards.BlackPossibleEnPassant << file;
    }
    
    private const ulong Square = 0x8000000000000000;
    public static ulong GetSquare(int file, int rank) // overload that takes individual values
    {
        return Square >> (rank * 8 + 7 - file);
    }
    public static ulong GetSquare((int file, int rank) square) // overload that takes individual values
    {
        return Square >> (square.rank * 8 + 7 - square.file);
    }

    public static ulong GetFile(int file)
    {
        return Bitboards.File >> (7 - file);
    }

    public static ulong GetRank(int rank)
    {
        return Bitboards.Rank >> (8 * rank);
    }

    public static ulong GetWhitePassedPawnMask(int file, int rank)
    {
        return Bitboards.PassedPawnMasks[file] >> (rank * 8 + 8);
    }
    public static ulong GetBlackPassedPawnMask(int file, int rank)
    {
        return Bitboards.PassedPawnMasks[file] << ((8 - rank) * 8);
    }

    public static bool ValidSquare(int file, int rank)
    {
        return file is >= 0 and < 8 && rank is >= 0 and < 8;
    }
}