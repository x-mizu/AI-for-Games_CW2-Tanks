using System;
using System.Collections;
using System.Collections.Generic;

namespace GridWorld
{
    class FSM
    {
        /// <summary>
        /// Our very simple sample Tank AI has only two states. Add any new states in here.
        /// </summary>
        public enum FSMState { Explore, Battle };

        /// <summary>
        /// The state of the board from my point of view.
        /// </summary>
        PlayerWorldState MyWorldState;

        /// <summary>
        /// The map of all permanent objects in grid squares seen (i.e. ignore tanks unless they have been destroyed). Unseen GridSquares are null.
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
        /// The square where my enemy tank is.
        /// </summary>
        private GridSquare TargetTankSquare = null;

        /// <summary>
        /// The command for this turn.
        /// </summary>
        public Command MyCommand;

        private bool bInit = false;

        private bool bInBattle = false;

        private int KillCount = 0;

        /// <summary>
        /// The state our tank is currently in. It starts in the FindNewUnseenSquare state.
        /// </summary>
        FSMState CurrentState = FSMState.Explore;

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

            if (!bInit)
            {
                bInit = true;
                FindUnseenSquare();
            }

            if (KillCount < MyWorldState.Kills)
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
            if (!bInBattle)
                return aStar.MyCommand;

            return MyCommand;
        }

        /// <summary>
        /// Call the function associated with state. Add any new states in here.
        /// </summary>
        public void DoState()
        {
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
        /// Find any unseen GridSquare. NOT very scientific.
        /// </summary>
        /// <returns></returns>
        private void Battle()
        {
            KillCount = MyWorldState.Kills;

            bInBattle = true;
            int deltaX = TargetTankSquare.X - MyWorldState.MyGridSquare.X;
            int deltaY = TargetTankSquare.Y - MyWorldState.MyGridSquare.Y;

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

            if (deltaX != 0 && deltaY != 0)
            {
                
            }
            
            return;
        }

        /// <summary>
        /// Issue a command for my tank to move towards the unseen square. NOT very scientific.
        /// </summary>
        private void Explore()
        {
            
            if (MyMap[TargetSquare.X, TargetSquare.Y].HasBeenSeen)
            {
                // TargetUnseenSquare has been seen
                FindUnseenSquare(); // so change state to find a new unseen square
                return;
            }

            if (!aStar.MoveAlongMyPath()) // try to move along the path
            {
                // some problem with the path
                FindUnseenSquare(); // so change state to find a new unseen square
                return;
            }
           
        }

        private void FindUnseenSquare()
        {
            foreach (var square in MyMap)
                if (MyMap[square.X, square.Y].HasBeenSeen == false)
                {
                    FindAndMoveAlongPath(MyWorldState.MyGridSquare.X, MyWorldState.MyGridSquare.Y, square.X, square.Y);
                    return;
                }
        }

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
