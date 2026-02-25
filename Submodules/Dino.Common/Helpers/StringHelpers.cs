using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dino.Common.Helpers
{
    public static class StringHelpers
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return String.IsNullOrEmpty(str);
        }

        public static bool IsNotNullOrEmpty(this string str)
        {
            return !String.IsNullOrEmpty(str);
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return String.IsNullOrWhiteSpace(str);
        }

        public static bool IsNotNullOrWhiteSpace(this string str)
        {
            return !String.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Converts the string to byte-array, and returns it.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The string as byte-array.</returns>
        public static byte[] ToByteArray(this string str)
        {
            return (Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Converts a string to DateTime safely.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The parsed datetime.</returns>
        public static DateTime? ToSafeDateTime(this string str)
        {
            DateTime? dateTime = null;

            DateTime tmp;
            if (DateTime.TryParse(str, out tmp))
            {
                dateTime = tmp;
            }

            return dateTime;
        }

        /// <summary>
        /// Removes the numbers from a string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The string without any numbers.</returns>
        public static string RemoveNumbers(this string str)
        {
            var regex = new Regex("[0-9]");
            return (regex.Replace(str, String.Empty));
        }

        /// <summary>
        /// Removes all non numeric chars from a string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The string with only numbers.</returns>
        public static string RemoveNonNumeric(this string str)
        {
            var regex = new Regex(@"[^\d]");
            return (regex.Replace(str, String.Empty));
        }

        /// <summary>
        /// Masks a string with the specific character ignoring the last X characters.
        /// </summary>
        /// <param name="str">The string to mask.</param>
        /// <param name="showFromEnd">The number of characters from the end of the string to leave untouched.</param>
        /// <param name="maskWith">THe character to mask the string with.</param>
        /// <returns>The masked string.</returns>
        public static string Mask(this string str, int showFromEnd, char maskWith)
        {
            var newStr = new StringBuilder(str);

            for (var i = 0; i < (str.Length - showFromEnd); i++)
            {
                newStr[i] = maskWith;
            }

            return newStr.ToString();
        }

        public static string SafeClone(this string str)
        {
            string newStr = null;

            if (str != null)
            {
                newStr = (string)str.Clone();
            }

            return newStr;
        }

        public static string GetLast(this string source, int tail_length)
        {
            if (tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length);
        }

        /// <summary>
        /// Converts the string to camel case (First letter is lower and no spaces)
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <returns>The converted string.</returns>
        public static string ToCamelCase(this string source)
        {
            source = source.Replace(" ", "");

            return Char.ToLowerInvariant(source[0]) + source.Substring(1);
        }

        /// <summary>
        /// Converts the string from camel case to regular (First letter is upper and no spaces)
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <returns>The converted string.</returns>
        public static string FromCamelCase(this string source)
        {
            source = source.Replace(" ", "");

            return Char.ToUpperInvariant(source[0]) + source.Substring(1);
        }

        /// <summary>
        /// Gets a part of the string if its too long, and adds three dots at the end (...).
        /// </summary>
        /// <param name="source">The string.</param>
        /// <returns>The result string.</returns>
	    public static string ShortangeString(this string source, int maxCharacters, string charactersAtEndIfTooLong = "...")
        {
            return source.Length <= maxCharacters
                ? source
                : source.Substring(0, maxCharacters) + charactersAtEndIfTooLong;
        }


        /// <summary>
        /// Generates a random string.
        /// </summary>
	    private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-.()";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray<char>());
        }



        /// <summary>
        /// Parses a basic string to HTML suitable string.
        /// NOTICE: Doesn't perform HtmlEncode!
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The parsed string.</returns>
        public static string ParseBasicStringForHtml(this string str)
        {
            return str?.Replace("\n", "<br/>") ?? str;
        }


        public static string ReplaceFirstOccurrance(this string original, string oldValue, string newValue)
        {
            if (String.IsNullOrEmpty(original))
                return String.Empty;
            if (String.IsNullOrEmpty(oldValue))
                return original;
            if (String.IsNullOrEmpty(newValue))
                newValue = String.Empty;
            int loc = original.IndexOf(oldValue);
            return original.Remove(loc, oldValue.Length).Insert(loc, newValue);
        }

        public static string StringJoin(this IEnumerable<string> values, string seperator)
        {
            return string.Join(seperator, values);
        }

        public static List<int> SplitInt(this string str, char splitChar)
        {
            if (str.IsNullOrEmpty())
			{
                return new List<int>();
			}

	        var parts = str.Split(splitChar);

            var result = new List<int>();
            foreach (var currPart in parts)
            {
	            result.Add(int.Parse(currPart));
            }

            return result;
        }

        /// <summary>
        /// Trims all types of whitespace characters including non-breaking spaces and other Unicode whitespace.
        /// This is more robust than the standard Trim() which only handles basic whitespace.
        /// Useful for cleaning data from Excel imports where non-breaking spaces (U+00A0) are common.
        /// </summary>
        /// <param name="input">The string to trim.</param>
        /// <returns>The trimmed string with all Unicode whitespace removed from start and end.</returns>
        public static string TrimAllWhitespace(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Remove all whitespace characters from start and end, including:
            // - Regular spaces
            // - Non-breaking spaces (\u00A0)
            // - Tabs, newlines, and other Unicode whitespace
            int start = 0;
            int end = input.Length - 1;

            // Trim from start
            while (start <= end && char.IsWhiteSpace(input[start]))
            {
                start++;
            }

            // Trim from end
            while (end >= start && char.IsWhiteSpace(input[end]))
            {
                end--;
            }

            return start > end ? string.Empty : input.Substring(start, end - start + 1);
        }
    }
}
