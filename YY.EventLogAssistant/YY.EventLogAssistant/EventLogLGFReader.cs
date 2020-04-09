using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YY.LogReader.Models;
using YY.LogReader.Models.EventLog;
using YY.LogReader.Services;

namespace YY.LogReader.EventLog
{
    internal sealed class EventLogLGFReader : EventLogReader
    {
        private const long _defaultBeginLineForLGF = 3;

        private int _indexCurrentFile;
        private string[] _logFilesWithData;
        public string CurrentFile
        {
            get
            {
                if (_logFilesWithData.Length <= _indexCurrentFile)                
                    return null;
                else
                    return _logFilesWithData[_indexCurrentFile];
            }
        }
        private long _eventCount = -1;

        StreamReader _stream;
        StringBuilder _eventSource;

        LogParserLGF _logParser;
        private LogParserLGF LogParser
        {
            get 
            {
                if (_logParser == null)
                    _logParser = new LogParserLGF(this);

                return _logParser;
            }
        }

        internal EventLogLGFReader() : base() { }
        internal EventLogLGFReader(string logFilePath) : base(logFilePath)
        {
            _indexCurrentFile = 0;
            _logFilesWithData = Directory
                .GetFiles(_logFileDirectoryPath, "*.lgp")
                .OrderBy(i => i)
                .ToArray();
            _eventSource = new StringBuilder();            
        }

        public override bool Read(out EventLogRowData rowData)
        {
            try
            {
                if (_stream == null)
                {
                    if (_logFilesWithData.Length <= _indexCurrentFile)
                    {
                        rowData = null;
                        return false;
                    }
                    
                    InitializeStream(_defaultBeginLineForLGF, _indexCurrentFile);
                    _currentFileEventNumber = 0;
                }
                _eventSource.Clear();

                BeforeReadFileEventArgs beforeReadFileArgs = new BeforeReadFileEventArgs(CurrentFile);
                if (_currentFileEventNumber == 0)
                    RaiseBeforeReadFile(beforeReadFileArgs);

                if (beforeReadFileArgs.Cancel)
                {
                    NextFile();
                    return Read(out rowData);
                }

                string sourceData;
                bool newLine = true;
                int countBracket = 0;
                bool textBlockOpen = false;

                while (true)
                {
                    sourceData = _stream.ReadLine();
                    if (sourceData == null)
                    {
                        NextFile();
                        return Read(out rowData);
                    }

                    if (newLine)
                    {
                        _eventSource.Append(sourceData);
                    }
                    else
                    {
                        _eventSource.AppendLine();
                        _eventSource.Append(sourceData);
                    }

                    if (_logParser.ItsEndOfEvent(sourceData, ref countBracket, ref textBlockOpen))
                    {
                        newLine = true;
                        _currentFileEventNumber += 1;
                        string prepearedSourceData = _eventSource.ToString();

                        RaiseBeforeRead(new BeforeReadEventArgs(prepearedSourceData, _currentFileEventNumber));

                        try
                        {
                            EventLogRowData eventData = LogParser.Parse(prepearedSourceData);
                            rowData = eventData;
                        }
                        catch (Exception ex)
                        {
                            RaiseOnError(new OnErrorEventArgs(ex, prepearedSourceData, false));
                            rowData = null;
                        }

                        RaiseAfterRead(new AfterReadEventArgs(rowData, _currentFileEventNumber));

                        return true;
                    }
                    else
                    {
                        newLine = false;
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseOnError(new OnErrorEventArgs(ex, null, true));
                rowData = null;
                return false;
            }
        }

        public override bool GoToEvent(long eventNumber)
        {
            Reset();

            int fileIndex = -1;
            long currentLineNumber = -1;
            long currentEventNumber = 0;
            bool moved = false;

            foreach (string logFile in _logFilesWithData)
            {
                fileIndex += 1;
                currentLineNumber = -1;

                IEnumerable<string> allLines = File.ReadLines(logFile);
                foreach (string line in allLines)
                {
                    currentLineNumber += 1;
                    if (Regex.IsMatch(line, @"^{\d{4}\d{2}\d{2}\d+"))
                    {
                        currentEventNumber += 1;
                    }

                    if (currentEventNumber == eventNumber)
                    {
                        moved = true;
                        break;
                    }
                }

                if (currentEventNumber == eventNumber)
                {
                    moved = true;
                    break;
                }
            }

            if (moved && fileIndex >= 0 && currentLineNumber >= 0)
            {
                InitializeStream(currentLineNumber, fileIndex);
                _eventCount = eventNumber - 1;

                return true;
            }
            else
            {
                return false;
            }
        }

        public override EventLogPosition GetCurrentPosition()
        {
            return new EventLogPosition(
                _currentFileEventNumber, 
                _logFilePath, 
                CurrentFile, 
                GetCurrentFileStreamPosition());
        }

        public override void SetCurrentPosition(EventLogPosition newPosition)
        {
            Reset();

            if(newPosition.CurrentFileReferences != _logFilePath)
                throw new Exception("Invalid data file with references");

            int indexOfFileData = Array.IndexOf(_logFilesWithData, newPosition.CurrentFileData);
            if (indexOfFileData < 0)
                throw new Exception("Invalid data file");
            _indexCurrentFile = indexOfFileData;

            _currentFileEventNumber = newPosition.EventNumber;

            InitializeStream(_defaultBeginLineForLGF, _indexCurrentFile);

            if (newPosition.StreamPosition != null)
                SetCurrentFileStreamPosition((long)newPosition.StreamPosition);
        }

        public override long Count()
        {
            if(_eventCount < 0)
                _eventCount = LogParser.GetEventCount();

            return _eventCount;
        }

        public override void Reset()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            _indexCurrentFile = 0;
        }

        public override void NextFile()
        {
            RaiseAfterReadFile(new AfterReadFileEventArgs(CurrentFile));

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            _indexCurrentFile += 1;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        public long GetCurrentFileStreamPosition()
        {
            if (_stream != null)
                return _stream.GetPosition();
            else
                return 0;
        }

        public void SetCurrentFileStreamPosition(long position)
        {
            if (_stream != null)
                _stream.SetPosition(position);
        }

        protected override void ReadEventLogReferences()
        {
            string Text = string.Empty;
            using (FileStream FS = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader SR = new StreamReader(FS))
                Text = SR.ReadToEnd();

            int beginBlockIndex = Text.IndexOf("{");
            if (beginBlockIndex < 0)
                return;

            Text = Text.Substring(beginBlockIndex);
            string[] ObjectTexts = LogParser.ParseEventLogString("{" + Text + "}");

            string LastProcessedObjectForDebug;
            foreach (string TextObject in ObjectTexts)
            {
                LastProcessedObjectForDebug = TextObject;
                string[] a = LogParser.ParseEventLogString(TextObject);

                if ((a != null))
                {
                    switch (a[0])
                    {
                        case "1":
                            _users.Add(new Users()
                            {
                                Code = long.Parse(a[3]),
                                Uuid = a[1].ToGuid(),
                                Name = a[2]
                            });
                            break;
                        case "2":
                            _computers.Add(new Computers()
                            {
                                Code = int.Parse(a[2]),
                                Name = a[1]
                            });
                            break;
                        case "3":
                            _applications.Add(new Applications()
                            {
                                Code = int.Parse(a[2]),
                                Name = a[1]
                            });
                            break;
                        case "4":
                            _events.Add(new Events()
                            {
                                Code = int.Parse(a[2]),
                                Name = a[1]
                            });
                            break;
                        case "5":
                            _metadata.Add(new Metadata()
                            {
                                Code = int.Parse(a[3]),
                                Uuid = a[1].ToGuid(),
                                Name = a[2]
                            });
                            break;
                        case "6":
                            _workServers.Add(new WorkServers()
                            {
                                Code = int.Parse(a[2]),
                                Name = a[1]
                            });
                            break;
                        case "7":
                            _primaryPorts.Add(new PrimaryPorts()
                            {
                                Code = int.Parse(a[2]),
                                Name = a[1]
                            });
                            break;
                        case "8":
                            _secondaryPorts.Add(new SecondaryPorts()
                            {
                                Code = int.Parse(a[2]),
                                Name = a[1]
                            });
                            break;
                        //Case "9" - эти значения отсутствуют в файле
                        //Case "10"
                        case "11":
                            break;
                        case "12":
                            break;
                        case "13":
                            break;
                        //  Последние три значения содержат статус транзакции и важность события                        
                        default:
                            break;
                    }

                }
            }
        }

        private void InitializeStream(long linesToSkip, int fileIndex = 0)
        {
            FileStream fs = new FileStream(_logFilesWithData[fileIndex], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _stream = new StreamReader(fs);
            _stream.SkipLine(linesToSkip);
        }

        private sealed class LogParserLGF
        {
            private EventLogLGFReader _reader;

            public LogParserLGF(EventLogLGFReader reader)
            {
                _reader = reader;
            }

            public EventLogRowData Parse(string eventSource)
            {
                var parseResult = ParseEventLogString(eventSource);

                DateTime eventDate = DateTime.ParseExact(parseResult[0], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

                EventLogRowData eventData = new EventLogRowData();
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
                                
                MatchCollection matches = Regex.Matches(eventData.Data, @"[\d]+:[\dA-Za-zА-Яа-я]{32}}");
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
                    int count3 = CountSubstringInString(Str, "\"") % 2;//Math.IEEERemainder(CountSubstringInString(Str, "\""), 2);
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

            public long GetEventCount()
            {
                long eventCount = 0;

                foreach (var logFile in _reader._logFilesWithData)
                {
                    eventCount += File.ReadLines(logFile)
                        .Where(lineInt => Regex.IsMatch(lineInt, @"^{\d{4}\d{2}\d{2}\d+,"))
                        .LongCount();
                }

                return eventCount;
            }
        }
    }
}
