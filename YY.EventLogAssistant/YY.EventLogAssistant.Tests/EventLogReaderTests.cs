using System.IO;
using Xunit;

namespace YY.EventLogReaderAssistant.Tests
{
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

        #endregion
    }
}
