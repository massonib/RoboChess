namespace StockChessCS.Enums
{
    public enum PieceType
    {
        King,
        Queen,
        Bishop,
        Knight,
        Rook,
        Pawn
    }

    public enum MoveType
    {
        Move,
        Capture,  
        ShortCastle,
        LongCastle,
        Promotion,
        PromotionWithCapture,
        EnPassant
    }
}
