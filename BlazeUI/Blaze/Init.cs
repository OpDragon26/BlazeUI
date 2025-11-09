using System;

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
    
    public static void Start()
    {
        RefutationTable.Init((int)Math.Pow(2, 20) + 7);
        Bitboards.Init(Progress);
        EvaluationLookup.Init();
        ZobristHash.Init();
        Progress.Set(75, "Loading book...");
        Book.Book.Init(Books.Standard);
    }
    
    
}