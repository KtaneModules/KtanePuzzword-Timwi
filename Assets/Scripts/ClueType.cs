namespace Puzzword
{
    enum ClueType
    {
        [Layout(LayoutType._1Constant)]
        NotPresent,
        [Layout(LayoutType._1Constant)]
        HasSum,

        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Smallest,
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Largest,
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotSmallest,
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotLargest,
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Prime,
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotPrime,
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        Square,
        [Layout(LayoutType._1Symbol_1Subsymbol)]
        NotSquare,

        [Layout(LayoutType._1Symbol_1Constant)]
        LessThanConstant,
        [Layout(LayoutType._1Symbol_1Constant)]
        GreaterThanConstant,
        [Layout(LayoutType._1Symbol_1Constant)]
        LeftOfPosition,
        [Layout(LayoutType._1Symbol_1Constant)]
        RightOfPosition,
        [Layout(LayoutType._1Symbol_1Constant)]
        Divisible,
        [Layout(LayoutType._1Symbol_1Constant)]
        NotDivisible,

        [Layout(LayoutType._2USymbols)]
        Different,

        [Layout(LayoutType._2USymbols_1Constant)]
        Difference2,
        [Layout(LayoutType._2USymbols_1Constant)]
        Quotient2,
        [Layout(LayoutType._2USymbols_1Constant)]
        ModuloDiff2,
        [Layout(LayoutType._2USymbols_1Constant)]
        Sum2,
        [Layout(LayoutType._2USymbols_1Constant)]
        Product2,
        [Layout(LayoutType._2USymbols_1Constant)]
        Between2,

        [Layout(LayoutType._2OSymbols)]
        LessThan,

        [Layout(LayoutType._2OSymbols_1Constant)]
        ConcatenationDivisible,
        [Layout(LayoutType._2OSymbols_1Constant)]
        Modulo2,
        [Layout(LayoutType._2OSymbols_1Constant)]
        ConcatenationNotDivisible,
        [Layout(LayoutType._2OSymbols_1Constant)]
        Modulo2Not,

        [Layout(LayoutType._2UConstants)]
        Between,
        [Layout(LayoutType._2UConstants)]
        Outside,
        [Layout(LayoutType._2UConstants)]
        HasXor,
        [Layout(LayoutType._2UConstants)]
        HasXnor,

        // Wide screen
        [Layout(LayoutType._2USymbols_1Symbol)]
        Sum3,
        [Layout(LayoutType._2USymbols_1Symbol)]
        Product3,
        [Layout(LayoutType._3OSymbols)]
        Modulo3,
        [Layout(LayoutType._3OSymbols)]
        ConcatenationDivisible3
    }
}