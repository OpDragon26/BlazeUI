using System;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Evaluation;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Utils;
using Type = BlazeUI.Blaze.API.Type;

namespace BlazeUI.Debug_Utils;

public static class DebugInterface
{
    public static void Execute()
    {
        //Blaze.Init.Start();
        ZobristHash.Init();

        //TestHash();
        //DebugUtils.TestGameSpeed(15, 6);
        SingleGame();
        
        Environment.Exit(0);
    }

    private static void TestHash()
    {
        foreach (IHashComparison test in HashTests)
            test.Test();
    }
    
    private static readonly IHashComparison[] HashTests = [
        new HashCheck("move", new("8/3kp3/8/8/8/8/2KPR3/8 w - - 0 1"), new("8/3kp3/8/8/4R3/8/2KP4/8 b - - 0 1"), "e2e4", false),
        new HashCheck("capture", new("8/3kp3/8/8/4p3/8/2KPR3/8 w - - 0 1"), new("8/3kp3/8/8/4R3/8/2KP4/8 b - - 0 1"), "e2e4", false),
        new HashCheck("en passant numbers", new("2k5/8/8/8/4p3/8/3P4/2K5 w - - 0 1"), new("2k5/8/8/8/3Pp3/8/8/2K5 b - d3 0 1"), "d2d4", false),
        new HashCheck("playing en passant", new("2k5/8/8/8/3Pp3/8/8/2K5 b - d3 0 1"), new("2k5/8/8/8/8/3p4/8/2K5 w - - 0 2"), "e4d3", false),
        new HashCheck("playing castling", new("3k4/8/8/8/8/8/8/R3K3 w Q - 0 2"), new("3k4/8/8/8/8/8/8/2KR4 b - - 1 2"), "e1c1", false),
        new HashCheck("removing castling rights", new("3k4/8/8/8/8/8/8/R3K2R w KQ - 0 2"), new("3k4/8/8/8/8/8/4K3/R6R b - - 1 2"), "e1e2", false),
        new TranspositionCheck("transposition", new(Presets.StartingBoard), ["b1c3", "g8f6", "e2e4", "e7e5", "g1f3"], ["e2e4", "e7e5", "b1c3", "g8f6", "g1f3"], true),
    ];

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