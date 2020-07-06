using YY.EventLogReaderAssistant.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using YY.EventLogReaderAssistant.Helpers;

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
        private readonly Regex _regexDataUuid;

        #endregion

        #region Constructor

        public LogParserLGF(EventLogLGFReader reader)
        {
            _reader = reader;
            _regexDataUuid = new Regex(@"[\d]+:[\dA-Za-zА-Яа-я]{32}}");
        }

        #endregion

        #region Public Methods

        public LogParserReferencesLGF GetEventLogReferences()
        {
            LogParserReferencesLGF referencesInfo = new LogParserReferencesLGF(_reader, this);

            return referencesInfo;
        }
        public RowData Parse(string eventSource)
        {
            string[] parseResult = ParseEventLogString(eventSource);
            RowData dataRow = null;

            if (parseResult != null)
            {
                string transactionSourceString = parseResult[2].RemoveBraces();

                dataRow = new RowData()
                {
                    RowId = _reader.CurrentFileEventNumber,
                    Period = DateTime.ParseExact(parseResult[0], "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                    TransactionStatus = _reader.GetTransactionStatus(parseResult[1]),
                    TransactionDate = GetTransactionDate(transactionSourceString),
                    TransactionId = GetTransactionId(transactionSourceString),
                    User = _reader.GetUserByCode(parseResult[3]),
                    Computer = _reader.GetComputerByCode(parseResult[4]),
                    Application = _reader.GetApplicationByCode(parseResult[5]),
                    ConnectId = parseResult[6].ToInt32(),
                    Event = _reader.GetEventByCode(parseResult[7]),
                    Severity = _reader.GetSeverityByCode(parseResult[8]),
                    Comment = parseResult[9].RemoveQuotes(),
                    Metadata = _reader.GetMetadataByCode(parseResult[10]),
                    Data = GetData(parseResult[11]),
                    DataPresentation = parseResult[12].RemoveQuotes(),
                    WorkServer = _reader.GetWorkServerByCode(parseResult[13]),
                    PrimaryPort = _reader.GetPrimaryPortByCode(parseResult[14]),
                    SecondaryPort = _reader.GetSecondaryPortByCode(parseResult[15]),
                    Session = parseResult[16].ToInt64()
                };
                dataRow.DataUuid = GetDataUuid(dataRow.Data);
            }

            return dataRow;
        }
        public string[] ParseEventLogString(string sourceString)
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

        private bool AddResultString(ref string[] resultStrings, ref int i, ref string bufferString, bool isSpecialString)
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
        private bool IsSpeacialString(string sourceString, int partNumber)
        {
            bool isSpecialString = partNumber == 1 &&
                                   !string.IsNullOrEmpty(sourceString)
                                   && sourceString[0] == '\"';

            return isSpecialString;
        }
        private bool IsCorrectLogPart(string sourceString, bool isSpecialString)
        {
            int counterBeginCurlyBrace, counterEndCurlyBrace;

            if (isSpecialString)
            {
                counterBeginCurlyBrace = 0;
                counterEndCurlyBrace = 0;
            }
            else
            {
                counterBeginCurlyBrace = CountSubstring(sourceString, "{");
                counterEndCurlyBrace = CountSubstring(sourceString, "}");
            }
            int counterSlash = CountSubstring(sourceString, "\"") % 2;

            return counterBeginCurlyBrace == counterEndCurlyBrace & counterSlash == 0;
        }
        private string RemoveSpecialSymbols(string sourceString)
        {
            char[] denied = new[] { '\n', '\t', '\r' };
            StringBuilder newString = new StringBuilder();

            foreach (var ch in sourceString)
                if (!denied.Contains(ch))
                    newString.Append(ch);

            return newString.ToString();
        }
        private string RemoveDoubleQuotes(string sourceString)
        {
            if (sourceString.StartsWith("\"") && sourceString.EndsWith("\""))
                return sourceString.Substring(1, sourceString.Length - 2);
            else
                return sourceString;
        }
        private int GetDelimeterIndex(string sourceString, bool isSpecialString = false)
        {
            int delimIndex;

            if (isSpecialString)
            {
                delimIndex = sourceString.IndexOf("\",", StringComparison.Ordinal) + 1;
            }
            else
            {
                delimIndex = sourceString.IndexOf(",", StringComparison.Ordinal);
            }

            return delimIndex;
        }
        private string GetData(string sourceString)
        {
            string data = sourceString;

            if (data == "{\"U\"}")
                data = string.Empty;
            else if (data.StartsWith("{"))
            {
                string[] parsedObjects = ParseEventLogString(data);
                if (parsedObjects != null)
                { 
                    if (parsedObjects.Length == 2)
                    {
                        if (parsedObjects[0] == "\"S\"" || parsedObjects[0] == "\"R\"")
                        {
                            data = parsedObjects[1].RemoveQuotes();
                        }
                    }
                }
            }

            return data;
        }
        private string GetDataUuid(string sourceData)
        {
            string dataUuid;

            MatchCollection matches = _regexDataUuid.Matches(sourceData);
            if (matches.Count > 0)
            {
                string[] dataPartsUuid = sourceData.Split(':');
                if (dataPartsUuid.Length == 2)
                {
                    dataUuid = dataPartsUuid[1].Replace("}", string.Empty);
                } else
                    dataUuid = string.Empty;
            }
            else
                dataUuid = string.Empty;

            return dataUuid;
        }
        private DateTime? GetTransactionDate(string sourceString)
        {
            DateTime? transactionDate;

            long transDate = sourceString.Substring(0, sourceString.IndexOf(",", StringComparison.Ordinal)).From16To10();
            try
            {
                if (!(transDate == 0))
                    transactionDate = new DateTime().AddSeconds((double)transDate / 10000);
                else
                    transactionDate = null;
            }
            catch
            {
                transactionDate = null;
            }

            return transactionDate;
        }
        private long? GetTransactionId(string sourceString)
        {
            long? transactionId = sourceString.Substring(sourceString.IndexOf(",", StringComparison.Ordinal) + 1).From16To10();

            return transactionId;
        }
        private int CountSubstring(string sourceString, string sourceSubstring)
        {
            int countSubstring = (sourceString.Length - sourceString.Replace(sourceSubstring, "").Length) / sourceSubstring.Length;

            return countSubstring;
        }

        #endregion
    }
}
