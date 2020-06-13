using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    sealed class HasSumConstraint : Constraint
    {
        public int Sum { get; private set; }
        public HasSumConstraint(int sum) : base(null) { Sum = sum; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                return null;
            var val = grid[ix.Value].Value;
            int numRemaining = 0, remainingCell = -1;
            for (var i = 0; i < grid.Length; i++)
            {
                if (i != ix.Value && grid[i] != null && grid[i].Value + minValue + val + minValue == Sum)
                    return Enumerable.Empty<Constraint>();
                if (grid[i] == null)
                {
                    numRemaining++;
                    remainingCell = i;
                }
            }
            if (numRemaining == 1)
                for (var v = 0; v < takens[remainingCell].Length; v++)
                    if (!Enumerable.Range(0, grid.Length).Any(i => i != remainingCell && grid[i].Value + minValue + v + minValue == Sum))
                        takens[remainingCell][v] = true;
            return null;
        }
    }
}