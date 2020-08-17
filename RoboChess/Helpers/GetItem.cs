using StockChessCS;
using StockChessCS.Interfaces;
using StockChessCS.Models;
using System.Linq;

namespace RoboChess.Helpers
{
    public static class GetItem
    {
        public static ChessPiece GetChessPiece(PartialMove position, MultiThreadedObservableCollection<IBoardItem> BoardItems)
        {
            return BoardItems.OfType<ChessPiece>().Where(p => p.Rank == position.Rank && p.File == position.File).FirstOrDefault();
        }

        public static BoardSquare GetBoardSquare(PartialMove position, MultiThreadedObservableCollection<IBoardItem> BoardItems)
        {
            return BoardItems.OfType<BoardSquare>().Where(p => p.Rank == position.Rank && p.File == position.File).FirstOrDefault();
        }

        public static IBoardItem GetBoardItem(PartialMove position, MultiThreadedObservableCollection<IBoardItem> BoardItems)
        {
            //Return chess pieces as a higher priority.
            var piece = GetChessPiece(position, BoardItems);
            if(piece == null) return GetBoardSquare(position, BoardItems);
            return piece;
        }
    }
}
