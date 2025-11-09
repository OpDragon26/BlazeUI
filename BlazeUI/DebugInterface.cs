using System;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Evaluation;
using BlazeUI.Blaze.Interface;
using BlazeUI.Blaze.Utils;
using Type = BlazeUI.Blaze.Interface.Type;

namespace BlazeUI;

public static class DebugInterface
{
    public static void Execute()
    {
        Blaze.Init.Start();
        
        //DebugUtils.TestGameSpeed(15, 6);
        
        CompareEval("k7/5B2/6B1/7b/3b4/5B2/5rB1/K7 w - - 0 1");
        
        //Examine("rnbqk1nr/ppppppbp/6p1/8/3PP3/8/PPP2PPP/RNBQKBNR w KQkq - 1 3");
        
        //SingleGame();
        
        /*
        Board testBoard = new("rnbqk1nr/ppppppbp/6p1/8/3PP3/8/PPP2PPP/RNBQKBNR w KQkq - 1 3");
        Console.WriteLine(Evaluator.StaticEvaluate(testBoard));
        testBoard = new Board("rnbqk1nr/pppppp1p/6pb/8/3PP3/8/PPP2PPP/RN1QKBNR w KQkq - 1 3");
        Console.WriteLine(Evaluator.StaticEvaluate(testBoard));
        */
        Environment.Exit(0);
    }

    private static void CompareEval(string FEN)
    {
        Board toEval = new Board(FEN);

        Console.WriteLine($"standard eval: {Evaluator.BareBonesEval(toEval)}");
        Console.WriteLine($"lookup eval: {Evaluator.BareBonesEvalLookup(toEval)}");
    }
    
    private static void Examine(string FEN)
    {
        DebugUtils.ExamineEval(new Board(FEN), 6);
    }

    private static void SingleGame()
    {
        new CLIMatch(new(Presets.StartingBoard), Type.Autoplay, Side.White, 6, debug: true, clear: false).Play();
    }
}