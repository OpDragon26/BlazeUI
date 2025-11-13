using System.Collections.Generic;
using Avalonia.Controls;

namespace BlazeUI.Board_Interface;

public class HighlightGrid : Grid
{
    private List<HighlightItem> highlights = new();
    
    private class HighlightItem((int x, int y) pos, string id)
    {
        public (int x, int y) Pos = pos;
        public string ID = id;
    }
}