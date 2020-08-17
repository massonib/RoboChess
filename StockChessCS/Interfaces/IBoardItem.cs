using StockChessCS.Enums;

namespace StockChessCS.Interfaces
{
    public interface IBoardItem
    {
        int Rank { get; set; }
        char File { get; set; }
        string Position();
        ChessBoardItem ItemType { get; set; }
    }
}
