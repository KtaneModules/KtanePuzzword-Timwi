using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    // Unused — too slow
    sealed class HasXnorConstraint : Constraint
    {
        public int Value1 { get; private set; }
        public int Value2 { get; private set; }
        public HasXnorConstraint(int value1, int value2) : base(null) { Value1 = value1; Value2 = value2; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                return null;
            bool found1 = false, found2 = false, possible1 = false, possible2 = false;
            int remainingCellCount = 0, remainingCell = -1;
            for (var i = 0; i < grid.Length; i++)
            {
                if (grid[i] == null)
                {
                    if (!takens[i][Value1 - minValue])
                        possible1 = true;
                    if (!takens[i][Value2 - minValue])
                        possible2 = true;
                    remainingCellCount++;
                    remainingCell = i;
                }
                else
                {
                    if (grid[i].Value + minValue == Value1)
                        possible1 = found1 = true;
                    if (grid[i].Value + minValue == Value2)
                        possible2 = found2 = true;
                    if (found1 && found2)
                        return Enumerable.Empty<Constraint>();
                }
            }
            possible1 |= found1;
            possible2 |= found2;
            if (remainingCellCount == 1)
            {
                for (var v = 0; v < takens[remainingCell].Length; v++)
                    if ((found1 && v + minValue != Value2) || (found2 && v + minValue != Value1) || (!found1 && !found2 && (v + minValue == Value1 || v + minValue == Value2)))
                        takens[remainingCell][v] = true;
                return Enumerable.Empty<Constraint>();
            }
            if (!possible1 && !possible2)
                return Enumerable.Empty<Constraint>();
            if (!possible1 || !possible2)
                for (var i = 0; i < grid.Length; i++)
                    for (var v = 0; v < takens[i].Length; v++)
                        if ((!possible1 && v + minValue == Value2) || (!possible2 && v + minValue == Value1))
                            takens[i][v] = true;
            return null;
        }
    }
}
