using System;
using System.Collections;
using System.Collections.Generic;

namespace GridWorld
{
    class FSM
    {
        /// <summary>
        /// This Tank AI has two states.
        /// </summary>
        public enum FSMState { Explore, Battle };

        /// <summary>
        /// The state of the board from my point of view.
        /// </summary>
        PlayerWorldState MyWorldState;

        /// <summary>
        /// The map of all objects in grid squares seen. Unseen GridSquares are null.
        /// </summary>
        PathSquare[,] MyMap = null;

        /// <summary>
        /// The object for the A* algorithm.
        /// </summary>
        public AStar aStar;

        /// <summary>
        /// The square where my tank is headed.
        /// </summary>
        private PathSquare TargetSquare = null;

        /// <summary>
        /// The square where target enemy tank is.
        /// </summary>
        private GridSquare TargetTankSquare = null;

        /// <summary>
        /// The command for this turn.
        /// </summary>
        public Command MyCommand;

        /// <summary>
        /// Varaible to indicate the first round.
        /// </summary>
        private bool bInit = false;

        /// <summary>
        /// Variable that indicates if the agent is in a battle.
        /// </summary>
        private bool bInBattle = false;

        /// <summary>
        /// Variable that keep kill count.
        /// </summary>
        private int KillCount = 0;

        /// <summary>
        /// The state our tank is currently in. It starts in the Explore state.
        /// </summary>
        FSMState CurrentState = FSMState.Explore;

        /// <summary>
        /// Constructor, get a new instance of the A* class.
        /// </summary>
        public FSM()
        {
            aStar = new AStar();
        }

        /// <summary>
        /// Initialize the class with the current World State and Map of the grid.
        /// </summary>
        public void Initialize(PlayerWorldState pws, PathSquare[,] map)
        {
            this.MyWorldState = pws;
            this.MyMap = map;
            aStar.Initialize(pws, map);

            // if is the first round, finds an unseen square to go to
            if (!bInit)
            {
                bInit = true;
                FindUnseenSquare();
            }

            // if in battle and the target is destroyed, leave Battle state
            if (bInBattle && KillCount < MyWorldState.Kills)
            {
                bInBattle = false;
                TargetTankSquare = null;
            }
               
            
        }

        /// <summary>
        /// Return the command for the round.
        /// </summary>
        public Command GetCommand()
        {
            // if not in battle, gets the comand stored in the A* object
            if (!bInBattle)
                return aStar.MyCommand;

            // return the command when in Battle state
            return MyCommand;
        }

        /// <summary>
        /// Call the function associated with state.
        /// </summary>
        public void DoState()
        {
            // if there is an enemy in sight or if the tank is in Battle --> Battle State
            if (EnemyInSight() || bInBattle)
                CurrentState = FSMState.Battle;
            else
                CurrentState = FSMState.Explore;

            
            switch (CurrentState)
            {
                case FSMState.Explore:
                    Explore();
                    break;
                case FSMState.Battle:
                    Battle();
                    break;
                default:
                    throw (new Exception("Illegal State in DoState()"));
            }
        }

        /// <summary>
        /// Logic for the battle state.
        /// </summary>
        private void Battle()
        {
            // saves current kill count
            KillCount = MyWorldState.Kills;

            // sets variable indicating that the tank is in Battle State
            bInBattle = true;

            // get difference between positions
            int deltaX = TargetTankSquare.X - MyWorldState.MyGridSquare.X;
            int deltaY = TargetTankSquare.Y - MyWorldState.MyGridSquare.Y;

            // if the enemy tank and the agent are in the X axis (horizontal align)
            if (deltaX == 0)
            {
                if ( (deltaY > 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Up) ||
                     (deltaY < 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Down))
                {
                    MyCommand = new Command(Command.Move.Stay, true);
                    return;
                }
                else if ((deltaY > 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Right) ||
                     (deltaY < 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Left))
                {
                    MyCommand = new Command(Command.Move.RotateLeft, true);
                    return;
                }
                else if ((deltaY > 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Left) ||
                     (deltaY < 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Right))
                {
                    MyCommand = new Command(Command.Move.RotateRight, true);
                    return;
                }
            }

            // if the enemy tank and the agent are in the Y axis (vertial align)
            if (deltaY == 0)
            {
                if ((deltaX > 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Right) ||
                     (deltaX < 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Left))
                {
                    MyCommand = new Command(Command.Move.Stay, true);
                    return;
                }
                else if ((deltaX > 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Down) ||
                     (deltaX < 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Up))
                {
                    MyCommand = new Command(Command.Move.RotateLeft, true);
                    return;
                }
                else if ((deltaX > 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Up) ||
                     (deltaX < 0 && MyWorldState.MyFacing == PlayerWorldState.Facing.Down))
                {
                    MyCommand = new Command(Command.Move.RotateRight, true);
                    return;
                }
            }

            // if the tanks are not aligned
            if (deltaX != 0 && deltaY != 0)
            {
                
            }
            
            return;
        }

        /// <summary>
        /// Logic for the Explore State.
        /// </summary>
        private void Explore()
        {
            // if the target square has been seen, find a new enseen square
            if (MyMap[TargetSquare.X, TargetSquare.Y].HasBeenSeen)
            {
                FindUnseenSquare();
                return;
            }
            
            if (!aStar.MoveAlongMyPath()) // try to move along the path
            {
                // some problem with the path
                FindUnseenSquare(); // so find a new unseen square
                return;
            }
           
        }

        /// <summary>
        /// Finds a new unseen square. Gets all the unseen squares and choose one randomly.
        /// </summary>
        private void FindUnseenSquare()
        {
            List<PathSquare> SquaresNotSeen = new List<PathSquare>();

            foreach (var square in MyMap)
                if (MyMap[square.X, square.Y].HasBeenSeen == false)
                    SquaresNotSeen.Add(square);
                

            PathSquare Location = SquaresNotSeen[new Random().Next(SquaresNotSeen.Count)];

            FindAndMoveAlongPath(MyWorldState.MyGridSquare.X, MyWorldState.MyGridSquare.Y, Location.X, Location.Y);
        }

        /// <summary>
        /// Finds a path to the destination square and set the A* object to move along that path.
        /// </summary>
        private void FindAndMoveAlongPath(int x1, int y1, int x2, int y2)
        {
            aStar.MyPath = aStar.FindPath(x1, y1, x2, y2); // find a path
            if (aStar.MyPath != null) // path exists
            {
                TargetSquare = MyMap[x2, y2]; // (x,y) is unseen
                aStar.MyPathIndex = 0;
                aStar.MoveAlongMyPath();
                return;
            }
        }

        /// <summary>
        /// Checks if an enemy is in line of sight of the Agent. Only considers vertical and horizontal alignments.
        /// </summary>
        private bool EnemyInSight()
        {
            foreach (var square in MyWorldState.MyVisibleSquares)
            {
                if (square.Player != MyWorldState.ID && (
                    square.Contents == GridSquare.ContentType.TankUp ||
                    square.Contents == GridSquare.ContentType.TankRight ||
                    square.Contents == GridSquare.ContentType.TankLeft ||
                    square.Contents == GridSquare.ContentType.TankDown
                    ))
                {
                    if (aStar.GetEstimatedDistance(MyWorldState.MyGridSquare.X, MyWorldState.MyGridSquare.Y, square.X, square.Y)
                        <= MyWorldState.MaximumVisionDistance)
                    {
                        int deltaX = square.X - MyWorldState.MyGridSquare.X;
                        int deltaY = square.Y - MyWorldState.MyGridSquare.Y;

                        if (deltaX == 0 || deltaY == 0)
                        {
                            bool reachable = false;

                            if (deltaX == 0)
                            {
                                if (deltaY > 0)
                                    reachable = CheckIfEnemyIsReachable(MyWorldState.MyGridSquare.Y, square.Y, square.X, false);
                                if (deltaY < 0)
                                    reachable = CheckIfEnemyIsReachable(square.Y, MyWorldState.MyGridSquare.Y, square.X, false);
                            }
                            if (deltaY == 0)
                            {
                                if (deltaX > 0)
                                    reachable = CheckIfEnemyIsReachable(MyWorldState.MyGridSquare.X, square.X, square.Y, true);
                                if (deltaX < 0)
                                    reachable = CheckIfEnemyIsReachable(square.X, MyWorldState.MyGridSquare.X, square.Y, true);
                            }
                            if (reachable)
                            {
                                TargetTankSquare = square;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the square is directly reachable.
        /// </summary>
        private bool CheckIfEnemyIsReachable(int x1, int x2, int y, bool directionX)
        {
            if (directionX)
            {
                for (int x = x1 + 1; x < x2; x++)
                    if (MyMap[x, y].IsBlocked)
                        return false;
            }
            else
            {
                for (int x = x1 + 1; x < x2; x++)
                    if (MyMap[y, x].IsBlocked)
                        return false;
            }
            

            return true;
        }

        /// <summary>
        /// Get Neighbours of an square.
        /// </summary>
        private List<GridSquare> GetNeighbours(int x, int y)
        {
            List<GridSquare> Neighbours = new List<GridSquare>();

            if (y + 1 < MyWorldState.GridHeightInSquares)
                Neighbours.Add(MyMap[x, y + 1].GridSq); // Up

            if (y - 1 >= 0)
                Neighbours.Add(MyMap[x, y - 1].GridSq); // Down

            if (x + 1 < MyWorldState.GridWidthInSquares)
                Neighbours.Add(MyMap[x + 1, y].GridSq); // Right

            if (x - 1 >= 0)
                Neighbours.Add(MyMap[x - 1, y].GridSq); // Left

            return Neighbours;
        }
    }
}
