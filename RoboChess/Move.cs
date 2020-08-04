using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboChess
{
    public class Move
    {
        public string Text;
        public PartialMove From;
        public PartialMove To;

        public Move(string text)
        {
            Text = text;
            From = new PartialMove(text.Substring(0, 2));
            To = new PartialMove(text.Substring(2, 2));
        }
    }

    public class PartialMove
    {
        public int Rank;
        public char File;
        public string Text;

        public PartialMove(string boardPosition)
        {
            Text = boardPosition;
            File = boardPosition.ToCharArray()[0];
            Rank = boardPosition.ToCharArray()[1];
        }
    }
}
