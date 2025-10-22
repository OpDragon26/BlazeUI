using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazeUI.Blaze;

public static class Bitboards
{
    /*
    The magic lookup returns a span of moves to be copied into the move array and its lenght, and a bitboard with squares that are captures, but might land on a friendly piece
    The returned moves all land on empty squares, while the bitboard shows moves that land on occupied squares. 
    Select the enemy pieces from those captured using the AND operation and a second magic lookup is initiated using that bitboard, which returns another span of moves
    */

    public static readonly ulong[,] RookMasks = new ulong[8,8];
    public static readonly ulong[,] BishopMasks = new ulong[8,8];

    private static readonly ulong[,][] RookBlockers = new ulong[8,8][];
    private static readonly ulong[,][] BishopBlockers = new ulong[8,8][];
    private static readonly (Move[] moves, ulong captures)[,][] RookMoves = new (Move[] moves, ulong captures)[8,8][];
    private static readonly (Move[] moves, ulong captures)[,][] BishopMoves = new (Move[] moves, ulong captures)[8,8][];
    private static readonly ulong[,][] RookCaptureCombinations = new ulong[8,8][]; // for each square, for all blockers each combination
    private static readonly ulong[,][] BishopCaptureCombinations = new ulong[8,8][];
    
    public static readonly ulong[,] KnightMasks = new ulong[8,8];
    private static readonly ulong[,][] KnightCombinations = new ulong[8,8][];
    public static readonly ulong[,] KingMasks = new ulong[8,8];
    private static readonly ulong[,][] KingCombinations = new ulong[8,8][];
    
    public static readonly ulong[,] WhitePawnMoveMasks = new ulong[8,8];
    private static readonly ulong[,][] WhitePawnMoveCombinations = new ulong[8,8][];
    public static readonly ulong[,] BlackPawnMoveMasks = new ulong[8,8];
    private static readonly ulong[,][] BlackPawnMoveCombinations = new ulong[8,8][];
    public static readonly ulong[,] WhitePawnCaptureMasks = new ulong[8,8];
    private static readonly ulong[,][] WhitePawnCaptureCombinations = new ulong[8,8][];
    public static readonly ulong[,] BlackPawnCaptureMasks = new ulong[8,8];
    private static readonly ulong[,][] BlackPawnCaptureCombinations = new ulong[8,8][];
    
    public static readonly ulong[,] SmallRookMasks = new ulong[8,8];
    public static readonly ulong[,] SmallBishopMasks = new ulong[8,8];
    private static readonly ulong[,][] SmallRookCombinations = new ulong[8,8][];
    private static readonly ulong[,][] SmallBishopCombinations = new ulong[8,8][];
    private static readonly ulong[,][] SmallRookBitboards = new ulong[8,8][];
    private static readonly ulong[,][] SmallBishopBitboards = new ulong[8,8][];
    
    private static readonly ulong[,][] BlockCaptures = new ulong[8,8][];
    private static ulong[]? BlockMoves;
    
    private const ulong Frame = 0xFF818181818181FF;

    public const ulong BlackPossibleEnPassant = 0x100000000;
    public const ulong WhitePossibleEnPassant = 0x1000000;
    
    private static ulong[]? EnPassantMasks; // contains both the source and the destination
    
    public static readonly ulong WhiteShortCastleMask = 0x6000000000000000;
    public static readonly ulong WhiteLongCastleMask = 0xC00000000000000;
    public static readonly ulong BlackShortCastleMask = 0x60;
    public static readonly ulong BlackLongCastleMask = 0xC;
    public static readonly Move WhiteShortCastle = new((4,0), (6,0), type: 0b0010, priority: 6);
    public static readonly Move WhiteLongCastle = new((4,0), (2,0), type: 0b0011, priority: 3);
    public static readonly Move BlackShortCastle = new((4,7), (6,7), type: 0b1010, priority: 6);
    public static readonly Move BlackLongCastle = new((4,7), (2,7), type: 0b1011, priority: 3);
    
    public static readonly ulong[] PassedPawnMasks = new ulong[8];
    public static readonly ulong[] NeighbourMasks = new ulong[8];
    public static readonly ulong[,,,] PathLookup =  new ulong[8,8,8,8];
    
    public const ulong RightPawns = 0xf0f0f0f0f0f000;
    public const ulong LeftPawns = 0xf0f0f0f0f0f00;
    public const ulong CenterPawns = 0x3c3c3c3c3c3c00;
    public const ulong LeftPawnMask = 0x7070707070700;
    public const ulong RightPawnMask = 0xe0e0e0e0e0e000;
    public const ulong CenterPawnMask = 0x18181818181800;

    public const ulong FirstSlice =  0xffff;
    public const ulong SecondSlice = 0xffff0000;
    public const ulong ThirdSlice =  0xffff00000000;
    public const ulong FourthSlice = 0xffff000000000000;
    
    public static readonly int[,] PriorityWeights =
    {
        {0,1,2,3,3,2,1,0},
        {1,2,3,4,4,3,2,1},
        {2,3,4,5,5,4,3,2},
        {3,4,5,6,6,5,4,3},
        {3,4,5,6,6,5,4,3},
        {2,3,4,5,5,4,3,2},
        {1,2,3,4,4,3,2,1},
        {0,1,2,3,3,2,1,0},
    };

    public static readonly int[][] AdjacentFiles = [[0,1], [0,1,2], [1,2,3], [2,3,4], [3,4,5], [4,5,6], [5,6,7], [6,7]];
    
    public static class MagicLookupArrays
    {
        public static readonly (ulong magicNumber, int push, int highest)[,] RookMove = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] BishopMove = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] RookCapture = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] BishopCapture = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] KnightMove = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] KingMove = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] WhitePawnMove = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] BlackPawnMove = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] WhitePawnCapture = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] BlackPawnCapture = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] RookBitboardNumbers = new (ulong magicNumber, int push, int highest)[8,8];
        public static readonly (ulong magicNumber, int push, int highest)[,] BishopBitboardNumbers = new (ulong magicNumber, int push, int highest)[8,8];
        public static (ulong magicNumber, int push, int highest) EnPassantNumbers;
        public static readonly (ulong magicNumber, int push, int highest)[,] BlockCaptureNumbers = new (ulong magicNumber, int push, int highest)[8,8];
        public static (ulong magicNumber, int push, int highest) BlockMoveNumber;
        public static (ulong magicNumber, int push, int highest) AttackLineNumber;
        
        public static (ulong magicNumber, int push, int highest) RightPawnEvalNumber;
        public static (ulong magicNumber, int push, int highest) LeftPawnEvalNumber;
        public static (ulong magicNumber, int push, int highest) CenterPawnEvalNumber;
        
        public static readonly (Move[] moves, ulong captures)[,][] RookLookup = new (Move[] moves, ulong captures)[8,8][];
        public static readonly (Move[] moves, ulong captures)[,][] BishopLookup = new (Move[] moves, ulong captures)[8,8][];
        public static readonly ulong[,][] RookLookupCapturesArray = new ulong[8,8][];
        public static readonly ulong[,][] BishopLookupCapturesArray = new ulong[8,8][];
        public static readonly Move[,][][] RookCaptureLookup = new Move[8,8][][];
        public static readonly Move[,][][] BishopCaptureLookup = new Move[8,8][][];
        public static readonly Move[,][][] KnightLookup = new Move[8,8][][];
        public static readonly Move[,][][] KnightCaptureLookup = new Move[8,8][][];
        public static readonly Move[,][][] KingLookup = new Move[8,8][][];
        public static readonly Move[,][][] KingCaptureLookup = new Move[8,8][][];
        public static readonly Move[,][][] WhitePawnLookup = new Move[8,8][][];
        public static readonly Move[,][][] BlackPawnLookup = new Move[8,8][][];
        public static readonly Move[,][][] WhitePawnCaptureLookup = new Move[8,8][][];
        public static readonly Move[,][][] BlackPawnCaptureLookup = new Move[8,8][][];
        public static readonly ulong[,][] RookBitboardLookup = new ulong[8,8][];
        public static readonly ulong[,][] BishopBitboardLookup = new ulong[8,8][];
        public static readonly int[,][] RookMobilityLookupArray = new int[8,8][];
        public static readonly int[,][] BishopMobilityLookupArray = new int[8,8][];
        public static readonly int[,] KnightMobilityLookup = new int[8,8];
        public static Move[] EnPassantLookupArray = [];
        public static readonly int[,][] KingSafetyLookup = new int[8,8][];
        public static readonly Move[,][] BlockCaptureMoveLookup = new Move[8,8][];
        public static readonly Move[,][][] BlockMoveLookup = new Move[8,8][][];
        public static readonly Move[,][][] BlockCaptureMovePawnLookup = new Move[8,8][][];
        public static readonly Move[,][][] BlockMovePawnLookup = new Move[8,8][][];
        public static readonly ulong[,][] AttackLineLookup = new ulong[8,8][];
        
        public static readonly ulong[,][] RookPinLineBitboardLookup =  new ulong[8,8][];
        public static readonly ulong[,][] BishopPinLineBitboardLookup = new ulong[8,8][];
        public static readonly List<BitboardUtils.PinSearchResult>[,][] RookPinLookup = new List<BitboardUtils.PinSearchResult>[8,8][];
        public static readonly List<BitboardUtils.PinSearchResult>[,][] BishopPinLookup = new List<BitboardUtils.PinSearchResult>[8,8][];

        public static Evaluation.PawnEvaluation[] RightPawnEvalLookup = [];
        public static Evaluation.PawnEvaluation[] LeftPawnEvalLookup = [];
        public static Evaluation.PawnEvaluation[] CenterPawnEvalLookup = [];
        
        public static Evaluation.RookEvaluation[] FirstRookEvaluationLookup = [];
        public static Evaluation.RookEvaluation[] SecondRookEvaluationLookup = [];
        public static Evaluation.RookEvaluation[] ThirdRookEvaluationLookup = [];
        public static Evaluation.RookEvaluation[] FourthRookEvaluationLookup = [];
        
        public static Evaluation.QueenEvaluation[] FirstQueenEvaluationLookup = [];
        public static Evaluation.QueenEvaluation[] SecondQueenEvaluationLookup = [];
        public static Evaluation.QueenEvaluation[] ThirdQueenEvaluationLookup = [];
        public static Evaluation.QueenEvaluation[] FourthQueenEvaluationLookup = [];
        
        public static Evaluation.KnightEvaluation[] FirstKnightEvaluationLookup = [];
        public static Evaluation.KnightEvaluation[] SecondKnightEvaluationLookup = [];
        public static Evaluation.KnightEvaluation[] ThirdKnightEvaluationLookup = [];
        public static Evaluation.KnightEvaluation[] FourthKnightEvaluationLookup = [];
        
        public static Evaluation.BishopEvaluation[] FirstBishopEvaluationLookup = [];
        public static Evaluation.BishopEvaluation[] SecondBishopEvaluationLookup = [];
        public static Evaluation.BishopEvaluation[] ThirdBishopEvaluationLookup = [];
        public static Evaluation.BishopEvaluation[] FourthBishopEvaluationLookup = [];
        public static readonly Evaluation.KingEvaluation[,] KingEvaluationLookup = new Evaluation.KingEvaluation[8,8];
    }

    public const ulong File = 0x8080808080808080;
    public const ulong Rank = 0xFF00000000000000;

    private const ulong UpDiagonal = 0x102040810204080;
    private const ulong DownDiagonal = 0x8040201008040201;

    private const ulong SmallFile = 0x80808080808000;
    private const ulong SmallRank = 0x7E00000000000000;

    public const ulong KingSafetyAppliesWhite = 0xC7C7000000000000; 
    public const ulong KingSafetyAppliesBlack = 0xC7C7;

    private static bool init;
    private static bool inProgress;

    public static void Init()
    {
        if (init) return;
        List<ulong> enPassantBitboards = new List<ulong>();
        List<ulong> blockMoveList = new();
        Timer t = new Timer();
        t.Start();
        
        Console.WriteLine("Initializing magic bitboards. This should take approximately 20 seconds");
        
        // Create the masks for every square on the board
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                // The last bit also has to be evaluated in every direction, since it matters whether it's blocked or not
                RookMasks[file, rank] = (Rank >> (rank * 8)) ^ (File >> (7 - file));
                RookBlockers[file, rank] = BitboardUtils.Combinations(RookMasks[file, rank]);
                RookMoves[file, rank] = new (Move[] moves, ulong captures)[RookBlockers[file, rank].Length];
                
                List<ulong> rCombinations = new List<ulong>();
                for (int i = 0; i < RookBlockers[file, rank].Length; i++) // for every blocker combination
                {
                    RookMoves[file, rank][i] = BitboardUtils.GetMoves(RookBlockers[file, rank][i], (file, rank), Pieces.WhiteRook);
                    rCombinations.AddRange(BitboardUtils.Combinations(RookMoves[file, rank][i].captures));
                }
                RookCaptureCombinations[file, rank] = rCombinations.Distinct().ToArray();
                //RookCaptureCombinations[file, rank] = rCombinations.ToArray();
                //Console.WriteLine(RookCaptureCombinations[file, rank].Length);
                
                // bishop masks
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
                
                BishopMasks[file, rank] = relativeUD ^ relativeDD;
                BishopBlockers[file, rank] = BitboardUtils.Combinations(BishopMasks[file, rank]);
                BishopMoves[file, rank] = new (Move[] moves, ulong captures)[BishopBlockers[file, rank].Length];
                
                List<ulong> bCombinations = new List<ulong>();
                for (int i = 0; i < BishopBlockers[file, rank].Length; i++)
                {
                    BishopMoves[file, rank][i] = BitboardUtils.GetMoves(BishopBlockers[file, rank][i], (file, rank), Pieces.WhiteBishop);
                    bCombinations.AddRange(BitboardUtils.Combinations(BishopMoves[file, rank][i].captures));
                }
                BishopCaptureCombinations[file, rank] = bCombinations.Distinct().ToArray();
                
                SmallRookMasks[file, rank] = ((SmallRank >> (rank * 8)) ^ (SmallFile >> (7 - file))) & ~BitboardUtils.GetSquare(file, rank);
                SmallRookCombinations[file, rank] = BitboardUtils.Combinations(SmallRookMasks[file, rank]);
                SmallRookBitboards[file, rank] = new ulong[SmallRookCombinations[file, rank].Length];
                for (int i = 0; i < SmallRookCombinations[file, rank].Length; i++)
                    SmallRookBitboards[file, rank][i] = BitboardUtils.GetMoveBitboards(SmallRookCombinations[file, rank][i], (file, rank), Pieces.WhiteRook);
                
                SmallBishopMasks[file, rank] = (relativeUD ^ relativeDD) & ~Frame;
                SmallBishopCombinations[file, rank] = BitboardUtils.Combinations(SmallBishopMasks[file, rank]);
                SmallBishopBitboards[file, rank] = new ulong[SmallBishopCombinations[file, rank].Length];
                for (int i = 0; i < SmallBishopCombinations[file, rank].Length; i++)
                    SmallBishopBitboards[file, rank][i] = BitboardUtils.GetMoveBitboards(SmallBishopCombinations[file, rank][i], (file, rank), Pieces.WhiteBishop);
                
                // knight masks
                KnightMasks[file, rank] = BitboardUtils.GetMask((file, rank), BitboardUtils.KnightPattern);
                MagicLookupArrays.KnightMobilityLookup[file, rank] = (int)(ulong.PopCount(KnightMasks[file, rank]) * Weights.MobilityMultiplier) * 3;
                KnightCombinations[file, rank] = BitboardUtils.Combinations(KnightMasks[file, rank]);
                
                // king masks
                ulong kingMask = ulong.MaxValue;
                        
                for (int k = 0; k < 8; k++)
                {
                    if (!(k == file || k == file - 1 || k == file + 1))
                    {
                        kingMask &= ~(File >> (7 - k));
                    }
                            
                    if (!(k == rank || k == rank - 1 || k == rank + 1))
                    {
                        kingMask &= ~(Rank >> (k * 8));
                    }
                }
                
                kingMask &= ~BitboardUtils.GetSquare(file, rank);
                
                KingMasks[file, rank] = kingMask;
                KingCombinations[file, rank] = BitboardUtils.Combinations(kingMask);
                
                // blocking checks
                
                // captures
                BlockCaptures[file, rank] = BitboardUtils.GetSingleBits(RookMasks[file, rank] | BishopMasks[file, rank] | KnightMasks[file, rank]);
                
                // regular moves
                blockMoveList.AddRange(BitboardUtils.Combinations(relativeUD, 3));
                blockMoveList.AddRange(BitboardUtils.Combinations(relativeDD, 3));
                blockMoveList.AddRange(BitboardUtils.Combinations(Rank >> (rank * 8), 3));
                blockMoveList.AddRange(BitboardUtils.Combinations(File >> (7 - file), 3));
                blockMoveList.AddRange(BitboardUtils.Combinations(KnightMasks[file, rank], 3));
                
                // pawn moves
                
                // white pawns
                ulong wpmMask = 0;
                ulong wpcMask = 0;
                wpmMask |= BitboardUtils.GetSquare(file, rank + 1);
                if (BitboardUtils.ValidSquare(file + 1, rank + 1)) wpcMask |= BitboardUtils.GetSquare(file + 1, rank + 1);
                if (BitboardUtils.ValidSquare(file - 1, rank + 1)) wpcMask |= BitboardUtils.GetSquare(file - 1, rank + 1);
                
                if (rank == 1) wpmMask |= BitboardUtils.GetSquare(file, rank + 2);
                
                WhitePawnMoveMasks[file, rank] = wpmMask;
                WhitePawnMoveCombinations[file, rank] = BitboardUtils.Combinations(wpmMask);
                WhitePawnCaptureMasks[file, rank] = wpcMask;
                WhitePawnCaptureCombinations[file, rank] = BitboardUtils.Combinations(wpcMask);
                if (rank == 4) // white en passant rank
                {
                    if (BitboardUtils.ValidSquare(file + 1, 5)) enPassantBitboards.Add(BitboardUtils.GetSquare(file, rank) | BitboardUtils.GetSquare(file + 1, 5));
                    if (BitboardUtils.ValidSquare(file - 1, 5)) enPassantBitboards.Add(BitboardUtils.GetSquare(file, rank) | BitboardUtils.GetSquare(file - 1, 5));
                }
                
                // black pawns
                ulong bpmMask = 0;
                ulong bpcMask = 0;
                bpmMask |= BitboardUtils.GetSquare(file, rank - 1);
                if (BitboardUtils.ValidSquare(file + 1, rank - 1)) bpcMask |= BitboardUtils.GetSquare(file + 1, rank - 1);
                if (BitboardUtils.ValidSquare(file - 1, rank - 1)) bpcMask |= BitboardUtils.GetSquare(file - 1, rank - 1);
                
                if (rank == 6) bpmMask |= BitboardUtils.GetSquare(file, rank - 2);
                
                BlackPawnMoveMasks[file, rank] = bpmMask;
                BlackPawnMoveCombinations[file, rank] = BitboardUtils.Combinations(bpmMask);
                BlackPawnCaptureMasks[file, rank] = bpcMask;
                BlackPawnCaptureCombinations[file, rank] = BitboardUtils.Combinations(bpcMask);
                
                if (rank == 3) // black en passant rank
                {
                    if (BitboardUtils.ValidSquare(file + 1, 2)) enPassantBitboards.Add(BitboardUtils.GetSquare(file, rank) | BitboardUtils.GetSquare(file + 1, 2));
                    if (BitboardUtils.ValidSquare(file - 1, 2)) enPassantBitboards.Add(BitboardUtils.GetSquare(file, rank) | BitboardUtils.GetSquare(file - 1, 2));
                }
                
                if (rank != 0) // only needs to be checked once per file
                    continue;
                
                // passed files
                // triple files, used to check for passed pawns

                ulong passedMask = ulong.MaxValue;
                
                for (int k = 0; k < 8; k++)
                {
                    if (!(k == file || k == file - 1 || k == file + 1))
                    {
                        passedMask &= ~(File >> (7 - k));
                    }
                }
                
                PassedPawnMasks[file] = passedMask;
                
                ulong neighborMask = ulong.MaxValue;
                
                for (int k = 0; k < 8; k++)
                {
                    if (!(k == file - 1 || k == file + 1))
                    {
                        neighborMask &= ~(File >> (7 - k));
                    }
                }
                
                NeighbourMasks[file] = neighborMask;
            }
        }

        BlockMoves = blockMoveList.Distinct().ToArray();
        MagicLookupArrays.BlockMoveNumber = (4154364917966041783, 46, 262133); //MagicNumbers.GenerateRepeat(BlockMoves, 1, 46);
        EnPassantMasks = enPassantBitboards.ToArray();
        MagicLookupArrays.EnPassantNumbers = (15417481889308385644, 58, 63); // MagicNumbers.GenerateRepeat(EnPassantMasks, 10000);
        MagicLookupArrays.EnPassantLookupArray = new Move[MagicLookupArrays.EnPassantNumbers.highest + 1];
        foreach (ulong mask in EnPassantMasks) // for each possible en passant
        {
            MagicLookupArrays.EnPassantLookupArray[(mask * MagicLookupArrays.EnPassantNumbers.magicNumber) >> MagicLookupArrays.EnPassantNumbers.push] = BitboardUtils.GetEnPassantMoves(mask);
        }
        
        // pawn eval combinations
        List<ulong> rightPawns = BitboardUtils.Combinations(RightPawns, 8);
        List<ulong> leftPawns = BitboardUtils.Combinations(LeftPawns, 8);
        List<ulong> centerPawns = BitboardUtils.Combinations(CenterPawns, 8);
        
        MagicLookupArrays.RightPawnEvalNumber = (17067507152026048335, 37, 134217725); // MagicNumbers.GenerateMagicNumberParallel(rightPawns.Distinct().ToArray(),37 ,7, false);
        MagicLookupArrays.LeftPawnEvalNumber = (615594976254142229, 37, 134217609); // MagicNumbers.GenerateMagicNumberParallel(leftPawns.Distinct().ToArray(), 37, 7, false);
        MagicLookupArrays.CenterPawnEvalNumber = (15570990422680516493, 37, 134217566); // MagicNumbers.GenerateMagicNumberParallel(centerPawns.Distinct().ToArray(), 37, 7, false);
        
        MagicLookupArrays.RightPawnEvalLookup = new Evaluation.PawnEvaluation[MagicLookupArrays.RightPawnEvalNumber.highest+ 1];
        MagicLookupArrays.LeftPawnEvalLookup = new Evaluation.PawnEvaluation[MagicLookupArrays.LeftPawnEvalNumber.highest + 1];
        MagicLookupArrays.CenterPawnEvalLookup = new Evaluation.PawnEvaluation[MagicLookupArrays.CenterPawnEvalNumber.highest + 1];

        Parallel.For(0, 3, e =>
        {
            switch (e)
            {
                case 0:
                    foreach (ulong combination in rightPawns)
                        MagicLookupArrays.RightPawnEvalLookup[(combination * MagicLookupArrays.RightPawnEvalNumber.magicNumber) >> MagicLookupArrays.RightPawnEvalNumber.push] = 
                            Evaluation.GeneratePawnEval(combination, Evaluation.Section.Right);
                    break;
                case 1:
                    foreach (ulong combination in leftPawns)
                        MagicLookupArrays.LeftPawnEvalLookup[(combination * MagicLookupArrays.LeftPawnEvalNumber.magicNumber) >> MagicLookupArrays.LeftPawnEvalNumber.push] = 
                            Evaluation.GeneratePawnEval(combination, Evaluation.Section.Left);
                    break;
                case 2:
                    foreach (ulong combination in centerPawns)
                        MagicLookupArrays.CenterPawnEvalLookup[(combination * MagicLookupArrays.CenterPawnEvalNumber.magicNumber) >> MagicLookupArrays.CenterPawnEvalNumber.push] = 
                            Evaluation.GeneratePawnEval(combination, Evaluation.Section.Center);
                    break;
            }
        });
        
        List<ulong> firstSlice = BitboardUtils.Combinations(FirstSlice, 9);
        List<ulong> secondSlice = BitboardUtils.Combinations(SecondSlice, 9);
        List<ulong> thirdSlice = BitboardUtils.Combinations(ThirdSlice, 9);
        List<ulong> fourthSlice = BitboardUtils.Combinations(FourthSlice, 9);
        
        MagicLookupArrays.FirstRookEvaluationLookup = new Evaluation.RookEvaluation[firstSlice.Max() + 1];
        MagicLookupArrays.SecondRookEvaluationLookup = new Evaluation.RookEvaluation[secondSlice.Max(n => n >> 16) + 1];
        MagicLookupArrays.ThirdRookEvaluationLookup = new Evaluation.RookEvaluation[thirdSlice.Max(n => n >> 32) + 1];
        MagicLookupArrays.FourthRookEvaluationLookup = new Evaluation.RookEvaluation[fourthSlice.Max(n => n >> 48) + 1];
        
        MagicLookupArrays.FirstQueenEvaluationLookup = new Evaluation.QueenEvaluation[firstSlice.Max() + 1];
        MagicLookupArrays.SecondQueenEvaluationLookup = new Evaluation.QueenEvaluation[secondSlice.Max(n => n >> 16) + 1];
        MagicLookupArrays.ThirdQueenEvaluationLookup = new Evaluation.QueenEvaluation[thirdSlice.Max(n => n >> 32) + 1];
        MagicLookupArrays.FourthQueenEvaluationLookup = new Evaluation.QueenEvaluation[fourthSlice.Max(n => n >> 48) + 1];
        
        MagicLookupArrays.FirstKnightEvaluationLookup = new Evaluation.KnightEvaluation[firstSlice.Max() + 1];
        MagicLookupArrays.SecondKnightEvaluationLookup = new Evaluation.KnightEvaluation[secondSlice.Max(n => n >> 16) + 1];
        MagicLookupArrays.ThirdKnightEvaluationLookup = new Evaluation.KnightEvaluation[thirdSlice.Max(n => n >> 32) + 1];
        MagicLookupArrays.FourthKnightEvaluationLookup = new Evaluation.KnightEvaluation[fourthSlice.Max(n => n >> 48) + 1];
        
        MagicLookupArrays.FirstBishopEvaluationLookup = new Evaluation.BishopEvaluation[firstSlice.Max() + 1];
        MagicLookupArrays.SecondBishopEvaluationLookup = new Evaluation.BishopEvaluation[secondSlice.Max(n => n >> 16) + 1];
        MagicLookupArrays.ThirdBishopEvaluationLookup = new Evaluation.BishopEvaluation[thirdSlice.Max(n => n >> 32) + 1];
        MagicLookupArrays.FourthBishopEvaluationLookup = new Evaluation.BishopEvaluation[fourthSlice.Max(n => n >> 48) + 1];
        
        Parallel.For(0, 4, e =>
        {
            switch (e)
            {
                case 0:
                    foreach (ulong combination in firstSlice)
                    {
                        MagicLookupArrays.FirstRookEvaluationLookup[combination] = Evaluation.GenerateRookEval(combination, Evaluation.Slice.First);
                        MagicLookupArrays.FirstQueenEvaluationLookup[combination] = Evaluation.GenerateStandardEval<Evaluation.QueenEvaluation>(combination, Evaluation.Slice.First, Pieces.WhiteQueen, Pieces.BlackQueen);
                        MagicLookupArrays.FirstKnightEvaluationLookup[combination] = Evaluation.GenerateStandardEval<Evaluation.KnightEvaluation>(combination, Evaluation.Slice.First, Pieces.WhiteKnight, Pieces.BlackKnight);
                        MagicLookupArrays.FirstBishopEvaluationLookup[combination] = Evaluation.GenerateStandardEval<Evaluation.BishopEvaluation>(combination, Evaluation.Slice.First, Pieces.WhiteBishop, Pieces.BlackBishop);
                    }

                    break;
                case 1:
                    foreach (ulong combination in secondSlice)
                    {
                        MagicLookupArrays.SecondRookEvaluationLookup[combination >> 16] = Evaluation.GenerateRookEval(combination, Evaluation.Slice.Second);
                        MagicLookupArrays.SecondQueenEvaluationLookup[combination >> 16] = Evaluation.GenerateStandardEval<Evaluation.QueenEvaluation>(combination, Evaluation.Slice.Second, Pieces.WhiteQueen, Pieces.BlackQueen);
                        MagicLookupArrays.SecondKnightEvaluationLookup[combination >> 16] = Evaluation.GenerateStandardEval<Evaluation.KnightEvaluation>(combination, Evaluation.Slice.Second, Pieces.WhiteKnight, Pieces.BlackKnight);
                        MagicLookupArrays.SecondBishopEvaluationLookup[combination >> 16] = Evaluation.GenerateStandardEval<Evaluation.BishopEvaluation>(combination, Evaluation.Slice.Second, Pieces.WhiteBishop, Pieces.BlackBishop);
                    }

                    break;
                case 2:
                    foreach (ulong combination in thirdSlice)
                    {
                        MagicLookupArrays.ThirdRookEvaluationLookup[combination >> 32] = Evaluation.GenerateRookEval(combination, Evaluation.Slice.Third);
                        MagicLookupArrays.ThirdQueenEvaluationLookup[combination >> 32] = Evaluation.GenerateStandardEval<Evaluation.QueenEvaluation>(combination, Evaluation.Slice.Third, Pieces.WhiteQueen, Pieces.BlackQueen);
                        MagicLookupArrays.ThirdKnightEvaluationLookup[combination >> 32] = Evaluation.GenerateStandardEval<Evaluation.KnightEvaluation>(combination, Evaluation.Slice.Third, Pieces.WhiteKnight, Pieces.BlackKnight);
                        MagicLookupArrays.ThirdBishopEvaluationLookup[combination >> 32] = Evaluation.GenerateStandardEval<Evaluation.BishopEvaluation>(combination, Evaluation.Slice.Third, Pieces.WhiteBishop, Pieces.BlackBishop);
                    }
                    break;
                case 3:
                    foreach (ulong combination in fourthSlice)
                    {
                        MagicLookupArrays.FourthRookEvaluationLookup[combination >> 48] = Evaluation.GenerateRookEval(combination, Evaluation.Slice.Fourth);
                        MagicLookupArrays.FourthQueenEvaluationLookup[combination >> 48] = Evaluation.GenerateStandardEval<Evaluation.QueenEvaluation>(combination, Evaluation.Slice.Fourth, Pieces.WhiteQueen, Pieces.BlackQueen);
                        MagicLookupArrays.FourthKnightEvaluationLookup[combination >> 48] = Evaluation.GenerateStandardEval<Evaluation.KnightEvaluation>(combination, Evaluation.Slice.Fourth, Pieces.WhiteKnight, Pieces.BlackKnight);
                        MagicLookupArrays.FourthBishopEvaluationLookup[combination >> 48] = Evaluation.GenerateStandardEval<Evaluation.BishopEvaluation>(combination, Evaluation.Slice.Fourth, Pieces.WhiteBishop, Pieces.BlackBishop);
                    }
                    break;
            }
        });
        
        // attack line lookup
        ulong[] attackLines = BitboardUtils.GetValidCombinations(64, 2).ToArray();
        MagicLookupArrays.AttackLineNumber = (8710915622236860111, 48, 65530); //MagicNumbers.GenerateRepeat(attackLines.Distinct().ToArray(), 1);
        
        //Console.WriteLine("Generating Magic Numbers");
        
        //int done = 0;
        // create magic numbers and add to lookup
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
            {
                // attack line lookup
                MagicLookupArrays.AttackLineLookup[file, rank] = new ulong[MagicLookupArrays.AttackLineNumber.highest + 1];
                foreach (ulong line in attackLines)
                {
                    MagicLookupArrays.AttackLineLookup[file, rank][(line * MagicLookupArrays.AttackLineNumber.magicNumber) >>MagicLookupArrays.AttackLineNumber.push] = 
                        BitboardUtils.GetAttackLines((file, rank), line);
                }
                
                // rook numbers
                MagicLookupArrays.RookMove[file, rank] = MagicNumbers.RookNumbers[file, rank];
                MagicLookupArrays.RookLookup[file, rank] = new (Move[] moves, ulong captures)[MagicLookupArrays.RookMove[file, rank].highest + 1];
                MagicLookupArrays.RookLookupCapturesArray[file, rank] = new ulong[MagicLookupArrays.RookMove[file, rank].highest + 1];
                
                for (int i = 0; i < RookBlockers[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.RookLookup[file, rank][(RookBlockers[file, rank][i] * MagicLookupArrays.RookMove[file, rank].magicNumber) >> MagicLookupArrays.RookMove[file, rank].push] = RookMoves[file, rank][i];
                    MagicLookupArrays.RookLookupCapturesArray[file, rank][(RookBlockers[file, rank][i] * MagicLookupArrays.RookMove[file, rank].magicNumber) >> MagicLookupArrays.RookMove[file, rank].push] = RookMoves[file, rank][i].captures;
                }
                
                // bishop numbers
                MagicLookupArrays.BishopMove[file, rank] = MagicNumbers.BishopNumbers[file, rank];
                MagicLookupArrays.BishopLookup[file, rank] = new (Move[] moves, ulong captures)[MagicLookupArrays.BishopMove[file, rank].highest + 1];
                MagicLookupArrays.BishopLookupCapturesArray[file, rank] = new ulong[MagicLookupArrays.BishopMove[file, rank].highest + 1];
                
                for (int i = 0; i < BishopBlockers[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.BishopLookup[file, rank][(BishopBlockers[file, rank][i] * MagicLookupArrays.BishopMove[file, rank].magicNumber) >> MagicLookupArrays.BishopMove[file, rank].push] = BishopMoves[file, rank][i];
                    MagicLookupArrays.BishopLookupCapturesArray[file, rank][(BishopBlockers[file, rank][i] * MagicLookupArrays.BishopMove[file, rank].magicNumber) >> MagicLookupArrays.BishopMove[file, rank].push] = BishopMoves[file, rank][i].captures;
                }
                
                // rook captures
                MagicLookupArrays.RookCapture[file, rank] = MagicNumbers.RookCaptureNumbers[file, rank]; // MagicNumbers.GenerateRepeat(RookCaptureCombinations[file, rank], 1000);
                MagicLookupArrays.RookCaptureLookup[file, rank] = new Move[MagicLookupArrays.RookCapture[file, rank].highest + 1][];
                
                for (int i = 0; i < RookCaptureCombinations[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.RookCaptureLookup[file, rank][(RookCaptureCombinations[file, rank][i] * MagicLookupArrays.RookCapture[file, rank].magicNumber) >> MagicLookupArrays.RookCapture[file, rank].push] = BitboardUtils.GetBitboardMoves(RookCaptureCombinations[file, rank][i], (file, rank), 50, capture: true);
                }
                
                // bishop captures
                MagicLookupArrays.BishopCapture[file, rank] = MagicNumbers.BishopCaptureNumbers[file, rank]; // MagicNumbers.GenerateRepeat(BishopCaptureCombinations[file, rank], 1000);
                MagicLookupArrays.BishopCaptureLookup[file, rank] = new Move[MagicLookupArrays.BishopCapture[file, rank].highest + 1][];
                
                for (int i = 0; i < BishopCaptureCombinations[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.BishopCaptureLookup[file, rank][(BishopCaptureCombinations[file, rank][i] * MagicLookupArrays.BishopCapture[file, rank].magicNumber) >> MagicLookupArrays.BishopCapture[file, rank].push] = BitboardUtils.GetBitboardMoves(BishopCaptureCombinations[file, rank][i], (file, rank), 50, capture: true);
                }
                
                MagicLookupArrays.RookBitboardNumbers[file, rank] = MagicNumbers.RookBitboardNumbers[file, rank];
                MagicLookupArrays.RookBitboardLookup[file, rank] = new ulong[MagicLookupArrays.RookBitboardNumbers[file, rank].highest + 1];
                MagicLookupArrays.RookMobilityLookupArray[file, rank] = new int[MagicLookupArrays.RookBitboardNumbers[file, rank].highest + 1];

                for (int i = 0; i < SmallRookCombinations[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.RookBitboardLookup[file, rank][(SmallRookCombinations[file, rank][i] * MagicLookupArrays.RookBitboardNumbers[file, rank].magicNumber) >> MagicLookupArrays.RookBitboardNumbers[file, rank].push] = SmallRookBitboards[file, rank][i];
                    MagicLookupArrays.RookMobilityLookupArray[file, rank][(SmallRookCombinations[file, rank][i] * MagicLookupArrays.RookBitboardNumbers[file, rank].magicNumber) >> MagicLookupArrays.RookBitboardNumbers[file, rank].push] = (int)(ulong.PopCount(SmallRookBitboards[file, rank][i]) * Weights.MobilityMultiplier);
                }
                
                MagicLookupArrays.BishopBitboardNumbers[file, rank] = MagicNumbers.BishopBitboardNumbers[file, rank];
                MagicLookupArrays.BishopBitboardLookup[file, rank] = new ulong[MagicLookupArrays.BishopBitboardNumbers[file, rank].highest + 1];
                MagicLookupArrays.BishopMobilityLookupArray[file, rank] = new int[MagicLookupArrays.BishopBitboardNumbers[file, rank].highest + 1];
                
                for (int i = 0; i < SmallBishopCombinations[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.BishopBitboardLookup[file, rank][(SmallBishopCombinations[file, rank][i] * MagicLookupArrays.BishopBitboardNumbers[file, rank].magicNumber) >> MagicLookupArrays.BishopBitboardNumbers[file, rank].push] = SmallBishopBitboards[file, rank][i];
                    MagicLookupArrays.BishopMobilityLookupArray[file, rank][(SmallBishopCombinations[file, rank][i] * MagicLookupArrays.BishopBitboardNumbers[file, rank].magicNumber) >> MagicLookupArrays.BishopBitboardNumbers[file, rank].push] = (int)(ulong.PopCount(SmallBishopBitboards[file, rank][i]) * Weights.MobilityMultiplier);
                }
                
                // knight moves
                // since the potential captures and moves are based on the same combinations, the same magic numbers can be used
                MagicLookupArrays.KnightMove[file, rank] = MagicNumbers.KnightNumbers[file, rank];
                MagicLookupArrays.KnightLookup[file, rank] = new Move[MagicLookupArrays.KnightMove[file, rank].highest + 1][];
                MagicLookupArrays.KnightCaptureLookup[file, rank] = new Move[MagicLookupArrays.KnightMove[file, rank].highest + 1][];
                
                for (int i = 0; i < KnightCombinations[file, rank].Length; i++) // for each combination
                {
                    MagicLookupArrays.KnightLookup[file, rank][(KnightCombinations[file, rank][i] * MagicLookupArrays.KnightMove[file, rank].magicNumber) >> MagicLookupArrays.KnightMove[file, rank].push] = BitboardUtils.GetBitboardMoves(KnightCombinations[file, rank][i], (file, rank), 5);
                    MagicLookupArrays.KnightCaptureLookup[file, rank][(KnightCombinations[file, rank][i] * MagicLookupArrays.KnightMove[file, rank].magicNumber) >> MagicLookupArrays.KnightMove[file, rank].push] = BitboardUtils.GetBitboardMoves(KnightCombinations[file, rank][i], (file, rank), 50, capture: true);
                }
                
                // king moves
                MagicLookupArrays.KingMove[file, rank] = MagicNumbers.KingNumbers[file, rank]; // MagicNumbers.GenerateRepeat(KingCombinations[file, rank], 5000);
                MagicLookupArrays.KingLookup[file, rank] = new Move[MagicLookupArrays.KingMove[file, rank].highest + 1][];
                MagicLookupArrays.KingCaptureLookup[file, rank] = new Move[MagicLookupArrays.KingMove[file, rank].highest + 1][];
                MagicLookupArrays.KingSafetyLookup[file, rank] = new int[MagicLookupArrays.KingMove[file, rank].highest + 1];
                
                for (int i = 0; i < KingCombinations[file, rank].Length; i++) // for each combination
                {
                    MagicLookupArrays.KingLookup[file, rank][(KingCombinations[file, rank][i] * MagicLookupArrays.KingMove[file, rank].magicNumber) >> MagicLookupArrays.KingMove[file, rank].push] = BitboardUtils.GetBitboardMoves(KingCombinations[file, rank][i], (file, rank), 5);
                    MagicLookupArrays.KingCaptureLookup[file, rank][(KingCombinations[file, rank][i] * MagicLookupArrays.KingMove[file, rank].magicNumber) >> MagicLookupArrays.KingMove[file, rank].push] = BitboardUtils.GetBitboardMoves(KingCombinations[file, rank][i], (file, rank), 3,  capture: true);
                    MagicLookupArrays.KingSafetyLookup[file, rank][(KingCombinations[file, rank][i] * MagicLookupArrays.KingMove[file, rank].magicNumber) >> MagicLookupArrays.KingMove[file, rank].push] = Weights.KingSafetyBonuses[UInt64.PopCount(KingCombinations[file, rank][i])];
                }
                
                // pin lines
                // rook pin lines
                MagicLookupArrays.RookPinLineBitboardLookup[file, rank] = new ulong[MagicLookupArrays.RookMove[file, rank].highest + 1];
                
                for (int i = 0; i < RookBlockers[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.RookPinLineBitboardLookup[file, rank][(RookBlockers[file, rank][i] * MagicLookupArrays.RookMove[file, rank].magicNumber) >> MagicLookupArrays.RookMove[file, rank].push] = BitboardUtils.GetPinLine(RookBlockers[file, rank][i], (file, rank), Pieces.WhiteRook);
                }
                
                // bishop pin lines
                MagicLookupArrays.BishopPinLineBitboardLookup[file, rank] = new ulong[MagicLookupArrays.BishopMove[file, rank].highest + 1];
                
                for (int i = 0; i < BishopBlockers[file, rank].Length; i++) // for each blocker
                {
                    MagicLookupArrays.BishopPinLineBitboardLookup[file, rank][(BishopBlockers[file, rank][i] * MagicLookupArrays.BishopMove[file, rank].magicNumber) >> MagicLookupArrays.BishopMove[file, rank].push] = BitboardUtils.GetPinLine(BishopBlockers[file, rank][i], (file, rank), Pieces.WhiteBishop);
                }
                
                // pin search
                MagicLookupArrays.RookPinLookup[file,rank] = new List<BitboardUtils.PinSearchResult>[MagicLookupArrays.RookMove[file, rank].highest + 1];

                for (int i = 0; i < RookBlockers[file, rank].Length; i++)
                {
                    MagicLookupArrays.RookPinLookup[file, rank][(RookBlockers[file, rank][i] * MagicLookupArrays.RookMove[file, rank].magicNumber) >> MagicLookupArrays.RookMove[file, rank].push] = BitboardUtils.GeneratePinResult((file, rank), RookBlockers[file, rank][i], Pieces.WhiteRook);
                }
                
                MagicLookupArrays.BishopPinLookup[file, rank] = new List<BitboardUtils.PinSearchResult>[MagicLookupArrays.BishopMove[file, rank].highest + 1];

                for (int i = 0; i < BishopBlockers[file, rank].Length; i++)
                {
                    MagicLookupArrays.BishopPinLookup[file, rank][(BishopBlockers[file, rank][i] * MagicLookupArrays.BishopMove[file, rank].magicNumber) >> MagicLookupArrays.BishopMove[file, rank].push] = BitboardUtils.GeneratePinResult((file, rank), BishopBlockers[file, rank][i], Pieces.WhiteBishop);
                }
                
                // blocking checks
                // block captures
                MagicLookupArrays.BlockCaptureNumbers[file, rank] = MagicNumbers.BlockCaptureNumbers[file, rank]; //MagicNumbers.GenerateRepeat(BlockCaptures[file, rank], 10000);
                MagicLookupArrays.BlockCaptureMoveLookup[file, rank] = new Move[MagicLookupArrays.BlockCaptureNumbers[file, rank].highest + 1];
                MagicLookupArrays.BlockCaptureMovePawnLookup[file, rank] = new Move[MagicLookupArrays.BlockCaptureNumbers[file, rank].highest + 1][];
                
                for (int i = 0; i < BlockCaptures[file, rank].Length; i++)
                {
                    MagicLookupArrays.BlockCaptureMoveLookup[file, rank][(BlockCaptures[file, rank][i] * MagicLookupArrays.BlockCaptureNumbers[file, rank].magicNumber) >> MagicLookupArrays.BlockCaptureNumbers[file, rank].push] = BitboardUtils.GetBitboardMoves(BlockCaptures[file, rank][i], (file, rank), 25)[0];
                    if (rank != 0 && rank != 7) 
                        MagicLookupArrays.BlockCaptureMovePawnLookup[file, rank][(BlockCaptures[file, rank][i] * MagicLookupArrays.BlockCaptureNumbers[file, rank].magicNumber) >> MagicLookupArrays.BlockCaptureNumbers[file, rank].push] = BitboardUtils.GetBitboardMoves(BlockCaptures[file, rank][i], (file, rank), 25, pawn: true,  capture: true);
                }
                
                // block moves
                MagicLookupArrays.BlockMoveLookup[file, rank] = new Move[MagicLookupArrays.BlockMoveNumber.highest + 1][];
                MagicLookupArrays.BlockMovePawnLookup[file, rank] = new Move[MagicLookupArrays.BlockMoveNumber.highest + 1][];
                
                foreach (ulong move in BlockMoves)
                {
                    MagicLookupArrays.BlockMoveLookup[file, rank][(move * MagicLookupArrays.BlockMoveNumber.magicNumber) >> MagicLookupArrays.BlockMoveNumber.push] = BitboardUtils.GetBitboardMoves(move, (file, rank), 5);
                    if (rank != 0 && rank != 7) 
                        MagicLookupArrays.BlockMovePawnLookup[file, rank][(move * MagicLookupArrays.BlockMoveNumber.magicNumber) >> MagicLookupArrays.BlockMoveNumber.push] = BitboardUtils.GetBitboardMoves(move, (file, rank), 5, pawn: true);
                }

                MagicLookupArrays.KingEvaluationLookup[file, rank] = new Evaluation.KingEvaluation
                {
                    wEval = (int)(Pieces.Value[Pieces.WhiteKing] * Weights.MaterialMultiplier) + Weights.Pieces[Pieces.WhiteKing, file, rank],
                    bEval = (int)(Pieces.Value[Pieces.BlackKing] * Weights.MaterialMultiplier) - Weights.Pieces[Pieces.WhiteKing, file, 7-rank],
                    wEvalEndgame = (int)(Pieces.Value[Pieces.WhiteKing] * Weights.MaterialMultiplier) + Weights.EndgamePieces[Pieces.WhiteKing, file, rank],
                    bEvalEndgame = (int)(Pieces.Value[Pieces.BlackKing] * Weights.MaterialMultiplier) - Weights.EndgamePieces[Pieces.WhiteKing, file, 7-rank],
                };
                
                //Console.WriteLine($"Square done {++done}/64");
                // pawn moves
                if (rank == 0 || rank == 7)
                    continue;
                
                // white pawns
                // moves
                MagicLookupArrays.WhitePawnMove[file, rank] = MagicNumbers.WhitePawnMoveNumbers[file, rank];
                MagicLookupArrays.WhitePawnLookup[file, rank] = new Move[MagicLookupArrays.WhitePawnMove[file, rank].highest + 1][];

                for (int i = 0; i < WhitePawnMoveCombinations[file, rank].Length; i++) // for each combination
                {
                    MagicLookupArrays.WhitePawnLookup[file, rank][(WhitePawnMoveCombinations[file, rank][i] * MagicLookupArrays.WhitePawnMove[file, rank].magicNumber) >> MagicLookupArrays.WhitePawnMove[file, rank].push] = BitboardUtils.GetPawnMoves(WhitePawnMoveCombinations[file, rank][i], (file, rank), 0);
                }
                // captures
                MagicLookupArrays.WhitePawnCapture[file, rank] = MagicNumbers.WhiteCaptureMoveNumbers[file, rank];
                MagicLookupArrays.WhitePawnCaptureLookup[file, rank] = new Move[MagicLookupArrays.WhitePawnCapture[file, rank].highest + 1][];

                for (int i = 0; i < WhitePawnCaptureCombinations[file, rank].Length; i++) // for each combination
                {
                    MagicLookupArrays.WhitePawnCaptureLookup[file, rank][(WhitePawnCaptureCombinations[file, rank][i] * MagicLookupArrays.WhitePawnCapture[file, rank].magicNumber) >> MagicLookupArrays.WhitePawnCapture[file, rank].push] = BitboardUtils.GetPawnCaptures(WhitePawnCaptureCombinations[file, rank][i], (file, rank), 0);
                }
                
                // black pawns
                // moves
                MagicLookupArrays.BlackPawnMove[file, rank] = MagicNumbers.BlackPawnMoveNumbers[file, rank];
                MagicLookupArrays.BlackPawnLookup[file, rank] = new Move[MagicLookupArrays.BlackPawnMove[file, rank].highest + 1][];

                for (int i = 0; i < BlackPawnMoveCombinations[file, rank].Length; i++) // for each combination
                {
                    MagicLookupArrays.BlackPawnLookup[file, rank][(BlackPawnMoveCombinations[file, rank][i] * MagicLookupArrays.BlackPawnMove[file, rank].magicNumber) >> MagicLookupArrays.BlackPawnMove[file, rank].push] = BitboardUtils.GetPawnMoves(BlackPawnMoveCombinations[file, rank][i], (file, rank), 1);
                }
                // captures
                MagicLookupArrays.BlackPawnCapture[file, rank] = MagicNumbers.BlackCaptureMoveNumbers[file, rank];
                MagicLookupArrays.BlackPawnCaptureLookup[file, rank] = new Move[MagicLookupArrays.BlackPawnCapture[file, rank].highest + 1][];

                for (int i = 0; i < BlackPawnCaptureCombinations[file, rank].Length; i++) // for each combination
                {
                    MagicLookupArrays.BlackPawnCaptureLookup[file, rank][(BlackPawnCaptureCombinations[file, rank][i] * MagicLookupArrays.BlackPawnCapture[file, rank].magicNumber) >> MagicLookupArrays.BlackPawnCapture[file, rank].push] = BitboardUtils.GetPawnCaptures(BlackPawnCaptureCombinations[file, rank][i], (file, rank), 1);
                }
            }
        }
        
        // init pathfinder
        for (int startRank = 0; startRank < 8; startRank++)
        for (int startFile = 0; startFile < 8; startFile++)
        for (int endRank = 0; endRank < 8; endRank++)
        for (int endFile = 0; endFile < 8; endFile++)
        {
            if (startRank == endRank && startFile == endFile)
            {
                PathLookup[startFile, startRank, endFile, endRank] = 0;
                continue;
            }
            
            ulong path = 0;
            
            if (endFile == startFile) // both are from the same file
            {
                int current = startRank;
                int moveBy = startRank < endRank ? 1 : -1;
                do
                {
                    path |= BitboardUtils.GetSquare(startFile, current);
                    current += moveBy;
                } while (current != endRank);
            }
            
            else if (endRank == startRank) // both are from the same file
            {
                int current = startFile;
                int moveBy = startFile < endFile ? 1 : -1;
                do
                {
                    path |= BitboardUtils.GetSquare(current, startRank);
                    current += moveBy;
                } while (current != endFile);
            }
            
            else if (startFile - startRank == endFile - endRank) // on the same up diagonal
            {
                int currentFile = startFile;
                int currentRank = startRank;
                (int file, int rank) moveBy = startRank < endRank ? (1, 1) : (-1, -1);
                do
                {
                    path |= BitboardUtils.GetSquare(currentFile, currentRank);
                    currentFile += moveBy.file;
                    currentRank += moveBy.rank;
                } while ((currentFile, currentRank) != (endFile, endRank));
            }
            
            else if ((7 - startFile) - startRank == (7 - endFile) - endRank) // on the same down diagonal
            {
                int currentFile = startFile;
                int currentRank = startRank;
                (int file, int rank) moveBy = startRank < endRank ? (-1, 1) : (1, -1);
                do
                {
                    path |= BitboardUtils.GetSquare(currentFile, currentRank);
                    currentFile += moveBy.file;
                    currentRank += moveBy.rank;
                } while ((currentFile, currentRank) != (endFile, endRank));
            }

            // in an L shape
            if (path != 0 || (Math.Abs(startFile - endFile) == 1 && Math.Abs(startRank - endRank) == 2) || (Math.Abs(startFile - endFile) == 2 && Math.Abs(startRank - endRank) == 1)) 
                path |= BitboardUtils.GetSquare(startFile, startRank) | BitboardUtils.GetSquare(endFile, endRank);
            
            PathLookup[startFile, startRank, endFile, endRank] = path;
        }

        Console.WriteLine($"Bitboards initialized in {t.Stop()}ms");
        init = true;
    }

    public static void StartInit()
    {
        if (inProgress)
            return;
        Thread t = new Thread(() =>
        {
            inProgress = true;
            Init();
            inProgress = false;
        });
        t.Start();
    }

    private static void WaitForFinish()
    {
        while (inProgress)
        {
            Thread.Sleep(10);
        }
    }

    public static bool Poll()
    {
        return init;
    }
}