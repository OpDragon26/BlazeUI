using System.Linq;
using BlazeUI.Blaze.Board_Representation;
using BlazeUI.Blaze.Move_Generation;
using static BlazeUI.Blaze.Utils.MoveUtils;

namespace BlazeUI.Blaze.Utils;

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
        (int file, int rank) src;
        int flag = 0;
        
        switch (alg.Length)
        {
            case 2: // pawn move forward
                dest = ParseSquare(alg);
                int offset = board.side * 2 - 1;
                if (board.GetPiece(dest.file, dest.rank + offset) == (board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn))
                    src = (dest.file, dest.rank + offset);
                else if (board.GetPiece(dest.file, dest.rank + 2 * offset) == (board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn))
                {
                    src = (dest.file, dest.rank + 2 * offset);
                    flag = board.side == 0 ? 0b0001 : 0b1001;
                }
                else
                    throw new NotationParsingException($"No pawn can move to the given square: {alg}");

                return new Move(src, dest, type: flag, pawn: true);
            
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
                        
                        if (board.side == 0 && board.GetPiece(Indices[alg[0]], dest.rank - 1) == Pieces.WhitePawn)
                            return new Move((Indices[alg[0]], dest.rank - 1), dest, pawn: true, type: flag);
                        if (board.GetPiece(Indices[alg[0]], dest.rank + 1) == Pieces.BlackPawn)
                            return new Move((Indices[alg[0]], dest.rank + 1), dest, pawn: true, type: flag);
                    }
                    else if (ValidPieces.Contains(alg[0]))
                        // piece capture: Nxe4
                        // treated as it wasn't a capture
                        return ParseMove($"{alg[0]}{alg[2]}{alg[3]}", board);
                    else
                        throw new  NotationParsingException($"Invalid file or piece: {alg[0]}");
                }
                else if (alg[2] == '=')
                {
                    // promotion: e8=Q
                    if (validPromotions.Contains(alg[3]))
                    {
                        dest = ParseSquare($"{alg[0]}{alg[1]}");
                        src = board.GetPiece(dest.file, dest.rank + (board.side * 2 - 1)) == (board.side == 0 ? Pieces.WhitePawn : Pieces.BlackPawn) 
                            ? (dest.file, dest.rank - (board.side * 2 - 1)) : throw new NotationParsingException("No pawn can move to the given square");
                        return new Move(src, dest, pawn: true, promotion: Promotions[char.ToLower(alg[3])]);
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
                    throw new NotationParsingException($"Unknown notation: {alg}");
                }
                
                // doubly disambiguated move: Nd3e5
                if (ValidPieces.Contains(alg[0]))
                    return new Move(ParseSquare($"{alg[1]}{alg[2]}"), ParseSquare($"{alg[3]}{alg[4]}"));
                throw new NotationParsingException($"Unknown notation: {alg}");
            
            case 6:
                // doubly disambiguated capture: Nd3xe5
                if (ValidPieces.Contains(alg[0]) && alg[3] == 'x')
                    return new Move(ParseSquare($"{alg[1]}{alg[2]}"), ParseSquare($"{alg[4]}{alg[5]}"));
                // capture promotion: fxe8=Q
                if (alg[1] == 'x' && alg[4] == '=' && ValidFiles.Contains(alg[0]) && validPromotions.Contains(alg[5]))
                {
                    // capture promotion
                    dest = ParseSquare($"{alg[2]}{alg[3]}");
                        
                    if (board.side == 0 && board.GetPiece(Indices[alg[0]], dest.rank - 1) == Pieces.WhitePawn)
                        return new Move((Indices[alg[0]], dest.rank - 1), dest, pawn: true, promotion: Promotions[char.ToLower(alg[5])]);
                    if (board.GetPiece(Indices[alg[0]], dest.rank + 1) == Pieces.BlackPawn)
                        return new Move((Indices[alg[0]], dest.rank + 1), dest, pawn: true, promotion: Promotions[char.ToLower(alg[5])]);
                }
                
                throw new NotationParsingException($"Unknown notation: {alg}");
            
            case 7:
                if (alg[6] == '#' || alg[6] == '+') // move is a check or checkmate
                    return ParseMove($"{alg[0]}{alg[1]}{alg[2]}{alg[3]}{alg[4]}{alg[5]}", board);
                throw new NotationParsingException($"Unknown notation: {alg}");
            default:
                throw new NotationParsingException($"Unknown notation: {alg}");
        }
    }
}