using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using BlazeUI.Blaze.API;
using BlazeUI.Blaze.Board_Representation;

namespace BlazeUI.Utils;

public static class BoardUIUtils
{
    public static Bitmap GetPieceBitmap(uint piece)
    {
        string pieceFile = $"{((piece & Pieces.ColorMask) == 0 ? "white" : "black")}_{PieceName[piece & Pieces.TypeMask]}.png";
        //Console.WriteLine($"{Convert.ToString(piece, toBase:2).PadLeft(4, '0')} -> {pieceFile}");
        return new Bitmap(Path.Combine("assets", "pieces", pieceFile));
    }
    
    private static readonly Dictionary<uint, string> PieceName = new()
    {
        { 0b000 , "pawn" },
        { 0b001 , "rook" },
        { 0b010 , "knight" },
        { 0b011 , "bishop" },
        { 0b100 , "queen" },
        { 0b101 , "king"}
    };
    
    public class LockState
    {
        private bool White;
        private bool Black;

        public void Lock()
        {
            White = true;
            Black = true;
        }

        public void Unlock()
        {
            White = false;
            Black = false;
        }

        public void Lock(Side side)
        {
            if (side == Side.White)
                White = true;
            else 
                Black = true;
        }

        public void Unlock(Side side)
        {
            if (side == Side.White)
                White = false;
            else
                Black = false;
        }

        public bool IsLocked(Side side)
        {
            return side == Side.White ? White : Black;
        }
    }
}

public static class Invert
{
    public static (int x, int y) Switch((int x, int y) from, Side to)
    {
        return to == Side.White ? ObjWhite(from) : ObjBlack(from);
    }

    private static (int x, int y) ObjWhite((int x, int y) obj)
    {
        return (obj.x, 7 - obj.y);
    }

    private static (int x, int y) ObjBlack((int x, int y) obj)
    {
        return (7 - obj.x, obj.y);
    }
}