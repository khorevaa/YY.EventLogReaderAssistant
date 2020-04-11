using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace YY.EventLogAssistant.Services
{
    internal static class StringExtensions
    {
        public static long From16To10(this string Str)
        {
            return Convert.ToInt64(Str.ToUpper(), 16);
        }

        public static string RemoveQuotes(this string s)
        {
            string functionReturnValue = s;

            if (functionReturnValue.StartsWith("\""))
            {
                functionReturnValue = functionReturnValue.Substring(1);
            }

            if (functionReturnValue.EndsWith("\""))
            {
                functionReturnValue = functionReturnValue.Substring(0, functionReturnValue.Length - 1);
            }

            return functionReturnValue;
        }

        public static string RemoveBraces(this string sourceString)
        {
            return sourceString.Replace("}", "").Replace("{", "");
        }

        public static int ToInt32(this string sourceString)
        {
            return Convert.ToInt32(sourceString);
        }

        public static long ToInt64(this string sourceString)
        {
            return Convert.ToInt64(sourceString);
        }

        public static string ConvertEncoding(this string s, Encoding source, Encoding result)
        {
            byte[] souceBytes = source.GetBytes(s);
            byte[] resultBytes = Encoding.Convert(source, result, souceBytes);
            return result.GetString(resultBytes);
            //return result.GetString(source.GetBytes(s));
        }

        public static string FromAnsiToUTF8(this string s)
        {
            return ConvertEncoding(s,
                Encoding.GetEncoding(1252),
                Encoding.UTF8);
        }

        public static string FromWIN1251ToUTF8(this string s)
        {
            Encoding win1251 = CodePagesEncodingProvider.Instance.GetEncoding(1251);
            return ConvertEncoding(s, win1251, Encoding.UTF8);
        }

        public static string FromWIN1252ToUTF8(this string s)
        {
            return ConvertEncoding(s,
                Encoding.GetEncoding("windows-1252"),
                Encoding.UTF8);
        }

        public static Guid ToGuid(this string s)
        {
            Guid guidFromString = Guid.Empty;
            Guid.TryParse(s, out guidFromString);
            return guidFromString;
        }

        public static bool IsLike(this string s, string pattern)
        {
            // Characters matched so far
            int matched = 0;

            // Loop through pattern string
            for (int i = 0; i < pattern.Length; )
            {
                // Check for end of string
                if (matched > s.Length)
                    return false;

                // Get next pattern character
                char c = pattern[i++];
                if (c == '[') // Character list
                {
                    // Test for exclude character
                    bool exclude = (i < pattern.Length && pattern[i] == '!');
                    if (exclude)
                        i++;
                    // Build character list
                    int j = pattern.IndexOf(']', i);
                    if (j < 0)
                        j = s.Length;
                    HashSet<char> charList = CharListToSet(pattern.Substring(i, j - i));
                    i = j + 1;

                    if (charList.Contains(s[matched]) == exclude)
                        return false;
                    matched++;
                }
                else if (c == '?') // Any single character
                {
                    matched++;
                }
                else if (c == '#') // Any single digit
                {
                    if (!Char.IsDigit(s[matched]))
                        return false;
                    matched++;
                }
                else if (c == '*') // Zero or more characters
                {
                    if (i < pattern.Length)
                    {
                        // Matches all characters until
                        // next character in pattern
                        char next = pattern[i];
                        int j = s.IndexOf(next, matched);
                        if (j < 0)
                            return false;
                        matched = j;
                    }
                    else
                    {
                        // Matches all remaining characters
                        matched = s.Length;
                        break;
                    }
                }
                else // Exact character
                {
                    if (matched >= s.Length || c != s[matched])
                        return false;
                    matched++;
                }
            }
            // Return true if all characters matched
            return (matched == s.Length);
        }

        private static HashSet<char> CharListToSet(string charList)
        {
            HashSet<char> set = new HashSet<char>();

            for (int i = 0; i < charList.Length; i++)
            {
                if ((i + 1) < charList.Length && charList[i + 1] == '-')
                {
                    // Character range
                    char startChar = charList[i++];
                    i++; // Hyphen
                    char endChar = (char)0;
                    if (i < charList.Length)
                        endChar = charList[i++];
                    for (int j = startChar; j <= endChar; j++)
                        set.Add((char)j);
                }
                else set.Add(charList[i]);
            }
            return set;
        }

        public static string GetHashMD5(this string input)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));

            return sb.ToString();

        }

        private static readonly Dictionary<int, char> CharactersToMap = new Dictionary<int, char>  
        {
            {130, '‚'},
            {131, 'ƒ'},
            {132, '„'},
            {133, '…'},
            {134, '†'},
            {135, '‡'},
            {136, 'ˆ'},
            {137, '‰'},
            {138, 'Š'},
            {139, '‹'},
            {140, 'Œ'},
            {145, '‘'},
            {146, '’'},
            {147, '“'},
            {148, '”'},
            {149, '•'},
            {150, '–'},
            {151, '—'},
            {152, '˜'},
            {153, '™'},
            {154, 'š'},
            {155, '›'},
            {156, 'œ'},
            {159, 'Ÿ'},
            {173, '-'}
        };

        public static string ConvertFromWindowsToUnicode(this string txt)  
        {
            Encoding utf8 = Encoding.GetEncoding("utf-8");
            Encoding win1251 = Encoding.GetEncoding("windows-1252");

            byte[] utf8Bytes = win1251.GetBytes(txt);
            byte[] win1251Bytes = Encoding.Convert(win1251, utf8, utf8Bytes);
            string result = win1251.GetString(win1251Bytes);

            return result;
        }

    }
}
