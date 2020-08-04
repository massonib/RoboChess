using System;

namespace RoboChess
{
    public class Move
    {
        public string Text;
        public PartialMove From;
        public PartialMove To;

        private bool _isPromotion;
        public bool IsPromotion 
        {
            get => _isPromotion;
            set 
            {
                _isPromotion = value;
                if(value) Text += "q";
            } 
        }

        public Move(string text)
        {
            Text = text;
            From = new PartialMove(text.Substring(0, 2));
            To = new PartialMove(text.Substring(2, 2));
        }

        public Move(PartialMove from, PartialMove to)
        {
            Text = from.Text + to.Text;
            From = from;
            To = to;
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
            File = Convert.ToChar(boardPosition.Substring(0, 1));
            Rank = Convert.ToInt32(boardPosition.Substring(1, 1));
        }
    }
}
