namespace BlazeUI.Blaze.Evaluation;
using Board_Representation;
using Magic_Lookup;
using Utils;

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

            int whitePawns = EvalUtils.CountPawns(board.bitboards[Pieces.WhitePawn], file);
            int blackPawns = EvalUtils.CountPawns(board.bitboards[Pieces.BlackPawn], file);

            if (whitePawns != 0)
            {
                eval.MgWhite -= whitePawns * Weights.MgDoublePawnPenalty;
                eval.EgWhite -= whitePawns * Weights.EgDoublePawnPenalty;
                if (EvalUtils.IsPawnIsolated(board.bitboards[Pieces.WhitePawn], file))
                {
                    eval.MgWhite -= whitePawns * Weights.MgIsolatedPawnPenalty;
                    eval.EgWhite -= whitePawns * Weights.EgIsolatedPawnPenalty;
                }
            }

            if (blackPawns != 0)
            {
                eval.MgBlack -= blackPawns * Weights.MgDoublePawnPenalty;
                eval.EgBlack -= blackPawns * Weights.EgDoublePawnPenalty;
                if (EvalUtils.IsPawnIsolated(board.bitboards[Pieces.BlackPawn], file))
                {
                    eval.MgBlack -= blackPawns * Weights.MgIsolatedPawnPenalty;
                    eval.EgBlack -= blackPawns * Weights.EgIsolatedPawnPenalty;
                }
            }
            
            for (int rank = 0; rank < 8; rank++)
            {
                if ((allPieces & BitboardUtils.GetSquare(file, rank)) != 0)
                {
                    uint piece = board.GetPiece(file, rank);
                    eval.GamePhase += PestoEval.PhaseIncrement[Pieces.TypeOf(piece)];
                    
                    if (Pieces.ColorOf(piece) == 0)
                    {
                        eval.MgWhite += PestoEval.MgValue[piece] + PestoEval.GetWhiteMgEval(piece, file, rank);
                        eval.EgWhite += PestoEval.EgValue[piece] + PestoEval.GetWhiteEgEval(piece, file, rank);

                        if (piece == Pieces.WhitePawn &&
                            EvalUtils.IsPawnPassedWhite(board.bitboards[Pieces.BlackPawn], file, rank))
                        {
                            eval.MgWhite += Weights.MgPassedBonus[rank];
                            eval.EgWhite += Weights.EgPassedBonus[rank];
                        }
                        
                        else if (piece == Pieces.WhiteRook && (pawns & BitboardUtils.GetFile(file)) == 0)
                            eval.MgWhite += Weights.OpenFileAdvantage;
                    }
                    else
                    {
                        piece = Pieces.TypeOf(piece);
                        eval.MgBlack += PestoEval.MgValue[piece] + PestoEval.GetBlackMgEval(piece, file, rank);
                        eval.EgBlack += PestoEval.EgValue[piece] + PestoEval.GetBlackEgEval(piece, file, rank);
                        
                        if (piece == Pieces.WhitePawn &&
                            EvalUtils.IsPawnPassedBlack(board.bitboards[Pieces.WhitePawn], file, rank))
                        {
                            eval.MgBlack += Weights.MgPassedBonus[7 - rank];
                            eval.EgBlack += Weights.EgPassedBonus[7 - rank];
                        }
                        
                        else if (piece == Pieces.WhiteRook && (pawns & BitboardUtils.GetFile(file)) == 0)
                            eval.MgBlack += Weights.OpenFileAdvantage;
                    }
                }
            }
        }
        
        // add or take eval according to which side has castled
        if ((board.castled & 0b10) != 0) // white castled
            eval.MgWhite += Weights.CastlingBonus;
        else
        {
            if ((board.castling & 0b1000) != 0) // can't short castle
                eval.MgWhite -= Weights.NoCastlingPenalty;
            if ((board.castling & 0b100) != 0) // can't long castle
                eval.MgWhite -= Weights.NoCastlingPenalty;
        }
            
        if ((board.castled & 0b1) != 0) // black castled
            eval.MgWhite += Weights.CastlingBonus;
        else
        {
            if ((board.castling & 0b10) != 0) // can't short castle
                eval.MgBlack -= Weights.NoCastlingPenalty;
            if ((board.castling & 0b1) != 0) // can't long castle
                eval.MgBlack -= Weights.NoCastlingPenalty;
        }
        
        // check if white's king is in the right spot (likely castled) to have its safety evaluated
        if ((Bitboards.KingSafetyAppliesWhite & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
            // take from eval if the pawns in front of the king are missing
            eval.MgWhite -= EvalUtils.WhiteKingSafetyPenalty(board.KingPositions[0].file, board.bitboards[Pieces.WhitePawn]);
        else
            eval.MgWhite -= Weights.NoCastlingPenalty;

        if ((Bitboards.KingSafetyAppliesBlack & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
            eval.MgWhite -= EvalUtils.BlackKingSafetyPenalty(board.KingPositions[1].file, board.bitboards[Pieces.BlackPawn]);
        else
            eval.MgBlack -= Weights.NoCastlingPenalty;

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