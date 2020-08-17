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
        public const string limitStrength = "setoption name UCI_LimitStrength value true";

        // Commands from engine
        // ====================
        public const string uciok = "uciok";
        public const string readyok = "readyok";
        public const string bestmove = "bestmove";

        //Commands requiring input parameter
        public static string Elo(int elo)
        {
            return "setoption name UCI_Elo value " + elo;
        }

        public static string SkillLevel(int skill)
        {
            return "setoption name Skill Level value " + skill;
        }
    }
}
