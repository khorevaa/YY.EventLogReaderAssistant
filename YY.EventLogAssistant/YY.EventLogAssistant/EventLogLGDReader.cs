using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using YY.EventLogAssistant.Models;
using YY.EventLogAssistant.Services;

namespace YY.EventLogAssistant
{
    internal sealed class EventLogLGDReader : EventLogReader    
    {
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
        private List<EventLogRowData> _readBuffer;
        private long _lastRowId;
        private const int _readBufferSize = 10000;
        private long _lastRowNumberFromBuffer;
        private long _eventCount = -1;

        internal  EventLogLGDReader() : base() { }
        internal EventLogLGDReader(string logFilePath) : base(logFilePath) 
        {            
            _readBuffer = new List<EventLogRowData>();
            _lastRowId = 0;
            _lastRowNumberFromBuffer = 0;
        }

        public override bool Read(out EventLogRowData rowData)
        {
            try
            {
                BeforeReadFileEventArgs beforeReadFileArgs = new BeforeReadFileEventArgs(_logFilePath);
                if (_eventCount < 0)
                    RaiseBeforeReadFile(beforeReadFileArgs);

                if (beforeReadFileArgs.Cancel)
                {
                    rowData = null;
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
                                        EventLogRowData bufferRowData = new EventLogRowData();
                                        bufferRowData.RowID = reader.GetInt64OrDefault(0);
                                        bufferRowData.Period = reader.GetInt64OrDefault(1).ToDateTimeFormat();
                                        bufferRowData.ConnectId = reader.GetInt64OrDefault(2);
                                        bufferRowData.Session = reader.GetInt64OrDefault(3);
                                        bufferRowData.TransactionStatus = (TransactionStatus)reader.GetInt64OrDefault(4);
                                        bufferRowData.TransactionDate = reader.GetInt64OrDefault(5).ToNullableDateTimeELFormat();
                                        bufferRowData.TransactionId = reader.GetInt64OrDefault(6);
                                        bufferRowData.Severity = (Severity)reader.GetInt64OrDefault(15);
                                        bufferRowData.Comment = reader.GetStringOrDefault(16);
                                        bufferRowData.Data = reader.GetStringOrDefault(17).FromWIN1251ToUTF8();
                                        bufferRowData.DataPresentation = reader.GetStringOrDefault(18);
                                        bufferRowData.User = _users.Where(i => i.Code == reader.GetInt64OrDefault(7)).FirstOrDefault();
                                        bufferRowData.Computer = _computers.Where(i => i.Code == reader.GetInt64OrDefault(8)).FirstOrDefault();
                                        bufferRowData.Application = _applications.Where(i => i.Code == reader.GetInt64OrDefault(9)).FirstOrDefault();
                                        bufferRowData.Event = _events.Where(i => i.Code == reader.GetInt64OrDefault(10)).FirstOrDefault();
                                        bufferRowData.PrimaryPort = _primaryPorts.Where(i => i.Code == reader.GetInt64OrDefault(11)).FirstOrDefault();
                                        bufferRowData.SecondaryPort = _secondaryPorts.Where(i => i.Code == reader.GetInt64OrDefault(12)).FirstOrDefault();
                                        bufferRowData.WorkServer = _workServers.Where(i => i.Code == reader.GetInt64OrDefault(13)).FirstOrDefault();
                                        bufferRowData.Metadata = _metadata.Where(i => i.Code == reader.GetInt64OrDefault(18)).FirstOrDefault();

                                        _readBuffer.Add(bufferRowData);
                                    }
                                    catch (Exception ex)
                                    {
                                        RaiseOnError(new OnErrorEventArgs(ex, reader.GetRowAsString(), false));
                                        rowData = null;
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
                    rowData = null;
                    return false;
                }

                RaiseBeforeRead(new BeforeReadEventArgs(null, _eventCount));

                rowData = _readBuffer
                    .Where(bufRow => bufRow.RowID > _lastRowId)
                    .First();
                _lastRowNumberFromBuffer = _lastRowNumberFromBuffer + 1;
                _lastRowId = rowData.RowID;

                RaiseAfterRead(new AfterReadEventArgs(rowData, _eventCount));

                return true;
            }
            catch(Exception ex)
            {
                RaiseOnError(new OnErrorEventArgs(ex, null, true));
                rowData = null;
                return false;
            }
        }

        public override bool GoToEvent(long eventNumber)
        {
            Reset();

            long eventCount = Count();
            if (eventCount >= eventNumber)
            {
                using (_connection = new SQLiteConnection(ConnectionString))
                {
                    _connection.Open();

                    string queryText = String.Format(
                         "Select\n" +
                         "    el.RowId,\n" +
                         "From\n" +
                         "    EventLog el\n" +
                         "Where RowID > {0}\n" +
                         "Order By rowID\n" +
                         "Limit 1 OFFSET {1}\n", _lastRowId, eventNumber);

                    using (SQLiteCommand cmd = new SQLiteCommand(queryText, _connection))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _lastRowId = reader.GetInt64OrDefault(0);
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

                    SQLiteCommand cmd = new SQLiteCommand(_connection);
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = queryText;
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

        protected override void ReadEventLogReferences()
        {
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
            }
        }
    }
}
