
namespace GridWorld
{
    /// <summary>
    /// A PathSquare holds all of the data about a GridSquare that we need for pathfinding.
    /// </summary>
    public class PathSquare
    {
        /// <summary>
        /// Create a new PathSquare at (x,y) corresponding to an unseen GridSquare (assumed empty).
        /// </summary>
        public PathSquare(int x, int y)
        {
            GridSquare gs = new GridSquare(x, y); // empty GridSquare

            IsBlocked = false; // so not blocked
            HasBeenSeen = false; // not yet seen
            G = -1; // distance from start to (x,y)
            H = -1; // estimated distance from (x,y) to goal
            OnClosedList = -1; // not on Closed list
            OnOpenList = -1; // not on Open list
            IsGoal = -1; // not a goal
            Parent = null; // The preceding square to this one in the path
            GridSq = gs; // the GridSquare for this square
        }

        /// <summary>
        /// The x-coordinate of this square.
        /// </summary>
        public int X
        { get { return GridSq.X; } }

        /// <summary>
        /// The y-coordinate of this square.
        /// </summary>
        public int Y
        { get { return GridSq.Y; } }

        /// <summary>
        /// Has this square been seen yet?.
        /// </summary>
        public bool HasBeenSeen;

        /// <summary>
        /// This PathSquare is blocked if it contains a .
        /// </summary>
        public bool IsBlocked;

        /// <summary>
        /// The total estimated distance from the start to the finish via this square.
        /// </summary>
        public int F
        { get { return G + H; } }

        /// <summary>
        /// Distance from start to this square.
        /// </summary>
        public int G;

        /// <summary>
        /// Optimistic estimate if the distance from this square to the finish.
        /// </summary>
        public int H;

        /// <summary>
        /// If OnClosedList == PathIndex then this PathSquare is on the closed list for A*
        /// (of squares which have been investigated already).
        /// Implemented this way so that we can search for multiple paths in a single turn by 
        /// incrementing PathIndex, without having to update every square of the Map, which
        /// would be very slow. 
        /// </summary>
        public int OnClosedList;

        /// <summary>
        /// If OnOpenList == PathIndex then this PathSquare is on the open list for A*
        /// (of squares which are about to be investigated).
        /// Implemented this way so that we can search for multiple paths in a single turn by 
        /// incrementing PathIndex, without having to update every square of the Map, which
        /// would be very slow. 
        /// </summary>
        public int OnOpenList;

        /// <summary>
        /// If IsGoal == PathIndex then this PathSquare is a goal square for A*.
        /// Implemented this way so that we can search for multiple paths in a single turn by 
        /// incrementing PathIndex, without having to update every square of the Map, which
        /// would be very slow. 
        /// </summary>
        public int IsGoal;

        /// <summary>
        /// The preceding square to this one in the path.
        /// </summary>
        public PathSquare Parent = null;

        /// <summary>
        /// The GridSquare corresponding to this PathSquare.
        /// </summary>
        public GridSquare GridSq = null;
    }
}
