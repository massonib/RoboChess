using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using StockChessCS;
using StockChessCS.Enums;
using StockChessCS.Helpers;
using StockChessCS.Interfaces;
using System.Net.Sockets;
using System.Net;
using System.IO;
using RoboChess.Helpers;
using static RoboChess.Helpers.GetItem;

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
                    Moxa.RedLight(false);
                    Moxa.GreenLight(true);
                    AddToLog("Player's " + (PlayerIsWhite ? "(White)" : "(Black)") + " turn" );
                    Task.Run(() => WaitForPlayer());
                }
                else
                {
                    Moxa.RedLight(true);
                    Moxa.GreenLight(false);
                    //Now it is the robot's turn. It is allowed to take longer now, since it is not validating.
                    AddToLog("Robot's " + (!PlayerIsWhite ? "(White)" : "(Black)") + " turn");
                    DeeperMoveAnalysis();
                }
            }
        }

        private Move _playerMove;
        public Move PlayerMove
        {
            get => _playerMove;
            set
            {
                if (value == null) return;
                Set(ref _playerMove, value);
                AddToLog("Player move submitted: " + value.Text);
                SendMoveToEngine(value.Text);
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

        private int _elo = 500;
        public int Elo
        {
            get => _elo;
            set
            {
                Set(ref _elo, value);
                Engine.SendCommand(UciCommands.Elo(value));
            }
        }

        private int _skillLevel = 5;
        public int SkillLevel
        {
            get => _skillLevel;
            set
            {
                var skill = value;
                if (skill > 20) skill = 20;
                else if (skill < 1) skill = 1;
                Set(ref _skillLevel, skill);
                Engine.SendCommand(UciCommands.SkillLevel(skill));
            }
        }

        private MultiThreadedObservableCollection<IBoardItem> _boardItems;
        public MultiThreadedObservableCollection<IBoardItem> BoardItems
        {
            get => _boardItems;
            set => Set(ref _boardItems, value);
        }

        #endregion

        #region Text Boxes Logging && Moves
        private readonly StringBuilder Log;

        private string _logger;
        public string Logger
        {
            get => _logger;
            set => Set(ref _logger, value);
        }
        internal void AddToLog(string text)
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
            Engine = new Engine(es);
            Engine.engine.EngineMessage += EngineMessage;
            BoardItems = Chess.BoardSetup();

            Moxa = new Moxa(502, "10.2.10.15");
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
            Engine.SendCommand(UciCommands.ucinewgame);
            Engine.SendCommand(UciCommands.SkillLevel(SkillLevel));
            //engine.SendCommand(UciCommands.limitStrength);
            //engine.SendCommand(UciCommands.Elo(Elo));

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
            Moxa.OpenGripper();
            Robot.Reset();
            Camera.Reset();

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
                var selectedPiece = GetChessPiece(move.From, BoardItems);
                var target = GetBoardItem(move.To, BoardItems);
                Chess.MovePiece(selectedPiece, target, BoardItems, out var moveType);
                if(!playerTurn && (moveType == MoveType.Capture || moveType == MoveType.EnPassant || moveType == MoveType.PromotionWithCapture))
                {
                    numPlayerPiecesTaken++;
                }              
                playerTurn = !playerTurn;
            }
            return playerTurn;
        }       
        #endregion

        #region Interpreting the engine message.
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
                if (message.Contains("none")) CheckMate = true; //set to true. The player has won!

                //Get the engine move and piece. If none, then the player has achieved checkmate.
                var engineMove = new Move(message.Split(' ').ElementAt(1));
                var enginerPiece = GetChessPiece(engineMove.From, BoardItems);             
                                        
                if (IsPlayerMove) 
                {
                    //If the player made an illegal move, the engine will try moving for the player.
                    //Otherwise, the engine will just make its move. 
                    if (!CheckMate && enginerPiece.Color == PieceColor.White == PlayerIsWhite) // Player made illegal move
                    {
                        //Player made an illegal move. It is still the player's turn.
                        IllegalPlayerMove();
                    }
                    else
                    {
                        //Player move is valid. Add it to the move log, and update the BoardItems.
                        AddMove(PlayerMove.Text); 
                        Chess.MovePiece(GetChessPiece(PlayerMove.From, BoardItems),
                            GetBoardItem(PlayerMove.To, BoardItems), BoardItems, out _);
                    }                    
                }
                //Check to make doubly sure that the engine is moving its own pieces
                else if(enginerPiece.Color != PieceColor.White == PlayerIsWhite)
                {
                    AddMove(engineMove.Text);
                    Chess.MovePiece(enginerPiece, GetBoardItem(engineMove.To, BoardItems), BoardItems, out var moveType);
                    switch (moveType)
                    {
                        case MoveType.Move:
                            MovePiece(engineMove);
                            break;
                        case MoveType.Capture:
                            CapturePiece(engineMove);
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
                        case MoveType.PromotionWithCapture:
                            Promotion(engineMove, true);
                            break;
                        case MoveType.EnPassant:
                            EnPassant(engineMove);
                            break;
                    }
                    Robot.Reset();
                    Camera.Reset();
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
            Engine.SendMoves(Moves.ToString(), Engine.DeepAnalysisTime);
        }

        private void SendMoveToEngine(string move)
        {
            Engine.SendMoves(Moves.ToString() + " " + move, Engine.MoveValidationTime);
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

        internal void NewMessage(string header, string message)
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

        private bool _moxaConnected;
        public bool MoxaConnected
        {
            get => _moxaConnected;
            set => Set(ref _moxaConnected, value);
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

        internal void UpdateConnectionStatus(bool startNewGame = true)
        {
            CameraConnected = Camera != null && Camera.Connected;
            RobotConnected = Robot != null && Robot.Connected;
            CanStartGame = CameraConnected && RobotConnected;
            if (CanStartGame && startNewGame) NewGame();
        }

        public Engine Engine;
        public Moxa Moxa;
        public TcpListener Server;
        public Robot Robot;
        public Camera Camera;
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
                Camera = new Camera(client, reader, writer);
                Task.Run(() => ListenToCamera());
            }
            else if (message.Contains("Robot"))
            {
                Robot = new Robot(client, reader, writer);
                Task.Run(() => ListenToRobot());
            }
            UpdateConnectionStatus();
        }

        private void ListenToCamera()
        {           
            while (true)
            {
                var partialMoves = Camera.ReadLine();
                if (!Camera.Connected) break;
                if (partialMoves == null || !partialMoves.Any()) continue;

                try
                {
                    var move = Camera.GetMoveFromPartialMoves(partialMoves, BoardItems, PlayerIsWhite ? PieceColor.White : PieceColor.Black);
                    if (move == null) SubmitError();
                    else PlayerMove = move;
                }
                catch
                {                 
                    SubmitError();
                    //Camera read error or the connection was reset
                    if (!Camera.Connected) break;
                }
            }
            UpdateConnectionStatus(false);  
        }

        private void ListenToRobot()
        {
            while (true)
            {
                string message;
                try
                {
                    message = Robot.ReadLine();
                }
                catch
                {
                    //Robot read error or the connection was reset
                    if (!Robot.Connected) break;
                }
            }
            UpdateConnectionStatus(false);
        }

        private void SubmitError()
        {
            //The camera has not found a valid move if it gets this far.
            AddToLog("Camera error");
            NewMessage("Camera Error", "Camera could not identify player move. Please (1) reset the pieces, (2) press the yellow reset button, and (3) make the move again.");
            IsPlayerMove = true;
        }


        private void WaitForPlayer()
        {
            //Update the lights to be the player's turn
            while (!exitWaitForPlayer)
            {
                bool[] inputs;
                try
                {
                    inputs = Moxa.Read();
                }
                catch
                {
                    //connection error or the connection has been reset
                    continue;
                }

                if (inputs[0])
                {
                    //Trigger the camera. The camera will then send the move to the engine.
                    Camera.Trigger();
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
                    Camera.Reset();
                    AddToLog("Camera reset.");
                }
            }
        }
        #endregion

        #region Sending chess moves to the robot
        private void MovePiece(Move move)
        {
            Robot.Move(move.From);
            PickupPiece();
            Robot.Move(move.To);
            DropOffPiece();
        }

        private void CapturePiece(Move move)
        {
            //Remove the captured piece
            Robot.Move(move.To);
            PickupPiece();
            DropOffTakenPiece();
            //And then move the engine piece
            MovePiece(move);
        }

        private void Promotion(Move move, bool isCapture)
        {
            if (isCapture)
            {
                //Remove the captured piece before completing the promotion
                Robot.Move(move.To);
                PickupPiece();
                DropOffTakenPiece();
            }

            //Move the queen to the move.To position
            Robot.MoveToExtraQueenPosition();
            PickupPiece();
            Robot.Move(move.To);
            DropOffPiece();

            //Now move the pawn off the board over to the queen position
            Robot.Move(move.From);
            PickupPiece();
            Robot.MoveToExtraQueenPosition();
            DropOffPiece();
        }

        private void EnPassant(Move move)
        {
            //move the engine's pawn
            MovePiece(move);

            //then remove the captured pawn                            
            var capturedPawnLocation = new PartialMove(move.To.File + move.From.Rank.ToString());
            Robot.Move(capturedPawnLocation);
            PickupPiece();
            DropOffTakenPiece();
        }

        private void DropOffPiece()
        {
            Robot.LowerArm();
            Moxa.OpenGripper();
            Robot.RaiseArm();
        }

        private int numPlayerPiecesTaken = 0;
        private void DropOffTakenPiece()
        {
            //Move the arm to the from location
            numPlayerPiecesTaken++;
            Robot.GoToCapturedPieces(numPlayerPiecesTaken);
            Robot.LowerArm();
            Moxa.OpenGripper();
            Robot.RaiseArm();
        }

        private void PickupPiece()
        {
            Moxa.OpenGripper();//Technically, it should already be open
            Robot.LowerArm();
            Moxa.CloseGripper();
            Robot.RaiseArm();
        }
        #endregion
    }
}
