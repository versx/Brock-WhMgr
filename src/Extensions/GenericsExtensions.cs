namespace WhMgr.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class GenericsExtensions
    {
        /// <summary>
        /// Converts a string to its object representation.
        /// </summary>
        /// <typeparam name="T">The type of object to convert the string to.</typeparam>
        /// <param name="value">The actual string value.</param>
        /// <returns>Returns an object relating to the converted string.</returns>
        public static T StringToObject<T>(this string value)
        {
            //TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
            //return (T)tc.ConvertFromString(value);
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StringToObject: {ex}");
                return default;
            }
        }

        /// <summary>
        /// Converts a string to its object representation.
        /// </summary>
        /// <param name="value">The actual string value.</param>
        /// <param name="obj">The object type to convert the string to.</param>
        /// <returns>Returns an object relating to the converted string.</returns>
        public static object StringToObject(this string value, object obj)
        {
            return Enum.Parse(obj.GetType(), value, true);
        }

        /// <summary>
        /// Converts an object to its string representation.
        /// </summary>
        /// <typeparam name="T">The type of object to convert.</typeparam>
        /// <param name="value">The actual object value.</param>
        /// <returns>Returns the string representation of the converted object.</returns>
        public static string ObjectToString<T>(this T value)
        {
            //TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
            //return tc.ConvertToString(value);
            try
            {
                return Enum.GetName(typeof(T), value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ObjectToString: {ex}");
                return value.ToString();
            }
        }

        public static bool UnorderedEquals<T>(this IList<T> list1, IList<T> list2)
        {
            if (list1 == null && list2 == null)
                return true;

            if (list1 == null || list2 == null)
                return false;

            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(list1[i], list2[i]))
                    return false;
            }

            return true;
        }

        public static bool ScrambledEquals<T>(this IEnumerable<T> list1, IEnumerable<T> list2, IEqualityComparer<T> comparer)
        {
            var cnt = new Dictionary<T, int>(comparer);
            foreach (T s in list1)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                    continue;
                }

                cnt.Add(s, 1);
            }
            foreach (T s in list2)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                    continue;
                }

                return false;
            }
            return cnt.Values.All(c => c == 0);
        }

        public static string ToHumanReadableString(this Type type)
        {
            if (type == typeof(string))
                return "Text";
            else if (type == typeof(int))
                return "Number";
            else if (type == typeof(double) ||
                     type == typeof(float))
                return "Decimal";
            else if (type == typeof(bool))
                return "Boolean";
            return type.Name.ToString();
        }

        public static bool Intersects<T>(this List<T> list1, List<T> list2)
        {
            if (list1 is null)
            {
                throw new ArgumentNullException(nameof(list1));
            }
            if (list2 is null)
            {
                throw new ArgumentNullException(nameof(list2));
            }
            foreach (var item in list1)
            {
                if (list2.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}