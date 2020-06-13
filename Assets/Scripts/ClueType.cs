namespace ProperPassword
{
    enum ClueType
    {
        [Layout(LayoutType._1Constant)]
        NotPresent,     // $"There is no {v}."
        [Layout(LayoutType._1Constant)]
        HasSum,     // $"There are two values that add up to {solution[i] + solution[j]}."

        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Smallest,       // $"{(char) (i + 'A')} has the smallest value."
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Largest,        // $"{(char) (i + 'A')} has the largest value."
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotSmallest,    // $"{(char) (i + 'A')} does not have the smallest value."
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotLargest,     // $"{(char) (i + 'A')} does not have the largest value."
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Prime,          // $"{(char) (i + 'A')} is a prime number."
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotPrime,       // $"{(char) (i + 'A')} is not a prime number."
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Square,         // $"{(char) (i + 'A')} is a square number."
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotSquare,      // $"{(char) (i + 'A')} is not a square number."

        [Layout(LayoutType._1Symbol_1Constant)]
        LessThanConstant,        // $"{(char) (i + 'A')} is less than {v}."
        [Layout(LayoutType._1Symbol_1Constant)]
        GreaterThanConstant,        // $"{(char) (i + 'A')} is greater than {v}."
        [Layout(LayoutType._1Symbol_1Constant)]
        LeftOfPosition,     // $"There is a {solution[i]} further left than {(char) (j + 'A')}."
        [Layout(LayoutType._1Symbol_1Constant)]
        RightOfPosition,    // $"There is a {solution[i]} further right than {(char) (j + 'A')}."
        [Layout(LayoutType._1Symbol_1Constant)]
        Divisible,          // $"{(char) (i + 'A')} is divisible by {m}."
        [Layout(LayoutType._1Symbol_1Constant)]
        NotDivisible,   // $"{(char) (i + 'A')} is not divisible by {m}."

        [Layout(LayoutType._2USymbols_1Constant)]
        Difference2,    // $"The absolute difference of {(char) (i + 'A')} and {(char) (j + 'A')} is {Math.Abs(solution[i] - solution[j])}."
        [Layout(LayoutType._2USymbols_1Constant)]
        Quotient2,  // $"Of {(char) (i + 'A')} and {(char) (j + 'A')}, one is {solution[i] / solution[j]} times the other."
        [Layout(LayoutType._2USymbols_1Constant)]
        ModuloDiff2,    // $"{(char) (i + 'A')} is a multiple of {m} away from {(char) (j + 'A')}."
        [Layout(LayoutType._2USymbols_1Constant)]
        Sum2,    // $"The sum of {(char) (i + 'A')} and {(char) (j + 'A')} is {solution[i] + solution[j]}."
        [Layout(LayoutType._2USymbols_1Constant)]
        Product2,    // $"The product of {(char) (i + 'A')} and {(char) (j + 'A')} is {solution[i] * solution[j]}."
        [Layout(LayoutType._2USymbols_1Constant)]
        Between2,     // $"{k} is between {(char) (i + 'A')} and {(char) (j + 'A')}."

        [Layout(LayoutType._2OSymbols)]
        LessThan,    // $"{(char) (i + 'A')} is less than {(char) (j + 'A')}."

        [Layout(LayoutType._2OSymbols_1Constant)]
        Modulo2,    // $"{(char) (i + 'A')} modulo {(char) (j + 'A')} is {solution[i] % solution[j]}."
        [Layout(LayoutType._2OSymbols_1Constant)]
        ConcatenationDivisible,      // $"The concatenation of {(char) (i + 'A')}{(char) (j + 'A')} is divisible by {m}."
        [Layout(LayoutType._2OSymbols_1Constant)]
        ConcatenationNotDivisible,   // $"The concatenation of {(char) (i + 'A')}{(char) (j + 'A')} is not divisible by {m}."

        [Layout(LayoutType._2UConstants)]
        Between,        // $"There is a value between {low} and {high}."
        [Layout(LayoutType._2UConstants)]
        Outside,            // $"There is a value outside of {low} to {high}."
        [Layout(LayoutType._2UConstants)]
        HasXor,         // $"There is a {v1} or a {v2}, but not both."
        [Layout(LayoutType._2UConstants)]
        HasXnor,        // $"There is a {v1} and a {v2}, or neither."

        // Wide screen
        [Layout(LayoutType._2USymbols_1Symbol)]
        Sum3,        // $"{(char) (i + 'A')} + {(char) (j + 'A')} = {(char) (k + 'A')}"
        [Layout(LayoutType._2USymbols_1Symbol)]
        Product3,        // $"{(char) (i + 'A')} × {(char) (j + 'A')} = {(char) (k + 'A')}"
        [Layout(LayoutType._3OSymbols)]
        Modulo3,        // $"{(char) (i + 'A')} modulo {(char) (j + 'A')} = {(char) (k + 'A')}"
    }
}