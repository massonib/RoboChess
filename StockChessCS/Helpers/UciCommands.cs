namespace StockChessCS.Helpers
{
    public class UciCommands
    {
        // Commands to engine
        // ==================
        public const string uci = "uci";
        public const string isready = "isready";
        public const string ucinewgame = "ucinewgame";
        public const string position = "position startpos moves";
        public const string go_movetime = "go movetime";
        public const string stop = "stop";
        public const string limitStrength = "setoption UCI_LimitStrength value true";

        // Commands from engine
        // ====================
        public const string uciok = "uciok";
        public const string readyok = "readyok";
        public const string bestmove = "bestmove";

        //Commands requiring input parameter
        public static string SetElo(int elo)
        {
            return "setoption UCI_Elo value " + elo;
        }
    }
}
