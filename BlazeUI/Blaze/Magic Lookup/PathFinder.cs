using System;
using BlazeUI.Blaze.Utils;

namespace BlazeUI.Blaze.Magic_Lookup;
using static BitboardUtils;

public static class PathFinder
{
    public static readonly ulong[,,,] PathLookup =  new ulong[8,8,8,8];

    public static void Init()
    {
                // init pathfinder
        for (int startRank = 0; startRank < 8; startRank++)
        for (int startFile = 0; startFile < 8; startFile++)
        for (int endRank = 0; endRank < 8; endRank++)
        for (int endFile = 0; endFile < 8; endFile++)
        {
            if (startRank == endRank && startFile == endFile)
            {
                PathLookup[startFile, startRank, endFile, endRank] = 0;
                continue;
            }
            
            ulong path = 0;
            
            if (endFile == startFile) // both are from the same file
            {
                int current = startRank;
                int moveBy = startRank < endRank ? 1 : -1;
                do
                {
                    path |= GetSquare(startFile, current);
                    current += moveBy;
                } while (current != endRank);
            }
            
            else if (endRank == startRank) // both are from the same file
            {
                int current = startFile;
                int moveBy = startFile < endFile ? 1 : -1;
                do
                {
                    path |= GetSquare(current, startRank);
                    current += moveBy;
                } while (current != endFile);
            }
            
            else if (startFile - startRank == endFile - endRank) // on the same up diagonal
            {
                int currentFile = startFile;
                int currentRank = startRank;
                (int file, int rank) moveBy = startRank < endRank ? (1, 1) : (-1, -1);
                do
                {
                    path |= GetSquare(currentFile, currentRank);
                    currentFile += moveBy.file;
                    currentRank += moveBy.rank;
                } while ((currentFile, currentRank) != (endFile, endRank));
            }
            
            else if ((7 - startFile) - startRank == (7 - endFile) - endRank) // on the same down diagonal
            {
                int currentFile = startFile;
                int currentRank = startRank;
                (int file, int rank) moveBy = startRank < endRank ? (-1, 1) : (1, -1);
                do
                {
                    path |= GetSquare(currentFile, currentRank);
                    currentFile += moveBy.file;
                    currentRank += moveBy.rank;
                } while ((currentFile, currentRank) != (endFile, endRank));
            }

            // in an L shape
            if (path != 0 || (Math.Abs(startFile - endFile) == 1 && Math.Abs(startRank - endRank) == 2) || (Math.Abs(startFile - endFile) == 2 && Math.Abs(startRank - endRank) == 1)) 
                path |= GetSquare(startFile, startRank) | GetSquare(endFile, endRank);
            
            PathLookup[startFile, startRank, endFile, endRank] = path;
        }
    }
}