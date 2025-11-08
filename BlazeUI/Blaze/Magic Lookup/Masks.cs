using BlazeUI.Blaze.Utils;

namespace BlazeUI.Blaze.Magic_Lookup;
using static BitboardUtils;

public static class Masks
{
    public static readonly ulong[,] RookMasks = new ulong[8,8];
    public static readonly ulong[,] BishopMasks = new ulong[8,8];
    
    public static readonly ulong[,] WhitePawnMoveMasks = new ulong[8,8];
    public static readonly ulong[,] BlackPawnMoveMasks = new ulong[8,8];
    public static readonly ulong[,] WhitePawnCaptureMasks = new ulong[8,8];
    public static readonly ulong[,] BlackPawnCaptureMasks = new ulong[8,8];
    
    public static readonly ulong[,] SmallRookMasks = new ulong[8,8];
    public static readonly ulong[,] SmallBishopMasks = new ulong[8,8];
    
    public static readonly ulong WhiteShortCastleMask = 0x6000000000000000;
    public static readonly ulong WhiteLongCastleMask = 0xC00000000000000;
    public static readonly ulong BlackShortCastleMask = 0x60;
    public static readonly ulong BlackLongCastleMask = 0xC;
    
    public static readonly ulong[] PassedPawnMasks = new ulong[8];
    public static readonly ulong[] NeighbourMasks = new ulong[8];
    public static readonly ulong[] SurroundMasks = new ulong[8];
    
    public const ulong File = 0x8080808080808080;
    public const ulong Rank = 0xFF00000000000000;

    public const ulong UpDiagonal = 0x102040810204080;
    public const ulong DownDiagonal = 0x8040201008040201;

    public const ulong SmallFile = 0x80808080808000;
    public const ulong SmallRank = 0x7E00000000000000;

    public const ulong KingSafetyAppliesWhite = 0xC7C7000000000000; 
    public const ulong KingSafetyAppliesBlack = 0xC7C7;

    public const ulong WhiteSafetyPawns = 0xffff0000000000;
    public const ulong BlackSafetyPawns = 0xffff00;
    
    private const ulong Frame = 0xFF818181818181FF;
    public const ulong CenterControlMask = 0x3c3c3c3c0000;

    public const ulong BlackPossibleEnPassant = 0x100000000;
    public const ulong WhitePossibleEnPassant = 0x1000000;

    public static void Init()
    {
        // The last bit also has to be evaluated in every direction, since it matters whether it's blocked or not
        for (int file = 0; file < 8; file++)
        for (int rank = 0; rank < 8; rank++)
        {
            RookMasks[file, rank] = GetRank(rank) ^ GetFile(file);
            
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
            
            SmallRookMasks[file, rank] = ((SmallRank >> (rank * 8)) ^ (SmallFile >> (7 - file))) & ~GetSquare(file, rank);
            SmallBishopMasks[file, rank] = (relativeUD ^ relativeDD) & ~Frame;
            
            // white pawns
            ulong wpmMask = 0;
            ulong wpcMask = 0;
            wpmMask |= GetSquare(file, rank + 1);
            if (ValidSquare(file + 1, rank + 1)) wpcMask |= GetSquare(file + 1, rank + 1);
            if (ValidSquare(file - 1, rank + 1)) wpcMask |= GetSquare(file - 1, rank + 1);
                
            if (rank == 1) wpmMask |= GetSquare(file, rank + 2);
                
            WhitePawnMoveMasks[file, rank] = wpmMask;
            WhitePawnCaptureMasks[file, rank] = wpcMask;
                
            // black pawns
            ulong bpmMask = 0;
            ulong bpcMask = 0;
            bpmMask |= GetSquare(file, rank - 1);
            if (ValidSquare(file + 1, rank - 1)) bpcMask |= GetSquare(file + 1, rank - 1);
            if (ValidSquare(file - 1, rank - 1)) bpcMask |= GetSquare(file - 1, rank - 1);
                
            if (rank == 6) bpmMask |= GetSquare(file, rank - 2);
                
            BlackPawnMoveMasks[file, rank] = bpmMask;
            BlackPawnCaptureMasks[file, rank] = bpcMask;
            
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
            SurroundMasks[file] = neighborMask | GetFile(file);
        }
    }
}