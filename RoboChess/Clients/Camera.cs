using StockChessCS.Enums;
using StockChessCS.Helpers;
using StockChessCS.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using StockChessCS;
using StockChessCS.Interfaces;
using static RoboChess.Helpers.GetItem;

namespace RoboChess
{
    public class Camera
    {
        public TcpClient Client;
        public StreamWriter Writer;
        public StreamReader Reader;
        public bool Connected => Client != null && Client.Connected;

        public Camera(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            Client = client;
            Writer = writer;
            Reader = reader;
        }

        public void Close()
        {
            Client.Close();
        }

        public void Reset()
        {
            Writer.WriteLine("0");
        }

        public void Trigger()
        {
            Writer.WriteLine("1");
        }

        public List<PartialMove> ReadLine()
        {
            string message = "";
            try
            {
                message = Reader.ReadLine();
            }
            catch
            {
                //camera read error or the connection was reset
                //if (!Client.Connected) return;
                return null;
            }

            if (message.Contains("reset")) return null;
            var partialMoves = new List<PartialMove>();
            var substrings = message.Trim(new char[] { '(', ')' }).Split(',');
            for (var i = 1; i < substrings.Count(); i++)
            {
                var partial = substrings[i];
                if (partial.Equals("#ERR") || partial.Equals("n")) continue;
                else if (partialMoves.Any(p => p.Text.Equals(partial))) continue; //duplicate
                partialMoves.Add(new PartialMove(partial));
            }
            return partialMoves;
        }

        internal static Move GetMoveFromPartialMoves(List<PartialMove> partialMoves, MultiThreadedObservableCollection<IBoardItem> BoardItems,
            PieceColor playerColor)
        {
            Move move = null;
            GetPieces(partialMoves, BoardItems, playerColor, out var playerPieces, out var opponentPiece, out var squares);
            //If there are four partial moves, then it should be a castling move
            //There are only four possible castling moves: e1g1, e1c1, e8g8, and e8c8
            //so we are handling this differently than other moves.
            if (playerPieces.Count() == 2 && squares.Count() == 2)
            {
                var king = playerPieces.FirstOrDefault(p => p.Piece == PieceType.King);
                var targetSquare = squares.FirstOrDefault(p => p.File.Equals('c') || p.File.Equals('g'));
                if (king != null && targetSquare != null)
                {
                    move = new Move(king.Position() + targetSquare.Position());
                }

                //If the move is a valid castling move, great!
                //Otherwise, assume the there is noise. Since no other move can have two player pieces or two squares, 
                //remove the last one from each (this will be the smaller blob, since they are ordered by area).
                if (move != null) return move;
                else
                {
                    playerPieces.RemoveAt(1); //Decrement, and check the next option
                    squares.RemoveAt(1);
                }
            }

            //Check if this move is indeed supposed to be en passant.
            if (playerPieces.Count() == 1 && opponentPiece != null && squares.Count() == 1)
            {
                var moveType = Chess.GetMoveType(playerPieces[0], squares[0]);
                if (moveType == MoveType.EnPassant) move = new Move(playerPieces[0].Position() + squares[0].Position());
                //If not, then assume there is noise and just use the first two potential moves, 
                //since the detected blobs are listing in order of area. Since it could be either a target piece or square, 
                //we need to re-find the pieces.
                else
                {
                    partialMoves = partialMoves.Take(2).ToList();
                    GetPieces(partialMoves, BoardItems, playerColor, out playerPieces, out opponentPiece, out squares);
                }
            }

            //Else if m == 2, OR 3, OR was just changed to 2 in the if statement above
            //If m = 3 or 4, we are assuming that the third (and fourth) substring are noise.
            if (playerPieces.Count() > 0 && (opponentPiece != null || squares.Count() > 0))
            {
                if (opponentPiece != null)
                {
                    var moveType = Chess.GetMoveType(playerPieces[0], opponentPiece);
                    move = new Move(playerPieces[0].Position() + opponentPiece.Position());
                    if (moveType == MoveType.Promotion) move.Text += "q";
                }
                else
                {
                    var moveType = Chess.GetMoveType(playerPieces[0], squares[0]);
                    move = new Move(playerPieces[0].Position() + squares[0].Position());
                    if (moveType == MoveType.Promotion) move.Text += "q";
                }
            }
            return move;
        }

        /// <summary>
        /// Returns the first piece that belongs to the player. Does not consider castling, but does do En Passant.
        /// </summary>
        /// <param name="partialMoves"></param>
        /// <returns></returns>
        private static void GetPieces(List<PartialMove> partialMoves, MultiThreadedObservableCollection<IBoardItem> BoardItems,
            PieceColor playerColor, out List<ChessPiece> playerPieces, out ChessPiece opponentPiece, out List<BoardSquare> squares)
        {
            playerPieces = new List<ChessPiece>();
            squares = new List<BoardSquare>();
            opponentPiece = null;
            foreach (var partialMove in partialMoves)
            {
                var selectedPiece = GetChessPiece(partialMove, BoardItems);
                if (selectedPiece == null) squares.Add(GetBoardSquare(partialMove, BoardItems));
                else if (selectedPiece.Color == playerColor) playerPieces.Add(GetChessPiece(partialMove, BoardItems));
                else if (opponentPiece == null) opponentPiece = selectedPiece;
            }
        }
    }
}
