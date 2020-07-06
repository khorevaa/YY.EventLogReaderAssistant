using YY.EventLogReaderAssistant.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant
{
    internal sealed class LogParserLGF
    {
        #region Static Methods

        public static bool ItsBeginOfEvent(string sourceString)
        {
            if (sourceString == null)
                return false;

            return Regex.IsMatch(sourceString, @"^{\d{4}\d{2}\d{2}\d+,");
        }

        public static bool ItsEndOfEvent(string sourceString, ref int count, ref bool textBlockOpen)
        {
            if (sourceString == null)
                return false;

            string bufferString = sourceString;

            for (int i = 0; i <= bufferString.Length - 1; i++)
            {
                string simb = bufferString.Substring(i, 1);
                if (simb == "\"")
                {
                    textBlockOpen = !textBlockOpen;
                }
                else if (simb == "}" & !textBlockOpen)
                {
                    count -= 1;
                }
                else if (simb == "{" & !textBlockOpen)
                {
                    count += 1;
                }
            }

            return (count == 0);
        }

        #endregion

        #region Private Member Variables

        private readonly EventLogLGFReader _reader;

        #endregion

        #region Constructor

        public LogParserLGF(EventLogLGFReader reader)
        {
            _reader = reader;
        }

        #endregion

        #region Public Methods

        public LogParserReferencesLGF GetEventLogReferences()
        {
            LogParserReferencesLGF referencesInfo = new LogParserReferencesLGF(_reader);

            return referencesInfo;
        }
        public RowData Parse(string eventSource)
        {
            string[] parseResult = ParseEventLogString(eventSource);
            RowData dataRow = null;

            if (parseResult != null)
            {
                dataRow = new RowData();
                dataRow.FillByStringParsedData(_reader, parseResult);
            }

            return dataRow;
        }
        public static string[] ParseEventLogString(string sourceString)
        {
            string[] resultStrings = null;
            string preparedString = sourceString.Substring(1, (sourceString.EndsWith(",") ? sourceString.Length - 3 : sourceString.Length - 2)) + ",";
            string bufferString = string.Empty;
            int i = 0, partNumber = 0, delimIndex = GetDelimeterIndex(preparedString);

            while (delimIndex > 0)
            {
                partNumber += 1;
                bufferString += preparedString.Substring(0, delimIndex).Trim();
                preparedString = preparedString.Substring(delimIndex + 1);
                bool isSpecialString = IsSpeacialString(bufferString, partNumber);

                if (AddResultString(ref resultStrings, ref i, ref bufferString, isSpecialString))
                {
                    i += 1;
                    bufferString = string.Empty;
                    partNumber = 0;
                    isSpecialString = false;
                }
                else
                    bufferString += ",";

                delimIndex = GetDelimeterIndex(preparedString, isSpecialString);
            }

            return resultStrings;
        }

        #endregion

        #region Private Methods

        private static bool AddResultString(ref string[] resultStrings, ref int i, ref string bufferString, bool isSpecialString)
        {
            bool output = false;

            if (IsCorrectLogPart(bufferString, isSpecialString))
            {
                Array.Resize(ref resultStrings, i + 1);
                bufferString = RemoveDoubleQuotes(bufferString);
                if (isSpecialString) bufferString = RemoveSpecialSymbols(bufferString);
                resultStrings[i] = bufferString;
                output = true;
            }

            return output;
        }
        private static bool IsSpeacialString(string sourceString, int partNumber)
        {
            bool isSpecialString = partNumber == 1 &&
                                   !string.IsNullOrEmpty(sourceString)
                                   && sourceString[0] == '\"';

            return isSpecialString;
        }
        private static bool IsCorrectLogPart(string sourceString, bool isSpecialString)
        {
            int counterBeginCurlyBrace = 0, counterEndCurlyBrace = 0;
            int counterSlash = CountSubstring(sourceString, "\"") % 2;

            if (!isSpecialString)
            {
                counterBeginCurlyBrace = CountSubstring(sourceString, "{");
                counterEndCurlyBrace = CountSubstring(sourceString, "}");
            }

            return counterBeginCurlyBrace == counterEndCurlyBrace & counterSlash == 0;
        }
        private static string RemoveSpecialSymbols(string sourceString)
        {
            char[] denied = new[] { '\n', '\t', '\r' };
            
            string newString = string.Join("", sourceString
                .Where(s => !denied.Contains(s))
                .ToArray());

            return newString;
        }
        private static string RemoveDoubleQuotes(string sourceString)
        {
            if (sourceString.StartsWith("\"") && sourceString.EndsWith("\""))
                return sourceString.Substring(1, sourceString.Length - 2);
            else
                return sourceString;
        }
        private static int GetDelimeterIndex(string sourceString, bool isSpecialString = false)
        {
            if (isSpecialString)
                return sourceString.IndexOf("\",", StringComparison.Ordinal) + 1;
            else
                return sourceString.IndexOf(",", StringComparison.Ordinal);
        }
        private static int CountSubstring(string sourceString, string sourceSubstring)
        {
            int countSubstring = (sourceString.Length - sourceString.Replace(sourceSubstring, "").Length) / sourceSubstring.Length;

            return countSubstring;
        }

        #endregion
    }
}
