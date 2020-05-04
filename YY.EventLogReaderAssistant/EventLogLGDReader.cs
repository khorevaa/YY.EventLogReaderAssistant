using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using YY.EventLogReaderAssistant.Models;
using YY.EventLogReaderAssistant.Services;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant
{
    internal sealed class EventLogLGDReader : EventLogReader    
    {
        #region Private Member Variables

        private string _connectionString;
        private string ConnectionString 
        { 
            get 
            { 
                if(_connectionString == null)
                    _connectionString = SQLiteExtensions.GetConnectionString(_logFilePath);
                return _connectionString;
            } 
        }
        private SQLiteConnection _connection;
        private readonly List<RowData> _readBuffer;
        private long _lastRowId;
        private const int _readBufferSize = 1000;
        private long _lastRowNumberFromBuffer;
        private long _eventCount = -1;

        #endregion

        #region Constructor

        internal EventLogLGDReader() : base() { }
        internal EventLogLGDReader(string logFilePath) : base(logFilePath) 
        {            
            _readBuffer = new List<RowData>();
            _lastRowId = 0;
            _lastRowNumberFromBuffer = 0;
        }

        #endregion

        #region Public Methods

        public override bool Read()
        {
            try
            {
                BeforeReadFileEventArgs beforeReadFileArgs = new BeforeReadFileEventArgs(_logFilePath);
                if (_eventCount < 0)
                    RaiseBeforeReadFile(beforeReadFileArgs);

                if (beforeReadFileArgs.Cancel)
                {
                    _currentRow = null;
                    return false;
                }

                #region bufferedRead

                if (_lastRowNumberFromBuffer == 0
                    || _lastRowNumberFromBuffer >= _readBufferSize)
                {
                    _readBuffer.Clear();
                    _lastRowNumberFromBuffer = 0;

                    using (_connection = new SQLiteConnection(ConnectionString))
                    {
                        _connection.Open();

                        string queryText = String.Format(
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
                         "Limit {1}\n", _lastRowId, _readBufferSize);

                        using (SQLiteCommand cmd = new SQLiteCommand(queryText, _connection))
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

                                        _readBuffer.Add(new RowData
                                        {
                                            RowID = reader.GetInt64OrDefault(0),
                                            Period = rowPeriod,
                                            ConnectId = reader.GetInt64OrDefault(2),
                                            Session = reader.GetInt64OrDefault(3),
                                            TransactionStatus = GetTransactionStatus(reader.GetInt64OrDefault(4)),
                                            TransactionDate = reader.GetInt64OrDefault(5).ToNullableDateTimeELFormat(),
                                            TransactionId = reader.GetInt64OrDefault(6),
                                            User = GetUserByCode(reader.GetInt64OrDefault(7)),
                                            Computer = GetComputerByCode(reader.GetInt64OrDefault(8)),
                                            Application = GetApplicationByCode(reader.GetInt64OrDefault(9)),
                                            Event = GetEventByCode(reader.GetInt64OrDefault(10)),
                                            PrimaryPort = GetPrimaryPortByCode(reader.GetInt64OrDefault(11)),
                                            SecondaryPort = GetSecondaryPortByCode(reader.GetInt64OrDefault(12)),
                                            WorkServer = GetWorkServerByCode(reader.GetInt64OrDefault(13)),
                                            Severity = GetSeverityByCode(reader.GetInt64OrDefault(14)),
                                            Comment = reader.GetStringOrDefault(15),
                                            Data = reader.GetStringOrDefault(16).FromWin1251ToUTF8(),
                                            DataPresentation = reader.GetStringOrDefault(17),
                                            Metadata = GetMetadataByCode(reader.GetInt64OrDefault(18))
                                    });
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
                    .Where(bufRow => bufRow.RowID > _lastRowId)
                    .First();
                _lastRowNumberFromBuffer += 1;
                _lastRowId = _currentRow.RowID;

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

            long eventCount = Count();
            if (eventCount >= eventNumber)
            {
                long eventNumberToSkip = eventNumber - 1;
                if (eventNumberToSkip <= 0)
                {
                    _lastRowId = 0;
                    _currentFileEventNumber = 0;
                    return true;
                }

                using (_connection = new SQLiteConnection(ConnectionString))
                {
                    _connection.Open();

                    string queryText = String.Format(
                         "Select\n" +
                         "    el.RowId\n" +
                         "From\n" +
                         "    EventLog el\n" +
                         "Where RowID > {0}\n" +
                         "Order By rowID\n" +
                         "Limit 1 OFFSET {1}\n", _lastRowId, eventNumberToSkip);

                    using (SQLiteCommand cmd = new SQLiteCommand(queryText, _connection))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _lastRowId = reader.GetInt64OrDefault(0);
                                _currentFileEventNumber = eventNumber;
                                return true;
                            }
                        }
                    }
                }               
            }            

            return false;
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
            System.Data.SQLite.SQLiteConnection.ClearAllPools();
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

            System.Data.SQLite.SQLiteConnection.ClearAllPools();
            if(_connection != null)
            {
                _connection.Dispose();
            }
            _readBuffer.Clear();
        }

        #endregion

        #region Private Methods

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

            using (_connection = new SQLiteConnection(ConnectionString))
            {
                _connection.Open();

                using (SQLiteCommand cmdUserCodes = new SQLiteCommand("Select Code, Name, UUID From UserCodes", _connection))
                {
                    using (SQLiteDataReader readerUserCodes = cmdUserCodes.ExecuteReader())
                        while (readerUserCodes.Read())
                        {
                            _users.Add(new Users()
                            {
                                Code = readerUserCodes.GetInt64(0),
                                Name = readerUserCodes.GetString(1),
                                Uuid = readerUserCodes.GetString(2).ToGuid()
                            });
                        }
                }

                using (SQLiteCommand cmdComputerCodes = new SQLiteCommand("Select Code, Name From ComputerCodes", _connection))
                {
                    using (SQLiteDataReader readerComputerCodes = cmdComputerCodes.ExecuteReader())
                        while (readerComputerCodes.Read())
                        {
                            _computers.Add(new Computers()
                            {
                                Code = readerComputerCodes.GetInt64(0),
                                Name = readerComputerCodes.GetString(1)
                            });
                        }
                }

                using (SQLiteCommand cmdEventCodes = new SQLiteCommand("Select Code, Name From EventCodes", _connection))
                {
                    using (SQLiteDataReader readerEventCodes = cmdEventCodes.ExecuteReader())
                        while (readerEventCodes.Read())
                        {
                            _events.Add(new Events()
                            {
                                Code = readerEventCodes.GetInt64(0),
                                Name = readerEventCodes.GetString(1)
                            });
                        }
                }

                using (SQLiteCommand cmdMetadataCodes = new SQLiteCommand("Select Code, Name, UUID From MetadataCodes", _connection))
                {
                    using (SQLiteDataReader readerMetadataCodes = cmdMetadataCodes.ExecuteReader())
                        while (readerMetadataCodes.Read())
                        {
                            _metadata.Add(new Metadata()
                            {
                                Code = readerMetadataCodes.GetInt64(0),
                                Name = readerMetadataCodes.GetString(1),
                                Uuid = readerMetadataCodes.GetString(2).ToGuid()
                            });
                        }
                }

                using (SQLiteCommand cmdAppCodes = new SQLiteCommand("Select Code, Name From AppCodes", _connection))
                {
                    using (SQLiteDataReader readerAppCodes = cmdAppCodes.ExecuteReader())
                        while (readerAppCodes.Read())
                        {
                            _applications.Add(new Applications()
                            {
                                Code = readerAppCodes.GetInt64(0),
                                Name = readerAppCodes.GetString(1)
                            });
                        }
                }

                using (SQLiteCommand cmdWorkServerCodes = new SQLiteCommand("Select Code, Name From WorkServerCodes", _connection))
                {
                    using (SQLiteDataReader readerWorkServerCodes = cmdWorkServerCodes.ExecuteReader())
                        while (readerWorkServerCodes.Read())
                        {
                            _workServers.Add(new WorkServers()
                            {
                                Code = readerWorkServerCodes.GetInt64(0),
                                Name = readerWorkServerCodes.GetString(1)
                            });
                        }
                }

                using (SQLiteCommand cmdPrimaryPortCodes = new SQLiteCommand("Select Code, Name From PrimaryPortCodes", _connection))
                {
                    using (SQLiteDataReader readerPrimaryPortCodes = cmdPrimaryPortCodes.ExecuteReader())
                        while (readerPrimaryPortCodes.Read())
                        {
                            _primaryPorts.Add(new PrimaryPorts()
                            {
                                Code = readerPrimaryPortCodes.GetInt64(0),
                                Name = readerPrimaryPortCodes.GetString(1)
                            });
                        }
                }

                using (SQLiteCommand cmdSecondaryPortCodes = new SQLiteCommand("Select Code, Name From SecondaryPortCodes", _connection))
                {
                    using (SQLiteDataReader readerSecondaryPortCodes = cmdSecondaryPortCodes.ExecuteReader())
                        while (readerSecondaryPortCodes.Read())
                        {
                            _secondaryPorts.Add(new SecondaryPorts()
                            {
                                Code = readerSecondaryPortCodes.GetInt64(0),
                                Name = readerSecondaryPortCodes.GetString(1)
                            });
                        }
                }

                _referencesReadDate = beginReadReferences;
            }
        }

        #endregion
    }
}
