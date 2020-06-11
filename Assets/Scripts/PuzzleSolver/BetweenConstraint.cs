using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    sealed class BetweenConstraint : Constraint
    {
        public int Low { get; private set; }
        public int High { get; private set; }
        public bool Reversed { get; private set; }
        public BetweenConstraint(int low, int high, bool reversed) : base(null) { Low = low; High = high; Reversed = reversed; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                return null;
            int remainingCell = -1, numRemaining = 0;
            for (var i = 0; i < grid.Length; i++)
                if (grid[i] == null)
                {
                    numRemaining++;
                    remainingCell = i;
                }
                else if (Reversed ? (grid[i].Value + minValue < Low || grid[i].Value + minValue > High) : (grid[i].Value + minValue > Low && grid[i].Value + minValue < High))
                    return Enumerable.Empty<Constraint>();
            if (numRemaining != 1)
                return null;
            for (var v = 0; v < takens[remainingCell].Length; v++)
                if (Reversed ? (v + minValue >= Low && v + minValue <= High) : (v + minValue <= Low || v + minValue >= High))
                    takens[remainingCell][v] = true;
            return null;
        }
    }
}
