using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Puzzword
{
    static class ExtensionMethods
    {
        /// <summary>
        ///     Brings the elements of the given list into a random order.</summary>
        /// <typeparam name="T">
        ///     Type of elements in the list.</typeparam>
        /// <param name="list">
        ///     List to shuffle.</param>
        /// <returns>
        ///     The list operated on.</returns>
        public static T Shuffle<T>(this T list, Random rnd) where T : IList
        {
            if (list == null)
                throw new ArgumentNullException("list");
            for (int j = list.Count; j >= 1; j--)
            {
                int item = rnd.Next(0, j);
                if (item < j - 1)
                {
                    var t = list[item];
                    list[item] = list[j - 1];
                    list[j - 1] = t;
                }
            }
            return list;
        }

        private static readonly Dictionary<LayoutType, LayoutTypeAttribute> _layoutTypeInfo = new Dictionary<LayoutType, LayoutTypeAttribute>();
        private static readonly Dictionary<ClueType, LayoutAttribute> _clueTypeInfo = new Dictionary<ClueType, LayoutAttribute>();

        static ExtensionMethods()
        {
            foreach (var f in typeof(LayoutType).GetFields(BindingFlags.Public | BindingFlags.Static))
                foreach (var attr in f.GetCustomAttributes(typeof(LayoutTypeAttribute), inherit: false))
                    _layoutTypeInfo[(LayoutType) f.GetValue(null)] = (LayoutTypeAttribute) attr;
            foreach (var f in typeof(ClueType).GetFields(BindingFlags.Public | BindingFlags.Static))
                foreach (var attr in f.GetCustomAttributes(typeof(LayoutAttribute), inherit: false))
                    _clueTypeInfo[(ClueType) f.GetValue(null)] = (LayoutAttribute) attr;
        }

        public static int GetNumLayouts(this LayoutType type) { return _layoutTypeInfo[type].NumLayouts; }
        public static ScreenType GetScreenType(this LayoutType type) { return _layoutTypeInfo[type].ScreenType; }
        public static LayoutType GetLayoutType(this ClueType type) { return _clueTypeInfo[type].Type; }
        public static ScreenType GetScreenType(this ClueType type) { return GetScreenType(GetLayoutType(type)); }
        public static LayoutType GetLayoutType(this Clue clue) { return _clueTypeInfo[clue.Type].Type; }
        public static ScreenType GetScreenType(this Clue clue) { return GetScreenType(GetLayoutType(clue.Type)); }
    }
}
