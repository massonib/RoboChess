using System.Collections.ObjectModel;
using System.Linq;
using StockChessCS.Enums;
using StockChessCS.Interfaces;
using StockChessCS.Models;

namespace StockChessCS.Helpers
{
    public static class Chess
    {
        public static MultiThreadedObservableCollection<IBoardItem> BoardSetup()
        {
            MultiThreadedObservableCollection<IBoardItem> items = new MultiThreadedObservableCollection<IBoardItem>();
            var files = ("abcdefgh").ToArray();

            // Board squares
            foreach (var fl in files)
            {                
                for (int rank = 1; rank <= 8; rank++)
                {
                    items.Add(new BoardSquare { Rank = rank, File = fl, ItemType = ChessBoardItem.Square });
                }               
            }
            // Pawns
            foreach (var fl in files)
            { 
                items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                    Piece = PieceType.Pawn, Rank = 7, File = fl });                
                items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                    Piece = PieceType.Pawn, Rank = 2, File = fl });
            }
            // Black pieces
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.Rook, Rank = 8, File = 'a' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.Knight, Rank = 8, File = 'b' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.Bishop, Rank = 8, File = 'c' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.Queen, Rank = 8, File = 'd' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.King, Rank = 8, File = 'e' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.Bishop, Rank = 8, File = 'f' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.Knight, Rank = 8, File = 'g' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.Black,
                Piece = PieceType.Rook, Rank = 8, File = 'h' });
            // White pieces                       
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.Rook, Rank = 1, File = 'a' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.Knight, Rank = 1, File = 'b' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.Bishop, Rank = 1, File = 'c' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.Queen, Rank = 1, File = 'd' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.King, Rank = 1, File = 'e' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.Bishop, Rank = 1, File = 'f' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.Knight, Rank = 1, File = 'g' });
            items.Add(new ChessPiece { ItemType = ChessBoardItem.Piece, Color = PieceColor.White,
                Piece = PieceType.Rook, Rank = 1, File = 'h' });
            
            return items;
        }        

        public static void MovePiece(ChessPiece selectedPiece, BoardSquare selectedSquare,
            MultiThreadedObservableCollection<IBoardItem> items, out MoveType moveType)
        {
            moveType = MoveType.Standard;
            switch (selectedPiece.Piece)
            {
                case PieceType.King:
                    KingMove(selectedPiece, selectedSquare, items, out moveType);
                    break;
                case PieceType.Pawn:
                    PawnMove(selectedPiece, selectedSquare, items, out moveType);
                    break;
                default:                   
                    Move(selectedPiece, selectedSquare);
                    break;
            }
        }
               
        private static void Move(ChessPiece piece, BoardSquare square)
        {
            piece.Rank = square.Rank;
            piece.File = square.File;
        }

        private static void KingMove(ChessPiece piece, BoardSquare targetSquare, MultiThreadedObservableCollection<IBoardItem> items,
            out MoveType moveType)
        {            
            if (piece.File == 'e' && targetSquare.File == 'g') // Short castle
            {
                moveType = MoveType.ShortCastle;
                var rook = items.OfType<ChessPiece>().Where(p => p.Color == piece.Color &&
                p.Piece == PieceType.Rook && p.File == 'h').FirstOrDefault();

                piece.File = 'g';
                rook.File = 'f';                
            }
            else if (piece.File == 'e' && targetSquare.File == 'c') // Long castle
            {
                moveType = MoveType.LongCastle;
                var rook = items.OfType<ChessPiece>().Where(p => p.Color == piece.Color &&
                p.Piece == PieceType.Rook && p.File == 'a').FirstOrDefault();

                piece.File = 'c';
                rook.File = 'd';
            }
            else 
            {
                moveType = MoveType.Standard;
                Move(piece, targetSquare); 
            }
        }

        private static void PawnMove(ChessPiece piece, BoardSquare targetSquare, MultiThreadedObservableCollection<IBoardItem> items, 
            out MoveType moveType)
        {
            moveType = MoveType.Standard;

            // Promotion
            switch (piece.Color)
            {   
                case PieceColor.Black:
                    if (piece.Rank == 1)
                    {
                        piece.Piece = PieceType.Queen;
                        moveType = MoveType.Promotion;
                    }
                    break;
                case PieceColor.White:
                    if (piece.Rank == 8)
                    {
                        piece.Piece = PieceType.Queen;
                        moveType = MoveType.Promotion;
                    }
                    break;
            }
            // En passant
            if (piece.File != targetSquare.File)
            {
                moveType = MoveType.EnPassant;
                var opponentPawn = items.OfType<ChessPiece>().Where(p => p.Color != piece.Color &&
                p.Piece == PieceType.Pawn && p.Rank == piece.Rank && p.File == targetSquare.File).FirstOrDefault();

                items.Remove(opponentPawn);
            }

            Move(piece, targetSquare);
        }

        public static void CapturePiece(ChessPiece selectedPiece, ChessPiece otherPiece,
            MultiThreadedObservableCollection<IBoardItem> items)
        {
            selectedPiece.Rank = otherPiece.Rank;
            selectedPiece.File = otherPiece.File;
            items.Remove(otherPiece);
        }
    }
}
