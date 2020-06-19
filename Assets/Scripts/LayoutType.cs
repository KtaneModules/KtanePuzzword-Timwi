namespace Puzzword
{
    enum LayoutType
    {
        // Narrow screens
        [LayoutType(2)] // horiz/vert; possibly 4 if we want to allow diagonal
        _1Constant,
        [LayoutType(4)] // horiz/vert each with smallest or largest first; possibly more if we want to allow diagonal
        _2UConstants,
        [LayoutType(10)] // we have 5 subsymbols each filled or non-filled
        _1Symbol_1Subsymbol,
        [LayoutType(6)] // above, below, left, right, inside horiz, inside vert
        _1Symbol_1Constant,
        [LayoutType(1)] // small symbol inside large
        _2OSymbols,
        [LayoutType(6)] // symbols superimposed, constant above/below/left/right/inside horiz/vert
        _2USymbols_1Constant,
        [LayoutType(4)] // small symbol inside large, constant above/below/left/right
        _2OSymbols_1Constant,

        // Wide screen
        [LayoutType(2, ScreenType = ScreenType.Wide)] // 2 symbols superimposed, another left/right of that
        _2USymbols_1Symbol,
        [LayoutType(2, ScreenType = ScreenType.Wide)] // small symbol inside large, third symbol left/right of that
        _3OSymbols
    }
}
