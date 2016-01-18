using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PuzzleProblem
{
    public static class GlobalVar
    {
        public const bool DEBUG = false;
    }

    /// <summary>
    /// Main class for program that handles initialization and calculation.
    /// </summary>
    public class PuzzleProblem
    {
        public static void Main()
        {
            PuzzleProblem pp = new PuzzleProblem();
        }

        public PuzzleProblem()
        {
            // Read the puzzle's configuration.
            Puzzle puzzle = new Puzzle("puzzle.txt");

            // Find the solution and output results.
            Puzzle solved = this.IDAStar(puzzle);
            if (solved.Solved())
            {
                int moves = 0;
                foreach (int i in solved.Path)
                {
                    Console.Write("{0}->", i);
                    moves++;
                }
                Console.WriteLine("COMPLETE");
                Console.WriteLine("# of moves: {0}", moves);
            }
            else
            {
                Console.WriteLine("No solution for puzzle configuration.");
            }

            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Invokes DPFSearch_Modified() using an iterative
        /// deepening technique.
        /// </summary>
        /// <param name="s">Initial state of the puzzle.</param>
        private Puzzle IDAStar(Puzzle s)
        {
            const int MAXITER = 8;
            int bound = s.MhDistance;

            if (GlobalVar.DEBUG)
                Console.WriteLine("Initial bound => " + bound);

            int numIter = 0;
            Puzzle p = new Puzzle();
            do
            {
                p = DPFSearch_Modified(s, 0, bound);
                if (!p.Solved())
                    bound = p.F;

                // DEBUG CODE
                if (GlobalVar.DEBUG)
                    Console.WriteLine("Bound => " + bound);
                // END DEBUG CODE

                numIter++;
            } while (!p.Solved() && numIter < MAXITER);

            return p;
        }

        /// <summary>
        /// Performs Iterative Deepening Depth-First Search.
        /// Modified for use with IDAStar().
        /// </summary>
        /// <param name="s">Current puzzle state.</param>
        /// <param name="g">Cost to reach current node.</param>
        /// <param name="bound">Max depth (i.e. distance) to compute to.</param>
        /// <returns>Solved puzzle state with moves.</returns>
        private Puzzle DPFSearch_Modified(Puzzle s, int g, int bound)
        {
            // Determine f(x) for s and see if it overshoots bound or is solved.
            s.F = g + s.MhDistance;
            if (s.F > bound || s.Solved())
                return s;

            // DEBUG CODE
            if (GlobalVar.DEBUG)
            {
                s.PrintPuzzle();
                Console.WriteLine("f = {0}, g = {1}, s.MhDistance = {2}", s.F, g, s.MhDistance);
                Console.ReadKey();
            }
            // END DEBUG CODE

            // Recursively call DPFSearch_Modified() for each successor
            // until the bound is reached or a solution is found.
            float min = float.PositiveInfinity;
            Puzzle pMin = new Puzzle();
            foreach (Puzzle successor in s.Successors())
            {
                Puzzle t = DPFSearch_Modified(successor, g + 1, bound);
                if (t.Solved())
                    return t;

                // Get the state with the lowest f(x) that overshoots the bound.
                if (t.F < min)
                {
                    min = t.F;
                    pMin = t;
                }
            }

            // Return state that overshoots the bound by the 
            // lowest amount so that a new bound can be set.
            return pMin;
        }
    }

    /// <summary>
    /// Main class that handles the structure of a puzzle.
    /// </summary>
    [Serializable]
    public class Puzzle
    {
        private int[,] puzzle;  // Puzzle configuration.
        private int n;          // Length/width of puzzle.
        private int f;          // f(x) of current puzzle configuration.
        private int mhDistance; // Manhatten Distance of puzzle configuration.
        private List<int> path; // Moves taken to get to this puzzle configuration.

        /// <summary>
        /// Parses the puzzle configuration file and populates the puzzle list
        /// with the parsed results.
        /// </summary>
        /// <param name="file">The file name of the puzzle configuration.</param>
        public Puzzle(string file)
        {
            // Write the given values into the puzzle list.
            using (StreamReader sr = new StreamReader(file))
            {
                string line = sr.ReadLine();
                string[] elements = line.Split(',');
                this.n = elements.Length - 1;
                puzzle = new int[elements.Length, elements.Length];
                for (int i = 0; i < elements.Length; i++)
                    puzzle[0, i] = Convert.ToInt32(elements[i]);

                // Read in the rest of the lines.
                int j = 1;
                while ((line = sr.ReadLine()) != null)
                {
                    elements = line.Split(',');
                    for (int i = 0; i < elements.Length; i++)
                        puzzle[j, i] = Convert.ToInt32(elements[i]);
                    j++;
                }
            }

            this.mhDistance = this.TotalManhattenDistance();
            this.f = this.mhDistance;
            this.path = new List<int>();
        }

        /// <summary>
        /// Constructor used in IDAStar algorithm.
        /// </summary>
        public Puzzle()
        {
            this.path = new List<int>();
        }


        /// <summary>
        /// Returns whether or not the puzzle is in a solved state.
        /// </summary>
        /// <returns>True if puzzle is in a solved state. False otherwise.</returns>
        public bool Solved()
        {
            int curr = 0;
            foreach (int i in this.puzzle)
            {
                if (i != curr)
                    return false;
                curr++;
            }
            return true;
        }

        /// <summary>
        /// Heuristic that returns the estimated (i.e. guess) number
        /// of moves left until the puzzle can be solved. This is
        /// guaranteed to be larger than the actual number of moves
        /// remaining.
        /// </summary>
        /// <returns>The minimum number of moves left to complete the puzzle.</returns>
        public int TotalManhattenDistance()
        {
            int distance = 0;
            for (int i = 0; i < this.n; i++)
                for (int j = 0; j < this.n; j++)
                    if (this.puzzle[i, j] != 0)
                        distance += this.ManhattenDistance(this.puzzle[i, j], i, j);
            return distance;
        }

        /// <summary>
        /// Returns the distance a single piece must move to be in its correct place.
        /// </summary>
        /// <param name="value">The numbered piece of interest.</param>
        /// <param name="x">Current x position of piece of interest.</param>
        /// <param name="y">Current y position of piece of interest.</param>
        /// <returns>Number of moves to correct spot based on Manhatten rules.</returns>
        public int ManhattenDistance(int value, int y, int x)
        {
            int actual_x = value % (this.n + 1);
            int actual_y = value / (this.n + 1);
            return Math.Abs(actual_x - x) + Math.Abs(actual_y - y);
        }

        /// <summary>
        /// Returns an array of objects of type PuzzleNode that contains
        /// all possible one move combinations for the current puzzle state.
        /// </summary>
        /// <returns>All possible single move combinations for current puzzle state.</returns>
        public Puzzle[] Successors()
        {
            // Find the empty tile so other pieces can be swapped into it.
            int[] zero = this.NumberLocation(0);

            // Possible tiles that can be swapped.
            // Note: "UP" means the 0 tile can 'move' up.
            // i.e. the tile above the 0 tile moves down.
            Dictionary<string, int> completeSwapList = new Dictionary<string, int>();
            completeSwapList["UP"] = zero[0] - 1;
            completeSwapList["DOWN"] = zero[0] + 1;
            completeSwapList["LEFT"] = zero[1] - 1;
            completeSwapList["RIGHT"] = zero[1] + 1;

            // Remove any moves that cannot be done.
            Dictionary<string, int> swapList = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> kvp in completeSwapList)
                if (kvp.Value >= 0 && kvp.Value <= this.n)
                    swapList[kvp.Key] = kvp.Value;

            // Make and store all possible moves and return the result.
            Puzzle[] successors = new Puzzle[swapList.Count];
            int i = 0;
            foreach (KeyValuePair<string, int> kvp in swapList)
            {
                Puzzle q = DeepClone(this);
                q.Swap(kvp.Key, zero);
                successors[i] = q;
                i++;
            }

            return successors;
        }

        /// <summary>
        /// Swaps the 0 tile for a numbered tile in a certain direction.
        /// Possible directions are UP, DOWN, LEFT, and RIGHT.
        /// </summary>
        /// <param name="direction">Direction to 'move' 0 tile to.</param>
        /// <param name="zero">Array of size 2 holding [y, x] coords of 0 tile.</param>
        /// <returns>Object of type Puzzle with the swapped tile.</returns>
        private Puzzle Swap(string direction, int[] zero)
        {
            int y = zero[0];
            int x = zero[1];
            switch (direction)
            {
                case "UP":
                    this.puzzle[y, x] = this.puzzle[y - 1, x];
                    this.puzzle[y - 1, x] = 0;
                    break;
                case "DOWN":
                    this.puzzle[y, x] = this.puzzle[y + 1, x];
                    this.puzzle[y + 1, x] = 0;
                    break;
                case "LEFT":
                    this.puzzle[y, x] = this.puzzle[y, x - 1];
                    this.puzzle[y, x - 1] = 0;
                    break;
                case "RIGHT":
                    this.puzzle[y, x] = this.puzzle[y, x + 1];
                    this.puzzle[y, x + 1] = 0;
                    break;
                default:
                    throw new Exception("Improper direction given...");
            }

            // DEBUG CODE
            if (GlobalVar.DEBUG)
            {
                Console.WriteLine("Moved 0 tile {0}", direction);
                this.PrintPuzzle();
                Console.ReadKey();
            }
            // END DEBUG CODE

            this.path.Add(puzzle[y, x]);
            this.mhDistance = this.TotalManhattenDistance();
            return this;
        }

        /// <summary>
        /// Returns the y and x coordinates of the given number.
        /// </summary>
        /// <param name="num">Number of interest (0 to n).</param>
        /// <returns>2-element array with coords as array[y, x]</returns>
        public int[] NumberLocation(int num)
        {
            int[] location = new int[2];
            for (int i = 0; i <= this.n; i++)
            {
                for (int j = 0; j <= this.n; j++)
                {
                    if (this.puzzle[i, j] == num)
                    {
                        location[0] = i;
                        location[1] = j;
                        return location;
                    }
                }
            }
            
            throw new Exception("Could not locate number in puzzle.");
        }

        /// <summary>
        /// Conducts a deep copy of an object.
        /// </summary>
        /// <param name="obj">The object to copy.</param>
        /// <returns>A new, copied object.</returns>
        private static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// Outputs the puzzle configuration.
        /// </summary>
        public void PrintPuzzle()
        {
            int i = 0;
            foreach (int j in this.puzzle)
            {
                Console.Write("{0} ", j);
                i++;
                if (i % (this.n + 1) == 0)
                    Console.WriteLine();
            }
            Console.WriteLine("----------");
        }

        public int[,] PuzzleConfig
        {
            get { return this.puzzle; }
            set { this.puzzle = value; }
        }

        public int N
        {
            get { return this.n; }
            set { this.n = value; }
        }

        public int F
        {
            get { return this.f; }
            set { this.f = value; }
        }

        public int MhDistance
        {
            get { return this.mhDistance; }
            set { this.mhDistance = value; }
        }

        public List<int> Path
        {
            get { return this.path; }
            set { this.path = value; }
        }
    }
}
