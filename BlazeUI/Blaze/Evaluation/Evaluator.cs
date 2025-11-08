namespace BlazeUI.Blaze.Evaluation;
using Board_Representation;
using Magic_Lookup;
using Utils;
using static EvaluationLookup;

public static class Evaluator
{
    // returns the heuristic evaluation of the board
    public static int StaticEvaluate(Board board)
    {
        EvalState eval = new();
        
        Lookup.WhitePawnLookup(board.bitboards[Pieces.WhitePawn], board.bitboards[Pieces.BlackPawn], ref eval);
        Lookup.BlackPawnLookup(board.bitboards[Pieces.BlackPawn], board.bitboards[Pieces.WhitePawn], ref eval);
        
        Lookup.WhiteRookLookup(board.bitboards[Pieces.WhiteRook], board.AllPieces(), board.AllPawns(),  ref eval);
        Lookup.BlackRookLookup(board.bitboards[Pieces.BlackRook], board.AllPieces(), board.AllPawns(),  ref eval);
        
        Lookup.WhiteKnightLookup(board.bitboards[Pieces.WhiteKnight], board.AllPieces(), ref eval);
        Lookup.BlackKnightLookup(board.bitboards[Pieces.BlackKnight], board.AllPieces(), ref eval);

        Lookup.WhiteBishopLookup(board.bitboards[Pieces.WhiteBishop], board.AllPieces(), ref eval);
        Lookup.BlackBishopLookup(board.bitboards[Pieces.BlackBishop], board.AllPieces(), ref eval);
        
        Lookup.WhiteQueenLookup(board.bitboards[Pieces.WhiteQueen], board.AllPieces(), ref eval);
        Lookup.BlackQueenLookup(board.bitboards[Pieces.BlackQueen], board.AllPieces(), ref eval);
        
        MagicLookup.KingEvalLookup(board.KingPositions[0]).WhiteFinal(ref eval);
        MagicLookup.KingEvalLookup(board.KingPositions[1]).BlackFinal(ref eval);
        
        if ((board.castled & 0b10) == 0) // white hasn't castled
        {
            if ((board.castling & 0b1000) != 0) // can't short castle
                eval.MgScore -= Weights.NoCastlingPenalty;
            if ((board.castling & 0b100) != 0) // can't long castle
                eval.MgScore -= Weights.NoCastlingPenalty;
        }
            
        if ((board.castled & 0b1) != 0) // black hasn't castled
        {
            if ((board.castling & 0b10) != 0) // can't short castle
                eval.MgScore += Weights.NoCastlingPenalty;
            if ((board.castling & 0b1) != 0) // can't long castle
                eval.MgScore += Weights.NoCastlingPenalty;
        }
        
        // check if white's king is in the right spot (likely castled) to have its safety evaluated
        if ((Bitboards.KingSafetyAppliesWhite & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
        {
            // take from eval if the pawns in front of the king are missing
            foreach (int file in Bitboards.AdjacentFiles[board.KingPositions[0].file])
                if ((BitboardUtils.GetFile(file) & board.bitboards[Pieces.WhitePawn]) == 0)
                    eval.MgScore -= 30;
        }

        if ((Bitboards.KingSafetyAppliesBlack & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
        {
            foreach (int file in Bitboards.AdjacentFiles[board.KingPositions[1].file])
                if ((BitboardUtils.GetFile(file) & board.bitboards[Pieces.BlackPawn]) == 0)
                    eval.MgScore += 30;
        }
        
        return eval.TaperedEval();
    }
}