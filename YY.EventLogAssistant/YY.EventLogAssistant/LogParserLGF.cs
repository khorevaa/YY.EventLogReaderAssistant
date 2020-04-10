using YY.EventLogAssistant.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using YY.EventLogAssistant.Services;
using System.Text.RegularExpressions;

namespace YY.EventLogAssistant
{
    internal sealed class LogParserLGF
    {
        private EventLogLGFReader _reader;
        private Regex regexDataUUID;

        public LogParserLGF(EventLogLGFReader reader)
        {
            _reader = reader;
            regexDataUUID = new Regex(@"[\d]+:[\dA-Za-zА-Яа-я]{32}}");
        }

        public RowData Parse(string eventSource)
        {
            var parseResult = ParseEventLogString(eventSource);

            DateTime eventDate = DateTime.ParseExact(parseResult[0], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            RowData eventData = new RowData();
            eventData.RowID = _reader.CurrentFileEventNumber;
            eventData.Period = eventDate;

            string TransStr = parseResult[2].ToString().Replace("}", "").Replace("{", "");
            long TransDate = TransStr.Substring(0, TransStr.IndexOf(",")).From16To10();
            try
            {
                if (!(TransDate == 0))
                    eventData.TransactionDate = new System.DateTime().AddSeconds((double)TransDate / 10000);
            }
            catch
            {
                eventData.TransactionDate = null;
            }

            eventData.TransactionId = TransStr.Substring(TransStr.IndexOf(",") + 1).From16To10();
            if (parseResult[1] == "R")
                eventData.TransactionStatus = TransactionStatus.Unfinished;
            else if (parseResult[1] == "N")
                eventData.TransactionStatus = TransactionStatus.NotApplicable;
            else if (parseResult[1] == "U")
                eventData.TransactionStatus = TransactionStatus.Committed;
            else if (parseResult[1] == "C")
                eventData.TransactionStatus = TransactionStatus.RolledBack;
            else
                eventData.TransactionStatus = TransactionStatus.Unknown;

            eventData.Comment = parseResult[9].RemoveQuotes();
            eventData.Session = Convert.ToInt32(parseResult[16]);
            eventData.ConnectId = Convert.ToInt32(parseResult[6]);

            eventData.DataPresentation = parseResult[12].RemoveQuotes();
            eventData.Data = parseResult[11];

            MatchCollection matches = regexDataUUID.Matches(eventData.Data);
            if (matches.Count > 0)
            {
                string[] dataPartsUUID = eventData.Data.Split(':');
                if (dataPartsUUID.Length == 2)
                {
                    string dataUUID = dataPartsUUID[1].Replace("}", string.Empty);
                    eventData.DataUUID = dataUUID;
                }
            }

            long userID = Convert.ToInt64(parseResult[3]);
            eventData.User = _reader.Users.Where(i => i.Code == userID).FirstOrDefault();

            long computerID = Convert.ToInt64(parseResult[4]);
            eventData.Computer = _reader.Computers.Where(i => i.Code == computerID).FirstOrDefault();

            long appID = Convert.ToInt64(parseResult[5]);
            eventData.Application = _reader.Applications.Where(i => i.Code == appID).FirstOrDefault();

            long eventID = Convert.ToInt64(parseResult[7]);
            eventData.Event = _reader.Events.Where(i => i.Code == eventID).FirstOrDefault();

            long metadataID = Convert.ToInt64(parseResult[10]);
            eventData.Metadata = _reader.Metadata.Where(i => i.Code == metadataID).FirstOrDefault();

            long workServerID = Convert.ToInt64(parseResult[13]);
            eventData.WorkServer = _reader.WorkServers.Where(i => i.Code == workServerID).FirstOrDefault();

            long pimaryPortID = Convert.ToInt64(parseResult[14]);
            eventData.PrimaryPort = _reader.PrimaryPorts.Where(i => i.Code == pimaryPortID).FirstOrDefault();

            long secondaryPortID = Convert.ToInt64(parseResult[15]);
            eventData.SecondaryPort = _reader.SecondaryPorts.Where(i => i.Code == secondaryPortID).FirstOrDefault();

            if (eventData.Data == "{\"U\"}") // 'empty reference
                eventData.Data = string.Empty;
            else if (eventData.Data.StartsWith("{"))
            {
                //'internal representation for different objects.
                var ParsedObject = ParseEventLogString(eventData.Data);
                if (ParsedObject.Length == 2)
                {
                    if (ParsedObject[0] == "\"S\"" || ParsedObject[0] == "\"R\"")
                    {
                        //'this is string or reference
                        eventData.Data = ParsedObject[1].RemoveQuotes(); // 'string value
                    }
                }
            }

            switch (parseResult[8].Trim())
            {
                case "I":
                    eventData.Severity = Severity.Information;
                    break;
                case "W":
                    eventData.Severity = Severity.Warning;
                    break;
                case "E":
                    eventData.Severity = Severity.Error;
                    break;
                case "N":
                    eventData.Severity = Severity.Note;
                    break;
                default:
                    eventData.Severity = Severity.Unknown;
                    break;
            }

            return eventData;
        }

        internal bool ItsEndOfEvent(string Str, ref int Count, ref bool TextBlockOpen)
        {
            string TempStr = Str;

            for (int i = 0; i <= TempStr.Length - 1; i++)
            {
                string Simb = TempStr.Substring(i, 1);
                if (Simb == "\"")
                {
                    TextBlockOpen = !TextBlockOpen;
                }
                else if (Simb == "}" & !TextBlockOpen)
                {
                    Count = Count - 1;
                }
                else if (Simb == "{" & !TextBlockOpen)
                {
                    Count = Count + 1;
                }
            }

            return (Count == 0);
        }

        internal string[] ParseEventLogString(string Text)
        {
            string[] ArrayLines = null;

            string Text2 = Text.Substring(1, (Text.EndsWith(",") ? Text.Length - 3 : Text.Length - 2)) + ",";

            string Str = "";

            int Delim = Text2.IndexOf(",");
            int i = 0;
            int partNumber = 0;
            bool isSpecialString = false;

            while (Delim > 0)
            {

                Str = Str + Text2.Substring(0, Delim).Trim();
                partNumber += 1;
                Text2 = Text2.Substring(Delim + 1);
                if (partNumber == 1 && !String.IsNullOrEmpty(Str) && Str[0] == '\"')
                    isSpecialString = true;

                int count1;
                int count2;
                if (isSpecialString)
                {
                    count1 = 0;
                    count2 = 0;
                }
                else
                {
                    count1 = CountSubstringInString(Str, "{");
                    count2 = CountSubstringInString(Str, "}");
                }
                int count3 = CountSubstringInString(Str, "\"") % 2; // Math.IEEERemainder(CountSubstringInString(Str, "\""), 2);
                if (count1 == count2 & count3 == 0)
                {
                    Array.Resize(ref ArrayLines, i + 1);
                    if (Str.StartsWith("\"") && Str.EndsWith("\""))
                    {
                        Str = Str.Substring(1, Str.Length - 2);
                    }
                    if (isSpecialString)
                    {
                        char[] denied = new[] { '\n', '\t', '\r' };
                        StringBuilder newString = new StringBuilder();
                        foreach (var ch in Str)
                            if (!denied.Contains(ch))
                                newString.Append(ch);
                        Str = newString.ToString();
                    }
                    ArrayLines[i] = Str;
                    i = i + 1;
                    Str = "";
                    partNumber = 0;
                    isSpecialString = false;
                }
                else
                {
                    Str = Str + ",";
                }

                if (isSpecialString) // Особая обработка для поля "DataPresentation"
                {
                    Delim = Text2.IndexOf("\",") + 1;
                }
                else
                {
                    Delim = Text2.IndexOf(",");
                }

            }
            return ArrayLines;
        }

        private int CountSubstringInString(string Str, string SubStr)
        {
            return (Str.Length - Str.Replace(SubStr, "").Length) / SubStr.Length;
        }
    }
}
