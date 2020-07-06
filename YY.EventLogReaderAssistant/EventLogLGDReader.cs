using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using YY.EventLogReaderAssistant.Models;
using System.Runtime.CompilerServices;
using YY.EventLogReaderAssistant.EventArguments;
using YY.EventLogReaderAssistant.Helpers;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant
{
    internal sealed class EventLogLGDReader : EventLogReader    
    {
        #region Private Member Variables

        private string _connectionString;
        private string ConnectionString => _connectionString ?? (_connectionString = SQLiteExtensions.GetConnectionString(_logFilePath));
        private SQLiteConnection _connection;
        private readonly List<RowData> _readBuffer;
        private long _lastRowId;
        private const int ReadBufferSize = 1000;
        private long _lastRowNumberFromBuffer;
        private long _eventCount = -1;

        #endregion

        #region Constructor

        internal EventLogLGDReader(string logFilePath) : base(logFilePath) 
        {            
            _readBuffer = new List<RowData>();
            _lastRowId = 0;
            _lastRowNumberFromBuffer = 0;
        }

        #endregion

        #region Public Properties

        public override string CurrentFile => _logFilePath;

        #endregion

        #region Public Methods

        public override bool Read()
        {
            try
            {
                RaiseBeforeReadFileEvent(out bool cancelBeforeReadFile);
                if (cancelBeforeReadFile)
                {
                    _currentRow = null;
                    return false;
                }

                #region bufferedRead

                if (_lastRowNumberFromBuffer == 0
                    || _lastRowNumberFromBuffer >= ReadBufferSize)
                {
                    _readBuffer.Clear();
                    _lastRowNumberFromBuffer = 0;

                    using (_connection = new SQLiteConnection(ConnectionString))
                    {
                        _connection.Open();
                        using (SQLiteCommand cmd = new SQLiteCommand(GetQueryTextForLogData(), _connection))
                        {
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    try
                                    {
                                        DateTime rowPeriod = reader.GetInt64OrDefault(1).ToDateTimeFormat();
                                        if (rowPeriod >= ReferencesReadDate)
                                            ReadEventLogReferences();

                                        RowData row = new RowData();
                                        row.FillBySqliteReader(this, reader);

                                        if (!EventAllowedByPeriod(row))
                                        {
                                            _currentRow = null;
                                            return false;
                                        }

                                        _readBuffer.Add(row);
                                    }
                                    catch (Exception ex)
                                    {
                                        RaiseOnError(new OnErrorEventArgs(ex, reader.GetRowAsString(), false));
                                        _currentRow = null;
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion

                if (_lastRowNumberFromBuffer >= _readBuffer.Count)
                {
                    RaiseAfterReadFile(new AfterReadFileEventArgs(_logFilePath));
                    _currentRow = null;
                    return false;
                }

                RaiseBeforeRead(new BeforeReadEventArgs(null, _eventCount));

                _currentRow = _readBuffer
                    .Where(bufRow => bufRow.RowId > _lastRowId)
                    .First();
                _lastRowNumberFromBuffer += 1;
                _lastRowId = _currentRow.RowId;
                _currentFileEventNumber += 1;

                RaiseAfterRead(new AfterReadEventArgs(_currentRow, _eventCount));

                return true;
            }
            catch(Exception ex)
            {
                RaiseOnError(new OnErrorEventArgs(ex, null, true));
                _currentRow = null;
                return false;
            }
        }
        public override bool GoToEvent(long eventNumber)
        {
            Reset();

            long eventNumberToSkip = eventNumber - 1;
            if (eventNumberToSkip <= 0)
            {
                _lastRowId = 0;
                _currentFileEventNumber = 0;
                return true;
            }

            long newValueLastRowId = GetLastRowId(eventNumberToSkip);
            if(newValueLastRowId == 0)
            {
                _lastRowId = newValueLastRowId;
                _currentFileEventNumber = 0;
                return false;
            } else
            {
                _lastRowId = newValueLastRowId;
                _currentFileEventNumber = eventNumber;
                return true;
            }
        }
        public override EventLogPosition GetCurrentPosition()
        {
            return new EventLogPosition(
                _currentFileEventNumber,
                _logFilePath,
                _logFilePath,
                null);
        }
        public override void SetCurrentPosition(EventLogPosition newPosition)
        {
            Reset();
            if (newPosition == null)
                return;

            if (newPosition.CurrentFileReferences != _logFilePath)
                throw new Exception("Invalid data file with references");

            if (newPosition.CurrentFileData != _logFilePath)
                throw new Exception("Invalid data file with references");

            GoToEvent(newPosition.EventNumber);
        }
        public override long Count()
        {
            if (_eventCount < 0)
            {
                using (_connection = new SQLiteConnection(ConnectionString))
                {
                    _connection.Open();

                    string queryText = String.Format(
                    "Select\n" +
                    "    COUNT(el.RowId) CNT\n" +
                    "From\n" +
                    "    EventLog el\n");

                    SQLiteCommand cmd = new SQLiteCommand(_connection)
                    {
                        CommandType = System.Data.CommandType.Text,
                        CommandText = queryText
                    };
                    
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        _eventCount = reader.GetInt64OrDefault(0);
                    }
                }
            }

            return _eventCount;
        }
        public override void Reset()
        {
            SQLiteConnection.ClearAllPools();
            if (_connection != null)
            {
                _connection.Dispose();
            }

            _lastRowId = 0;
            _lastRowNumberFromBuffer = 0;
            _readBuffer.Clear();
            _currentFileEventNumber = 0;
            _currentRow = null;
        }
        public override void Dispose()
        {
            base.Dispose();

            SQLiteConnection.ClearAllPools();
            _connection?.Dispose();
            _readBuffer.Clear();
        }

        #endregion

        #region Private Methods

        private void RaiseBeforeReadFileEvent(out bool cancel)
        {
            BeforeReadFileEventArgs beforeReadFileArgs = new BeforeReadFileEventArgs(_logFilePath);
            if (_eventCount < 0)
                RaiseBeforeReadFile(beforeReadFileArgs);

            cancel = beforeReadFileArgs.Cancel;
        }
        private string GetQueryTextForLogData()
        {
            string queryText = string.Format(
                "Select\n" +
                "    el.RowId,\n" +
                "    el.Date AS Date,\n" +
                "    el.ConnectId,\n" +
                "    el.Session,\n" +
                "    el.TransactionStatus,\n" +
                "    el.TransactionDate,\n" +
                "    el.TransactionId,\n" +
                "    el.UserCode AS UserCode,\n" +
                "    el.ComputerCode AS ComputerCode,\n" +
                "    el.appCode AS ApplicationCode,\n" +
                "    el.eventCode AS EventCode,\n" +
                "    el.primaryPortCode AS PrimaryPortCode,\n" +
                "    el.secondaryPortCode AS SecondaryPortCode,\n" +
                "    el.workServerCode AS WorkServerCode,\n" +
                "    el.Severity AS SeverityCode,\n" +
                "    el.Comment AS Comment,\n" +
                "    el.Data AS Data,\n" +
                "    el.DataPresentation AS DataPresentation,\n" +
                "    elm.metadataCode AS MetadataCode\n" +
                "From\n" +
                "    EventLog el\n" +
                "    left join EventLogMetadata elm on el.RowId = elm.eventLogID\n" +
                "    left join MetadataCodes mc on elm.metadataCode = mc.code\n" +
                "Where RowID > {0}\n" +
                "Order By rowID\n" +
                "Limit {1}\n", _lastRowId, ReadBufferSize);

            return queryText;
        }
        private bool EventAllowedByPeriod(RowData eventData)
        {
            if (Math.Abs(_readDelayMilliseconds) > 0 && eventData != null)
            {
                DateTimeOffset stopPeriod = DateTimeOffset.Now.AddMilliseconds(-_readDelayMilliseconds);
                if (eventData.Period >= stopPeriod)
                    return false;
            }

            return true;
        }
        private long GetLastRowId(long eventNumberToSkip)
        {
            long valueLastRowId = 0;

            using (_connection = new SQLiteConnection(ConnectionString))
            {
                _connection.Open();

                string queryText = string.Format(
                     "Select\n" +
                     "    el.RowId\n" +
                     "From\n" +
                     "    EventLog el\n" +
                     "Where RowID > {0}\n" +
                     "Order By rowID\n" +
                     "Limit 1 OFFSET {1}\n", _lastRowId, eventNumberToSkip);

                using (SQLiteCommand cmd = new SQLiteCommand(queryText, _connection))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                        if (reader.Read())
                            valueLastRowId = reader.GetInt64OrDefault(0);
            }

            return valueLastRowId;
        }
        protected override void ReadEventLogReferences()
        {
            DateTime beginReadReferences = DateTime.Now;

            using (_connection = new SQLiteConnection(ConnectionString))
            {
                _connection.Open();

                ReadReferencesByType(_users, "Select Code, Name, UUID From UserCodes");
                ReadReferencesByType(_computers, "Select Code, Name From ComputerCodes");
                ReadReferencesByType(_events, "Select Code, Name From EventCodes");
                ReadReferencesByType(_metadata, "Select Code, Name, UUID From MetadataCodes");
                ReadReferencesByType(_applications, "Select Code, Name From AppCodes");
                ReadReferencesByType(_workServers, "Select Code, Name From WorkServerCodes");
                ReadReferencesByType(_primaryPorts, "Select Code, Name From PrimaryPortCodes");
                ReadReferencesByType(_secondaryPorts, "Select Code, Name From SecondaryPortCodes");
                
                _referencesReadDate = beginReadReferences;
            }

            base.ReadEventLogReferences();
        }
        private void ReadReferencesByType<T>(List<T> referenceCollection, string cmdSqliteText) where T: IReferenceObject, new()
        {
            referenceCollection.Clear();

            using (SQLiteCommand cmdReadReferences = new SQLiteCommand(cmdSqliteText, _connection))
            {
                using (SQLiteDataReader readerReferences = cmdReadReferences.ExecuteReader())
                    while (readerReferences.Read())
                    {
                        IReferenceObject referenceObject = new T();
                        referenceObject.FillBySqliteReader(readerReferences);
                        referenceCollection.Add((T)referenceObject);
                    }
            }
        }

        #endregion
    }
}
