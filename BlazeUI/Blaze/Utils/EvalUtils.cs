using System;

namespace BlazeUI.Blaze.Utils;
using Magic_Lookup;
using Evaluation;
using static Magic_Lookup.Masks;
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
        return (NeighbourMasks[file] & pawns) == 0;
    }
    
    public static int CountPawns(ulong pawns, int file)
    {
        return (int)ulong.PopCount(pawns & BitboardUtils.GetFile(file));
    }

    public static (int start, int end) FindRelevantFiles(EvalData.Section section)
    {
        return section switch
        {
            EvalData.Section.LeftEdge => (0, 2),
            EvalData.Section.Left => (2, 3),
            EvalData.Section.CenterLeft => (3, 4),
            EvalData.Section.CenterRight => (4, 5),
            EvalData.Section.Right => (5, 6),
            EvalData.Section.RightEdge => (6, 8),
            _ => throw new Exception("no")
        };
    }
    
    public static int WhiteKingSafetyPenalty(int kingFile, ulong pawns)
    {
        ulong countBitboard = SurroundMasks[kingFile] & WhiteSafetyPawns;
        return Weights.KingSafetyPenalty[(int)(ulong.PopCount(countBitboard & pawns))];
    }
    
    public static int BlackKingSafetyPenalty(int kingFile, ulong pawns)
    {
        ulong countBitboard = SurroundMasks[kingFile] & BlackSafetyPawns;
        return Weights.KingSafetyPenalty[(int)(ulong.PopCount(countBitboard & pawns))];
    }

    public static int EvaluateRookMobility(int file, int rank, int index)
    {
        ulong controlled = Combinations.SmallRookBitboards[file, rank][index];
        return (int)(ulong.PopCount(controlled) * Weights.MobilityMultiplier
                     + ulong.PopCount(controlled & CenterControlMask) * Weights.CenterControlMultiplier);
    }

    public static int EvaluateBishopMobility(int file, int rank, int index)
    {
        ulong controlled = Combinations.SmallBishopBitboards[file, rank][index];
        return (int)(ulong.PopCount(controlled) * Weights.MobilityMultiplier
                     + ulong.PopCount(controlled & CenterControlMask) * Weights.CenterControlMultiplier);
    }
}