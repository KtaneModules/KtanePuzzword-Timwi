using System;

namespace Puzzword
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    sealed class LayoutAttribute : Attribute
    {
        public LayoutType Type { get; private set; }
        public LayoutAttribute(LayoutType type) { Type = type; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    sealed class LayoutTypeAttribute : Attribute
    {
        public int NumLayouts { get; private set; }
        public ScreenType ScreenType { get; set; }
        public LayoutTypeAttribute(int numLayouts) { NumLayouts = numLayouts; }
    }
}
