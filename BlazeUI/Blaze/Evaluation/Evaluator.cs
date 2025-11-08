namespace BlazeUI.Blaze.Evaluation;
using Board_Representation;
using Magic_Lookup;
using Utils;
using static Weights;
using static PestoEval;
using static Board_Representation.Pieces;
using static Utils.EvalUtils;

public static class Evaluator
{
    // returns the heuristic evaluation of the board
    public static int StaticEvaluate(Board board)
    {
        Eval eval = new();
        
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
                eval.MgWhite -= whitePawns * MgDoublePawnPenalty;
                eval.EgWhite -= whitePawns * EgDoublePawnPenalty;
                if (IsPawnIsolated(board.bitboards[WhitePawn], file))
                {
                    eval.MgWhite -= whitePawns * MgIsolatedPawnPenalty;
                    eval.EgWhite -= whitePawns * EgIsolatedPawnPenalty;
                }
            }

            if (blackPawns != 0)
            {
                eval.MgBlack -= blackPawns * MgDoublePawnPenalty;
                eval.EgBlack -= blackPawns * EgDoublePawnPenalty;
                if (IsPawnIsolated(board.bitboards[BlackPawn], file))
                {
                    eval.MgBlack -= blackPawns * MgIsolatedPawnPenalty;
                    eval.EgBlack -= blackPawns * EgIsolatedPawnPenalty;
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
                        eval.MgWhite += MgValue[piece] + GetWhiteMgEval(piece, file, rank);
                        eval.EgWhite += EgValue[piece] + GetWhiteEgEval(piece, file, rank);

                        if (piece == WhitePawn &&
                            IsPawnPassedWhite(board.bitboards[BlackPawn], file, rank))
                        {
                            eval.MgWhite += MgPassedBonus[rank];
                            eval.EgWhite += EgPassedBonus[rank];
                        }
                        
                        else if (piece == WhiteRook && (pawns & BitboardUtils.GetFile(file)) == 0)
                            eval.MgWhite += OpenFileAdvantage;
                    }
                    else
                    {
                        piece = TypeOf(piece);
                        eval.MgBlack += MgValue[piece] + GetBlackMgEval(piece, file, rank);
                        eval.EgBlack += EgValue[piece] + GetBlackEgEval(piece, file, rank);
                        
                        if (piece == WhitePawn &&
                            IsPawnPassedBlack(board.bitboards[WhitePawn], file, rank))
                        {
                            eval.MgBlack += MgPassedBonus[7 - rank];
                            eval.EgBlack += EgPassedBonus[7 - rank];
                        }
                        
                        else if (piece == WhiteRook && (pawns & BitboardUtils.GetFile(file)) == 0)
                            eval.MgBlack += OpenFileAdvantage;
                    }
                }
            }
        }
        
        // add or take eval according to which side has castled
        if ((board.castled & 0b10) != 0) // white castled
            eval.MgWhite += CastlingBonus;
        else
        {
            if ((board.castling & 0b1000) != 0) // can't short castle
                eval.MgWhite -= NoCastlingPenalty;
            if ((board.castling & 0b100) != 0) // can't long castle
                eval.MgWhite -= NoCastlingPenalty;
        }
            
        if ((board.castled & 0b1) != 0) // black castled
            eval.MgWhite += CastlingBonus;
        else
        {
            if ((board.castling & 0b10) != 0) // can't short castle
                eval.MgBlack -= NoCastlingPenalty;
            if ((board.castling & 0b1) != 0) // can't long castle
                eval.MgBlack -= NoCastlingPenalty;
        }
        
        // check if white's king is in the right spot (likely castled) to have its safety evaluated
        if ((Bitboards.KingSafetyAppliesWhite & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
            // take from eval if the pawns in front of the king are missing
            eval.MgWhite -= WhiteKingSafetyPenalty(board.KingPositions[0].file, board.bitboards[WhitePawn]);
        else
            eval.MgWhite -= NoCastlingPenalty;

        if ((Bitboards.KingSafetyAppliesBlack & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
            eval.MgWhite -= BlackKingSafetyPenalty(board.KingPositions[1].file, board.bitboards[BlackPawn]);
        else
            eval.MgBlack -= NoCastlingPenalty;

        return eval.Calculate();
    }

    private struct Eval
    {
        public int MgWhite;
        public int MgBlack;
        public int EgWhite;
        public int EgBlack;
        public int GamePhase;

        public int Calculate()
        {
            int mgScore = MgWhite - MgBlack;
            int egScore = EgWhite - EgBlack;
            if (GamePhase > 24)
                GamePhase = 24;
            int egPhase = 24 - GamePhase;

            return (mgScore * GamePhase + egScore * egPhase) / 24;
        }
    }
}