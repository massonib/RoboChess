using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboChess
{
    public class RobotBoardRotation
    {
        private readonly Dictionary<char, int> FileDictionary;
        private readonly Dictionary<int, int> RankDictionary;
        private readonly bool rankCorrespondsToRobotX;

        public string X(PartialMove move)
        {
            if (rankCorrespondsToRobotX)
            {
                return RankDictionary[move.Rank].ToString();
            }
            else return FileDictionary[move.File].ToString();
        }

        public string Y(PartialMove move)
        {
            if (rankCorrespondsToRobotX)
            {
                return FileDictionary[move.File].ToString(); 
            }
            else return RankDictionary[move.Rank].ToString();
        }

        public RobotBoardRotation(int boardRotation)
        {
            if (boardRotation == 1 )
            {
                rankCorrespondsToRobotX = true;
                //Board is rotated such that the white pieces are closest to the robot (bottom corner == a1)
                FileDictionary = new Dictionary<char, int>
                {
                    { 'a', 0 },
                    { 'b', 1 },
                    { 'c', 2 },
                    { 'd', 3 },
                    { 'e', 4 },
                    { 'f', 5 },
                    { 'g', 6 },
                    { 'h', 7 },
                };
                RankDictionary = new Dictionary<int, int>
                {
                    { 1, 0 },
                    { 2, 1 },
                    { 3, 2 },
                    { 4, 3 },
                    { 5, 4 },
                    { 6, 5 },
                    { 7, 6 },
                    { 8, 7 },
                };
            }
            else if (boardRotation == 2)
            {
                rankCorrespondsToRobotX = false;
                //Board is rotated with h1 as the bottom corner (player is closest to white)
                FileDictionary = new Dictionary<char, int>
                {
                    { 'a', 7 },
                    { 'b', 6 },
                    { 'c', 5 },
                    { 'd', 4 },
                    { 'e', 3 },
                    { 'f', 2 },
                    { 'g', 1 },
                    { 'h', 0 },
                };
                RankDictionary = new Dictionary<int, int>
                {
                    { 1, 0 },
                    { 2, 1 },
                    { 3, 2 },
                    { 4, 3 },
                    { 5, 4 },
                    { 6, 5 },
                    { 7, 6 },
                    { 8, 7 },
                };
            }
        }
    }
}
