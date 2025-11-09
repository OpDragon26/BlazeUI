namespace BlazeUI.Blaze.Evaluation;
using static EvalData;

public static class Precomputer
{
    public static TEval GenerateEval<TEval, TEvalTest>(uint piece, Slice slice) where TEval : Evaluation<TEvalTest>, new() where TEvalTest : EvalTest, new()
    {
        TEval eval = new TEval();
        
        
        
        return eval;
    }
}