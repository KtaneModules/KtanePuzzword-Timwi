using System;
using System.Linq;
using PuzzleSolvers;

namespace Puzzword
{
    struct Clue
    {
        public int[] Values { get; private set; }
        public Constraint Constraint { get; private set; }
        public ClueType Type { get; private set; }
        public Clue(int[] values, ClueType type, Constraint constraint)
        {
            Values = values ?? new int[0];
            Constraint = constraint;
            Type = type;
        }

        public static Clue Between(int low, int high) { return new Clue(new[] { low, high }, ClueType.Between, new BetweenConstraint(low, high, reversed: false)); }
        public static Clue Between2(int i, int j, int value) { return new Clue(new[] { i, j, value }, ClueType.Between2, new TwoCellLambdaConstraint(i, j, (a, b) => (a < value && value < b) || (b < value && value < a))); }
        public static Clue ConcatenationDivisible(int i, int j, int m) { return new Clue(new[] { i, j, m }, ClueType.ConcatenationDivisible, new TwoCellLambdaConstraint(i, j, (a, b) => int.Parse(a.ToString() + b.ToString()) % m == 0)); }
        public static Clue ConcatenationNotDivisible(int i, int j, int m) { return new Clue(new[] { i, j, m }, ClueType.ConcatenationNotDivisible, new TwoCellLambdaConstraint(i, j, (a, b) => int.Parse(a.ToString() + b.ToString()) % m != 0)); }
        public static Clue Difference2(int i, int j, int diff) { return new Clue(new[] { i, j, diff }, ClueType.Difference2, new TwoCellLambdaConstraint(i, j, (a, b) => Math.Abs(a - b) == diff)); }
        public static Clue Divisible(int i, int m) { return new Clue(new[] { i, m }, ClueType.Divisible, new OneCellLambdaConstraint(i, a => a % m == 0)); }
        public static Clue GreaterThanConstant(int i, int v) { return new Clue(new[] { i, v }, ClueType.GreaterThanConstant, new OneCellLambdaConstraint(i, a => a > v)); }
        public static Clue HasSum(int sum) { return new Clue(new[] { sum }, ClueType.HasSum, new HasSumConstraint(sum)); }
        public static Clue HasXnor(int v1, int v2) { return new Clue(new[] { v1, v2 }, ClueType.HasXnor, new HasXnorConstraint(v1, v2)); }
        public static Clue HasXor(int v1, int v2) { return new Clue(new[] { v1, v2 }, ClueType.HasXor, new HasXorConstraint(v1, v2)); }
        public static Clue Largest(int i) { return new Clue(new[] { i }, ClueType.Largest, new MinMaxConstraint(i, MinMaxMode.Max)); }
        public static Clue LeftOfPosition(int i, int v) { return new Clue(new[] { i, v }, ClueType.LeftOfPosition, new LeftOfPositionConstraint(v, i)); }
        public static Clue LessThan(int i, int j) { return new Clue(new[] { j, i }, ClueType.LessThan, new TwoCellLambdaConstraint(i, j, (a, b) => a < b)); }
        public static Clue LessThanConstant(int i, int v) { return new Clue(new[] { i, v }, ClueType.LessThanConstant, new OneCellLambdaConstraint(i, a => a < v)); }
        public static Clue Modulo2(int i, int j, int modulo) { return new Clue(new[] { i, j, modulo }, ClueType.Modulo2, new TwoCellLambdaConstraint(i, j, (a, b) => b != 0 && a % b == modulo)); }
        public static Clue Modulo3(int i, int j, int k) { return new Clue(new[] { i, j, k }, ClueType.Modulo3, new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a % b == c)); }
        public static Clue ModuloDiff2(int i, int j, int modulo) { return new Clue(new[] { i, j, modulo }, ClueType.ModuloDiff2, new TwoCellLambdaConstraint(i, j, (a, b) => a % modulo == b % modulo)); }
        public static Clue NotDivisible(int i, int m) { return new Clue(new[] { i, m }, ClueType.NotDivisible, new OneCellLambdaConstraint(i, a => a % m != 0)); }
        public static Clue NotLargest(int i) { return new Clue(new[] { i }, ClueType.NotLargest, new NotMinMaxConstraint(i, MinMaxMode.Max)); }
        public static Clue NotPresent(int v) { return new Clue(new[] { v }, ClueType.NotPresent, new NotPresentConstraint(v)); }
        public static Clue NotPrime(int i) { return new Clue(new[] { i }, ClueType.NotPrime, new OneCellLambdaConstraint(i, a => !Data.Primes.Contains(a))); }
        public static Clue NotSmallest(int i) { return new Clue(new[] { i }, ClueType.NotSmallest, new NotMinMaxConstraint(i, MinMaxMode.Min)); }
        public static Clue NotSquare(int i) { return new Clue(new[] { i }, ClueType.NotSquare, new OneCellLambdaConstraint(i, a => !Data.Squares.Contains(a))); }
        public static Clue Outside(int low, int high) { return new Clue(new[] { low, high }, ClueType.Outside, new BetweenConstraint(low, high, reversed: true)); }
        public static Clue Prime(int i) { return new Clue(new[] { i }, ClueType.Prime, new OneCellLambdaConstraint(i, a => Data.Primes.Contains(a))); }
        public static Clue Product2(int i, int j, int product) { return new Clue(new[] { i, j, product }, ClueType.Product2, new TwoCellLambdaConstraint(i, j, (a, b) => a * b == product)); }
        public static Clue Product3(int i, int j, int k) { return new Clue(new[] { i, j, k }, ClueType.Product3, new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a * b == c)); }
        public static Clue Quotient2(int i, int j, int quotient) { return new Clue(new[] { i, j, quotient }, ClueType.Quotient2, new TwoCellLambdaConstraint(i, j, (a, b) => a * quotient == b || b * quotient == a)); }
        public static Clue RightOfPosition(int i, int v) { return new Clue(new[] { i, v }, ClueType.RightOfPosition, new RightOfPositionConstraint(v, i)); }
        public static Clue Smallest(int i) { return new Clue(new[] { i }, ClueType.Smallest, new MinMaxConstraint(i, MinMaxMode.Min)); }
        public static Clue Square(int i) { return new Clue(new[] { i }, ClueType.Square, new OneCellLambdaConstraint(i, a => Data.Squares.Contains(a))); }
        public static Clue Sum2(int i, int j, int sum) { return new Clue(new[] { i, j, sum }, ClueType.Sum2, new TwoCellLambdaConstraint(i, j, (a, b) => a + b == sum)); }
        public static Clue Sum3(int i, int j, int k) { return new Clue(new[] { i, j, k }, ClueType.Sum3, new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a + b == c)); }
    }
}