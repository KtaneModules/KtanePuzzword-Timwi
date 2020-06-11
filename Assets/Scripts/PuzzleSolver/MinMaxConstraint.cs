using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    sealed class MinMaxConstraint : Constraint
    {
        public MinMaxMode Mode { get; private set; }
        public MinMaxConstraint(int cell, MinMaxMode mode) : base(new[] { cell }) { Mode = mode; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                return null;

            if (ix.Value == AffectedCells[0])
            {
                // Cell affected by the constraint is decided: everything else can’t be smaller/greater
                for (var i = 0; i < grid.Length; i++)
                    if (grid[i] == null)
                        for (var v = 0; v < takens[i].Length; v++)
                            if ((Mode == MinMaxMode.Min) ? (v < grid[AffectedCells[0]].Value) : (v > grid[AffectedCells[0]].Value))
                                takens[i][v] = true;
                return Enumerable.Empty<Constraint>();
            }
            else
            {
                // Another cell was filled in: the affected cell can’t be greater/smaller
                var value = grid[ix.Value].Value;
                for (var v = 0; v < takens[AffectedCells[0]].Length; v++)
                    if ((Mode == MinMaxMode.Min) ? (v > value) : (v < value))
                        takens[AffectedCells[0]][v] = true;
                return null;
            }
        }
    }
}
