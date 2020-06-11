using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    sealed class HasXorConstraint : Constraint
    {
        public int Value1 { get; private set; }
        public int Value2 { get; private set; }
        public HasXorConstraint(int value1, int value2) : base(null) { Value1 = value1; Value2 = value2; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                return null;
            if (grid[ix.Value].Value + minValue == Value1)
            {
                for (var i = 0; i < takens.Length; i++)
                    takens[i][Value2 - minValue] = true;
                return Enumerable.Empty<Constraint>();
            }
            if (grid[ix.Value].Value + minValue == Value2)
            {
                for (var i = 0; i < takens.Length; i++)
                    takens[i][Value1 - minValue] = true;
                return Enumerable.Empty<Constraint>();
            }
            int remainingCell = -1;
            for (var i = 0; i < grid.Length; i++)
                if (grid[i] == null)
                {
                    if (remainingCell == -1)
                        remainingCell = i;
                    else
                        return null;
                }
            for (var v = 0; v < takens[remainingCell].Length; v++)
                if (v + minValue != Value1 && v + minValue != Value2)
                    takens[remainingCell][v] = true;
            return Enumerable.Empty<Constraint>();
        }
    }
}
