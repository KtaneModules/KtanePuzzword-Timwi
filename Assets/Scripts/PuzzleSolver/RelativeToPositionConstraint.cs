using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolvers
{
    abstract class RelativeToPositionConstraint : Constraint
    {
        public int Value { get; private set; }
        public int Position { get; private set; }
        public bool IsLeft { get; private set; }
        public RelativeToPositionConstraint(int value, int position, bool isLeft) : base(null) { Value = value; Position = position; IsLeft = isLeft; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            // If the required value is already present, we’re good
            if (ix != null && (IsLeft ? (ix.Value < Position) : (ix.Value > Position)) && grid[ix.Value].Value + minValue == Value)
                return Enumerable.Empty<Constraint>();

            // If there is only one unfilled cell left of the position, it needs to have this value
            var unmarkedCells = Enumerable.Range(IsLeft ? 0 : Position + 1, IsLeft ? Position : grid.Length - Position - 1).Where(i => grid[i] == null).ToArray();
            if (unmarkedCells.Length == 1)
            {
                for (var v = 0; v < takens[unmarkedCells[0]].Length; v++)
                    if (v + minValue != Value)
                        takens[unmarkedCells[0]][v] = true;
            }
            return null;
        }
    }

    sealed class LeftOfPositionConstraint : RelativeToPositionConstraint { public LeftOfPositionConstraint(int value, int position) : base(value, position, true) { } }
    sealed class RightOfPositionConstraint : RelativeToPositionConstraint { public RightOfPositionConstraint(int value, int position) : base(value, position, false) { } }
}
