using System.Collections.Generic;

namespace PuzzleSolvers
{
    sealed class NotPresentConstraint : Constraint
    {
        public int Value { get; private set; }
        public NotPresentConstraint(int value) : base(null) { Value = value; }
        public override IEnumerable<Constraint> MarkTakens(bool[][] takens, int?[] grid, int? ix, int minValue, int maxValue)
        {
            if (ix == null)
                foreach (var arr in takens)
                    arr[Value - minValue] = true;
            return null;
        }
    }
}
