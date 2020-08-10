using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using StockChessCS;
using StockChessCS.Enums;
using StockChessCS.Helpers;
using StockChessCS.Interfaces;
using StockChessCS.Models;
using EasyModbus;
using System.Net.Sockets;
using System.Net;
using System.IO;
using RoboChess.Helpers;
using System.Diagnostics;

namespace RoboChess
{
    public class MainViewModel : Observable
    {

        #region Properties
        private bool _canStartGame;
        public bool CanStartGame
        {
            get => _canStartGame;
            set => Set(ref _canStartGame, value);
        }       

        private bool _isPlayerMove;
        public bool IsPlayerMove
        {
            get => _isPlayerMove;
            set
            {
                Set(ref _isPlayerMove, value);
                if (value)
                {
                    RedLight(false);
                    GreenLight(true);
                    AddToLog("Player's " + (PlayerIsWhite ? "(White)" : "(Black)") + " turn" );
                    Task.Run(() => WaitForPlayer());
                }
                else
                {
                    RedLight(true);
                    GreenLight(false);
                    //Now it is the robot's turn. It is allowed to take longer now, since it is not validating.
                    AddToLog("Robot's " + (!PlayerIsWhite ? "(White)" : "(Black)") + " turn");
                    DeeperMoveAnalysis();
                }
            }
        }

        private string _playerMove;
        public string PlayerMove
        {
            get => _playerMove;
            set
            {
                Set(ref _playerMove, value);
                SendMoveToEngine(value);
            }
        }

        private bool _checkMate;
        public bool CheckMate
        {
            get => _checkMate;
            set => Set(ref _checkMate, value);
        }

        private bool _playerIsWhite = true;
        public bool PlayerIsWhite
        {
            get => _playerIsWhite;
            set
            {
                Set(ref _playerIsWhite, value);
                NewGame();
            }
        }

        private int _elo = 1500;
        public int Elo
        {
            get => _elo;
            set
            {
                Set(ref _elo, value);
                engine.SendCommand(UciCommands.SetElo(value));
            }
        }

        private MultiThreadedObservableCollection<IBoardItem> _boardItems;
        public MultiThreadedObservableCollection<IBoardItem> BoardItems
        {
            get => _boardItems;
            set => Set(ref _boardItems, value);
        }

        private IEngineService engine;
        private short deepAnalysisTime = 5000;
        private short moveValidationTime = 1;

        #endregion

        #region Text Boxes Logging && Moves
        private readonly StringBuilder Log;

        private string _logger;
        public string Logger
        {
            get => _logger;
            set => Set(ref _logger, value);
        }
        private void AddToLog(string text)
        {
            Log.Append(" " + text + "\n");
            Logger = Log.ToString();
        }

        private readonly StringBuilder Moves;

        private string _gameMoves;
        public string GameMoves
        {
            get => _gameMoves;
            set => Set(ref _gameMoves, value);
        }

        private void AddMove(string move)
        {
            Moves.Append(" " + move);
            GameMoves = Moves.ToString();
        }

        private void ResetTextBoxes()
        {
            Moves.Clear();
            GameMoves = Moves.ToString();
            Log.Clear();
            Logger = Log.ToString();
        }
        #endregion

        #region Commands
        public ICommand ResetServerCommand { get; }
        public ICommand NewGameCommand { get; }
        public ICommand LoadGameCommand { get; }
        public ICommand CloseMessageCommand { get; }
        #endregion

        #region Constructor
        public MainViewModel(IEngineService es)
        {
            Moves = new StringBuilder();
            Log = new StringBuilder();
            engine = es;
            BoardItems = Chess.BoardSetup();
            engine.EngineMessage += EngineMessage;
            engine.StartEngine();
            engine.SendCommand(UciCommands.limitStrength);
            BoardRotation = new RobotBoardRotation(2);

            ConnectToMoxa();
            SetupTCP();

            ResetServerCommand = new CommandWithNoCondition(ResetServer);
            NewGameCommand = new CommandWithNoCondition(NewGame);
            LoadGameCommand = new CommandWithNoCondition(LoadGame);
            CloseMessageCommand = new CommandWithNoCondition(CloseMessage);
        }
        #endregion

        #region NewGame

        private void NewGameAsync(string[] moves = null)
        {
            //Exit current threads if possible
            exitWaitForPlayer = true;
            Task.Delay(1000).Wait();
            exitWaitForPlayer = false;

            //Initialize variables 
            numPlayerPiecesTaken = 0;
            ResetTextBoxes();
            CheckMate = false;

            //Initialize the digital chess pieces
            BoardItems = Chess.BoardSetup();

            //Initialize the engine
            engine.SendCommand(UciCommands.ucinewgame);
            engine.SendCommand(UciCommands.limitStrength);
            engine.SendCommand(UciCommands.SetElo(Elo));

            //Load the game or start a new one.
            var playerTurn = PlayerIsWhite;
            if (moves != null && moves.Length > 0 && moves[0].Length > 0)
            {
                try
                {
                    playerTurn = LoadGameFrom(moves);
                    AddToLog("Game loaded successfully");
                }
                catch
                {
                    NewMessage("Error", "Invalid input moves. Text must be in the format: 'e2e4 g8f6', and contain only valid moves." +
                        " Starting new game.");
                    WaitForMessageToClose();
                }
            }

            //Wait for the player to reset the board if the robot is the first to move.
            if (!playerTurn)
            {
                NewMessage("Setup", "After the board has been reset, press OK.");
                WaitForMessageToClose();
            }

            //Set things to the home position and then reset the camera
            OpenGripper();
            ResetRobot();
            ResetCamera();

            //This boolean controls whose turn it is. It also triggers events.
            IsPlayerMove = playerTurn;
        }

        //NewGame is called asyncronously to allow the WaitForMessageToClose() method to work properly,
        //otherwise, the main thread would be blocked and the message cannot be closed by the user.
        private void NewGame() => Task.Run(() => NewGameAsync());
        private void LoadGame() => Task.Run(() => NewGameAsync(GameMoves.Trim().Split()));
        private bool LoadGameFrom(string[] moves)
        {
            var playerTurn = PlayerIsWhite;
            foreach(var stringMove in moves)
            {
                AddMove(stringMove);
                var move = new Move(stringMove);
                var selectedPiece = GetPieceAt(move.From);
                var targetPiece = GetPieceAt(move.To);

                if (targetPiece != null)
                {
                    Chess.CapturePiece(selectedPiece, targetPiece, BoardItems, out _);
                    if (!playerTurn) numPlayerPiecesTaken++;
                }
                else
                {
                    Chess.MovePiece(selectedPiece, GetSquareAt(move.To), BoardItems, out var moveType);
                    if (!playerTurn && moveType == MoveType.EnPassant) numPlayerPiecesTaken++;
                }

                playerTurn = !playerTurn;
            }
            return playerTurn;
        }       
        #endregion

        #region Engine
        private void EngineMessage(string message)
        {
            if (message.Contains(UciCommands.bestmove)) // Message is in the form: bestmove <move> ponder <move>
            {
                if (CheckMate)
                {
                    NewMessage("Game Over", "Checkmate has been reached. Please start a new game or load from a previous position.");
                    return;
                }
                if (!message.Contains("ponder") && !IsPlayerMove) CheckMate = true; //set to true

                //Get the engine move and piece. If none, then the player has achieved checkmate.
                Move engineMove = null;
                ChessPiece enginerPiece = null;
                if (message.Contains("none")) CheckMate = true; //set to true. The player has won!
                else
                {
                    engineMove = new Move(message.Split(' ').ElementAt(1));
                    enginerPiece = GetPieceAt(engineMove.From);
                }
                                        
                if (IsPlayerMove)
                {
                    //If the player made an illegal move, the engine will try moving for the player.
                    //Otherwise, the engine will just make its move. 
                    if (enginerPiece != null && enginerPiece.Color == PieceColor.White == PlayerIsWhite) // Player made illegal move
                    {
                        //Player made an illegal move. It is still the player's turn.
                        IllegalPlayerMove();
                    }
                    else
                    {
                        //Player move is valid
                        AddMove(PlayerMove);
                        var playerMove = new Move(PlayerMove);
                        var playerPiece = GetPieceAt(playerMove.From);
                        var targetPiece = GetPieceAt(playerMove.To);
                        if (targetPiece != null) //Player wants to move the piece
                        {
                            Chess.CapturePiece(playerPiece, targetPiece, BoardItems, out _);
                        }
                        else
                        {
                            //Check if player wants to capture a piece or just move
                            Chess.MovePiece(playerPiece, GetSquareAt(playerMove.To), BoardItems, out _);
                        }
                    }                    
                }
                else // Engine move
                {
                    var targetPiece = GetPieceAt(engineMove.To);
                    AddMove(engineMove.Text);
                    if (targetPiece != null)
                    {
                        Chess.CapturePiece(enginerPiece, targetPiece, BoardItems, out var moveType);
                        if(moveType == MoveType.Promotion) Promotion(engineMove, true);
                        else CapturePiece(engineMove);
                    }
                    else
                    {
                        Chess.MovePiece(enginerPiece, GetSquareAt(engineMove.To), BoardItems, out var moveType);
                        switch (moveType)
                        {
                            case MoveType.Standard:
                                MovePiece(engineMove);
                                break;
                            case MoveType.ShortCastle:
                                MovePiece(engineMove);
                                MovePiece(new Move("h" + engineMove.From.Rank + "f" + engineMove.From.Rank));
                                break;
                            case MoveType.LongCastle:
                                MovePiece(engineMove);
                                MovePiece(new Move("a" + engineMove.From.Rank + "d" + engineMove.From.Rank));
                                break;
                            case MoveType.Promotion:
                                Promotion(engineMove, false);
                                break;
                            case MoveType.EnPassant:
                                EnPassant(engineMove);                                   
                                break;                             
                        }
                    }
                    ResetRobot();
                    ResetCamera();
                }
                IsPlayerMove = !IsPlayerMove;
            }            
        }

        private void IllegalPlayerMove()
        {
            NewMessage("Illegal Player Move", "Please (1) replace the pieces, (2) press the camera reset button, (3) and make a new move.");
            Task.Run(() => WaitForPlayer());
        }

        private void DeeperMoveAnalysis()
        {
            SendMovesToEngine(Moves.ToString(), deepAnalysisTime);
        }

        private void SendMoveToEngine(string move)
        {
            SendMovesToEngine(Moves.ToString() + " " + move, moveValidationTime);
        }

        private void SendMovesToEngine(string moves, short time)
        {
            var command = UciCommands.position + moves;
            engine.SendCommand(command);
            command = UciCommands.go_movetime + " " + time.ToString();
            engine.SendCommand(command);
        }

        private ChessPiece GetPieceAt(PartialMove position)
        {
            return BoardItems.OfType<ChessPiece>().
                            Where(p => p.Rank == position.Rank && p.File == position.File).FirstOrDefault();
        }

        private BoardSquare GetSquareAt(PartialMove position)
        {
            return BoardItems.OfType<BoardSquare>().
                            Where(p => p.Rank == position.Rank && p.File == position.File).FirstOrDefault();
        }
        #endregion

        #region Message Control
        private bool _MessageOpen;
        public bool MessageOpen

        {
            get => _MessageOpen;
            set => Set(ref _MessageOpen, value);
        }

        private string _message;
        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        private string _messageHeader;
        public string MessageHeader
        {
            get => _messageHeader;
            set => Set(ref _messageHeader, value);
        }

        private void NewMessage(string header, string message)
        {
            Message = message;
            MessageHeader = header;
            MessageOpen = true;
        }

        private void CloseMessage()
        {
            MessageOpen = false;
        }

        private void WaitForMessageToClose()
        {
            while (MessageOpen) { }; //wait
        }
        #endregion

        #region TCP Connections     
        private bool _robotConnected;
        public bool RobotConnected
        {
            get => _robotConnected;
            set => Set(ref _robotConnected, value);
        }

        private bool _cameraConnected;
        public bool CameraConnected
        {
            get => _cameraConnected;
            set => Set(ref _cameraConnected, value);
        }

        private bool _serverIsOn;
        public bool ServerIsOn
        {
            get => _serverIsOn;
            set => Set(ref _serverIsOn, value);
        }

        private int _tcpPort = 2000;
        public int TCPPort
        {
            get => _tcpPort;
            set => Set(ref _tcpPort, value);
        }

        private void UpdateConnectionStatus(bool startNewGame = true)
        {
            CameraConnected = Camera != null && Camera.Connected;
            RobotConnected = Robot != null && Robot.Connected;
            CanStartGame = CameraConnected && RobotConnected;
            if (CanStartGame && startNewGame) NewGame();
        }   

        public TcpListener Server;
        public TcpClient Robot;
        public StreamWriter RobotWriter;
        public StreamReader RobotReader;
        public TcpClient Camera;
        public StreamWriter CameraWriter;
        public StreamReader CameraReader;
        public IPAddress Address = IPAddress.Parse("10.2.10.1");
        public bool exitWaitForPlayer;

        private void SetupTCP()
        {
            Server = new TcpListener(Address, TCPPort);
            Server.Start();
            ServerIsOn = true;
            accept_connection();  //accepts incoming connections
        }

        private void ResetServer()
        {
            Server.Stop();
            if(Robot != null) Robot.Close();
            if(Camera != null) Camera.Close();
            SetupTCP();
            UpdateConnectionStatus(false);
        }

        private void accept_connection()
        {
            try
            {
                Server.BeginAcceptTcpClient(handle_connection, Server);  //this is called asynchronously and will run in a different thread
            }
            catch
            {
                //The server was likely reset
            }
        }

        private void handle_connection(IAsyncResult result)  //the parameter is a delegate, used to communicate between threads
        {
            try
            {
                accept_connection();  //once again, checking for any other incoming connections
                TcpClient client = Server.EndAcceptTcpClient(result);  //creates the TcpClient
                NetworkStream ns = client.GetStream();
                StreamWriter writer = new StreamWriter(ns) { AutoFlush = true };
                var reader = new StreamReader(ns);

                //Start listening for the client
                Task.Run(() => ListenForInitialResponse(client, reader, writer));

                //Write the 
                Task.Delay(1000).Wait();
                writer.WriteLine("0"); //Initialize the client to get its name
            }
            catch
            {
                //The server was likely reset
            }
        }

        private void ListenForInitialResponse(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            var message = reader.ReadLine();
            if (message.Contains("Camera"))
            {
                Camera = client;
                CameraReader = reader;
                CameraWriter = writer;
                Task.Run(() => ListenToCamera());
                UpdateConnectionStatus();
            }
            else if (message.Contains("R,"))
            {
                Robot = client;
                RobotReader = reader;
                RobotWriter = writer;
                Task.Run(() => ListenToRobot());
                UpdateConnectionStatus();              
            }
        }

        private void ListenToCamera()
        {
            while (true)
            {
                string message = "";
                try
                {
                    message = CameraReader.ReadLine();
                }
                catch
                {
                    //camera read error or the connection was reset
                    if (!Camera.Connected) break;
                    continue;
                }

                if (message.Contains("reset")) continue;
                var m = 0;
                var partialMoves = new List<PartialMove>();
                var substrings = message.Trim(new char[] { '(', ')' }).Split(',');
                for(var i = 1; i < substrings.Count(); i++)
                {
                    var partial = substrings[i];
                    if (partial.Equals("#ERR") || partial.Equals("n")) continue;
                    else if (partialMoves.Any(p => p.Text.Equals(partial))) continue; //duplicate
                    partialMoves.Add(new PartialMove(partial));
                    m++;
                }

                try
                {
                    if (m == 4)
                    {
                        var move = "";
                        //There are only four possible castling moves: e1g1, e1c1, e8g8, and e8c8
                        if (PlayerIsWhite && message.Contains("e1"))
                        {
                            if (message.Contains("g1")) move = "e1g1";
                            else if (message.Contains("c1")) move = "e1c1";
                        }
                        else if (message.Contains("e8"))
                        {
                            if (message.Contains("g8")) move = "e8g8";
                            else if (message.Contains("c8")) move = "e8c8";
                        }

                        //If the move is a valid castling move, great!
                        //Otherwise, assume the there is noise and just use the first two potential moves, 
                        //since the detected blobs are listing in order of area
                        if (move.Length == 4)
                        {
                            PlayerMove = move;
                            continue;
                        }
                    }
                    //Else if m == 2, OR 3, OR was just changed to 2 in the if statement above
                    //If m = 3 or 4, we are assuming that the third (and fourth) substring are noise.
                    if (m > 1)
                    {
                        var move = new Move(partialMoves[0], partialMoves[1]);
                        var selectedPiece = GetPieceAt(move.From);
                        //If the piece is valid and the same color as the player, then the order of the move is correct
                        //otherwise, try the move the other way around
                        if (selectedPiece == null || selectedPiece.Color == PieceColor.White != PlayerIsWhite)
                        {
                            move = new Move(partialMoves[1], partialMoves[0]);
                            selectedPiece = GetPieceAt(move.From);
                        }
                        //Check if valid move was found
                        if (selectedPiece != null && selectedPiece.Color == PieceColor.White == PlayerIsWhite)
                        {
                            if (selectedPiece.Piece == PieceType.Pawn)
                            {
                                //Check for promotion. We need to add a q to the end if it is.
                                move.IsPromotion = (PlayerIsWhite && move.To.Rank == 8) || (!PlayerIsWhite && move.To.Rank == 1);
                            }
                            PlayerMove = move.Text;
                            continue;
                        }
                    }
                    //The camera has not found a valid move if it gets this far.
                    AddToLog("Camera error");
                    NewMessage("Camera Error", "Camera could not identify player move. Please (1) reset the pieces, (2) press the yellow reset button, and (3) make the move again.");
                    IsPlayerMove = true;
                }
                catch
                {
                    //The camera has not found a valid move if it gets this far.
                    AddToLog("Camera error");
                    NewMessage("Camera Error", "Camera could not identify player move. Please (1) reset the pieces, (2) press the yellow reset button, and (3) make the move again.");
                    IsPlayerMove = true;
                }
            }
            UpdateConnectionStatus(false);
        }

        private void ResetCamera()
        {
            CameraWriter.WriteLine("0");
        }

        private void ListenToRobot()
        {
            while (true)
            {
                string message;
                try
                {
                    message = RobotReader.ReadLine();
                    if (message == null) throw new Exception();
                    if (message.Contains("completed")) RobotIsMoving = false;
                }
                catch
                {
                    //Robot read error or the connection was reset
                    if (!Robot.Connected) break;
                    continue;
                }       
            }
            UpdateConnectionStatus(false);
        }
        #endregion

        #region Moxa 
        private bool _moxaConnected;
        public bool MoxaConnected
        {
            get => _moxaConnected;
            set => Set(ref _moxaConnected, value);
        }

        private ModbusClient ModbusClient;
        private ModbusServer ModbusServer;

        private void ConnectToMoxa()
        {
            ModbusServer = new ModbusServer() { Port = 502 };
            ModbusClient = new ModbusClient() { Port = 502, IPAddress = "10.2.10.15" };
            ModbusServer.Listen();
            ModbusClient.Connect();
            RedLight(true);
            GreenLight(false);
        }

        private void OpenGripper()
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    ModbusClient.Connect();
                    ModbusClient.WriteSingleCoil(0, false);
                    Task.Delay(200).Wait();
                    complete = true;
                    ModbusClient.Disconnect();
                }
                catch
                {
                }
            }
        }

        private void CloseGripper()
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    ModbusClient.Connect();
                    ModbusClient.WriteSingleCoil(0, true);
                    Task.Delay(500).Wait();
                    complete = true;
                    ModbusClient.Disconnect();
                }
                catch
                {
                }
            }
        }

        private void RedLight(bool turnOn)
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    ModbusClient.Connect();
                    ModbusClient.WriteSingleCoil(1, turnOn);
                    complete = true;
                    ModbusClient.Disconnect();
                }
                catch
                {
                }
            }    
        }

        private void GreenLight(bool turnOn)
        {
            var complete = false;
            while (!complete)
            {
                try
                {
                    ModbusClient.Connect();
                    ModbusClient.WriteSingleCoil(2, turnOn);
                    complete = true;
                    ModbusClient.Disconnect();
                }
                catch
                {
                }
            }
        }

        private void WaitForPlayer()
        {
            //Update the lights to be the player's turn
            while (!exitWaitForPlayer)
            {
                bool[] inputs;
                try
                {
                    ModbusClient.Connect();
                    inputs = ModbusClient.ReadDiscreteInputs(0, 3);
                    ModbusClient.Disconnect();
                }
                catch
                {
                    //connection error or the connection has been reset
                    continue;
                }
  
                if (inputs[0])
                {
                    //Trigger the camera. The camera will then send the move to the engine.
                    CameraWriter.WriteLine("1");
                    AddToLog("Player move submitted. Triggering camera.");
                    return;
                }
                if (inputs[1])
                {
                    var moves = GameMoves.Trim().Split();
                    var reduced = moves.Take(moves.Length - 2).ToArray(); //Remove the last two moves (engine and player)
                    Task.Run(() => NewGameAsync(reduced));
                    return;
                }
                if (inputs[2])
                {
                    ResetCamera();
                    AddToLog("Camera reset.");
                }
            }
        }
        #endregion

        #region Moving the Robot
        private RobotBoardRotation BoardRotation;

        public void MovePiece(Move move)
        {
            MoveRobot(move.From);
            PickupPiece();
            MoveRobot(move.To);
            DropOffPiece();
        }

        public void CapturePiece(Move move)
        {
            //Remove the captured piece
            MoveRobot(move.To);
            PickupPiece();
            DropOffTakenPiece();
            //And then move the engine piece
            MovePiece(move);
        }

        private void DropOffPiece()
        {
            LowerArm();
            OpenGripper();
            RaiseArm();
        }

        private void DropOffTakenPiece()
        {
            //Move the arm to the from location
            GoToCapturedPieces();
            LowerArm();
            OpenGripper();
            RaiseArm();
        }

        private void PickupPiece()
        {
            OpenGripper();//Technically, it should already be open
            LowerArm();
            CloseGripper();
            RaiseArm();
        }

        private void Promotion(Move move, bool isCapture)
        {
            if (isCapture)
            {
                //Remove the captured piece before completing the promotion
                MoveRobot(move.To);
                PickupPiece();
                DropOffTakenPiece();
            }

            //Move the queen to the move.To position
            MoveRobotToExtraQueenPosition();
            PickupPiece();
            MoveRobot(move.To);
            DropOffPiece();

            //Now move the pawn off the board over to the queen position
            MoveRobot(move.From);
            PickupPiece();
            MoveRobotToExtraQueenPosition();
            DropOffPiece();
        }

        private void EnPassant(Move move)
        {
            //move the engine's pawn
            MovePiece(move);

            //then remove the captured pawn                            
            var capturedPawnLocation = new PartialMove(move.To.File + move.From.Rank.ToString());
            MoveRobot(capturedPawnLocation);
            PickupPiece();
            DropOffTakenPiece();
        }

        private void MoveRobot(PartialMove move)
        {
            RobotIsMoving = true;
            RobotWriter.WriteLine("1," + BoardRotation.X(move) + "," + BoardRotation.Y(move));
            WaitForRobot();
        }

        private void MoveRobotToExtraQueenPosition()
        {
            RobotIsMoving = true;
            RobotWriter.WriteLine("5");
            WaitForRobot();
        }

        private void LowerArm()
        {
            RobotIsMoving = true;
            RobotWriter.WriteLine("2");
            WaitForRobot();
        }

        private void RaiseArm()
        {
            RobotIsMoving = true;
            RobotWriter.WriteLine("3");
            WaitForRobot();
        }

        private int numPlayerPiecesTaken = 0;
        private void GoToCapturedPieces()
        {
            numPlayerPiecesTaken++;
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
            RobotWriter.WriteLine("4," + robotOffsetX.ToString() + "," + robotOffsetY.ToString());
            WaitForRobot();
        }

        private void ResetRobot()
        {
            RobotIsMoving = true;
            RobotWriter.Flush();
            RobotWriter.WriteLine("0");
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
                AddToLog("Robot connection is bad. Try resetting the server.");
            }
        }
        #endregion
    }
}
