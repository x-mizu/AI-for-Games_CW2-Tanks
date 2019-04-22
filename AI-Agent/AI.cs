// AI Agent - Tanks - Version 3.0 - Matheus Shimizu Felisberto

namespace GridWorld
{
    /// <summary>
    /// This is a player that uses a Finite State Machine (FSM)
    /// with two states to control a Tank.
    /// 
    /// It starts in the Explore() state. The state diagram is:
    ///                   
    ///                  Enemy destroyed
    ///     Battle -----------}--------- Explore                          
    ///        |                            |
    ///         --------------{-------------
    ///                   Enemy seen
    /// 
    /// It uses A* search to plot a path to the unseen square.
    /// 
    /// </summary>
    public class AI : BasePlayer
    {
        /// <summary>
        /// The state of the board from my point of view.
        /// </summary>
        static PlayerWorldState MyWorldState;

        /// <summary>
        /// The map of all permanent objects in grid squares seen (i.e. ignore tanks unless they have been destroyed). Unseen GridSquares are null.
        /// </summary>
        PathSquare[,] MyMap = null;

        private FSM fsm;

        /// <summary>
        /// The constuctor function is only called once (when the AIPlayer is first created).
        /// Initialise any data structures you need in here and set your AI player's name.
        /// </summary>
        public AI() : base()
        {
            this.Name = "X-Mizu's AI"; // the name that will appear on the client and server.
            fsm = new FSM();
        }

        /// <summary>
        /// This is the function that is called to get your moves for each turn.
        /// </summary>
        /// <param name="igrid">This contains the board information - note that this contains GridSquare information only for squares I can see</param>
        /// <returns>A Command for this turn - Up, Down, Left or Right for my tank.</returns>
        public override ICommand GetTurnCommands(IPlayerWorldState igrid)
        {
            MyWorldState = (PlayerWorldState)igrid; // the board
            UpdateMyMap(); // update my map with the new squares seen
            fsm.Initialize(MyWorldState, MyMap);

            //WriteTrace("MyWorldState:");
            //WriteTrace(MyWorldState);

            WriteTrace("\r\nMyMap:");
            WriteTrace(MyMapToString());

            fsm.DoState();

            WriteTrace("\r\nMyPath:");
            WriteTrace(fsm.aStar.SearchToString(fsm.aStar.MyPath));

            if (fsm.GetCommand() != null)
                WriteTrace(fsm.GetCommand().ToString());
            else
                WriteTrace("null Command");

            return fsm.GetCommand();
        }

        /// <summary>
        /// Update my "map" of all permanent objects (rocks and destroyed tanks) and empty GridSquares I have seen so far.
        /// </summary>
        private void UpdateMyMap()
        {
            int x, y;

            // initialise mygrid if it hasn't yet been created
            if (MyMap == null)
            {
                MyMap = new PathSquare[MyWorldState.GridWidthInSquares, MyWorldState.GridHeightInSquares];
                for (x = 0; x < MyWorldState.GridWidthInSquares; x++)
                    for (y = 0; y < MyWorldState.GridHeightInSquares; y++)
                        MyMap[x, y] = new PathSquare(x,y); // set each unseen grid square to null
            }

            // remove previous tank location from MyMap
            foreach (var square in MyMap)
            {
                if (
                    square.GridSq.Contents == GridSquare.ContentType.TankUp ||
                    square.GridSq.Contents == GridSquare.ContentType.TankRight ||
                    square.GridSq.Contents == GridSquare.ContentType.TankLeft ||
                    square.GridSq.Contents == GridSquare.ContentType.TankDown
                    )
                {
                    if (square.GridSq.Player != MyWorldState.ID)
                        square.HasBeenSeen = false;
                    square.GridSq = new GridSquare(square.X, square.Y, GridSquare.ContentType.Empty);
                    square.IsBlocked = false;
                }
            }

            // update the map in mygrid
            foreach (GridSquare gs in MyWorldState.MyVisibleSquares)
            {
                if (MyMap[gs.X, gs.Y].HasBeenSeen == false) // not yet seen GridSquare gs 
                {
                    MyMap[gs.X, gs.Y].HasBeenSeen = true;

                    // record fixed objects - rocks, destroyed tanks and empty squares
                    if (gs.Contents == GridSquare.ContentType.Rock)
                    {
                        MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.Rock);
                        MyMap[gs.X, gs.Y].IsBlocked = true;
                    }
                    else if (gs.Contents == GridSquare.ContentType.DestroyedTank)
                    {
                        MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.DestroyedTank);
                        MyMap[gs.X, gs.Y].IsBlocked = true;                    
                    }
                    else if (gs.Contents == GridSquare.ContentType.Empty)
                        MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.Empty);
                    // record moving objects - tanks
                    else if (gs.Contents == GridSquare.ContentType.TankDown)
                    {
                        MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankDown, gs.Player);
                        MyMap[gs.X, gs.Y].IsBlocked = true;
                    }
                    else if (gs.Contents == GridSquare.ContentType.TankLeft)
                    {
                        MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankLeft, gs.Player);
                        MyMap[gs.X, gs.Y].IsBlocked = true;
                    }
                    else if (gs.Contents == GridSquare.ContentType.TankRight)
                    {
                        MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankRight, gs.Player);
                        MyMap[gs.X, gs.Y].IsBlocked = true;
                    }
                    else if (gs.Contents == GridSquare.ContentType.TankUp)
                    {
                        MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankUp, gs.Player);
                        MyMap[gs.X, gs.Y].IsBlocked = true;
                    }
                    
                }
                else if (gs.Contents == GridSquare.ContentType.DestroyedTank && MyMap[gs.X, gs.Y].GridSq.Contents != GridSquare.ContentType.DestroyedTank)
                {
                    MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.DestroyedTank); // record new destroyed tanks even if seen before.
                    MyMap[gs.X, gs.Y].IsBlocked = true;
                }
                else if (gs.Contents == GridSquare.ContentType.TankDown)
                {
                    MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankDown, gs.Player);
                    MyMap[gs.X, gs.Y].IsBlocked = true;
                }
                else if (gs.Contents == GridSquare.ContentType.TankLeft)
                {
                    MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankLeft, gs.Player);
                    MyMap[gs.X, gs.Y].IsBlocked = true;
                }
                else if (gs.Contents == GridSquare.ContentType.TankRight)
                {
                    MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankRight, gs.Player);
                    MyMap[gs.X, gs.Y].IsBlocked = true;
                }
                else if (gs.Contents == GridSquare.ContentType.TankUp)
                {
                    MyMap[gs.X, gs.Y].GridSq = new GridSquare(gs.X, gs.Y, GridSquare.ContentType.TankUp, gs.Player);
                    MyMap[gs.X, gs.Y].IsBlocked = true;
                }
            }

           
        }

        /// <summary>
        /// Output the Tanks board in a readable text format.
        /// . = empty square, # = Rock, D = Destroyed Tank, space = NotVisible, 
        /// ^,v,>, = tank with facing (including your own tank).
        /// The world is enclosed by a border.
        /// </summary>
        /// <returns></returns>
        public string MyMapToString()
        {
            string outstr = "\r\n";
            int x, y;

            // top border
            outstr += "  ";
            for (x = 0; x < MyWorldState.GridWidthInSquares; x++)
                outstr += x % 10;
            outstr += "\r\n  ";
            for (x = 0; x < MyWorldState.GridWidthInSquares; x++)
                outstr += "-";
            outstr += "\r\n";

            for (y = MyWorldState.GridHeightInSquares - 1; y >= 0; y--)
            {
                outstr += y % 10 + "|";

                for (x = 0; x < MyWorldState.GridWidthInSquares; x++)
                {
                    if (MyMap[x, y].HasBeenSeen)
                    {
                        switch (MyMap[x, y].GridSq.Player)
                        {
                            case 0:
                                if (MyMap[x, y].GridSq.Contents == GridSquare.ContentType.Rock)
                                    outstr += "#";
                                else if (MyMap[x, y].GridSq.Contents == GridSquare.ContentType.Empty)
                                    outstr += "."; // empty
                                else if (MyMap[x, y].GridSq.Contents == GridSquare.ContentType.DestroyedTank)
                                    outstr += "D";
                                break;
                            default:
                                if (MyMap[x, y].GridSq.Contents == GridSquare.ContentType.TankUp)
                                    outstr += "^";
                                else if (MyMap[x, y].GridSq.Contents == GridSquare.ContentType.TankDown)
                                    outstr += "v";
                                else if (MyMap[x, y].GridSq.Contents == GridSquare.ContentType.TankLeft)
                                    outstr += "<";
                                else if (MyMap[x, y].GridSq.Contents == GridSquare.ContentType.TankRight)
                                    outstr += ">";
                                break;
                        }
                    }
                    else
                    {
                        outstr += " ";
                    }
                }

                outstr += "|" + y % 10 + "\r\n"; // right border
            }

            // bottom border
            outstr += "  ";
            for (x = 0; x < MyWorldState.GridWidthInSquares; x++)
                outstr += "-";
            outstr += "\r\n  ";
            for (x = 0; x < MyWorldState.GridWidthInSquares; x++)
                outstr += x % 10;
            outstr += "\r\n";

            // stats
            outstr += "Your tank at (" + MyWorldState.MyGridSquare.X + "," + MyWorldState.MyGridSquare.X + ").";
            outstr += " Empty: " + MyWorldState.EmptySquaresSeen + ", Rock: " + MyWorldState.RockSquaresSeen + ", Shots: " + MyWorldState.ShotsFired + ", Kills: " + MyWorldState.Kills + ", Score: " + MyWorldState.Score + ".\r\n";

            return outstr;
        }

    }
}

