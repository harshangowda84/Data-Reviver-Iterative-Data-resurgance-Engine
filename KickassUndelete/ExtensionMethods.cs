using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KickassUndelete {
    /// <summary>
    /// A static class of useful extension methods.
    /// </summary>
    static class ExtensionMethods {
        /// <summary>
        /// Retrieve a range of items from a generic IList.
        /// </summary>
        public static IList<T> GetRange<T>(this IList<T> list, int startIndex, int length) {
            return list.Where((item, index) => index >= startIndex && index < startIndex + length).ToList();
        }
    }
}
