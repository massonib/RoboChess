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

        private string _gameMoves;
        public string GameMoves
        {
            get => _gameMoves;
            set => Set(ref _gameMoves, value);
        }

        private StringBuilder _moves;
        public StringBuilder Moves
        {
            get => _moves;
            set
            {
                Set(ref _moves, value);
                GameMoves = Moves.ToString();
            }
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
                    Task.Run(() => WaitForPlayer());
                }
                else
                {
                    RedLight(true);
                    GreenLight(false);
                    //Now it is the robot's turn. It is allowed to take longer now, since it is not validating.
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

        #region Commands
        public ICommand ResetServerCommand { get; }
        public ICommand NewGameCommand { get; }
        public ICommand LoadGameCommand { get; }
        public ICommand CloseErrorMessageCommand { get; }
        #endregion

        #region Constructor
        public MainViewModel(IEngineService es)
        {
            Moves = new StringBuilder();
            engine = es;
            BoardItems = Chess.BoardSetup();
            engine.EngineMessage += EngineMessage;
            engine.StartEngine();
            engine.SendCommand(UciCommands.limitStrength);

            ConnectToMoxa();
            SetupTCP();

            ResetServerCommand = new CommandWithNoCondition(ResetServer);
            NewGameCommand = new CommandWithNoCondition(NewGame);
            LoadGameCommand = new CommandWithNoCondition(LoadGame);
            CloseErrorMessageCommand = new CommandWithNoCondition(CloseErrorMessage);
        }
        #endregion

        #region NewGame
        private void NewGame()
        {
            exitAllThreads = true;
            Task.Delay(1000).Wait();
            exitAllThreads = false;
            fileOffset = 0;
            rankOffset = 0;

            BoardItems = Chess.BoardSetup();
            if (Moves.Length > 0) Moves.Clear();
            if (CheckMate) CheckMate = false;
      
           // if (IsEngineThinking) IsEngineThinking = false;
            engine.SendCommand(UciCommands.ucinewgame);
            engine.SendCommand(UciCommands.limitStrength);
            engine.SendCommand(UciCommands.SetElo(Elo));

            //This boolean controls whose turn it is. It also triggers events.
            IsPlayerMove = PlayerIsWhite;
        }

        private void LoadGame()
        {
            LoadGameFrom(Moves.ToString());
        }

        private void LoadGameFrom(string gameMoves)
        {
            exitAllThreads = true;
            Task.Delay(1000).Wait();
            exitAllThreads = false;
            fileOffset = 0;
            rankOffset = 0;

            var moves = gameMoves.Trim().Split();
            BoardItems = Chess.BoardSetup();
            if (Moves.Length > 0) Moves.Clear();
            if (CheckMate) CheckMate = false;
            engine.SendCommand(UciCommands.ucinewgame);
            engine.SendCommand(UciCommands.limitStrength);
            engine.SendCommand(UciCommands.SetElo(Elo));

            var playerTurn = PlayerIsWhite;
            foreach(var stringMove in moves)
            {
                Moves.Append(" " + stringMove);
                var move = new Move(stringMove);
                var selectedPiece = GetPieceAt(move.From);
                var targetPiece = GetPieceAt(move.To);
              
                if (targetPiece != null) Chess.CapturePiece(selectedPiece, targetPiece, BoardItems);
                else Chess.MovePiece(selectedPiece, GetSquareAt(move.To), BoardItems);
                playerTurn = !playerTurn;
            }
            IsPlayerMove = playerTurn;
        }
        #endregion

        #region Engine
        private void EngineMessage(string message)
        {
            if (message.Contains(UciCommands.bestmove)) // Message is in the form: bestmove <move> ponder <move>
            {
                if (!message.Contains("ponder")) CheckMate = true;

                var move = new Move(message.Split(' ').ElementAt(1));
                var selectedPiece = GetPieceAt(move.From);
                //If the player made an illegal move, the engine will try moving for the player.
                //Otherwise, the engine will just make its move.
                if (IsPlayerMove && selectedPiece.Color == PieceColor.White == PlayerIsWhite) // Player made illegal move
                {
                    //Player made an illegal move. It is still the player's turn.
                    IllegalPlayerMove();
                }
                else
                {
                    
                    if (IsPlayerMove)
                    {
                        //Player move is valid
                        Moves.Append(" " + PlayerMove);
                        var playerMove = new Move(PlayerMove);
                        selectedPiece = GetPieceAt(playerMove.From);
                        var targetPiece = GetPieceAt(playerMove.To);                   
                        if (targetPiece != null) //Player wants to move the piece
                        {
                            Chess.CapturePiece(selectedPiece, targetPiece, BoardItems);
                        }
                        else
                        {
                            //Check if player wants to capture a piece or just move
                            Chess.MovePiece(selectedPiece, GetSquareAt(playerMove.To), BoardItems);                       
                        }
                    }
                    else // Engine move
                    {
                        var targetPiece = GetPieceAt(move.To);
                        Moves.Append(" " + move.Text);
                        if (targetPiece != null)
                        {
                            Chess.CapturePiece(selectedPiece, targetPiece, BoardItems);
                            CapturePiece(move);
                        }
                        else
                        {
                            Chess.MovePiece(selectedPiece, GetSquareAt(move.To), BoardItems);
                            MovePiece(move);
                        }
                        //Give half a second before taking the new image.
                        Task.Delay(500).Wait();
                        ResetCamera();
                    }
                    IsPlayerMove = !IsPlayerMove;
                }
            }
        }

        private void IllegalPlayerMove()
        {
            ErrorMessage = "Illegal player move. Please (1) replace the pieces, (2) press the camera reset button, (3) and make a new move.";
            Task.Run(() => WaitForPlayer());
        }

        private void DeeperMoveAnalysis()
        {
            SendMovesToEngine(GameMoves, deepAnalysisTime);
        }

        private void SendMoveToEngine(string move)
        {
            SendMovesToEngine(GameMoves + " " + move, moveValidationTime);
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

        private void UpdateConnectionStatus()
        {
            CameraConnected = Camera != null && Camera.Connected;
            RobotConnected = Robot != null && Robot.Connected;
            CanStartGame = CameraConnected && RobotConnected;
            if (CanStartGame) NewGame();
        }

        private bool _errorMessageOpen;
        public bool ErrorMessageOpen

        {
            get => _errorMessageOpen;
            set => Set(ref _errorMessageOpen, value);
        }

        private void CloseErrorMessage()
        {
            ErrorMessageOpen = false;
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                //Anytime there is a new error message, open the message
                Set(ref _errorMessage, value);
                ErrorMessageOpen = true;
            }
        }

        public TcpListener Server;
        public TcpClient Robot;
        public StreamWriter RobotWriter;
        public StreamReader RobotReader;
        public TcpClient Camera;
        public StreamWriter CameraWriter;
        public StreamReader CameraReader;
        public IPAddress Address = IPAddress.Parse("10.2.10.1");
        public bool exitAllThreads;

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
            SetupTCP();
        }

        private void accept_connection()
        {
            Server.BeginAcceptTcpClient(handle_connection, Server);  //this is called asynchronously and will run in a different thread
        }

        private void handle_connection(IAsyncResult result)  //the parameter is a delegate, used to communicate between threads
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
            ResetCamera();
        }

        private void ListenForInitialResponse(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            var message = reader.ReadLine();
            if (message.Contains("Camera"))
            {
                Camera = client;
                CameraReader = reader;
                CameraWriter = writer;
                CameraConnected = true;
                UpdateConnectionStatus();
                ListenToCamera();
            }
            else if (message.Contains("R,"))
            {
                Robot = client;
                RobotReader = reader;
                RobotWriter = writer;
                RobotConnected = true;
                UpdateConnectionStatus();
                ListenToRobot();
            }
        }

        private void ListenToCamera()
        {
            while (!exitAllThreads)
            {
                var message = CameraReader.ReadLine();

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

                if (m == 0) { }
                else if (m == 2)
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
                    if (selectedPiece.Piece == PieceType.Pawn)
                    {
                        //Check for promotion. We need to add a q to the end if it is.
                        move.IsPromotion = (PlayerIsWhite && move.To.Rank == 8) || (!PlayerIsWhite && move.To.Rank == 1);
                    }

                    PlayerMove = move.Text;
                }
                else if (m == 4)
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
                    if (move.Length == 0) IllegalPlayerMove();
                    PlayerMove = move;
                }
                else
                {
                    ErrorMessage = "Camera could not identify player move. Please (1) reset the pieces, (2) press the yellow reset button, and (3) make the move again.";
                    IsPlayerMove = true;
                }
            }
        }

        private void ResetCamera()
        {
            CameraWriter.WriteLine("0");
        }

        private void ListenToRobot()
        {
            while (!exitAllThreads)
            {
                var message = RobotReader.ReadLine();

                if (message.Contains("completed")) RobotIsMoving = false;
            }
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
            ModbusClient.Connect();
            ModbusClient.WriteSingleCoil(0, false);
            ModbusClient.Disconnect();
        }

        private void CloseGripper()
        {
            ModbusClient.Connect();
            ModbusClient.WriteSingleCoil(0, true);
            ModbusClient.Disconnect();
        }

        private void RedLight(bool turnOn)
        {
            ModbusClient.Connect();
            ModbusClient.WriteSingleCoil(1, turnOn);
            ModbusClient.Disconnect();
        }

        private void GreenLight(bool turnOn)
        {
            ModbusClient.Connect();
            ModbusClient.WriteSingleCoil(2, turnOn);
            ModbusClient.Disconnect();
        }

        private void WaitForPlayer()
        {
            //Update the lights to be the player's turn
            while (!exitAllThreads)
            {
                ModbusClient.Connect();
                var inputs = ModbusClient.ReadDiscreteInputs(0, 3);
                ModbusClient.Disconnect();
                if (inputs[0])
                {
                    //Trigger the camera. The camera will then send the move to the engine.
                    CameraWriter.WriteLine("1");
                    return;
                }
                if (inputs[1])
                {
                    Task.Run(() => LoadGameFrom(GameMoves));
                    return;
                }
                if (inputs[2])
                {
                    ResetCamera();
                    ResetRobot();
                }
            }
        }
        #endregion

        #region Moving the Robot
        private static readonly Dictionary<char, int> FileDictionary = new Dictionary<char, int> 
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

        public void MovePiece(Move move)
        {
            PickupPiece(move.From);
            DropOffPiece(move.To);
            ResetRobot();
        }

        public void CapturePiece(Move move)
        {
            PickupPiece(move.To);
            DropOffTakenPiece();
            MovePiece(move);
            ResetRobot();
        }

        private void DropOffPiece(PartialMove move)
        {
            //Move the arm to the to location
            MoveRobot(move);
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

        private void PickupPiece(PartialMove move)
        {
            //Move the arm to the from location
            MoveRobot(move);
            OpenGripper();//Technically, it should already be open
            LowerArm();
            CloseGripper();
            RaiseArm();
        }

        private void MoveRobot(PartialMove move)
        {
            RobotIsMoving = true;
            RobotWriter.WriteLine("1," + FileDictionary[move.File] + "," + (move.Rank - 1).ToString());
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

        private int fileOffset = 0;
        private int rankOffset = 0;
        private void GoToCapturedPieces()
        {
            RobotIsMoving = true;
            RobotWriter.WriteLine("4," + fileOffset.ToString() + "," + rankOffset.ToString());
            WaitForRobot();
            rankOffset++;
            if (rankOffset == 8)
            {
                rankOffset = 0;
                fileOffset++;
            }
        }

        private void ResetRobot()
        {
            RobotIsMoving = true;
            RobotWriter.WriteLine("0");
            WaitForRobot();
        }

        private bool RobotIsMoving = false;
        private void WaitForRobot()
        {
            while (RobotIsMoving && !exitAllThreads) { } //wait
        }
        #endregion
    }
}
