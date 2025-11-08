using System.Collections.Generic;
using System.Linq;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;
using BlazeUI.Blaze.Utils;

namespace BlazeUI.Blaze.Magic_Lookup;
using static BitboardUtils;
using static Masks;
public static class Combinations
{
    public static readonly ulong[,][] RookBlockers = new ulong[8,8][];
    public static readonly ulong[,][] BishopBlockers = new ulong[8,8][];
    public static readonly (Move[] moves, ulong captures)[,][] RookMoves = new (Move[] moves, ulong captures)[8,8][];
    public static readonly (Move[] moves, ulong captures)[,][] BishopMoves = new (Move[] moves, ulong captures)[8,8][];
    public static readonly ulong[,][] RookCaptureCombinations = new ulong[8,8][]; // for each square, for all blockers each combination
    public static readonly ulong[,][] BishopCaptureCombinations = new ulong[8,8][];
    
    public static readonly ulong[,][] KnightCombinations = new ulong[8,8][];
    public static readonly ulong[,][] KingCombinations = new ulong[8,8][];
    
    public static readonly ulong[,][] WhitePawnMoveCombinations = new ulong[8,8][];
    public static readonly ulong[,][] BlackPawnMoveCombinations = new ulong[8,8][];
    public static readonly ulong[,][] WhitePawnCaptureCombinations = new ulong[8,8][];
    public static readonly ulong[,][] BlackPawnCaptureCombinations = new ulong[8,8][];
    
    public static readonly ulong[,][] SmallRookCombinations = new ulong[8,8][];
    public static readonly ulong[,][] SmallBishopCombinations = new ulong[8,8][];
    public static readonly ulong[,][] SmallRookBitboards = new ulong[8,8][];
    public static readonly ulong[,][] SmallBishopBitboards = new ulong[8,8][];

    public static readonly ulong[,][] BlockCaptures = new ulong[8,8][];
    public static ulong[]? BlockMoves;
    
    public static void Init()
    {
        List<ulong> blockMoveList = new();
        
        for (int file = 0; file < 8; file++)
        for (int rank = 0; rank < 8; rank++)
        {
            RookBlockers[file, rank] = Combinations(RookMasks[file, rank]);
            RookMoves[file, rank] = new (Move[] moves, ulong captures)[RookBlockers[file, rank].Length];
                
            List<ulong> rCombinations = new List<ulong>();
            for (int i = 0; i < RookBlockers[file, rank].Length; i++) // for every blocker combination
            {
                RookMoves[file, rank][i] = GetMoves(RookBlockers[file, rank][i], (file, rank), Pieces.WhiteRook);
                rCombinations.AddRange(Combinations(RookMoves[file, rank][i].captures));
            }
            RookCaptureCombinations[file, rank] = rCombinations.Distinct().ToArray();
            
            BishopBlockers[file, rank] = Combinations(BishopMasks[file, rank]);
            BishopMoves[file, rank] = new (Move[] moves, ulong captures)[BishopBlockers[file, rank].Length];
                
            List<ulong> bCombinations = new List<ulong>();
            for (int i = 0; i < BishopBlockers[file, rank].Length; i++)
            {
                BishopMoves[file, rank][i] = GetMoves(BishopBlockers[file, rank][i], (file, rank), Pieces.WhiteBishop);
                bCombinations.AddRange(Combinations(BishopMoves[file, rank][i].captures));
            }
            BishopCaptureCombinations[file, rank] = bCombinations.Distinct().ToArray();
            
            SmallRookCombinations[file, rank] = Combinations(SmallRookMasks[file, rank]);
            SmallRookBitboards[file, rank] = new ulong[SmallRookCombinations[file, rank].Length];
            for (int i = 0; i < SmallRookCombinations[file, rank].Length; i++)
                SmallRookBitboards[file, rank][i] = GetMoveBitboards(SmallRookCombinations[file, rank][i], (file, rank), Pieces.WhiteRook);
                
            SmallBishopCombinations[file, rank] = Combinations(SmallBishopMasks[file, rank]);
            SmallBishopBitboards[file, rank] = new ulong[SmallBishopCombinations[file, rank].Length];
            for (int i = 0; i < SmallBishopCombinations[file, rank].Length; i++)
                SmallBishopBitboards[file, rank][i] = GetMoveBitboards(SmallBishopCombinations[file, rank][i], (file, rank), Pieces.WhiteBishop);
            
            KnightCombinations[file, rank] = Combinations(KnightMasks[file, rank]);
            KingCombinations[file, rank] = Combinations(KingMasks[file, rank]);
                
            // pawn moves
            WhitePawnMoveCombinations[file, rank] = Combinations(WhitePawnMoveMasks[file, rank]);
            WhitePawnCaptureCombinations[file, rank] = Combinations(WhitePawnCaptureMasks[file, rank]);
                
            BlackPawnMoveCombinations[file, rank] = Combinations(BlackPawnMoveMasks[file, rank]);
            BlackPawnCaptureCombinations[file, rank] = Combinations(BlackPawnCaptureMasks[file, rank]);
            
            // blocking checks
            // captures
            BlockCaptures[file, rank] = GetSingleBits(RookMasks[file, rank] | BishopMasks[file, rank] | KnightMasks[file, rank]);
                
            // regular moves
            ulong relativeUD = UpDiagonal;
            ulong relativeDD = DownDiagonal;
                
            int UDPush = rank - file;
            int DDPush = rank + file - 7;
                
            if (UDPush >= 0)
                relativeUD >>= UDPush * 8;
            else // negative
                relativeUD <<= -UDPush * 8;
            if (DDPush >= 0)
                relativeDD >>= DDPush * 8;
            else
                relativeDD <<= -DDPush * 8;
                
            blockMoveList.AddRange(Combinations(relativeUD, 3));
            blockMoveList.AddRange(Combinations(relativeDD, 3));
            blockMoveList.AddRange(Combinations(Rank >> (rank * 8), 3));
            blockMoveList.AddRange(Combinations(File >> (7 - file), 3));
            blockMoveList.AddRange(Combinations(KnightMasks[file, rank], 3));
        }
        
        BlockMoves = blockMoveList.Distinct().ToArray();
    }
}