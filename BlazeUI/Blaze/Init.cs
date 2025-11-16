using System;
using System.Threading;

namespace BlazeUI.Blaze;
using Board_Representation;
using Book;
using Magic_Lookup;
using Search;
using Utils;
using Evaluation;

public static class Init
{
    public static readonly General.CompletionPoint Progress = new();
    public static InitStatus init = InitStatus.Uninitialized;
    
    public static void Start()
    {
        if (init != InitStatus.Uninitialized)
            return;
        init = InitStatus.Waiting;
        
        TranspositionTable.Init((int)Math.Pow(2, 20) + 7);
        RefutationTable.Init((int)Math.Pow(2, 20) + 7);
        Progress.Set(0, "Generating Masks...");
        Masks.Init();
        Progress.Set(20, "Generating Combinations...");
        Combinations.Init();
        Progress.Set(55, "Initializing Magic Lookup...");
        Bitboards.Init();
        PathFinder.Init();
        Progress.Set(90, "Initializing Evaluation Lookup");
        EvaluationLookup.Init();
        ZobristHash.Init();
        Progress.Set(100, "Loading book...");
        Book.Book.Init(Books.Standard);
        
        init = InitStatus.Complete;
    }

    public enum InitStatus
    {
        Uninitialized,
        Waiting,
        Complete,
    }

    public static void StartInit()
    {
        new Thread(Start).Start();
    }
}