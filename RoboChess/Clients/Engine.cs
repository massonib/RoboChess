using StockChessCS.Helpers;
using StockChessCS.Interfaces;

namespace RoboChess
{
    public class Engine
    {
        public IEngineService engine;
        public short DeepAnalysisTime = 1000;
        public short MoveValidationTime = 1;

        public Engine(IEngineService engineService)
        {
            engine = engineService;
            engine.StartEngine();
            engine.SendCommand(UciCommands.limitStrength);
        }

        internal void SendCommand(string text)
        {
            engine.SendCommand(text);
        }

        internal void SendMoves(string moves, short time)
        {
            var command = UciCommands.position + moves;
            engine.SendCommand(command);
            command = UciCommands.go_movetime + time.ToString();
            engine.SendCommand(command);
        }
    }
}
