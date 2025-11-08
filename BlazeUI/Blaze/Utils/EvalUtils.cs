namespace BlazeUI.Blaze.Utils;
using Magic_Lookup;
using Evaluation;

public static class EvalUtils
{
    public static bool IsPawnPassedWhite(ulong blackPawns, int file, int rank)
    {
        return (BitboardUtils.GetWhitePassedPawnMask(file, rank) & blackPawns) == 0;
    }

    public static bool IsPawnPassedBlack(ulong whitePawns, int file, int rank)
    {
        return (BitboardUtils.GetBlackPassedPawnMask(file, rank) & whitePawns) == 0;
    }
    
    public static bool IsPawnIsolated(ulong pawns, int file)
    {
        return (Bitboards.NeighbourMasks[file] & pawns) == 0;
    }
    
    public static int CountPawns(ulong pawns, int file)
    {
        return (int)ulong.PopCount(pawns & BitboardUtils.GetFile(file));
    }
    
    public static int WhiteKingSafetyPenalty(int kingFile, ulong pawns)
    {
        ulong countBitboard = Bitboards.SurroundMasks[kingFile] & Bitboards.WhiteSafetyPawns;
        return Weights.KingSafetyPenalty[(int)(ulong.PopCount(countBitboard & pawns))];
    }
    
    public static int BlackKingSafetyPenalty(int kingFile, ulong pawns)
    {
        ulong countBitboard = Bitboards.SurroundMasks[kingFile] & Bitboards.BlackSafetyPawns;
        return Weights.KingSafetyPenalty[(int)(ulong.PopCount(countBitboard & pawns))];
    }

    public static int EvaluateRookMobility(int file, int rank, int index)
    {
        ulong controlled = Bitboards.SmallRookBitboards[file, rank][index];
        return (int)(ulong.PopCount(controlled) * Weights.MobilityMultiplier
                     + ulong.PopCount(controlled & Bitboards.CenterControlMask) * Weights.CenterControlMultiplier);
    }

    public static int EvaluateBishopMobility(int file, int rank, int index)
    {
        ulong controlled = Bitboards.SmallBishopBitboards[file, rank][index];
        return (int)(ulong.PopCount(controlled) * Weights.MobilityMultiplier
                     + ulong.PopCount(controlled & Bitboards.CenterControlMask) * Weights.CenterControlMultiplier);
    }
}