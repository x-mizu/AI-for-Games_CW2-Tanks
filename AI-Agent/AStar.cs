using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GridWorld
{
    // The principal way that this code should be used is via the function:
    // public ArrayList FindPath(int fromX, int fromY, int toX, int toY)
    // which returns an ArrayList of PathSquares if there is a path, or
    // null if no path exists.
    class AStar
    {
        /// <summary>
        /// The state of the board from my point of view.
        /// </summary>
        PlayerWorldState MyWorldState;

        /// <summary>
        /// The map of all permanent objects in grid squares seen (i.e. ignore tanks unless they have been destroyed). Unseen GridSquares are null.
        /// </summary>
        PathSquare[,] MyMap = null;

        /// <summary>
        /// The command for this turn.
        /// </summary>
        public Command MyCommand;

        /// <summary>
        /// The current path which my tank is moving along
        /// </summary>
        public List<PathSquare> MyPath = null;

        /// <summary>
        /// An index to the next square of MyPath which my tank should move to.
        /// </summary>
        public int MyPathIndex;

        /// <summary>
        /// Each time we create a path, we increment the PathIndex.
        /// PathIndex is used to record whether a grid square has been inspected in the current path.
        /// This allows multiple paths to be created each turn. 
        /// </summary>
        private int PathIndex = 0;

        /// <summary>
        /// A sorted open list of squares "to be investigated" for pathfinding.
        /// The most interesting square comes first in the list.
        /// </summary>
        private List<PathSquare> OpenList = new List<PathSquare>();

        /// <summary>
        /// The square which the A* search starts from. Used only by SearchToString().
        /// </summary>
        private PathSquare StartSquare = null;

        /// <summary>
        /// The square which the A* is heading towards.
        /// Note that generally the search is heading towards a group of squares adjacent to this square, 
        /// so that TargetSquare is usually used only to determine the distance estimate.
        /// </summary>
        private PathSquare TargetSquare = null;

        /// <summary>
        /// Temporary ArrayList used to hold the Neighbours of the current square.
        /// </summary>
        private ArrayList Neighbours = new ArrayList();


        public void Initialize(PlayerWorldState pws, PathSquare[,] map)
        {
            this.MyWorldState = pws;
            this.MyMap = map;
        }

        /// <summary>
        /// Get the Command.Move direction that moves from fromsq to tosq,
        /// or null if none exists.
        /// </summary>
        Command.Move GetMoveDirection(PathSquare fromsq, PathSquare tosq)
        {
            if (fromsq.X == tosq.X)
            {
                if (fromsq.Y == tosq.Y - 1)
                    return Command.Move.Up;
                else if (fromsq.Y == tosq.Y + 1)
                    return Command.Move.Down;
            }
            else if (fromsq.Y == tosq.Y)
            {
                if (fromsq.X == tosq.X - 1)
                    return Command.Move.Right;
                else if (fromsq.X == tosq.X + 1)
                    return Command.Move.Left;
            }

            return Command.Move.Stay; // no legal move from fromsq to tosq
        }

        /// <summary>
        /// Get the Command.Move rotation that rotates your tank to face in Command.Move direction dir,
        /// or Command.Move.Stay if none exists.
        /// </summary>
        Command.Move GetMoveRotation(Command.Move dir)
        {
            switch (dir)
            {
                case Command.Move.Up:
                    switch (MyWorldState.MyFacing)
                    {
                        case PlayerWorldState.Facing.Up:
                            return Command.Move.Stay;
                        case PlayerWorldState.Facing.Down:
                            return Command.Move.RotateLeft;
                        case PlayerWorldState.Facing.Left:
                            return Command.Move.RotateRight;
                        case PlayerWorldState.Facing.Right:
                            return Command.Move.RotateLeft;
                    }
                    break;
                case Command.Move.Down:
                    switch (MyWorldState.MyFacing)
                    {
                        case PlayerWorldState.Facing.Up:
                            return Command.Move.RotateLeft;
                        case PlayerWorldState.Facing.Down:
                            return Command.Move.Stay;
                        case PlayerWorldState.Facing.Left:
                            return Command.Move.RotateLeft;
                        case PlayerWorldState.Facing.Right:
                            return Command.Move.RotateRight;
                    }
                    break;
                case Command.Move.Left:
                    switch (MyWorldState.MyFacing)
                    {
                        case PlayerWorldState.Facing.Up:
                            return Command.Move.RotateLeft;
                        case PlayerWorldState.Facing.Down:
                            return Command.Move.RotateRight;
                        case PlayerWorldState.Facing.Left:
                            return Command.Move.Stay;
                        case PlayerWorldState.Facing.Right:
                            return Command.Move.RotateLeft;
                    }
                    break;
                case Command.Move.Right:
                    switch (MyWorldState.MyFacing)
                    {
                        case PlayerWorldState.Facing.Up:
                            return Command.Move.RotateRight;
                        case PlayerWorldState.Facing.Down:
                            return Command.Move.RotateLeft;
                        case PlayerWorldState.Facing.Left:
                            return Command.Move.RotateLeft;
                        case PlayerWorldState.Facing.Right:
                            return Command.Move.Stay;
                    }
                    break;
            }

            return Command.Move.Stay; // should never get here
        }

        /// <summary>
        /// Move along MyPath - always turn to face the direction of travel (or else I can get stuck
        /// behind a block that I cannot see).
        /// Return false if this move is likely to fail because the next square is blocked
        /// </summary>
        public bool MoveAlongMyPath()
        {
            if (MyPath != null && MyPathIndex < MyPath.Count) // not yet at end of path
            {
                PathSquare nextsquare = MyPath[MyPathIndex];
                if (nextsquare.IsBlocked)
                    return false; // next square is blocked so find a new path
                Command.Move dir = GetMoveDirection(nextsquare.Parent, nextsquare);
                Command.Move rot = GetMoveRotation(dir);
                if (rot != Command.Move.Stay)
                    MyCommand = new Command(rot, false); // rotate to face direction of travel
                else
                {
                    MyCommand = new Command(dir, false); // move along path
                    MyPathIndex++; // so go to next square of path
                }

                return true;
            }
            else
                return false;
        }


        /// <summary>
        /// Returns the most promising square to grow the path from - the zeroth element since the list
        /// is sorted in ascending order of F value
        /// </summary>
        /// <returns></returns>
        public PathSquare GetLowestFCostOpenListSquare()
        {
            return OpenList[0];
        }

        /// <summary>
        /// Finds a path from grid square (fromX,fromY) to grid square (toX,toY), avoiding all fixed
        /// obstacles, and returns the result as an ArrayList. The start and finish points are not included
        /// in the path (since the start point will usually be an Ant and the finish point will usually
        /// be food, and anthill or another ant).
        /// </summary>
        /// <param name="fromX">The X co-ordinate of the start square.</param>
        /// <param name="fromY">The Y co-ordinate of the start square.</param>
        /// <param name="toX">The X co-ordinate of the target square.</param>
        /// <param name="toY">The Y co-ordinate of the target square.</param>
        /// <returns>Returns an ArrayList storing the path, if one has been found, or null otherwise. 
        /// This ArrayList is also held in the Path class variable.</returns>
        public List<PathSquare> FindPath(int fromX, int fromY, int toX, int toY)
        {
            if (GetEstimatedDistance(fromX, fromY, toX, toY) == 0)
                return new List<PathSquare>(); // already at goal so return empty path

            // New path so increment this so that marks in Map[,] can be distinguished from those
            // of previous paths.
            PathIndex++;

            // Note that if you change terrain without reloading your AI the Map will
            // not be updated and you will get (weird!) results for the previous terrain.

            // set the target square - we want to get adjacent to this square.
            TargetSquare = MyMap[toX, toY];

            // label destination square. Note that there can be more than one, but here, only one is considered.
            MyMap[toX, toY].G = -1;
            MyMap[toX, toY].H = 1;
            MyMap[toX, toY].IsGoal = PathIndex; // this square is a goal for this path.
            MyMap[toX, toY].OnClosedList = -1;
            MyMap[toX, toY].OnOpenList = -1;
            MyMap[toX, toY].Parent = null;

            // set the start square - the first square the the path will be adjacent 
            // to this square.
            StartSquare = MyMap[fromX, fromY];

            StartSquare.G = 0; // Distance from StartSquare to StartSquare is zero
            StartSquare.H = GetEstimatedDistanceToGoal(StartSquare);

            // Now initialise the OpenList with the neighbours of StartSquare
            // and put StartSquare on the closed list.
            List<PathSquare> fromsq = GetReachableNeighbours(fromX, fromY);
            if (fromsq.Count == 0)
                return null; // no reachable squares adjacent to (fromX, fromY)
            OpenList.Clear();
            foreach (PathSquare ps in fromsq)
            {
                if (ps.IsGoal == PathIndex)
                {
                    // path (of length 1) found.
                    ps.Parent = StartSquare;
                    List<PathSquare> path = new List<PathSquare>();
                    path.Add(ps);
                    return path;
                }

                AddToOpenList(ps, StartSquare);
            }

            StartSquare.OnClosedList = PathIndex; // Put StartSquare on closed list

            return AStarSearch(); // search for the path.
        }

        /// <summary>
        /// Add a PathSquare to the correct sorted position on the open list, and update the PathSquare's data. 
        /// P and parent are guaranteed to be adjacent.
        /// </summary>
        public void AddToOpenList(PathSquare P, PathSquare parent)
        {
            P.G = parent.G + 1;
            P.H = GetEstimatedDistanceToGoal(P);
            P.OnClosedList = -1;
            P.OnOpenList = PathIndex;
            P.Parent = parent;

            int i;
            for (i = 0; i < OpenList.Count; i++)
            {
                PathSquare ps = (PathSquare)OpenList[i];
                if (ps.F >= P.F)
                    break;
            }

            OpenList.Insert(i, P); // insert in sorted position
        }

        /// <summary>
        /// Check whether the path from parent to P is cheaper than the current path, for P on the open list.
        /// Note parent must be adjacent to P.
        /// </summary>
        /// <param name="P"></param>
        /// <param name="parent"></param>
        public void UpdateOpenListEntry(PathSquare P, PathSquare parent)
        {
            if (P.G > parent.G + 1)
            {
                P.Parent = parent;
                P.G = parent.G + 1;
            }
        }

        /// <summary>
        /// Get an optimistic estimate of the distance from P to TargetSquare.
        /// The estimate assumes that there is no terrain blocking the path.
        /// </summary>
        public int GetEstimatedDistanceToGoal(PathSquare P)
        {
            return Math.Abs(TargetSquare.X - P.X) + Math.Abs(TargetSquare.Y - P.Y); // x distance + y distance
        }

        /// <summary>
        /// Get an optimistic estimate of the distance from (x1,y1) to (x2,y2).
        /// The estimate assumes that there is no terrain blocking the path.
        /// </summary>
        public int GetEstimatedDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x2 - x1) + Math.Abs(y2 - y1); // x distance + y distance
        }

        /// <summary>
        /// Saves the current path to ArrayList Path.
        /// </summary>
        /// <param name="S"></param>
        private List<PathSquare> SavePath(PathSquare goal)
        {
            List<PathSquare> path = new List<PathSquare>();
            PathSquare p = goal;
            while (p.Parent.G > 0)
            {
                path.Insert(0, p);
                p = p.Parent;
            }

            path.Insert(0, p);

            return path;
        }

        /// <summary>
        /// Sets the Neighbours ArrayList up with the PathSquares that can be reached from square (x,y).
        /// A square cannot be reached if it is blocked.
        private List<PathSquare> GetReachableNeighbours(int x, int y)
        {
            List<PathSquare> Neighbours = new List<PathSquare>();

            if (y + 1 < MyWorldState.GridHeightInSquares && MyMap[x, y + 1].IsBlocked == false)
                Neighbours.Add(MyMap[x, y + 1]); // Up

            if (y - 1 >= 0 && MyMap[x, y - 1].IsBlocked == false)
                Neighbours.Add(MyMap[x, y - 1]); // Down

            if (x + 1 < MyWorldState.GridWidthInSquares && MyMap[x + 1, y].IsBlocked == false)
                Neighbours.Add(MyMap[x + 1, y]); // Right

            if (x - 1 >= 0 && MyMap[x - 1, y].IsBlocked == false)
                Neighbours.Add(MyMap[x - 1, y]); // Left

            return Neighbours;
        }

        /// <summary>
        /// Is square S on the closed list of squares that have already been investigated?
        /// </summary>
        private bool IsOnClosedList(PathSquare S)
        {
            if (S.OnClosedList == PathIndex)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Is square S on the open list of squares will be investigated soon?
        /// </summary>
        private bool IsOnOpenList(PathSquare S)
        {
            if (S.OnOpenList == PathIndex)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Return the shortest path from any square currently on the OpenList to any goal square.
        /// A fairly faithful implementation of the pseudocode given in lectures.
        /// If no path exists, return null.
        /// </summary>
        private List<PathSquare> AStarSearch()
        {
            PathSquare S; // current square

            while (true)
            {
                if (OpenList.Count == 0)
                    return null; // If open list is empty, there is no path.
                else
                    S = GetLowestFCostOpenListSquare(); // Find lowest F cost square S on open list.

                // If S is the goal square, then save path and exit.
                if (S.IsGoal == PathIndex)
                    return SavePath(S);

                // Switch S to closed list.
                OpenList.Remove(S);
                S.OnClosedList = PathIndex;
                S.OnOpenList = -1;

                List<PathSquare> Neighbours = GetReachableNeighbours(S.X, S.Y); // put reachable cells from S in Neighbours ArrayList
                foreach (PathSquare N in Neighbours)
                {
                    if (!IsOnClosedList(N))
                    {
                        if (IsOnOpenList(N))
                            UpdateOpenListEntry(N, S); // check whether this is better than the best path to N seen so far
                        else
                            AddToOpenList(N, S); // mark this square for investigation later.
                    }
                }
            }
        }

        /// <summary>
        /// Output the current state of the search in a readable text format.
        /// # = blocked, . = empty and not on open or closed list,
        /// o = on open list, x = on closed list, S = start, T = target
        /// * = at head of open list - next to expand
        /// p = on path (if one exists)
        /// </summary>
        public string SearchToString(List<PathSquare> path)
        {
            string outstr = "";

            for (int y = MyWorldState.GridHeightInSquares - 1; y >= 0; y--)
            {
                for (int x = 0; x < MyWorldState.GridWidthInSquares; x++)
                {
                    if (MyMap[x, y] == TargetSquare)
                        outstr += "T";
                    else if (MyMap[x, y] == StartSquare)
                        outstr += "S";
                    else if (MyMap[x, y].IsBlocked)
                        outstr += "#";
                    else if (MyWorldState.MyGridSquare.X == x && MyWorldState.MyGridSquare.Y == y)
                        outstr += GetTankSymbol(MyWorldState.MyFacing);
                    else if (path != null && path.Contains(MyMap[x, y]))
                        outstr += "p";
                    else if (OpenList.Count > 0)
                        if (MyMap[x, y] == OpenList[0])
                            outstr += "*";
                        else if (MyMap[x, y].OnOpenList == PathIndex)
                            outstr += "o";
                        else if (MyMap[x, y].OnClosedList == PathIndex)
                            outstr += "x";
                        else
                            outstr += ".";
                }
                outstr += "\r\n";
            }

            return outstr;
        }

        /// <summary>
        /// Get the symbol >, v, ^ etc. for ths tank facing
        /// </summary>
        private string GetTankSymbol(PlayerWorldState.Facing facing)
        {
            if (facing == PlayerWorldState.Facing.Up)
                return "^";
            else if (facing == PlayerWorldState.Facing.Down)
                return "v";
            else if (facing == PlayerWorldState.Facing.Left)
                return "<";
            else if (facing == PlayerWorldState.Facing.Right)
                return ">";

            return " "; // shouldn't ever get here
        }
    }
}
