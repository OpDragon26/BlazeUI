using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazeUI.Blaze.Magic_Lookup;
using Board_Representation;
using Move_Generation;
using Utils;
using static Utils.BitboardUtils;
using static Masks;
public static class Bitboards
{
    /*
    The magic lookup returns a span of moves to be copied into the move array and its lenght, and a bitboard with squares that are captures, but might land on a friendly piece
    The returned moves all land on empty squares, while the bitboard shows moves that land on occupied squares. 
    Select the enemy pieces from those captured using the AND operation and a second magic lookup is initiated using that bitboard, which returns another span of moves
    */
    
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
    
    private static readonly ulong[,][] WhitePawnMoveCombinations = new ulong[8,8][];
    private static readonly ulong[,][] BlackPawnMoveCombinations = new ulong[8,8][];
    private static readonly ulong[,][] WhitePawnCaptureCombinations = new ulong[8,8][];
    private static readonly ulong[,][] BlackPawnCaptureCombinations = new ulong[8,8][];
    
    private static readonly ulong[,][] SmallRookCombinations = new ulong[8,8][];
    private static readonly ulong[,][] SmallBishopCombinations = new ulong[8,8][];
    public static readonly ulong[,][] SmallRookBitboards = new ulong[8,8][];
    public static readonly ulong[,][] SmallBishopBitboards = new ulong[8,8][];
    
    private static readonly ulong[,][] BlockCaptures = new ulong[8,8][];
    private static ulong[]? BlockMoves;
    
    private static ulong[]? EnPassantMasks; // contains both the source and the destination
    
    public static readonly Move WhiteShortCastle = new((4,0), (6,0), type: 0b0010, priority: 6);
    public static readonly Move WhiteLongCastle = new((4,0), (2,0), type: 0b0011, priority: 3);
    public static readonly Move BlackShortCastle = new((4,7), (6,7), type: 0b1010, priority: 6);
    public static readonly Move BlackLongCastle = new((4,7), (2,7), type: 0b1011, priority: 3);
    
    public static readonly ulong[,,,] PathLookup =  new ulong[8,8,8,8];
    
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
    
    public static class Lookup
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
        public static Move[] EnPassantLookupArray = [];
        public static readonly int[,][] KingSafetyLookup = new int[8,8][];
        public static readonly Move[,][] BlockCaptureMoveLookup = new Move[8,8][];
        public static readonly Move[,][][] BlockMoveLookup = new Move[8,8][][];
        public static readonly Move[,][][] BlockCaptureMovePawnLookup = new Move[8,8][][];
        public static readonly Move[,][][] BlockMovePawnLookup = new Move[8,8][][];
        public static readonly ulong[,][] AttackLineLookup = new ulong[8,8][];
        
        public static readonly ulong[,][] RookPinLineBitboardLookup =  new ulong[8,8][];
        public static readonly ulong[,][] BishopPinLineBitboardLookup = new ulong[8,8][];
        public static readonly List<PinSearchResult>[,][] RookPinLookup = new List<PinSearchResult>[8,8][];
        public static readonly List<PinSearchResult>[,][] BishopPinLookup = new List<PinSearchResult>[8,8][];
    }

    public static bool begunInit;
    public static bool init;
    private static bool inProgress;

    public static void Init(General.CompletionPoint progress)
    {
        if (begunInit) return;
        begunInit = true;
        List<ulong> enPassantBitboards = new List<ulong>();
        List<ulong> blockMoveList = new();
        General.Timer t = new General.Timer();
        t.Start();
        
        //Console.WriteLine("Initializing magic bitboards");
        progress.Set(0, "Generating masks...");
        Masks.Init();
        // Create the masks for every square on the board
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 7; file >= 0; file--)
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
                //RookCaptureCombinations[file, rank] = rCombinations.ToArray();
                //Console.WriteLine(RookCaptureCombinations[file, rank].Length);
                
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
                
                // knight masks
                KnightMasks[file, rank] = GetMask((file, rank), KnightPattern);
                KnightCombinations[file, rank] = Combinations(KnightMasks[file, rank]);
                
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
                
                kingMask &= ~GetSquare(file, rank);
                
                KingMasks[file, rank] = kingMask;
                KingCombinations[file, rank] = Combinations(kingMask);
                
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
                
                // pawn moves
                
                WhitePawnMoveCombinations[file, rank] = Combinations(WhitePawnMoveMasks[file, rank]);
                WhitePawnCaptureCombinations[file, rank] = Combinations(WhitePawnCaptureMasks[file, rank]);
                
                BlackPawnMoveCombinations[file, rank] = Combinations(BlackPawnMoveMasks[file, rank]);
                BlackPawnCaptureCombinations[file, rank] = Combinations(BlackPawnCaptureMasks[file, rank]);
                
                if (rank == 4) // white en passant rank
                {
                    if (ValidSquare(file + 1, 5)) enPassantBitboards.Add(GetSquare(file, rank) | GetSquare(file + 1, 5));
                    if (ValidSquare(file - 1, 5)) enPassantBitboards.Add(GetSquare(file, rank) | GetSquare(file - 1, 5));
                }
                
                if (rank == 3) // black en passant rank
                {
                    if (ValidSquare(file + 1, 2)) enPassantBitboards.Add(GetSquare(file, rank) | GetSquare(file + 1, 2));
                    if (ValidSquare(file - 1, 2)) enPassantBitboards.Add(GetSquare(file, rank) | GetSquare(file - 1, 2));
                }
                
                if (rank != 0) // only needs to be checked once per file
                    continue;
            }
        }
        
        progress.Set(30, "Generating block moves...");
        
        BlockMoves = blockMoveList.Distinct().ToArray();
        Lookup.BlockMoveNumber = (4154364917966041783, 46, 262133); //MagicNumbers.GenerateRepeat(BlockMoves, 1, 46);
        EnPassantMasks = enPassantBitboards.ToArray();
        Lookup.EnPassantNumbers = (15417481889308385644, 58, 63); // MagicNumbers.GenerateRepeat(EnPassantMasks, 10000);
        Lookup.EnPassantLookupArray = new Move[Lookup.EnPassantNumbers.highest + 1];
        foreach (ulong mask in EnPassantMasks) // for each possible en passant
        {
            Lookup.EnPassantLookupArray[(mask * Lookup.EnPassantNumbers.magicNumber) >> Lookup.EnPassantNumbers.push] = GetEnPassantMoves(mask);
        }

        
        // attack line lookup
        ulong[] attackLines = GetValidCombinations(64, 2).ToArray();
        Lookup.AttackLineNumber = (8710915622236860111, 48, 65530); //MagicNumbers.GenerateRepeat(attackLines.Distinct().ToArray(), 1);
        
        //Console.WriteLine("Generating Magic Numbers");
        progress.Set(45, "Loading magic lookup...");
        //int done = 0;
        // create magic numbers and add to lookup
        Parallel.For(0, 8, rank =>
        {
            Parallel.For(0, 8, file =>
            {
                // attack line lookup
                Lookup.AttackLineLookup[file, rank] =
                    new ulong[Lookup.AttackLineNumber.highest + 1];
                foreach (ulong line in attackLines)
                {
                    Lookup.AttackLineLookup[file, rank]
                            [(line * Lookup.AttackLineNumber.magicNumber) >> Lookup.AttackLineNumber.push] = GetAttackLines((file, rank), line);
                }

                // rook numbers
                Lookup.RookMove[file, rank] = MagicNumbers.RookNumbers[file, rank];
                Lookup.RookLookup[file, rank] = new (Move[] moves, ulong captures)[Lookup.RookMove[file, rank].highest + 1];
                Lookup.RookLookupCapturesArray[file, rank] = new ulong[Lookup.RookMove[file, rank].highest + 1];

                for (int i = 0; i < RookBlockers[file, rank].Length; i++) // for each blocker
                {
                    Lookup.RookLookup[file, rank]
                        [(RookBlockers[file, rank][i] * Lookup.RookMove[file, rank].magicNumber) >> Lookup.RookMove[file, rank].push] = RookMoves[file, rank][i];
                    Lookup.RookLookupCapturesArray[file, rank]
                        [(RookBlockers[file, rank][i] * Lookup.RookMove[file, rank].magicNumber) >> Lookup.RookMove[file, rank].push] = RookMoves[file, rank][i].captures;
                }

                // bishop numbers
                Lookup.BishopMove[file, rank] = MagicNumbers.BishopNumbers[file, rank];
                Lookup.BishopLookup[file, rank] = new (Move[] moves, ulong captures)[Lookup.BishopMove[file, rank].highest + 1];
                Lookup.BishopLookupCapturesArray[file, rank] = new ulong[Lookup.BishopMove[file, rank].highest + 1];

                for (int i = 0; i < BishopBlockers[file, rank].Length; i++) // for each blocker
                {
                    Lookup.BishopLookup[file, rank]
                        [(BishopBlockers[file, rank][i] * Lookup.BishopMove[file, rank].magicNumber) >> Lookup.BishopMove[file, rank].push] = BishopMoves[file, rank][i];
                    Lookup.BishopLookupCapturesArray[file, rank]
                            [(BishopBlockers[file, rank][i] * Lookup.BishopMove[file, rank].magicNumber) >> Lookup.BishopMove[file, rank].push] = BishopMoves[file, rank][i].captures;
                }

                // rook captures
                Lookup.RookCapture[file, rank] = MagicNumbers.RookCaptureNumbers[file, rank]; // MagicNumbers.GenerateRepeat(RookCaptureCombinations[file, rank], 1000);
                Lookup.RookCaptureLookup[file, rank] = new Move[Lookup.RookCapture[file, rank].highest + 1][];

                for (int i = 0; i < RookCaptureCombinations[file, rank].Length; i++) // for each blocker
                {
                    Lookup.RookCaptureLookup[file, rank]
                        [(RookCaptureCombinations[file, rank][i] * Lookup.RookCapture[file, rank].magicNumber) >> Lookup.RookCapture[file, rank].push] = GetBitboardMoves(RookCaptureCombinations[file, rank][i], (file, rank), 50, capture: true);
                }

                // bishop captures
                Lookup.BishopCapture[file, rank] = MagicNumbers.BishopCaptureNumbers[file, rank]; // MagicNumbers.GenerateRepeat(BishopCaptureCombinations[file, rank], 1000);
                Lookup.BishopCaptureLookup[file, rank] = new Move[Lookup.BishopCapture[file, rank].highest + 1][];

                for (int i = 0; i < BishopCaptureCombinations[file, rank].Length; i++) // for each blocker
                {
                    Lookup.BishopCaptureLookup[file, rank][(BishopCaptureCombinations[file, rank][i] * Lookup.BishopCapture[file, rank].magicNumber) >> Lookup.BishopCapture[file, rank].push] =
                        GetBitboardMoves(BishopCaptureCombinations[file, rank][i], (file, rank), 50, capture: true);
                }

                Lookup.RookBitboardNumbers[file, rank] = MagicNumbers.RookBitboardNumbers[file, rank];
                Lookup.RookBitboardLookup[file, rank] = new ulong[Lookup.RookBitboardNumbers[file, rank].highest + 1];
                Lookup.RookMobilityLookupArray[file, rank] = new int[Lookup.RookBitboardNumbers[file, rank].highest + 1];

                for (int i = 0; i < SmallRookCombinations[file, rank].Length; i++) // for each blocker
                {
                    Lookup.RookBitboardLookup[file, rank][(SmallRookCombinations[file, rank][i] * Lookup.RookBitboardNumbers[file, rank].magicNumber) >> Lookup.RookBitboardNumbers[file, rank].push] = 
                        SmallRookBitboards[file, rank][i];
                    Lookup.RookMobilityLookupArray[file, rank][(SmallRookCombinations[file, rank][i] * Lookup.RookBitboardNumbers[file, rank].magicNumber) >> Lookup.RookBitboardNumbers[file, rank].push] = 
                        EvalUtils.EvaluateRookMobility(file, rank, i);
                }

                Lookup.BishopBitboardNumbers[file, rank] = MagicNumbers.BishopBitboardNumbers[file, rank];
                Lookup.BishopBitboardLookup[file, rank] = new ulong[Lookup.BishopBitboardNumbers[file, rank].highest + 1];
                Lookup.BishopMobilityLookupArray[file, rank] = new int[Lookup.BishopBitboardNumbers[file, rank].highest + 1];

                for (int i = 0; i < SmallBishopCombinations[file, rank].Length; i++) // for each blocker
                {
                    Lookup.BishopBitboardLookup[file, rank][(SmallBishopCombinations[file, rank][i] * Lookup.BishopBitboardNumbers[file, rank].magicNumber) >> Lookup.BishopBitboardNumbers[file, rank].push] = 
                        SmallBishopBitboards[file, rank][i];
                    Lookup.BishopMobilityLookupArray[file, rank][(SmallBishopCombinations[file, rank][i] * Lookup.BishopBitboardNumbers[file, rank].magicNumber) >> Lookup.BishopBitboardNumbers[file, rank].push] = 
                        EvalUtils.EvaluateBishopMobility(file, rank, i);
                }

                // knight moves
                // since the potential captures and moves are based on the same combinations, the same magic numbers can be used
                Lookup.KnightMove[file, rank] = MagicNumbers.KnightNumbers[file, rank];
                Lookup.KnightLookup[file, rank] = new Move[Lookup.KnightMove[file, rank].highest + 1][];
                Lookup.KnightCaptureLookup[file, rank] = new Move[Lookup.KnightMove[file, rank].highest + 1][];

                for (int i = 0; i < KnightCombinations[file, rank].Length; i++) // for each combination
                {
                    Lookup.KnightLookup[file, rank][(KnightCombinations[file, rank][i] * Lookup.KnightMove[file, rank].magicNumber) >> Lookup.KnightMove[file, rank].push] = 
                        GetBitboardMoves(KnightCombinations[file, rank][i], (file, rank), 5);
                    Lookup.KnightCaptureLookup[file, rank][(KnightCombinations[file, rank][i] * Lookup.KnightMove[file, rank].magicNumber) >> Lookup.KnightMove[file, rank].push] = 
                        GetBitboardMoves(KnightCombinations[file, rank][i], (file, rank), 50, capture: true);
                }

                // king moves
                Lookup.KingMove[file, rank] = MagicNumbers.KingNumbers[file, rank]; // MagicNumbers.GenerateRepeat(KingCombinations[file, rank], 5000);
                Lookup.KingLookup[file, rank] = new Move[Lookup.KingMove[file, rank].highest + 1][];
                Lookup.KingCaptureLookup[file, rank] = new Move[Lookup.KingMove[file, rank].highest + 1][];
                Lookup.KingSafetyLookup[file, rank] = new int[Lookup.KingMove[file, rank].highest + 1];

                for (int i = 0; i < KingCombinations[file, rank].Length; i++) // for each combination
                {
                    Lookup.KingLookup[file, rank][(KingCombinations[file, rank][i] * Lookup.KingMove[file, rank].magicNumber) >> Lookup.KingMove[file, rank].push] =
                        GetBitboardMoves(KingCombinations[file, rank][i], (file, rank), 5);
                    Lookup.KingCaptureLookup[file, rank][(KingCombinations[file, rank][i] * Lookup.KingMove[file, rank].magicNumber) >> Lookup.KingMove[file, rank].push] =
                        GetBitboardMoves(KingCombinations[file, rank][i], (file, rank), 3, capture: true);
                }

                // pin lines
                // rook pin lines
                Lookup.RookPinLineBitboardLookup[file, rank] = new ulong[Lookup.RookMove[file, rank].highest + 1];

                for (int i = 0; i < RookBlockers[file, rank].Length; i++) // for each blocker
                {
                    Lookup.RookPinLineBitboardLookup[file, rank][(RookBlockers[file, rank][i] * Lookup.RookMove[file, rank].magicNumber) >> Lookup.RookMove[file, rank].push] =
                        GetPinLine(RookBlockers[file, rank][i], (file, rank), Pieces.WhiteRook);
                }

                // bishop pin lines
                Lookup.BishopPinLineBitboardLookup[file, rank] = new ulong[Lookup.BishopMove[file, rank].highest + 1];

                for (int i = 0; i < BishopBlockers[file, rank].Length; i++) // for each blocker
                {
                    Lookup.BishopPinLineBitboardLookup[file, rank][(BishopBlockers[file, rank][i] * Lookup.BishopMove[file, rank].magicNumber) >> Lookup.BishopMove[file, rank].push] =
                        GetPinLine(BishopBlockers[file, rank][i], (file, rank), Pieces.WhiteBishop);
                }

                // pin search
                Lookup.RookPinLookup[file, rank] = new List<PinSearchResult>[Lookup.RookMove[file, rank].highest + 1];

                for (int i = 0; i < RookBlockers[file, rank].Length; i++)
                {
                    Lookup.RookPinLookup[file, rank][(RookBlockers[file, rank][i] * Lookup.RookMove[file, rank].magicNumber) >> Lookup.RookMove[file, rank].push] =
                        GeneratePinResult((file, rank), RookBlockers[file, rank][i], Pieces.WhiteRook);
                }

                Lookup.BishopPinLookup[file, rank] = new List<PinSearchResult>[Lookup.BishopMove[file, rank].highest + 1];

                for (int i = 0; i < BishopBlockers[file, rank].Length; i++)
                {
                    Lookup.BishopPinLookup[file, rank][(BishopBlockers[file, rank][i] * Lookup.BishopMove[file, rank].magicNumber) >> Lookup.BishopMove[file, rank].push] =
                        GeneratePinResult((file, rank), BishopBlockers[file, rank][i], Pieces.WhiteBishop);
                }

                // blocking checks
                // block captures
                Lookup.BlockCaptureNumbers[file, rank] = MagicNumbers.BlockCaptureNumbers[file, rank]; //MagicNumbers.GenerateRepeat(BlockCaptures[file, rank], 10000);
                Lookup.BlockCaptureMoveLookup[file, rank] = new Move[Lookup.BlockCaptureNumbers[file, rank].highest + 1];
                Lookup.BlockCaptureMovePawnLookup[file, rank] = new Move[Lookup.BlockCaptureNumbers[file, rank].highest + 1][];

                for (int i = 0; i < BlockCaptures[file, rank].Length; i++)
                {
                    Lookup.BlockCaptureMoveLookup[file, rank][(BlockCaptures[file, rank][i] * Lookup.BlockCaptureNumbers[file, rank].magicNumber) >> Lookup.BlockCaptureNumbers[file, rank].push] =
                        GetBitboardMoves(BlockCaptures[file, rank][i], (file, rank), 25)[0];
                    if (rank != 0 && rank != 7)
                        Lookup.BlockCaptureMovePawnLookup[file, rank][(BlockCaptures[file, rank][i] * Lookup.BlockCaptureNumbers[file, rank].magicNumber) >> Lookup.BlockCaptureNumbers[file, rank].push] =
                            GetBitboardMoves(BlockCaptures[file, rank][i], (file, rank), 25, pawn: true, capture: true);
                }

                // block moves
                Lookup.BlockMoveLookup[file, rank] = new Move[Lookup.BlockMoveNumber.highest + 1][];
                Lookup.BlockMovePawnLookup[file, rank] = new Move[Lookup.BlockMoveNumber.highest + 1][];

                foreach (ulong move in BlockMoves)
                {
                    Lookup.BlockMoveLookup[file, rank][(move * Lookup.BlockMoveNumber.magicNumber) >> Lookup.BlockMoveNumber.push] =
                        GetBitboardMoves(move, (file, rank), 5);
                    if (rank != 0 && rank != 7)
                        Lookup.BlockMovePawnLookup[file, rank][(move * Lookup.BlockMoveNumber.magicNumber) >> Lookup.BlockMoveNumber.push] =
                            GetBitboardMoves(move, (file, rank), 5, pawn: true);
                }
                
                //Console.WriteLine($"Square done {++done}/64");
                // pawn moves
                if (rank != 0 && rank != 7)
                {
                    // white pawns
                    // moves
                    Lookup.WhitePawnMove[file, rank] = MagicNumbers.WhitePawnMoveNumbers[file, rank];
                    Lookup.WhitePawnLookup[file, rank] = new Move[Lookup.WhitePawnMove[file, rank].highest + 1][];

                    for (int i = 0; i < WhitePawnMoveCombinations[file, rank].Length; i++) // for each combination
                    {
                        Lookup.WhitePawnLookup[file, rank][(WhitePawnMoveCombinations[file, rank][i] * Lookup.WhitePawnMove[file, rank].magicNumber) >> Lookup.WhitePawnMove[file, rank].push] = 
                            GetPawnMoves(WhitePawnMoveCombinations[file, rank][i], (file, rank), 0);
                    }

                    // captures
                    Lookup.WhitePawnCapture[file, rank] = MagicNumbers.WhiteCaptureMoveNumbers[file, rank];
                    Lookup.WhitePawnCaptureLookup[file, rank] = new Move[Lookup.WhitePawnCapture[file, rank].highest + 1][];

                    for (int i = 0; i < WhitePawnCaptureCombinations[file, rank].Length; i++) // for each combination
                    {
                        Lookup.WhitePawnCaptureLookup[file, rank][(WhitePawnCaptureCombinations[file, rank][i] * Lookup.WhitePawnCapture[file, rank].magicNumber) >> Lookup.WhitePawnCapture[file, rank].push] =
                            GetPawnCaptures(WhitePawnCaptureCombinations[file, rank][i], (file, rank), 0);
                    }

                    // black pawns
                    // moves
                    Lookup.BlackPawnMove[file, rank] = MagicNumbers.BlackPawnMoveNumbers[file, rank];
                    Lookup.BlackPawnLookup[file, rank] = new Move[Lookup.BlackPawnMove[file, rank].highest + 1][];

                    for (int i = 0; i < BlackPawnMoveCombinations[file, rank].Length; i++) // for each combination
                    {
                        Lookup.BlackPawnLookup[file, rank][(BlackPawnMoveCombinations[file, rank][i] * Lookup.BlackPawnMove[file, rank].magicNumber) >> Lookup.BlackPawnMove[file, rank].push] =
                            GetPawnMoves(BlackPawnMoveCombinations[file, rank][i], (file, rank), 1);
                    }

                    // captures
                    Lookup.BlackPawnCapture[file, rank] = MagicNumbers.BlackCaptureMoveNumbers[file, rank];
                    Lookup.BlackPawnCaptureLookup[file, rank] = new Move[Lookup.BlackPawnCapture[file, rank].highest + 1][];

                    for (int i = 0; i < BlackPawnCaptureCombinations[file, rank].Length; i++) // for each combination
                    {
                        Lookup.BlackPawnCaptureLookup[file, rank][(BlackPawnCaptureCombinations[file, rank][i] * Lookup.BlackPawnCapture[file, rank].magicNumber) >> Lookup.BlackPawnCapture[file, rank].push] =
                            GetPawnCaptures(BlackPawnCaptureCombinations[file, rank][i], (file, rank), 1);
                    }
                }
            });
        });
        
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
                    path |= GetSquare(startFile, current);
                    current += moveBy;
                } while (current != endRank);
            }
            
            else if (endRank == startRank) // both are from the same file
            {
                int current = startFile;
                int moveBy = startFile < endFile ? 1 : -1;
                do
                {
                    path |= GetSquare(current, startRank);
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
                    path |= GetSquare(currentFile, currentRank);
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
                    path |= GetSquare(currentFile, currentRank);
                    currentFile += moveBy.file;
                    currentRank += moveBy.rank;
                } while ((currentFile, currentRank) != (endFile, endRank));
            }

            // in an L shape
            if (path != 0 || (Math.Abs(startFile - endFile) == 1 && Math.Abs(startRank - endRank) == 2) || (Math.Abs(startFile - endFile) == 2 && Math.Abs(startRank - endRank) == 1)) 
                path |= GetSquare(startFile, startRank) | GetSquare(endFile, endRank);
            
            PathLookup[startFile, startRank, endFile, endRank] = path;
        }

        //Console.WriteLine($"Bitboards initialized in {t.Stop()}ms");
        Thread.Sleep(20);
        init = true;
    }

    public static void StartInit()
    {
        if (inProgress)
            return;
        Thread t = new Thread(() =>
        {
            inProgress = true;
            Blaze.Init.Start();
            inProgress = false;
        });
        t.Start();
    }

    public static bool Poll()
    {
        return init;
    }
}