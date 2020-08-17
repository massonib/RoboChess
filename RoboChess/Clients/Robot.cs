using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RoboChess
{
    public class Robot
    {
        public TcpClient Client;
        public StreamWriter Writer;
        public StreamReader Reader;
        public bool Connected => Client != null && Client.Connected;

        public Robot( TcpClient client, StreamReader reader, StreamWriter writer)
        {
            BoardRotation = new RobotBoardRotation(2);
            Client = client;
            Writer = writer;
            Reader = reader;
        }

        public string ReadLine()
        {
            var message = Reader.ReadLine();
            if(message == null) throw new Exception();
            if (message.Contains("completed")) RobotIsMoving = false;
            return message;
        }

        public void Close()
        {
            Client.Close();
        }

        internal void Reset()
        {
            RobotIsMoving = true;
            Writer.Flush();
            Writer.WriteLine("0");
            WaitForRobot();
        }

        private int robotsLongestSingleMoveInSeconds = 10;
        private bool RobotIsMoving = false;
        private void WaitForRobot()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (RobotIsMoving && stopWatch.Elapsed.TotalSeconds < robotsLongestSingleMoveInSeconds) { } //wait
            if (stopWatch.Elapsed.TotalSeconds >= robotsLongestSingleMoveInSeconds)
            {
                RobotIsMoving = false;
                Debug.WriteLine("Robot connection is bad. Try resetting the server.");
            }
        }

        #region Moving the Robot
        private RobotBoardRotation BoardRotation;

        internal void Move(PartialMove move)
        {
            RobotIsMoving = true;
            Writer.WriteLine("1," + BoardRotation.X(move) + "," + BoardRotation.Y(move));
            WaitForRobot();
        }

        internal void MoveToExtraQueenPosition()
        {
            RobotIsMoving = true;
            Writer.WriteLine("5");
            WaitForRobot();
        }

        internal void LowerArm()
        {
            RobotIsMoving = true;
            Writer.WriteLine("2");
            WaitForRobot();
        }

        internal void RaiseArm()
        {
            RobotIsMoving = true;
            Writer.WriteLine("3");
            WaitForRobot();
        }

        internal void GoToCapturedPieces(int numPlayerPiecesTaken)
        {
            int robotOffsetY, robotOffsetX;
            if (numPlayerPiecesTaken < 9)
            {
                robotOffsetX = numPlayerPiecesTaken - 1;
                robotOffsetY = 0;
            }
            else
            {
                robotOffsetX = numPlayerPiecesTaken - 8 - 1;
                robotOffsetY = 1;
            }
            RobotIsMoving = true;
            Writer.WriteLine("4," + robotOffsetX.ToString() + "," + robotOffsetY.ToString());
            WaitForRobot();
        }
        #endregion
    }
}
