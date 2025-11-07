using BlazeUI.Blaze.Board_Representation;

namespace BlazeUI.Blaze.Evaluation;

public static class Evaluator
{
    // returns the heuristic evaluation of the board
    public static int StaticEvaluate(Board board)
    {
        int eval = 0;

        ulong all = board.AllPieces();
        
        if (!board.IsEndgame())
        {
            // pawns
            eval += EvaluationLookup.Lookup.PawnRegular(0, board.bitboards[Pieces.WhitePawn], board.bitboards[Pieces.BlackPawn]);
            eval += EvaluationLookup.Lookup.PawnRegular(1, board.bitboards[Pieces.BlackPawn], board.bitboards[Pieces.WhitePawn]);
            
            // rooks
            eval += EvaluationLookup.Lookup.RookRegular(0, board.bitboards[Pieces.WhiteRook], all, 
                board.bitboards[Pieces.WhitePawn], board.bitboards[Pieces.BlackPawn]);
            eval += EvaluationLookup.Lookup.RookRegular(1, board.bitboards[Pieces.BlackRook], all, 
                board.bitboards[Pieces.BlackPawn], board.bitboards[Pieces.WhitePawn]);
            
            // queens
            eval += EvaluationLookup.Lookup.QueenRegular(0, board.bitboards[Pieces.WhiteQueen], all);
            eval += EvaluationLookup.Lookup.QueenRegular(1, board.bitboards[Pieces.BlackQueen], all);
            
            // knight
            eval += EvaluationLookup.Lookup.KnightRegular(0, board.bitboards[Pieces.WhiteKnight], all);
            eval += EvaluationLookup.Lookup.KnightRegular(1, board.bitboards[Pieces.BlackKnight], all);
            
            // bishop
            eval += EvaluationLookup.Lookup.BishopRegular(0, board.bitboards[Pieces.WhiteBishop], all);
            eval += EvaluationLookup.Lookup.BishopRegular(1, board.bitboards[Pieces.BlackBishop], all);
            
            // king
            eval += MagicLookup.KingEvalLookup(board.KingPositions[0]).wEval;
            eval += MagicLookup.KingEvalLookup(board.KingPositions[1]).bEval;
            
            for (int file = 0; file < 8; file++)
            {
                // counts pawns on the file and applies a penalty for multiple on one file
                eval += Weights.DoublePawnPenalties[ulong.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.WhitePawn])];
                eval -= Weights.DoublePawnPenalties[ulong.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.BlackPawn])];
            }

            // add or take eval according to which side has castled
            if ((board.castled & 0b10) != 0) // white castled
                eval += Weights.CastlingBonus;
            else
            {
                if ((board.castling & 0b1000) != 0) // can't short castle
                    eval -= Weights.NoCastlingPenalty;
                if ((board.castling & 0b100) != 0) // can't long castle
                    eval -= Weights.NoCastlingPenalty;
            }
            
            if ((board.castled & 0b1) != 0) // black castled
                eval -= Weights.CastlingBonus;
            else
            {
                if ((board.castling & 0b10) != 0) // can't short castle
                    eval += Weights.NoCastlingPenalty;
                if ((board.castling & 0b1) != 0) // can't long castle
                    eval += Weights.NoCastlingPenalty;
            }
            
            
            // check if white's king is in the right spot (likely castled) to have its safety evaluated
            if ((Bitboards.KingSafetyAppliesWhite & BitboardUtils.GetSquare(board.KingPositions[0])) != 0)
            {
                // add to the eval based on the safety of white's king
                eval += MagicLookup.KingSafetyBonusLookup(board.KingPositions[0], board.WhitePieces());
                if ((Bitboards.KingMasks[board.KingPositions[0].file, board.KingPositions[0].rank] & board.BlackPieces()) != 0) // if there is an enemy piece adjacent to the king
                    eval -= 30;
            
                // take from eval if the pawns in front of the king are missing
                foreach (int file in Bitboards.AdjacentFiles[board.KingPositions[0].file])
                    if ((BitboardUtils.GetFile(file) & board.bitboards[Pieces.WhitePawn]) == 0)
                        eval -= 30;
            }

            if ((Bitboards.KingSafetyAppliesBlack & BitboardUtils.GetSquare(board.KingPositions[1])) != 0)
            {
                eval -= MagicLookup.KingSafetyBonusLookup(board.KingPositions[1], board.BlackPieces());
                if ((Bitboards.KingMasks[board.KingPositions[1].file, board.KingPositions[1].rank] & board.WhitePieces()) != 0) // if there is an enemy piece adjacent to the king
                    eval += 30;
            
                foreach (int file in Bitboards.AdjacentFiles[board.KingPositions[1].file])
                    if ((BitboardUtils.GetFile(file) & board.bitboards[Pieces.BlackPawn]) == 0)
                        eval += 30;
            }
        }
        else
        {
            // pawns
            eval += EvaluationLookup.Lookup.PawnEndgame(0, board.bitboards[Pieces.WhitePawn], board.bitboards[Pieces.BlackPawn]);
            eval += EvaluationLookup.Lookup.PawnEndgame(1, board.bitboards[Pieces.BlackPawn], board.bitboards[Pieces.WhitePawn]);
            
            // rooks
            eval += EvaluationLookup.Lookup.RookEndgame(0, board.bitboards[Pieces.WhiteRook], all);
            eval += EvaluationLookup.Lookup.RookEndgame(1, board.bitboards[Pieces.BlackRook], all);
            
            // queens
            eval += EvaluationLookup.Lookup.QueenEndgame(0, board.bitboards[Pieces.WhiteQueen], all);
            eval += EvaluationLookup.Lookup.QueenEndgame(1, board.bitboards[Pieces.BlackQueen], all);
            
            // knights
            eval += EvaluationLookup.Lookup.KnightEndgame(0, board.bitboards[Pieces.WhiteKnight], all);
            eval += EvaluationLookup.Lookup.KnightEndgame(1, board.bitboards[Pieces.BlackKnight], all);
            
            // bishops
            eval += EvaluationLookup.Lookup.BishopEndgame(0, board.bitboards[Pieces.WhiteBishop], all);
            eval += EvaluationLookup.Lookup.BishopEndgame(1, board.bitboards[Pieces.BlackBishop], all);
            
            // king
            eval += MagicLookup.KingEvalLookup(board.KingPositions[0]).wEvalEndgame;
            eval += MagicLookup.KingEvalLookup(board.KingPositions[1]).bEvalEndgame;
            
            for (int file = 0; file < 8; file++)
            {
                // counts pawns on the file and applies a penalty for multiple on one file
                eval += Weights.DoublePawnPenalties[ulong.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.WhitePawn])];
                eval -= Weights.DoublePawnPenalties[ulong.PopCount(BitboardUtils.GetFile(file) & board.bitboards[Pieces.BlackPawn])];
            }
        }

        return eval;
    }
}