using System.Linq;

namespace BlazeUI.Blaze.Utils;
using Board_Representation;
using Move_Generation;
using static MoveUtils;
using Magic_Lookup;

public static class AlgebraicNotation
{
    public static string NotateMove(Move move, Board board)
    {
        if ((board.GetPiece(move.Source) & Pieces.TypeMask) == Pieces.WhiteKing && move.Source.file == 4)
        {
            if (move.Destination.file == 6)
                return "O-O";
            if (move.Destination.file == 2)
                return "O-O-O";
        }

        string notation = "";

        if ((board.GetPiece(move.Source) & Pieces.TypeMask) == Pieces.WhitePawn)
        {
            // pawn move

            if (move.Source.file == move.Destination.file) // move forward
            {
                if (board.GetPiece(move.Destination) == Pieces.Empty)
                    notation = GetSquare(move.Destination);
                else
                    throw new NotationParsingException(
                        $"Failed to convert to Algebraic notation: {GetSquare(move.Destination)} is not empty: {move.GetUCI()}");
            }
            else // capture
            {
                if (board.GetPiece(move.Destination) != Pieces.Empty || move.Destination == board.enPassant)
                    notation = Files[move.Source.file] + "x" + GetSquare(move.Destination);
                else
                    throw new NotationParsingException(
                        $"Failed to convert to Algebraic notation: cannot capture empty: {GetSquare(move.Destination)}: {move.GetUCI()}");
            }

            if ((move.Promotion & Pieces.TypeMask) != 0b111)
                notation += '=' + char.ToUpper(PromotionStr[move.Promotion & Pieces.TypeMask][0]).ToString();
        }
        else
        {
            // piece move
            notation += AlgPieces[board.GetPiece(move.Source) & Pieces.TypeMask];
            
            Disambiguation disambiguation = FindLowestDisambiguation(board, GetFinderMask(board.GetPiece(move.Source) & Pieces.TypeMask, move.Destination.file, move.Destination.rank, board), move.Source, move.Destination);

            notation += disambiguation switch
            {
                Disambiguation.None => string.Empty,
                Disambiguation.Complete => GetSquare(move.Source),
                Disambiguation.File => Files[move.Source.file],
                Disambiguation.Rank => move.Source.rank + 1,
                _ => throw new NotationParsingException($"What? {disambiguation}")
            };

            if (board.GetPiece(move.Destination) != Pieces.Empty) // capture
                notation += 'x';
            
            notation += GetSquare(move.Destination);
        }
        
        Board tempBoard = new Board(board);
        tempBoard.MakeMove(move);
        Outcome outcome = tempBoard.GetOutcome();
        if (outcome is Outcome.BlackWin or Outcome.WhiteWin)
        {
            // checkmate
            notation += '#';
        }

        else if (MoveGenerator.Attacked(tempBoard.KingPositions[tempBoard.side], tempBoard, 1 - tempBoard.side))
        {
            // check
            notation += '+';
        }
        
        return notation;
    }
    
    public static Move ParseMove(string alg, Board board)
    {
        if (alg[^1] == '#' || alg[^1] == '+') // if the move is a check or a checkmate, remove the notation
            alg = alg[..^1]; // removes the last character
        
        if (alg.Equals("O-O"))
            return board.side == 0 ? Bitboards.WhiteShortCastle : Bitboards.BlackShortCastle;
        if (alg.Equals("O-O-O"))
            return board.side == 0 ? Bitboards.WhiteLongCastle : Bitboards.BlackLongCastle;

        (int file, int rank) dest;
        (int file, int rank) src = (8, 8);
        int flag = 0;
        
        switch (alg.Length)
        {
            case 2: // pawn move forward
            {
                dest = ParseSquare(alg);
                int offset = board.side * 2 - 1;
                
                uint pawn = board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn;
                int startRank = board.side * 5 + 1;

                int doubleMoveSrc = dest.rank + offset * 2;
                int singleMoveSrc = dest.rank + offset;
                
                if (doubleMoveSrc == startRank) // if a pawn can double move to the starting rank
                    if (board.GetPiece(dest.file, doubleMoveSrc) == pawn)
                        src = (dest.file, doubleMoveSrc);
                
                if (board.GetPiece(dest.file, singleMoveSrc) == pawn) // pawn can single move to the square
                    src = (dest.file, singleMoveSrc);

                if (src == (8, 8))
                    throw new NotationParsingException($"No pawn could move to the square: {alg}");
                
                return new Move(src, dest, type: flag, pawn: true);
            }
            case 3:
                // regular non-disambiguated piece move: Nf3
                dest = ParseSquare($"{alg[1]}{alg[2]}");
                return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.None, dest), dest);
            
            case 4:
                
                // pawn capture
                // piece capture
                // non-capture promotion
                // disambiguated piece move
                if (alg[1] == 'x')
                {
                    // capture
                    if (ValidFiles.Contains(alg[0]))
                    {
                        // pawn capture
                        dest = ParseSquare($"{alg[2]}{alg[3]}");
                        if (board.GetPiece(dest) == Pieces.Empty && dest != board.enPassant)
                            throw new  NotationParsingException("The targeted square is empty and isn't an en passant target");
                        
                        if (dest == board.enPassant)
                            flag = board.side == 0 ? 0b0100 : 0b1100;
                        
                        int srcFile = Indices[alg[0]];
                        int destRank;
                        
                        if (board.side == 0)
                        {
                            destRank = dest.rank - 1;
                            if (board.GetPiece(srcFile, destRank) == Pieces.WhitePawn)
                                return new Move((srcFile, destRank), dest, pawn: true, type: flag);

                            throw new NotationParsingException($"No pawn could capture on the square {GetSquare(dest)}");
                        }
                        
                        destRank = dest.rank + 1;
                        if (board.GetPiece(srcFile, destRank) == Pieces.BlackPawn)
                            return new Move((srcFile, destRank), dest, pawn: true, type: flag);
                        
                        throw new NotationParsingException($"No pawn could capture on the square {GetSquare(dest)}");
                    }
                    if (ValidPieces.Contains(alg[0]))
                        // piece capture: Nxe4
                        // treated as it wasn't a capture
                        return ParseMove($"{alg[0]}{alg[2]}{alg[3]}", board);
                    throw new  NotationParsingException($"Invalid file or piece: {alg[0]}");
                }
                if (alg[2] == '=')
                {
                    // promotion: e8=Q
                    if (validPromotions.Contains(alg[3]))
                    {
                        dest = ParseSquare($"{alg[0]}{alg[1]}");
                        int offset = board.side * 2 - 1;
                        uint pawn = board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn;
                        int possibleSrc = dest.rank + offset;
                        
                        if (board.GetPiece(dest.file, possibleSrc) == pawn)
                        {
                            src = (dest.file, possibleSrc);
                            uint promotion = Promotions[char.ToLower(alg[3])];
                            
                            return new Move(src, dest, pawn: true, promotion: promotion);
                        }
                        
                        throw new NotationParsingException("No pawn can move to the given square");
                        
                    }
                    throw new NotationParsingException($"Unknown or incorrect promotion: {alg[3]}");
                }
                else if (ValidFiles.Contains(alg[1]))
                {
                    // file disambiguation: Nfd2
                    dest = ParseSquare($"{alg[2]}{alg[3]}");
                    return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.File, dest, Indices[alg[1]]), dest);
                }
                else if (ValidRanks.Contains(alg[1]))
                {
                    // rank disambiguation
                    dest = ParseSquare($"{alg[2]}{alg[3]}");
                    return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.Rank, dest,int.Parse(alg[1].ToString())), dest);
                }
                throw new NotationParsingException($"Unknown notation: {alg}");
            case 5:
                // doubly disambiguated move
                // single disambiguated capture
                
                // disambiguated capture: Nfxe5
                if (ValidPieces.Contains(alg[0]) && alg[2] == 'x')
                {
                    dest = ParseSquare($"{alg[3]}{alg[4]}");
                    if (ValidFiles.Contains(alg[1])) // file disambiguation
                        return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.File, dest, Indices[alg[1]]), dest);
                    if (ValidRanks.Contains(alg[1])) // rank disambiguation
                       return new Move(FindMovingPiece(board, GetFinderMask(alg[0], dest.file, dest.rank, board), Disambiguation.Rank, dest, int.Parse(alg[1].ToString())), dest);
                }
                
                // doubly disambiguated move: Nd3e5
                if (ValidPieces.Contains(alg[0]))
                {
                    dest = ParseSquare($"{alg[3]}{alg[4]}");
                    src = ParseSquare($"{alg[1]}{alg[2]}");
                    
                    return new Move(src, dest);
                }
                throw new NotationParsingException($"Unknown notation: {alg}");
            
            case 6:
                // doubly disambiguated capture: Nd3xe5
                if (ValidPieces.Contains(alg[0]) && alg[3] == 'x')
                {
                    dest = ParseSquare($"{alg[4]}{alg[5]}");
                    src = ParseSquare($"{alg[1]}{alg[2]}");
                    
                    return new Move(src, dest);
                }
                // capture promotion: fxe8=Q
                if (alg[1] == 'x' && alg[4] == '=' && ValidFiles.Contains(alg[0]) && validPromotions.Contains(alg[5]))
                {
                    // capture promotion
                    dest = ParseSquare($"{alg[2]}{alg[3]}");
                    src = (Indices[alg[0]], dest.rank - 1);
                        
                    uint promotion = Promotions[alg[5]];
                    if (board.side == 0 && board.GetPiece(src) == Pieces.WhitePawn)
                        return new Move(src, dest, pawn: true, promotion: promotion);
                    if (board.GetPiece(src) == Pieces.BlackPawn)
                        return new Move(src, dest, pawn: true, promotion: promotion);
                }
                
                throw new NotationParsingException($"Unknown notation: {alg}");
            default:
                throw new NotationParsingException($"Unknown notation: {alg}");
        }
    }
}