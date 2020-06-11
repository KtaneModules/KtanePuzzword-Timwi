using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleSolvers;

namespace ProperPassword
{
    enum ConstraintGroup
    {
        Difference2,    // $"The absolute difference of {(char) (i + 'A')} and {(char) (j + 'A')} is {Math.Abs(solution[i] - solution[j])}."
        Quotient2,  // $"Of {(char) (i + 'A')} and {(char) (j + 'A')}, one is {solution[i] / solution[j]} times the other."
        ModuloDiff2,    // $"{(char) (i + 'A')} is a multiple of {m} away from {(char) (j + 'A')}."
        Modulo2,    // $"{(char) (i + 'A')} modulo {(char) (j + 'A')} is {solution[i] % solution[j]}."
        Sum2,    // $"The sum of {(char) (i + 'A')} and {(char) (j + 'A')} is {solution[i] + solution[j]}."
        Product2,    // $"The product of {(char) (i + 'A')} and {(char) (j + 'A')} is {solution[i] * solution[j]}."
        Between2,     // $"{k} is between {(char) (i + 'A')} and {(char) (j + 'A')}."
        LessThan,    // $"{(char) (i + 'A')} is less than {(char) (j + 'A')}."
        ConcatenationDivisible,      // $"The concatenation of {(char) (i + 'A')}{(char) (j + 'A')} is divisible by {m}."
        ConcatenationNotDivisible,   // $"The concatenation of {(char) (i + 'A')}{(char) (j + 'A')} is not divisible by {m}."
        Sum3,        // $"{(char) (i + 'A')} + {(char) (j + 'A')} = {(char) (k + 'A')}"
        Product3,        // $"{(char) (i + 'A')} × {(char) (j + 'A')} = {(char) (k + 'A')}"
        Modulo3,        // $"{(char) (i + 'A')} modulo {(char) (j + 'A')} = {(char) (k + 'A')}"
        LessThanConstant,        // $"{(char) (i + 'A')} is less than {v}."
        GreaterThanConstant,        // $"{(char) (i + 'A')} is greater than {v}."
        Smallest,       // $"{(char) (i + 'A')} has the smallest value."
        Largest,        // $"{(char) (i + 'A')} has the largest value."
        NotSmallest,    // $"{(char) (i + 'A')} does not have the smallest value."
        NotLargest,     // $"{(char) (i + 'A')} does not have the largest value."
        LeftOfPosition,     // $"There is a {solution[i]} further left than {(char) (j + 'A')}."
        RightOfPosition,    // $"There is a {solution[i]} further right than {(char) (j + 'A')}."
        Prime,          // $"{(char) (i + 'A')} is a prime number."
        NotPrime,       // $"{(char) (i + 'A')} is not a prime number."
        Square,         // $"{(char) (i + 'A')} is a square number."
        NotSquare,      // $"{(char) (i + 'A')} is not a square number."
        NotDivisible,   // $"{(char) (i + 'A')} is not divisible by {m}."
        Divisible,          // $"{(char) (i + 'A')} is divisible by {m}."
        NotPresent,     // $"There is no {v}."
        Between,        // $"There is a value between {low} and {high}."
        Outside,            // $"There is a value outside of {low} to {high}."
        HasXor,         // $"There is a {v1} or a {v2}, but not both."
        HasXnor,        // $"There is a {v1} and a {v2}, or neither."
    }

    struct PConstraint
    {
        public Constraint Constraint { get; private set; }
        public ConstraintGroup Group { get; private set; }
        public PConstraint(ConstraintGroup group, Constraint constraint)
        {
            Constraint = constraint;
            Group = group;
        }

        public static PConstraint Sum2(int i, int j, int sum) { return new PConstraint(ConstraintGroup.Sum2, new TwoCellLambdaConstraint(i, j, (a, b) => a + b == sum)); }
        public static PConstraint Product2(int i, int j, int product) { return new PConstraint(ConstraintGroup.Product2, new TwoCellLambdaConstraint(i, j, (a, b) => a * b == product)); }
        public static PConstraint Difference2(int i, int j, int diff) { return new PConstraint(ConstraintGroup.Difference2, new TwoCellLambdaConstraint(i, j, (a, b) => Math.Abs(a - b) == diff)); }
        public static PConstraint Quotient2(int i, int j, int quotient) { return new PConstraint(ConstraintGroup.Quotient2, new TwoCellLambdaConstraint(i, j, (a, b) => a * quotient == b || b * quotient == a)); }
        public static PConstraint ModuloDiff2(int i, int j, int modulo) { return new PConstraint(ConstraintGroup.ModuloDiff2, new TwoCellLambdaConstraint(i, j, (a, b) => a % modulo == b % modulo)); }
        public static PConstraint Modulo2(int i, int j, int modulo) { return new PConstraint(ConstraintGroup.Modulo2, new TwoCellLambdaConstraint(i, j, (a, b) => b != 0 && a % b == modulo)); }
        public static PConstraint Between2(int i, int j, int value) { return new PConstraint(ConstraintGroup.Between2, new TwoCellLambdaConstraint(i, j, (a, b) => (a < value && value < b) || (b < value && value < a))); }
        internal static PConstraint LessThan(int i, int j) { return new PConstraint(ConstraintGroup.LessThan, new TwoCellLambdaConstraint(i, j, (a, b) => a < b)); }
        internal static PConstraint ConcatenationDivisible(int i, int j, int m) { return new PConstraint(ConstraintGroup.ConcatenationDivisible, new TwoCellLambdaConstraint(i, j, (a, b) => int.Parse(a.ToString() + b.ToString()) % m == 0)); }
        internal static PConstraint ConcatenationNotDivisible(int i, int j, int m) { return new PConstraint(ConstraintGroup.ConcatenationNotDivisible, new TwoCellLambdaConstraint(i, j, (a, b) => int.Parse(a.ToString() + b.ToString()) % m != 0)); }
        internal static PConstraint Sum3(int i, int j, int k) { return new PConstraint(ConstraintGroup.Sum3, new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a + b == c)); }
        internal static PConstraint Product3(int i, int j, int k) { return new PConstraint(ConstraintGroup.Product3, new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a * b == c)); }
        internal static PConstraint Modulo3(int i, int j, int k) { return new PConstraint(ConstraintGroup.Modulo3, new ThreeCellLambdaConstraint(i, j, k, (a, b, c) => a % b == c)); }
        internal static PConstraint LessThanConstant(int i, int v) { return new PConstraint(ConstraintGroup.LessThanConstant, new OneCellLambdaConstraint(i, a => a < v)); }
        internal static PConstraint GreaterThanConstant(int i, int v) { return new PConstraint(ConstraintGroup.GreaterThanConstant, new OneCellLambdaConstraint(i, a => a > v)); }
        internal static PConstraint Smallest(int i) { return new PConstraint(ConstraintGroup.Smallest, new MinMaxConstraint(i, MinMaxMode.Min)); }
        internal static PConstraint Largest(int i) { return new PConstraint(ConstraintGroup.Largest, new MinMaxConstraint(i, MinMaxMode.Max)); }
        internal static PConstraint NotSmallest(int i) { return new PConstraint(ConstraintGroup.NotSmallest, new NotMinMaxConstraint(i, MinMaxMode.Min)); }
        internal static PConstraint NotLargest(int i) { return new PConstraint(ConstraintGroup.NotLargest, new NotMinMaxConstraint(i, MinMaxMode.Max)); }
        internal static PConstraint LeftOfPosition(int v, int i) { return new PConstraint(ConstraintGroup.LeftOfPosition, new LeftOfPositionConstraint(v, i)); }
        internal static PConstraint RightOfPosition(int v, int i) { return new PConstraint(ConstraintGroup.RightOfPosition, new RightOfPositionConstraint(v, i)); }
        internal static PConstraint Prime(int i) { return new PConstraint(ConstraintGroup.Prime, new OneCellLambdaConstraint(i, a => Data.Primes.Contains(a))); }
        internal static PConstraint NotPrime(int i) { return new PConstraint(ConstraintGroup.NotPrime, new OneCellLambdaConstraint(i, a => !Data.Primes.Contains(a))); }
        internal static PConstraint Square(int i) { return new PConstraint(ConstraintGroup.Square, new OneCellLambdaConstraint(i, a => Data.Squares.Contains(a))); }
        internal static PConstraint Divisible(int i, int m) { return new PConstraint(ConstraintGroup.Divisible, new OneCellLambdaConstraint(i, a => a % m != 0)); }
        internal static PConstraint NotDivisible(int i, int m) { return new PConstraint(ConstraintGroup.NotDivisible, new OneCellLambdaConstraint(i, a => a % m == 0)); }
        internal static PConstraint NotSquare(int i) { return new PConstraint(ConstraintGroup.NotSquare, new OneCellLambdaConstraint(i, a => !Data.Squares.Contains(a))); }
        internal static PConstraint NotPresent(int v) { return new PConstraint(ConstraintGroup.NotPresent, new NotPresentConstraint(v)); }
        internal static PConstraint Between(int low, int high) { return new PConstraint(ConstraintGroup.Between, new BetweenConstraint(low, high, reversed: false)); }
        internal static PConstraint Outside(int low, int high) { return new PConstraint(ConstraintGroup.Outside, new BetweenConstraint(low, high, reversed: true)); }
        internal static PConstraint HasXor(int v1, int v2) { return new PConstraint(ConstraintGroup.HasXor, new HasXorConstraint(v1, v2)); }
        internal static PConstraint HasXnor(int v1, int v2) { return new PConstraint(ConstraintGroup.HasXnor, new HasXnorConstraint(v1, v2)); }
    }
}