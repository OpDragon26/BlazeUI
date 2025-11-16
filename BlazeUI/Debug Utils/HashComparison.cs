using System;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;
using BlazeUI.Blaze.Utils;

namespace BlazeUI.Debug_Utils;

public interface IHashComparison
{
    public void Test();
}

public class HashCheck(string name, Board before, Board expected, string move, bool logBoard) : IHashComparison
{
    public void Test()
    {
        Console.WriteLine($"Testing {name}");
        
        Board temp = new Board(before);
        temp.MakeMove(new Move(move, temp));

        if (logBoard)
        {
            CLIUtils.PrintBoard(before);
            CLIUtils.PrintBoard(temp);
        }

        Console.WriteLine($"Control: {expected.hashKey.key}");
        Console.WriteLine($"Result:  {temp.hashKey.key}");
        bool correct = temp.Equals(expected);
        Console.WriteLine(correct ? "Passed" : "Failed");
    }
}

public class TranspositionCheck(string name, Board start, string[] firstSet, string[] secondSet, bool logBoard) : IHashComparison
{
    public void Test()
    {
        Board first = new(start);
        Board second = new(start);
        
        if (logBoard)
            CLIUtils.PrintBoard(start);
        
        first.PlayMoves(firstSet);
        second.PlayMoves(secondSet);

        if (logBoard)
        {
            Console.WriteLine("First:");
            CLIUtils.PrintBoard(first);
            Console.WriteLine("Second:");
            CLIUtils.PrintBoard(second);
        }

        Console.WriteLine($"First:  {first.hashKey.key}");
        Console.WriteLine($"Second: {second.hashKey.key}");
        bool correct = first.Equals(second);
        Console.WriteLine(correct ? "Passed" : "Failed");
    }
}