using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Meziantou.Framework.WPF.Collections;
using StockChessCS;
using StockChessCS.Commands;
using StockChessCS.Enums;
using StockChessCS.Helpers;
using StockChessCS.Interfaces;
using StockChessCS.Models;
using EasyModbus;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace RoboChess
{
    public class MainViewModel : Observable
    {
        private bool _canStartGame;
        public bool CanStartGame
        {
            get => _canStartGame;
            set => Set(ref _canStartGame, value);
        }

        private void UpdateCanStartGame()
        {
            CanStartGame = CameraConnected && RobotConnected;
        }

        private IEngineService engine;
        public StringBuilder Moves = new StringBuilder();
        private short deepAnalysisTime = 5000;
        private short moveValidationTime = 1;
        private TaskFactory ctxTaskFactory;

        #region Commands
        public ICommand ResetServerCommand { get; }
        public ICommand NewGameCommand { get; }
        public ICommand LoadGameCommand { get; }
        #endregion

        public MainViewModel(IEngineService es)
        {
            engine = es;
            BoardItems = Chess.BoardSetup();
            ctxTaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
            engine.EngineMessage += EngineMessage;
            engine.StartEngine();
            engine.SendCommand(UciCommands.limitStrength);

            ConnectToMoxa();
            SetupTCP();

            ResetServerCommand = new CommandWithNoCondition(ResetServer);
            NewGameCommand = new CommandWithNoCondition(NewGame);
            LoadGameCommand = new CommandWithNoCondition(LoadGame);
        }

        private MultiThreadedObservableCollection<IBoardItem> _boardItems;
        public MultiThreadedObservableCollection<IBoardItem> BoardItems
        {
            get =>  _boardItems;
            set => Set(ref _boardItems, value);
        }
      
        private bool _isEngineThinking;
        public bool IsEngineThinking
        {
            get => _isEngineThinking;
            set => Set(ref _isEngineThinking, value);
        }

        private bool _checkMate;
        public bool CheckMate
        {
            get => _checkMate;
            set => Set(ref _checkMate, value);
        }

        private bool _playerIsWhite;
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

        public bool CameraIsValidatingMove;

        public bool PlayerMoveIsValid;

        public bool ValidatingPlayerMove;

        private void NewGame()
        {
            BoardItems = Chess.BoardSetup();
            if (Moves.Length > 0) Moves.Clear();
            if (CheckMate) CheckMate = false;
            if (IsEngineThinking) IsEngineThinking = false;
            engine.SendCommand(UciCommands.ucinewgame);
            engine.SendCommand(UciCommands.limitStrength);
            engine.SendCommand(UciCommands.SetElo(Elo));

            if (!PlayerIsWhite)
            {
                engine.SendCommand(UciCommands.position);
                engine.SendCommand(UciCommands.go_movetime + " " + deepAnalysisTime.ToString());
                IsEngineThinking = true;
            }
        }

        private void LoadGame()
        {
            LoadGameFrom(Moves.ToString());
        }

        private void LoadGameFrom(string moves)
        {
            BoardItems = Chess.BoardSetup();
            if (Moves.Length > 0) Moves.Clear();
            if (CheckMate) CheckMate = false;
            if (IsEngineThinking) IsEngineThinking = false;
            engine.SendCommand(UciCommands.ucinewgame);
            engine.SendCommand(UciCommands.limitStrength);
            engine.SendCommand(UciCommands.SetElo(Elo));

            if (!PlayerIsWhite)
            {
                engine.SendCommand(UciCommands.position);
                engine.SendCommand(UciCommands.go_movetime + " " + deepAnalysisTime.ToString());
                IsEngineThinking = true;
            }
        }

        private ICommand _stopEngineCommand;
        public ICommand StopEngineCommand
        {
            get
            {
                if (_stopEngineCommand == null) _stopEngineCommand = new RelayCommand(o => StopEngine());
                return _stopEngineCommand;
            }
        }

        private void StopEngine()
        {
            engine.EngineMessage -= EngineMessage;
            engine.StopEngine();
        }

        private void EngineMessage(string message)
        {
            if (message.Contains(UciCommands.bestmove)) // Message is in the form: bestmove <move> ponder <move>
            {
                if (!message.Contains("ponder")) CheckMate = true;

                var move = new Move(message.Split(' ').ElementAt(1));
                var selectedPiece = BoardItems.OfType<ChessPiece>().
                    Where(p => p.Rank == move.From.Rank && p.File == move.From.File).Single();           

                if (selectedPiece.Color == PieceColor.White == PlayerIsWhite) // Player made illegal move
                {
                    RemoveLastMove();
                    PlayerMoveIsValid = false;
                    ValidatingPlayerMove = false;
                }
                else
                {
                    if (ValidatingPlayerMove)
                    {
                        var targetPiece = BoardItems.OfType<ChessPiece>().
                            Where(p => p.Rank == move.To.Rank && p.File == move.To.File).FirstOrDefault();

                        if(targetPiece != null) //Player wants to move the piece
                        {
                            Chess.CapturePiece(selectedPiece, targetPiece, BoardItems);
                            DeeperMoveAnalysis();
                        }
                        else
                        {
                            var targetSquare = BoardItems.OfType<BoardSquare>().
                                Where(p => p.Rank == move.To.Rank && p.File == move.To.File).FirstOrDefault();

                            //Check if player wants to capture a piece or just move
                            ctxTaskFactory.StartNew(() => Chess.MovePiece(selectedPiece, targetSquare, BoardItems)).Wait();
                            DeeperMoveAnalysis();
                        }
                        PlayerMoveIsValid = true;
                        ValidatingPlayerMove = false;                 
                    }
                    else // Engine move
                    {
                        Moves.Append(" " + move);
                        var targetPiece = BoardItems.OfType<ChessPiece>().
                            Where(p => p.Rank == move.To.Rank && p.File == move.To.File).FirstOrDefault();

                        if (targetPiece != null)
                        {
                            Chess.CapturePiece(selectedPiece, targetPiece, BoardItems);
                        }
                        else
                        {
                            var targetSquare = BoardItems.OfType<BoardSquare>().
                                Where(p => p.Rank == move.To.Rank && p.File == move.To.File).FirstOrDefault();
                            Chess.MovePiece(selectedPiece, targetSquare, BoardItems);
                        }
                        IsEngineThinking = false;
                    }
                }
            }
        }

        private void DeeperMoveAnalysis()
        {
            SendMovesToEngine(deepAnalysisTime);
            IsEngineThinking = true;
        }

        private void RemoveLastMove()
        {
            if (Moves.Length > 0)
            {
                var length = Moves.Length;
                var start = Moves.Length - 5;
                Moves.Remove(start, 5);
            }
        }

        private bool ValidateMove(string move)
        {
            Moves.Append(" " + move);
            SendMovesToEngine(moveValidationTime);

            PlayerMoveIsValid = false;
            ValidatingPlayerMove = true;
            while (ValidatingPlayerMove) { } //wait
            return PlayerMoveIsValid;
        }

        private void SendMovesToEngine(short time)
        {
            var command = UciCommands.position + Moves.ToString();
            engine.SendCommand(command);
            command = UciCommands.go_movetime + " " + time.ToString();
            engine.SendCommand(command);
        }


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
            ModbusClient = new ModbusClient() { Port = 512, IPAddress = "10.2.10.15" };
            ModbusServer = new ModbusServer() { Port = 512 };
            ModbusServer.Listen();
            ModbusClient.Connect();
        }

        private void OpenGripper()
        {
            ModbusClient.Connect();
            ModbusClient.WriteSingleCoil(0, true);
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
            ModbusClient.WriteSingleCoil(1, turnOn);
            ModbusClient.Disconnect();
        }


        #endregion

        private void WaitForPlayer()
        {
            RedLight(false);
            GreenLight(true);


            while (!exitAllThreads)
            {
                ModbusClient.Connect();
                var playerMove = ModbusClient.ReadDiscreteInputs(0, 1).FirstOrDefault();
                var undo = ModbusClient.ReadDiscreteInputs(1, 1).FirstOrDefault();
                var reset = ModbusClient.ReadDiscreteInputs(2, 1).FirstOrDefault();
                ModbusClient.Disconnect();
                if (playerMove)
                {

                }
                if (reset)
                {
                    ResetCamera();
                    ResetRobot();
                }
                if (undo)
                {
                    Task.Run(() => LoadGameFrom(Moves.ToString()));
                    return;
                }
            }

            RedLight(true);
            GreenLight(false);
        }

        private bool _serverIsOn;
        public bool ServerIsOn
        {
            get => _serverIsOn;
            set => Set(ref _serverIsOn, value);
        }
 

        private bool _errorMessageOpen;
        public bool ErrorMessageOpen

        {
            get => _errorMessageOpen;
            set => Set(ref _errorMessageOpen, value);
        }

        private int _tcpPort = 2000;
        public int TCPPort
        {
            get => _tcpPort;
            set => Set(ref _tcpPort, value);
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
            writer.WriteLine("0");
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
                UpdateCanStartGame();
                ListenToCamera();
            }
            else if (message.Contains("Robot"))
            {
                Robot = client;
                RobotReader = reader;
                RobotWriter = writer;
                RobotConnected = true;
                UpdateCanStartGame();
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
                    if (partial.Equals("#ERR")) continue;
                    else if (partialMoves.Any(p => p.Text.Equals(partial))) continue; //duplicate
                    partialMoves.Add(new PartialMove(partial));
                    m++;
                }

                if(m == 0) { }
                else if (m == 2)
                {
                    //Regular move
                    //Check the validity of each with the engine
                    if (ValidateMove(partialMoves[0].Text + partialMoves[1].Text)) { }
                    //Else try the other potential order
                    else if (ValidateMove(partialMoves[1].Text + partialMoves[0].Text)) { }
                    else
                    {
                        //ErrorMessage = "Camera could not find a valid move";
                    }

                    CameraIsValidatingMove = false;
                }
                else if (m == 4)
                {
                    //castling
                }
            }
        }

        private void ListenToRobot()
        {
            while (!exitAllThreads)
            {
                var message = RobotReader.ReadLine();              
            }
        }

        private static readonly Dictionary<char, int> FileDictionary = new Dictionary<char, int> 
        { 
            { 'a', 0 }, 
            { 'b', 1 },
            { 'c', 2 },
            { 'd', 3 },
            { 'e', 4 },
            { 'f', 5 },
            { 'g', 6 },
            { 'h', 8 },
        };

        private void PickupPiece(PartialMove move)
        {
            //Move the arm to the from location
            RobotWriter.WriteLine("1," + FileDictionary[move.File] + "," + move.Rank.ToString());
            OpenGripper();//Technically, it should already be open
            //Lower the arm
            RobotWriter.WriteLine("2");
            CloseGripper();
            //Raise the arm
            RobotWriter.WriteLine("3");
        }

        private void DropOffPiece(PartialMove move)
        {
            //Move the arm to the from location
            RobotWriter.WriteLine("1," + FileDictionary[move.File] + "," + move.Rank.ToString());       
            //Lower the arm
            RobotWriter.WriteLine("2");
            OpenGripper();
            //Raise the arm
            RobotWriter.WriteLine("3");
        }

        private int fileOffset = 0;
        private int rankOffset = 0;
        private void DropOffTakenPiece()
        {
            //Move the arm to the from location
            RobotWriter.WriteLine("4," + fileOffset.ToString()+ "," + rankOffset.ToString());
            //Lower the arm
            RobotWriter.WriteLine("2");
            OpenGripper();
            //Raise the arm
            RobotWriter.WriteLine("3");
        }

        public void MovePiece(Move move)
        {
            PickupPiece(move.From);
            DropOffPiece(move.To);
        }

        public void CapturePiece(Move move)
        {
            PickupPiece(move.To);
            DropOffTakenPiece();
            MovePiece(move);
        }

        private void ResetCamera()
        {
            CameraWriter.WriteLine("0");
        }

        private void ResetRobot()
        {
            RobotWriter.WriteLine("0");
        }

        #endregion
    }
}
