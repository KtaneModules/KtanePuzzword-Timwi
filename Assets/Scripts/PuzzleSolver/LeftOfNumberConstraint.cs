using System.Collections.Generic;

namespace PuzzleSolvers
{
    sealed class LeftOfNumberConstraint : Constraint
    {
        public int Value1 { get; private set; }
        public int Value2 { get; private set; }
        public LeftOfNumberConstraint(int v1, int v2) : base(null) { Value1 = v1; Value2 = v2; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                return null;
            if (grid[ix.Value].Value == Value1)
                for (var i = 0; i < ix.Value; i++)
                    takens[i][Value2 - minValue] = true;
            if (grid[ix.Value].Value == Value2)
                for (var i = ix.Value + 1; i < grid.Length; i++)
                    takens[i][Value1 - minValue] = true;
            return null;
        }
    }
}
