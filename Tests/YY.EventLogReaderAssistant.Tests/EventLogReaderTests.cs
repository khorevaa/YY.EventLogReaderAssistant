using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using Xunit;
using YY.EventLogReaderAssistant.Services;

namespace YY.EventLogReaderAssistant.Tests
{
    [CollectionDefinition("SQLite Event Log Test", DisableParallelization = true)]
    public class SQLiteEventLogTestDefinition { }

    [Collection("SQLite Event Log Test")]
    public class EventLogReaderTests
    {
        #region Private Member Variables

        private readonly string sampleDataDirectory;
        private readonly string sampleDatabaseFileLGF;
        private readonly string sampleDatabaseFileLGD;

        #endregion

        #region Constructor

        public EventLogReaderTests()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            sampleDataDirectory = Path.Combine(currentDirectory, "SampleData");
            sampleDatabaseFileLGF = Path.Combine(sampleDataDirectory, "LGFFormatEventLog", "1Cv8.lgf");
            sampleDatabaseFileLGD = Path.Combine(sampleDataDirectory, "SQLiteFormatEventLog", "1Cv8.lgd");
        }

        #endregion

        #region Public Methods

        [Fact]
        public void GetCount_NewFormat_LGD_Test()
        {
            GetCount_Test(sampleDatabaseFileLGD);
        }

        [Fact]
        public void GetCount_OldFormat_LGF_Test()
        {
            GetCount_Test(sampleDatabaseFileLGF);
        }

        [Fact]
        public void GetAndSetPosition_NewFormat_LGD_Test()
        {
            GetAndSetPosition_Test(sampleDatabaseFileLGD);
        }

        [Fact]
        public void GetAndSetPosition_OldFormat_LGF_Test()
        {
            GetAndSetPosition_Test(sampleDatabaseFileLGF);
        }

        [Fact]
        public void CountLogFiles_NewFormat_LGD_Test()
        {
            GetCountLogFiles_Test(sampleDatabaseFileLGD);
        }

        [Fact]
        public void CountLogFiles_OldFormat_LGF_Test()
        {
            GetCountLogFiles_Test(sampleDatabaseFileLGF);
        }

        [Fact]
        public void GoToEvent_NewFormat_LGD_Test()
        {
            GoToEvent_Test(sampleDatabaseFileLGD);
        }

        [Fact]
        public void GoToEvent_OldFormat_LGF_Test()
        {
            GoToEvent_Test(sampleDatabaseFileLGF);
        }

        [Fact]
        public void ReadRefferences_IfChanged_OldFormat_LGF_Test()
        {
            ReadRefferences_IfChanged_Test(sampleDatabaseFileLGF);
        }

        [Fact]
        public void ReadRefferences_IfChanged_NewFormat_LGD_Test()
        {
            ReadRefferences_IfChanged_Test(sampleDatabaseFileLGD);
        }

        #endregion

        #region Private Methods

        private void GetCount_Test(string eventLogPath)
        {
            long countRecords = 0;
            long countRecordsStepByStep = 0;            

            using (EventLogReader reader = EventLogReader.CreateReader(eventLogPath))
            {
                countRecords = reader.Count();

                while (reader.Read())
                {
                    countRecordsStepByStep += 1;
                }
            }

            Assert.NotEqual(0, countRecords);
            Assert.NotEqual(0, countRecordsStepByStep);
            Assert.Equal(countRecords, countRecordsStepByStep);
        }

        private void GetAndSetPosition_Test(string eventLogPath)
        {
            long countRecords = 0;
            long countRecordsStepByStep = 0;
            long countRecordsStepByStepAfterSetPosition = 0;

            using (EventLogReader reader = EventLogReader.CreateReader(eventLogPath))
            {
                countRecords = reader.Count();

                while (reader.Read())
                {
                    countRecordsStepByStep += 1;
                }

                reader.Reset();
                EventLogPosition position = reader.GetCurrentPosition();
                while (reader.Read()) ;
                reader.SetCurrentPosition(position);
                while (reader.Read())
                {
                    countRecordsStepByStepAfterSetPosition += 1;
                }
            }

            Assert.NotEqual(0, countRecords);
            Assert.NotEqual(0, countRecordsStepByStep);
            Assert.NotEqual(0, countRecordsStepByStepAfterSetPosition);
            Assert.Equal(countRecords, countRecordsStepByStep);
            Assert.Equal(countRecords, countRecordsStepByStepAfterSetPosition);
        }

        private void GetCountLogFiles_Test(string eventLogPath)
        {
            long countLogFiles = 0;

            using (EventLogReader reader = EventLogReader.CreateReader(eventLogPath))
            {
                reader.Reset();

                if (reader is EventLogLGFReader)
                {
                    EventLogLGFReader readerLGF = (EventLogLGFReader)reader;
                    while (readerLGF.CurrentFile != null)
                    {
                        reader.NextFile();
                        countLogFiles += 1;
                    }
                } else if(reader is EventLogLGDReader)
                {
                    countLogFiles = 1;
                }               
            }

            Assert.NotEqual(0, countLogFiles);
        }

        private void GoToEvent_Test(string eventLogPath)
        {
            string dataAfterGoEvent = string.Empty;
            string dataAfterSetPosition = string.Empty;

            using (EventLogReader reader = EventLogReader.CreateReader(eventLogPath))
            {
                reader.GoToEvent(5);
                EventLogPosition eventPosition = reader.GetCurrentPosition();
                if (reader.Read())
                    dataAfterGoEvent = reader.CurrentRow.Data;

                reader.Reset();

                reader.SetCurrentPosition(eventPosition);
                if (reader.Read())
                    dataAfterSetPosition = reader.CurrentRow.Data;
            }

            Assert.Equal(dataAfterGoEvent, dataAfterSetPosition);
        }

        private void ReadRefferences_IfChanged_Test(string eventLogPath)
        {
            DateTime lastReadReferencesDateBeforeRead = DateTime.MinValue;
            DateTime lastReadReferencesDate = DateTime.MinValue;

            using (EventLogReader reader = EventLogReader.CreateReader(eventLogPath))
            {
                lastReadReferencesDateBeforeRead = reader.ReferencesReadDate;
                Thread.Sleep(2000);

                if (reader is EventLogLGFReader)
                {
                    #region LGF

                    EventLogLGFReader lgfReader = (EventLogLGFReader)reader;
                    using (StreamWriter sw = File.AppendText(lgfReader.CurrentFile))
                    { 
                        string descriptionNewEvent = "Новое событие в процессе чтения!";
                        DateTime newLogRecordPeriod = DateTime.Now;
                        string newLogRecordPeriodAsString = newLogRecordPeriod.ToString("yyyyMMddHHmmss");
                        
                        sw.WriteLine(",");
                        sw.WriteLine($"{{{newLogRecordPeriodAsString},N,");
                        sw.WriteLine($"{{0,0}},1,1,2,2,3,N,\"{descriptionNewEvent}\",3,");
                        sw.WriteLine($"{{\"S\",\"{descriptionNewEvent}\"}},\"\",1,1,0,2,0,");
                        sw.WriteLine("{0}");
                        sw.WriteLine("}");
                    }

                    #endregion
                } else if(reader is EventLogLGDReader)
                {
                    #region LGD

                    string lgdConnectionString = SQLiteExtensions.GetConnectionString(eventLogPath, false);
                    using (SQLiteConnection connection = new SQLiteConnection(lgdConnectionString))
                    {
                        connection.Open();
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
                            "Where RowID = (SELECT MAX(RowID) from EventLog)\n");
                        using (SQLiteCommand sqliteCmd = new SQLiteCommand(queryText, connection))
                        {
                            long RowID = 0, ConnectId = 0, Session = 0,
                                TransactionStatus = 0, TransactionDate = 0, TransactionId = 0,
                                User = 0,  Computer = 0, Application = 0, Event = 0, PrimaryPort = 0,
                                SecondaryPort = 0, WorkServer = 0, Severity = 0, Metadata = 0;
                            string Comment = string.Empty, Data = string.Empty, DataPresentation = string.Empty;

                            using (SQLiteDataReader sqliteReader = sqliteCmd.ExecuteReader())
                            {
                                while (sqliteReader.Read())
                                {
                                    RowID = sqliteReader.GetInt64OrDefault(0);
                                    ConnectId = sqliteReader.GetInt64OrDefault(2);
                                    Session = sqliteReader.GetInt64OrDefault(3);
                                    TransactionStatus = sqliteReader.GetInt64OrDefault(4);
                                    TransactionDate = sqliteReader.GetInt64OrDefault(5);
                                    TransactionId = sqliteReader.GetInt64OrDefault(6);
                                    User = sqliteReader.GetInt64OrDefault(7);
                                    Computer = sqliteReader.GetInt64OrDefault(8);
                                    Application = sqliteReader.GetInt64OrDefault(9);
                                    Event = sqliteReader.GetInt64OrDefault(10);
                                    PrimaryPort = sqliteReader.GetInt64OrDefault(11);
                                    SecondaryPort = sqliteReader.GetInt64OrDefault(12);
                                    WorkServer = sqliteReader.GetInt64OrDefault(13);
                                    Severity = sqliteReader.GetInt64OrDefault(14);
                                    Comment = sqliteReader.GetStringOrDefault(15);
                                    Data = sqliteReader.GetStringOrDefault(16);
                                    DataPresentation = sqliteReader.GetStringOrDefault(17);
                                    Metadata = sqliteReader.GetInt64OrDefault(18);
                                }
                            }

                            string queryInsertLog =
                                "INSERT INTO EventLog " +
                                "(" +
                                "   RowId, " +
                                "   Date, " +
                                "   ConnectId, " +
                                "   Session, " +
                                "   TransactionStatus, " +
                                "   TransactionDate, " +
                                "   TransactionId, " +
                                "   UserCode, " +
                                "   ComputerCode, " +
                                "   appCode, " +
                                "   eventCode, " +
                                "   primaryPortCode, " +
                                "   secondaryPortCode, " +
                                "   workServerCode, " +
                                "   Severity, " +
                                "   Comment, " +
                                "   Data, " +
                                "   DataPresentation " +
                                ") " +
                                "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                            using (SQLiteCommand insertSQL = new SQLiteCommand(queryInsertLog, connection))
                            {
                                long newRowId = RowID + 1;
                                long newPeriod = DateTime.Now.ToLongDateTimeFormat();

                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, newRowId));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, newPeriod));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, ConnectId));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, Session));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, TransactionStatus));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, TransactionDate));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, TransactionId));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, User));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, Computer));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, Application));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, Event));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, PrimaryPort));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, SecondaryPort));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, WorkServer));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.Int64, Severity));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.String, Comment));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.String, Data));
                                insertSQL.Parameters.Add(new SQLiteParameter(DbType.String, DataPresentation));
                                insertSQL.ExecuteNonQuery();
                            }
                        }                        
                    }

                    #endregion
                }

                while (reader.Read());
                lastReadReferencesDate = reader.ReferencesReadDate;
            }

            Assert.NotEqual(DateTime.MinValue, lastReadReferencesDate);
            Assert.NotEqual(DateTime.MinValue, lastReadReferencesDateBeforeRead);
            Assert.True(lastReadReferencesDateBeforeRead < lastReadReferencesDate);
        }

        #endregion
    }
}
