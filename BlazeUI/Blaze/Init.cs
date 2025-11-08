using System;

namespace BlazeUI.Blaze;
using Board_Representation;
using Book;
using Magic_Lookup;
using Search;

public static class Init
{
    public static void Start()
    {
        RefutationTable.Init((int)Math.Pow(2, 20) + 7);
        Bitboards.Init();
        ZobristHash.Init();
        Book.Book.Init(Books.Standard);
    }
    
    
}