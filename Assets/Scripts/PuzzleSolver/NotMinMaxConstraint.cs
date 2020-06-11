using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    sealed class NotMinMaxConstraint : Constraint
    {
        public MinMaxMode Mode { get; private set; }
        public NotMinMaxConstraint(int cell, MinMaxMode mode) : base(new[] { cell }) { Mode = mode; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                return null;

            // NOTE: ‘min’ and ‘max’ are the min and max values in ‘grid’, NOT the actual min and max values
            int remainingCell = -1, min = int.MaxValue, max = int.MinValue;
            for (var i = 0; i < grid.Length; i++)
            {
                if (grid[i] == null)
                {
                    if (remainingCell == -1)
                        remainingCell = i;
                    else
                        return null;
                }
                else
                {
                    if (grid[i].Value < min)
                        min = grid[i].Value;
                    if (grid[i].Value > max)
                        max = grid[i].Value;
                }
            }

            if (remainingCell == AffectedCells[0])
            {
                for (var v = 0; v < takens[remainingCell].Length; v++)
                    if ((Mode == MinMaxMode.Min) ? (v <= min) : (v >= max))
                        takens[remainingCell][v] = true;
            }
            else if (grid[AffectedCells[0]].Value == ((Mode == MinMaxMode.Min) ? min : max))
            {
                for (var v = 0; v < takens[remainingCell].Length; v++)
                    if ((Mode == MinMaxMode.Min) ? (v >= min) : (v <= max))
                        takens[remainingCell][v] = true;
            }

            return Enumerable.Empty<Constraint>();
        }
    }
}
