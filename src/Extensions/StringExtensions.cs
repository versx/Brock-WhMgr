namespace WhMgr.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HandlebarsDotNet;

    using WhMgr.Diagnostics;

    public static class StringExtensions
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("STRING_EXTENSIONS", Program.LogLevel);

        /// <summary>
        /// Format text with provided anonymous object modal for
        /// Handlebars.Net templating engine to parse
        /// </summary>
        /// <param name="text"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string FormatText(this string text, dynamic args)
        {
            var template = Handlebars.Compile(text);
            return template(args);
        }

        public static IEnumerable<string> SplitInParts(this string s, int partLength)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));

            if (partLength <= 0)
                throw new ArgumentException("Part length must be a positive number.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
            {
                yield return s.Substring(i, Math.Min(partLength, s.Length));
            }
        }

        public static List<string> RemoveSpaces(this string value)
        {
            return value.Replace(", ", ",")
                        .Replace(" ,", ",")
                        .Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
        }
    }
}