using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("YY.EventLogAssistant.Tests")]
namespace YY.EventLogAssistant.Services
{
    internal static class StringExtensions
    {
        #region Private Member Variables

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

        #endregion

        #region Public Methods

        public static long From16To10(this string sourceValue)
        {
            return Convert.ToInt64(sourceValue.ToUpper(), 16);
        }

        public static string RemoveQuotes(this string sourceValue)
        {
            string functionReturnValue = sourceValue;

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

        public static string FromWin1251ToUTF8(this string sourceValue)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding utf8 = Encoding.GetEncoding("UTF-8");
            Encoding win1251 = Encoding.GetEncoding("windows-1251");

            return ConvertEncoding(sourceValue, win1251, utf8);
        }

        public static Guid ToGuid(this string sourceValue)
        {
            Guid guidFromString = Guid.Empty;
            Guid.TryParse(sourceValue, out guidFromString);
            return guidFromString;
        }

        #endregion

        #region Private Methods

        private static string ConvertEncoding(this string sourceString, Encoding source, Encoding result)
        {
            byte[] souceBytes = source.GetBytes(sourceString);
            byte[] resultBytes = Encoding.Convert(result, source, souceBytes);

            return source.GetString(resultBytes);
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

        #endregion
    }
}
