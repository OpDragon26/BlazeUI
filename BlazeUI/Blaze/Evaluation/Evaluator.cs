namespace BlazeUI.Blaze.Evaluation;
using Board_Representation;
using Magic_Lookup;
using Utils;
using static Weights;
using static PestoEval;
using static Board_Representation.Pieces;
using static Utils.EvalUtils;
using static EvaluationLookup;

public static class Evaluator
{
    // returns the heuristic evaluation of the board
    public static int StaticEvaluate(Board board)
    {
        Eval eval = new();
        
        PieceWiseEval(board, ref eval);
        
        // add or take eval according to which side has castled
        if ((board.castled & 0b10) != 0) // white castled
            eval.MiddleGameWhite += CastlingBonus;
        else 
            eval.MiddleGameWhite -= int.PopCount(board.castling & 0b1100) * NoCastlingPenalty;
            
        if ((board.castled & 0b1) != 0) // black castled
            eval.MiddleGameWhite += CastlingBonus;
        else
            eval.MiddleGameBlack -= int.PopCount(board.castling & 0b0011) * NoCastlingPenalty;
        
        // check if white's king is in the right spot (likely castled) to have its safety evaluated
        if ((Masks.KingSafetyAppliesWhite & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
            // take from eval if the pawns in front of the king are missing
            eval.MiddleGameWhite -= WhiteKingSafetyPenalty(board.KingPositions[0].file, board.bitboards[WhitePawn]);

        if ((Masks.KingSafetyAppliesBlack & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
            eval.MiddleGameWhite -= BlackKingSafetyPenalty(board.KingPositions[1].file, board.bitboards[BlackPawn]);

        return eval.Calculate();
    }

    private static void PieceWiseEval(Board board, ref Eval eval)
    {
        ulong allPieces = board.AllPieces();
        ulong pawns = board.AllPawns();
        
        for (int file = 0; file < 8; file++)
        {
            if ((allPieces & BitboardUtils.GetFile(file)) == 0)
                continue;

            int whitePawns = CountPawns(board.bitboards[WhitePawn], file);
            int blackPawns = CountPawns(board.bitboards[BlackPawn], file);
            
            if (whitePawns != 0)
            {
                eval.MiddleGameWhite -= (whitePawns - 1) * MgDoublePawnPenalty;
                eval.EndGameWhite -= (whitePawns - 1) * EgDoublePawnPenalty;
                
                if (IsPawnIsolated(board.bitboards[WhitePawn], file))
                {
                    eval.MiddleGameWhite -= whitePawns * MgIsolatedPawnPenalty;
                    eval.EndGameWhite -= whitePawns * EgIsolatedPawnPenalty;
                }
            }

            if (blackPawns != 0)
            {
                eval.MiddleGameBlack -= (blackPawns - 1) * MgDoublePawnPenalty;
                eval.EndGameBlack -= (blackPawns - 1) * EgDoublePawnPenalty;
                
                if (IsPawnIsolated(board.bitboards[BlackPawn], file))
                {
                    eval.MiddleGameBlack -= blackPawns * MgIsolatedPawnPenalty;
                    eval.EndGameBlack -= blackPawns * EgIsolatedPawnPenalty;
                }
            }
            
            for (int rank = 0; rank < 8; rank++)
            {
                if ((allPieces & BitboardUtils.GetSquare(file, rank)) != 0)
                {
                    uint piece = board.GetPiece(file, rank);
                    
                    eval.GamePhase += PhaseIncrement[TypeOf(piece)];
                    
                    if (ColorOf(piece) == 0)
                    {
                        eval.MiddleGameWhite += MiddleGameVal[piece] + GetWhiteMgEval(piece, file, rank);
                        eval.EndGameWhite += EndGameVal[piece] + GetWhiteEgEval(piece, file, rank);

                        if (piece == WhitePawn &&
                            IsPawnPassedWhite(board.bitboards[BlackPawn], file, rank))
                        {
                            eval.MiddleGameWhite += MiddleGamePassedBonus[rank];
                            eval.EndGameWhite += EndGamePassedBonus[rank];
                        }
                        else
                        {
                            eval.MiddleGameWhite += piece switch
                            {
                                WhiteRook => MagicLookup.RookMobilityLookup((file, rank), allPieces) + (((pawns & BitboardUtils.GetFile(file)) == 0) ? OpenFileAdvantage : 0),
                                WhiteBishop => MagicLookup.BishopMobilityLookup((file, rank), allPieces),
                                WhiteQueen => MagicLookup.RookMobilityLookup((file, rank), allPieces) + MagicLookup.BishopMobilityLookup((file, rank), allPieces),
                                _ => 0
                            };
                            
                            if (piece == WhiteRook && (pawns & BitboardUtils.GetFile(file)) == 0)
                                eval.MiddleGameWhite += OpenFileAdvantage;
                        }
                    }
                    else
                    {
                        piece = TypeOf(piece);
                        eval.MiddleGameBlack += MiddleGameVal[piece] + GetBlackMgEval(piece, file, rank);
                        eval.EndGameBlack += EndGameVal[piece] + GetBlackEgEval(piece, file, rank);
                        
                        if (piece == WhitePawn &&
                            IsPawnPassedBlack(board.bitboards[WhitePawn], file, rank))
                        {
                            eval.MiddleGameBlack += MiddleGamePassedBonus[7 - rank];
                            eval.EndGameBlack += EndGamePassedBonus[7 - rank];
                        }
                        else
                        {
                            eval.MiddleGameBlack += piece switch
                            {
                                WhiteRook => MagicLookup.RookMobilityLookup((file, rank), allPieces) + (((pawns & BitboardUtils.GetFile(file)) == 0) ? OpenFileAdvantage : 0),
                                WhiteBishop => MagicLookup.BishopMobilityLookup((file, rank), allPieces),
                                WhiteQueen => MagicLookup.RookMobilityLookup((file, rank), allPieces) + MagicLookup.BishopMobilityLookup((file, rank), allPieces),
                                _ => 0
                            };
                        }
                    }
                }
            }
        }
    }

    private static void PieceWiseEvalLookup(Board board, ref Eval eval)
    {
        eval.AddWhite(Lookup.RookEvalLookupWhite(board.bitboards[WhiteRook], board.AllPawns()));
        eval.AddBlack(Lookup.RookEvalLookupBlack(board.bitboards[BlackRook], board.AllPawns()));
        
        eval.AddWhite(Lookup.BishopEvalLookupWhite(board.bitboards[WhiteBishop]));
        eval.AddBlack(Lookup.BishopEvalLookupBlack(board.bitboards[BlackBishop]));
        
        eval.AddWhite(Lookup.KnightEvalLookupWhite(board.bitboards[WhiteKnight]));
        eval.AddBlack(Lookup.KnightEvalLookupBlack(board.bitboards[BlackKnight]));
        
        eval.AddWhite(Lookup.QueenEvalLookupWhite(board.bitboards[WhiteQueen]));
        eval.AddBlack(Lookup.QueenEvalLookupBlack(board.bitboards[BlackQueen]));
        
        eval.AddWhite(Lookup.PawnEvalLookupWhite(board.bitboards[WhitePawn], board.bitboards[BlackPawn]));
        eval.AddBlack(Lookup.PawnEvalLookupBlack(board.bitboards[BlackPawn], board.bitboards[WhitePawn]));
        
        eval.MiddleGameWhite += GetWhiteMgEval(WhiteKing, board.KingPositions[0].file, board.KingPositions[0].rank);
        eval.EndGameWhite += GetWhiteEgEval(WhiteKing, board.KingPositions[0].file, board.KingPositions[0].rank);
        
        eval.MiddleGameBlack += GetBlackMgEval(WhiteKing, board.KingPositions[1].file, board.KingPositions[1].rank);
        eval.EndGameBlack += GetBlackEgEval(WhiteKing, board.KingPositions[1].file, board.KingPositions[1].rank);
    }

    public static int BareBonesEval(Board board)
    {
        Eval eval = new();
        PieceWiseEval(board, ref eval);
        return eval.Calculate();
    }

    public static int BareBonesEvalLookup(Board board)
    {
        Eval eval = new();
        PieceWiseEvalLookup(board, ref eval);
        
        return eval.Calculate();
    }
}