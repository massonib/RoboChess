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

        public static void MovePiece(ChessPiece selectedPiece, IBoardItem target,
            MultiThreadedObservableCollection<IBoardItem> items, out MoveType moveType)
        {
            if(target is ChessPiece)
            {
                CapturePiece(selectedPiece, (ChessPiece)target, items, out moveType);
            }
            else
            {
                MovePiece(selectedPiece, (BoardSquare)target, items, out moveType);
            }
        }

        public static MoveType GetMoveType(ChessPiece selectedPiece, IBoardItem target)
        {
            if (target is ChessPiece) return GetCaptureType(selectedPiece, target);
            switch (selectedPiece.Piece)
            {
                case PieceType.King: return GetKingMoveType(selectedPiece, (BoardSquare)target);
                case PieceType.Pawn: return GetPawnMoveType(selectedPiece, (BoardSquare)target);
                default: return MoveType.Move;
            }
        }

        internal static void MovePiece(ChessPiece selectedPiece, BoardSquare selectedSquare,
            MultiThreadedObservableCollection<IBoardItem> items, out MoveType moveType)
        {
            moveType = MoveType.Move;
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
            moveType = GetKingMoveType(piece, targetSquare);

            //Update the piece locations
            if (moveType == MoveType.ShortCastle) 
            {
                var rook = items.OfType<ChessPiece>().Where(p => p.Color == piece.Color &&
                p.Piece == PieceType.Rook && p.File == 'h').FirstOrDefault();
                piece.File = 'g';
                rook.File = 'f';                
            }
            else if (moveType == MoveType.LongCastle)
            {
                moveType = MoveType.LongCastle;
                var rook = items.OfType<ChessPiece>().Where(p => p.Color == piece.Color &&
                p.Piece == PieceType.Rook && p.File == 'a').FirstOrDefault();

                piece.File = 'c';
                rook.File = 'd';
            }
            else 
            {
                Move(piece, targetSquare); 
            }
        }

        internal static MoveType GetKingMoveType(ChessPiece piece, BoardSquare targetSquare)
        {
            if (piece.File == 'e' && targetSquare.File == 'g') return MoveType.ShortCastle;
            else if (piece.File == 'e' && targetSquare.File == 'c') return MoveType.LongCastle;
            else return MoveType.Move;
        }

        private static void PawnMove(ChessPiece piece, BoardSquare targetSquare, MultiThreadedObservableCollection<IBoardItem> items, 
            out MoveType moveType)
        {
            //Default
            moveType = GetPawnMoveType(piece, targetSquare);
            Move(piece, targetSquare);

            if (moveType == MoveType.EnPassant)
            {
                //These pawns have an equal rank prior to the capture
                var opponentPawn = items.OfType<ChessPiece>().Where(p => p.Color != piece.Color &&
                p.Piece == PieceType.Pawn && p.Rank == piece.Rank && p.File == targetSquare.File).FirstOrDefault();
                items.Remove(opponentPawn);
            }
            //Change the piece to a queen for promotion. TODO: Player could select a knight in very special cases.
            if (moveType == MoveType.Promotion) piece.Piece = PieceType.Queen;       
        }

        private static MoveType GetPawnMoveType(ChessPiece piece, BoardSquare targetSquare)
        {
            //Check En passant prior to moving the pawn. If the pawn is moving diagonal, but it is not a capture, 
            //then it must be En Passant. The engine will determine if this move is valid.
            if (piece.File != targetSquare.File) return MoveType.EnPassant;
            if (IsPromotion(piece, targetSquare)) return MoveType.Promotion;
            return MoveType.Move;
        }

        internal static void CapturePiece(ChessPiece selectedPiece, ChessPiece otherPiece,
            MultiThreadedObservableCollection<IBoardItem> items, out MoveType moveType)
        {
            //First, get the moveType
            moveType = GetCaptureType(selectedPiece, otherPiece);

            //Update the pieces
            if (moveType == MoveType.PromotionWithCapture) selectedPiece.Piece = PieceType.Queen;
            selectedPiece.Rank = otherPiece.Rank;
            selectedPiece.File = otherPiece.File;
            items.Remove(otherPiece);   
        }

        internal static MoveType GetCaptureType(ChessPiece piece, IBoardItem target)
        {
            //Check for promotion with/without capture. Otherwise, it is just a standard capture.
            return IsPromotion(piece, target) ? MoveType.PromotionWithCapture : MoveType.Capture;
        }

        private static bool IsPromotion(ChessPiece piece, IBoardItem target)
        {
            //Check for promotion with/without capture.
            //Since pawns can never go backwards, if a pawn is moving to or capturing on rank 8 or 1, then it must be a promotion.
            return (piece.Piece == PieceType.Pawn && (target.Rank == 1 || target.Rank == 8));
        }
    }
}
