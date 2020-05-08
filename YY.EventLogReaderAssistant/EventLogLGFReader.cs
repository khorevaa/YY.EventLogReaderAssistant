using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using YY.EventLogReaderAssistant.Models;
using YY.EventLogReaderAssistant.Services;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant
{
    internal sealed class EventLogLGFReader : EventLogReader
    {
        #region Private Member Variables

        private const long _defaultBeginLineForLGF = 3;
        private int _indexCurrentFile;
        private readonly string[] _logFilesWithData;
        private long _eventCount = -1;

        StreamReader _stream;
        readonly StringBuilder _eventSource;

        private LogParserLGF _logParser;
        private LogParserLGF LogParser
        {
            get
            {
                if (_logParser == null)
                    _logParser = new LogParserLGF(this);

                return _logParser;
            }
        }

        #endregion

        #region Public Properties

        public override string CurrentFile
        {
            get
            {
                if (_logFilesWithData.Length <= _indexCurrentFile)
                    return null;
                else
                    return _logFilesWithData[_indexCurrentFile];
            }
        }

        #endregion

        #region Constructor

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

        #endregion

        #region Public Methods

        public override bool Read()
        {
            try
            {
                if (_stream == null)
                {
                    if (_logFilesWithData.Length <= _indexCurrentFile)
                    {
                        _currentRow = null;
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
                    return Read();
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
                        return Read();
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

                    if (LogParserLGF.ItsEndOfEvent(sourceData, ref countBracket, ref textBlockOpen))
                    {
                        newLine = true;
                        _currentFileEventNumber += 1;
                        string prepearedSourceData = _eventSource.ToString();

                        RaiseBeforeRead(new BeforeReadEventArgs(prepearedSourceData, _currentFileEventNumber));

                        try
                        {
                            RowData eventData = LogParser.Parse(prepearedSourceData);

                            if(eventData.Period >= ReferencesReadDate)
                            {
                                ReadEventLogReferences();
                                eventData = LogParser.Parse(prepearedSourceData);
                            }

                            _currentRow = eventData;
                        }
                        catch (Exception ex)
                        {
                            RaiseOnError(new OnErrorEventArgs(ex, prepearedSourceData, false));
                            _currentRow = null;
                        }

                        RaiseAfterRead(new AfterReadEventArgs(_currentRow, _currentFileEventNumber));

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
                _currentRow = null;
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
                    if(LogParserLGF.ItsBeginOfEvent(line))                    
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
            if (newPosition == null)
                return;

            if(newPosition.CurrentFileReferences != _logFilePath)
                throw new Exception("Invalid data file with references");

            int indexOfFileData = Array.IndexOf(_logFilesWithData, newPosition.CurrentFileData);
            if (indexOfFileData < 0)
                throw new Exception("Invalid data file");
            _indexCurrentFile = indexOfFileData;

            _currentFileEventNumber = newPosition.EventNumber;

            InitializeStream(_defaultBeginLineForLGF, _indexCurrentFile);
            long beginReadPosition =_stream.GetPosition();

            long newStreamPosition = (long)newPosition.StreamPosition;
            if(newStreamPosition < beginReadPosition)            
                newStreamPosition = beginReadPosition;            

            if (newPosition.StreamPosition != null)
                SetCurrentFileStreamPosition(newStreamPosition);
        }

        public override long Count()
        {
            if(_eventCount < 0)
                _eventCount = GetEventCount();

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
            _currentFileEventNumber = 0;
            _currentRow = null;
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

        protected override void ReadEventLogReferences()
        {
            _users.Clear();
            _computers.Clear();
            _events.Clear();
            _metadata.Clear();
            _applications.Clear();
            _workServers.Clear();
            _primaryPorts.Clear();
            _secondaryPorts.Clear();

            DateTime beginReadReferences = DateTime.Now;

            LogParser.ReadEventLogReferences(
                   _users,
                   _computers,
                   _applications,
                   _events,
                   _metadata,
                   _workServers,
                   _primaryPorts,
                   _secondaryPorts);

            _referencesReadDate = beginReadReferences;
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

        #endregion

        #region Private Methods

        private void InitializeStream(long linesToSkip, int fileIndex = 0)
        {
            FileStream fs = new FileStream(_logFilesWithData[fileIndex], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _stream = new StreamReader(fs);
            _stream.SkipLine(linesToSkip);
        }

        private long GetEventCount()
        {
            long eventCount = 0;

            foreach (var logFile in _logFilesWithData)
            {
                using (StreamReader logFileStream = new StreamReader(File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    do
                    {
                        string logFileCurrentString = logFileStream.ReadLine();
                        if(LogParserLGF.ItsBeginOfEvent(logFileCurrentString))
                            eventCount++;
                    } while (!logFileStream.EndOfStream);
                }
            }

            return eventCount;
        }

        #endregion
    }
}
