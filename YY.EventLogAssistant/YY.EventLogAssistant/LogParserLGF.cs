using YY.EventLogAssistant.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using YY.EventLogAssistant.Services;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace YY.EventLogAssistant
{
    internal sealed class LogParserLGF
    {
        #region Static Methods

        public static bool ItsBeginOfEvent(string sourceString)
        {
            return Regex.IsMatch(sourceString, @"^{\d{4}\d{2}\d{2}\d+,");
        }

        public static bool ItsEndOfEvent(string sourceString, ref int count, ref bool textBlockOpen)
        {
            string TempStr = sourceString;

            for (int i = 0; i <= TempStr.Length - 1; i++)
            {
                string Simb = TempStr.Substring(i, 1);
                if (Simb == "\"")
                {
                    textBlockOpen = !textBlockOpen;
                }
                else if (Simb == "}" & !textBlockOpen)
                {
                    count = count - 1;
                }
                else if (Simb == "{" & !textBlockOpen)
                {
                    count = count + 1;
                }
            }

            return (count == 0);
        }

        #endregion

        #region Private Member Variables

        private EventLogLGFReader _reader;
        private Regex _regexDataUUID;

        #endregion

        #region Constructor

        public LogParserLGF(EventLogLGFReader reader)
        {
            _reader = reader;
            _regexDataUUID = new Regex(@"[\d]+:[\dA-Za-zА-Яа-я]{32}}");
        }

        #endregion

        #region Public Methods

        public void ReadEventLogReferences(
                IList<Users> users, 
                IList<Computers> computers, 
                IList<Applications> applications,
                IList<Events> events,
                IList<Metadata> metadata,
                IList<WorkServers> workServers,
                IList<PrimaryPorts> primaryPorts,
                IList<SecondaryPorts> secondaryPorts)
        {
            string empty = string.Empty;
            string textReferencesData = empty;

            using (FileStream FS = new FileStream(_reader.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader SR = new StreamReader(FS))
                textReferencesData = SR.ReadToEnd();

            int beginBlockIndex = textReferencesData.IndexOf("{");
            if (beginBlockIndex < 0)
                return;

            textReferencesData = textReferencesData.Substring(beginBlockIndex);
            string[] objectTexts = ParseEventLogString("{" + textReferencesData + "}");
            string lastProcessedObjectForDebug;
            foreach (string TextObject in objectTexts)
            {
                lastProcessedObjectForDebug = TextObject;
                string[] parsedEventData = ParseEventLogString(TextObject);

                if ((parsedEventData != null))
                {
                    switch (parsedEventData[0])
                    {
                        case "1":
                            users.Add(new Users()
                            {
                                Code = parsedEventData[3].ToInt64(),
                                Uuid = parsedEventData[1].ToGuid(),
                                Name = parsedEventData[2]
                            });
                            break;
                        case "2":
                            computers.Add(new Computers()
                            {
                                Code = parsedEventData[2].ToInt64(),
                                Name = parsedEventData[1]
                            });
                            break;
                        case "3":
                            applications.Add(new Applications()
                            {
                                Code = parsedEventData[2].ToInt64(),
                                Name = parsedEventData[1]
                            });
                            break;
                        case "4":
                            events.Add(new Events()
                            {
                                Code = parsedEventData[2].ToInt64(),
                                Name = parsedEventData[1]
                            });
                            break;
                        case "5":
                            metadata.Add(new Metadata()
                            {
                                Code = parsedEventData[3].ToInt64(),
                                Uuid = parsedEventData[1].ToGuid(),
                                Name = parsedEventData[2]
                            });
                            break;
                        case "6":
                            workServers.Add(new WorkServers()
                            {
                                Code = parsedEventData[2].ToInt64(),
                                Name = parsedEventData[1]
                            });
                            break;
                        case "7":
                            primaryPorts.Add(new PrimaryPorts()
                            {
                                Code = parsedEventData[2].ToInt64(),
                                Name = parsedEventData[1]
                            });
                            break;
                        case "8":
                            secondaryPorts.Add(new SecondaryPorts()
                            {
                                Code = parsedEventData[2].ToInt64(),
                                Name = parsedEventData[1]
                            });
                            break;
                        //Case "9" - неизвестные значения, возможно связаны с разделением данных
                        //Case "10"
                        case "11":
                            break;
                        case "12":
                            break;
                        case "13":
                            break;
                        //  Последние значения хранят статус транзакции и уровень события                        
                        default:
                            break;
                    }
                }
            }
        }

        public RowData Parse(string eventSource)
        {
            string[] parseResult = ParseEventLogString(eventSource);
            string transactionSourceString = parseResult[2].RemoveBraces();

            RowData eventData = new RowData();
            eventData.RowID = _reader.CurrentFileEventNumber;
            eventData.Period = DateTime.ParseExact(parseResult[0], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            eventData.TransactionStatus = GetTransactionStatus(parseResult[1]);
            eventData.TransactionDate = GetTransactionDate(transactionSourceString);
            eventData.TransactionId = GetTransactionId(transactionSourceString);
            eventData.User = _reader.GetUserByCode(parseResult[3]);
            eventData.Computer = _reader.GetComputerByCode(parseResult[4]);
            eventData.Application = _reader.GetApplicationByCode(parseResult[5]);
            eventData.ConnectId = parseResult[6].ToInt32();
            eventData.Event = _reader.GetEventByCode(parseResult[7]);
            eventData.Severity = _reader.GetSeverityByCode(parseResult[8]);
            eventData.Comment = parseResult[9].RemoveQuotes();
            eventData.Metadata = _reader.GetMetadataByCode(parseResult[10]);
            eventData.Data = GetData(parseResult[11]);
            eventData.DataUUID = GetDataUUID(eventData.Data);
            eventData.DataPresentation = parseResult[12].RemoveQuotes();
            eventData.WorkServer = _reader.GetWorkServerByCode(parseResult[13]);
            eventData.PrimaryPort = _reader.GetPrimaryPortByCode(parseResult[14]);
            eventData.SecondaryPort = _reader.GetSecondaryPortByCode(parseResult[15]);
            eventData.Session = parseResult[16].ToInt64();

            return eventData;
        }

        #endregion

        #region Private Methods

        private string GetData(string sourceString)
        {
            string data = sourceString;

            if (data == "{\"U\"}")
                data = string.Empty;
            else if (data.StartsWith("{"))
            {
                string[] parsedObjects = ParseEventLogString(data);
                if (parsedObjects.Length == 2)
                {
                    if (parsedObjects[0] == "\"S\"" || parsedObjects[0] == "\"R\"")
                    {
                        data = parsedObjects[1].RemoveQuotes();
                    }
                }
            }

            return data;
        }

        private string GetDataUUID(string sourceData)
        {
            string dataUUID;

            MatchCollection matches = _regexDataUUID.Matches(sourceData);
            if (matches.Count > 0)
            {
                string[] dataPartsUUID = sourceData.Split(':');
                if (dataPartsUUID.Length == 2)
                {
                    dataUUID = dataPartsUUID[1].Replace("}", string.Empty);
                } else
                    dataUUID = string.Empty;
            }
            else
                dataUUID = string.Empty;

            return dataUUID;
        }

        private TransactionStatus GetTransactionStatus(string sourceString)
        {
            TransactionStatus transactionStatus;

            if (sourceString == "R")
                transactionStatus = TransactionStatus.Unfinished;
            else if (sourceString == "N")
                transactionStatus = TransactionStatus.NotApplicable;
            else if (sourceString == "U")
                transactionStatus = TransactionStatus.Committed;
            else if (sourceString == "C")
                transactionStatus = TransactionStatus.RolledBack;
            else
                transactionStatus = TransactionStatus.Unknown;

            return transactionStatus;
        }

        private DateTime? GetTransactionDate(string sourceString)
        {
            DateTime? transactionDate;

            long TransDate = sourceString.Substring(0, sourceString.IndexOf(",")).From16To10();
            try
            {
                if (!(TransDate == 0))
                    transactionDate = new System.DateTime().AddSeconds((double)TransDate / 10000);
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
            long? transactionId;

            transactionId = sourceString.Substring(sourceString.IndexOf(",") + 1).From16To10();

            return transactionId;
        }

        private int CountSubstring(string sourceString, string sourceSubstring)
        {
            int countSubstring = (sourceString.Length - sourceString.Replace(sourceSubstring, "").Length) / sourceSubstring.Length;

            return countSubstring;
        }

        private string[] ParseEventLogString(string sourceString)
        {
            string[] resultStrings = null;
            string preparedString = sourceString.Substring(1, (sourceString.EndsWith(",") ? sourceString.Length - 3 : sourceString.Length - 2)) + ",";
            string bufferString = string.Empty;

            int delimIndex = preparedString.IndexOf(",");
            int i = 0;
            int partNumber = 0;
            bool isSpecialString = false;

            while (delimIndex > 0)
            {
                bufferString = bufferString + preparedString.Substring(0, delimIndex).Trim();
                partNumber += 1;
                preparedString = preparedString.Substring(delimIndex + 1);
                if (partNumber == 1 && !String.IsNullOrEmpty(bufferString) && bufferString[0] == '\"')
                    isSpecialString = true;

                int counter1, counter2;
                if (isSpecialString)
                {
                    counter1 = 0;
                    counter2 = 0;
                }
                else
                {
                    counter1 = CountSubstring(bufferString, "{");
                    counter2 = CountSubstring(bufferString, "}");
                }

                int counter3 = CountSubstring(bufferString, "\"") % 2;
                if (counter1 == counter2 & counter3 == 0)
                {
                    Array.Resize(ref resultStrings, i + 1);
                    if (bufferString.StartsWith("\"") && bufferString.EndsWith("\""))
                    {
                        bufferString = bufferString.Substring(1, bufferString.Length - 2);
                    }

                    if (isSpecialString)
                    {
                        char[] denied = new[] { '\n', '\t', '\r' };
                        StringBuilder newString = new StringBuilder();
                        foreach (var ch in bufferString)
                            if (!denied.Contains(ch))
                                newString.Append(ch);

                        bufferString = newString.ToString();
                    }

                    resultStrings[i] = bufferString;
                    i += 1;
                    bufferString = string.Empty;
                    partNumber = 0;
                    isSpecialString = false;
                }
                else
                    bufferString += ",";

                if (isSpecialString)
                {
                    delimIndex = preparedString.IndexOf("\",") + 1;
                }
                else
                {
                    delimIndex = preparedString.IndexOf(",");
                }

            }
            return resultStrings;
        }

        #endregion
    }
}
