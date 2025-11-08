using System;

namespace BlazeUI.Blaze.Evaluation;
using static EvaluationLookup;
using Magic_Lookup;
public static class EvaluationData
{
    public enum Slice
    {
        First,
        Second,
        Third,
        Fourth,
    }
    
    public enum Section
    {
        Right, Left, Center
    }
    
    public abstract class StandardEvaluation
    {
        public int MgWhite;
        public int MgBlack;
        public int EgWhite;
        public int EgBlack;
        public int PhaseInc;
        public (int file, int rank)[] Coords = [];

        public abstract int MobilityLookup(ulong blockers);
        public abstract void FinalWhite(ulong blockers, ref EvalState evalState);
        public abstract void FinalBlack(ulong blockers, ref EvalState evalState);
        public abstract void Increment(ref EvalState evalState);
    }
    
    public readonly struct PassedBonus(ulong mask, int bonus)
    {
        public readonly int bonus = bonus;

        public bool Test(ulong enemyPawns)
        {
            return (mask & enemyPawns) == 0;
        }
    }
    
    public class PawnEvaluation
    {
        public int MgScoreWhite;
        public int MgScoreBlack;
        public int EgScoreWhite;
        public int EgScoreBlack;
        public PassedBonus[] MgPassedWhite = [];
        public PassedBonus[] MgPassedBlack = [];
        public PassedBonus[] EgPassedWhite = [];
        public PassedBonus[] EgPassedBlack = [];

        public void FinalWhite(ulong blackPawns, ref EvalState evalState)
        {
            evalState.MgScore += MgScoreWhite;
            foreach(PassedBonus p in MgPassedWhite)
                if (p.Test(blackPawns))
                    evalState.MgScore += p.bonus;
            
            evalState.EgScore += EgScoreWhite;
            foreach(PassedBonus p in EgPassedWhite)
                if (p.Test(blackPawns))
                    evalState.EgScore += p.bonus;
        }

        public void FinalBlack(ulong whitePawns, ref EvalState evalState)
        {
            evalState.MgScore += MgScoreBlack;
            foreach (PassedBonus p in MgPassedBlack)
                if (p.Test(whitePawns))
                    evalState.MgScore += p.bonus;
            
            evalState.EgScore += EgScoreBlack;
            foreach (PassedBonus p in EgPassedBlack)
                if (p.Test(whitePawns))
                    evalState.EgScore += p.bonus;
        }
    }
    
    public readonly struct OpenFileCheck(ulong file)
    {
        public int Test(ulong blockers)
        {
            if ((file & blockers) == 0) // at least semi open
                return Weights.OpenFileAdvantage;
            return 0;
        }
    }
    
    public class RookEvaluation : StandardEvaluation
    {
        public OpenFileCheck[] fileChecks = [];

        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in Coords)
                mobility += MagicLookup.RookMobilityLookup(pos, blockers);
            
            return mobility;
        }

        public void FinalWhite(ulong pawns, ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgWhite + MobilityLookup(blockers);
            foreach (OpenFileCheck check in  fileChecks)
                evalState.MgScore += check.Test(pawns);
            
            evalState.EgScore += EgWhite + MobilityLookup(blockers);
        }

        public void FinalBlack(ulong pawns, ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgBlack - MobilityLookup(blockers);
            foreach (OpenFileCheck check in  fileChecks)
                evalState.MgScore -= check.Test(pawns);
            
            evalState.EgScore += EgBlack - MobilityLookup(blockers);
        }

        public override void Increment(ref EvalState evalState)
        {
            evalState.Phase += PhaseInc;
        }

        public override void FinalWhite(ulong blockers, ref EvalState evalState)
        {
            throw new NotImplementedException();
        }

        public override void FinalBlack(ulong blockers, ref EvalState evalState)
        {
            throw new NotImplementedException();
        }
    }
    
    public class QueenEvaluation : StandardEvaluation
    {
        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in Coords)
            {
                mobility += MagicLookup.RookMobilityLookup(pos, blockers);
                mobility += MagicLookup.BishopMobilityLookup(pos, blockers);
            }

            return mobility;
        }

        public override void FinalWhite(ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgWhite + MobilityLookup(blockers);
            evalState.EgScore += EgWhite + MobilityLookup(blockers);
        }

        public override void FinalBlack(ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgBlack - MobilityLookup(blockers);
            evalState.EgScore += EgBlack - MobilityLookup(blockers);
        }

        public override void Increment(ref EvalState evalState)
        {
            evalState.Phase += PhaseInc;
        }
    }

    public class BishopEvaluation : StandardEvaluation
    {
        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in Coords)
                mobility += MagicLookup.BishopMobilityLookup(pos, blockers);

            return mobility;
        }

        public override void FinalWhite(ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgWhite + MobilityLookup(blockers);
            evalState.EgScore += EgWhite + MobilityLookup(blockers);
        }

        public override void FinalBlack(ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgBlack - MobilityLookup(blockers);
            evalState.EgScore += EgBlack - MobilityLookup(blockers);
        }

        public override void Increment(ref EvalState evalState)
        {
            evalState.Phase += PhaseInc;
        }
    }
    
    public class KnightEvaluation : StandardEvaluation
    {
        public override int MobilityLookup(ulong blockers)
        {
            int mobility = 0;

            foreach ((int file, int rank) pos in Coords)
                mobility += Bitboards.MagicLookupArrays.KnightMobilityLookup[pos.file, pos.rank];
            
            return mobility;
        }

        public override void FinalWhite(ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgWhite + MobilityLookup(blockers);
            evalState.EgScore += EgWhite + MobilityLookup(blockers);
        }

        public override void FinalBlack(ulong blockers, ref EvalState evalState)
        {
            evalState.MgScore += MgBlack - MobilityLookup(blockers);
            evalState.EgScore += EgBlack - MobilityLookup(blockers);
        }
        
        public override void Increment(ref EvalState evalState)
        {
            evalState.Phase += PhaseInc;
        }
    }
    
    public class KingEvaluation(int mgWhite, int mgBlack, int egWhite, int egBlack)
    {
        public void WhiteFinal(ref EvalState evalState)
        {
            evalState.MgScore += mgWhite;
            evalState.EgScore += egWhite;
        }

        public void BlackFinal(ref EvalState evalState)
        {
            evalState.MgScore += mgBlack;
            evalState.EgScore += egBlack;
        }
    }
}