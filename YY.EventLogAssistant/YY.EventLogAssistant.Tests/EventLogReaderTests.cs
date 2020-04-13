using System.IO;
using Xunit;

namespace YY.EventLogAssistant.Tests
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
        public void ReadEventLogEvents_OldFormat_LGF_Test()
        {
            long countLogFiles = 0;
            long countRecords = 0;
            long countRecordsStepByStep = 0;
            long countRecordsStepByStepAfterReset = 0;
            long countRecordsStepByStepAfterSetPosition = 0;

            using (EventLogReader reader = EventLogReader.CreateReader(sampleDatabaseFileLGF))
            {
                countRecords = reader.Count();

                while(reader.Read())
                {
                    countRecordsStepByStep += 1;
                }

                reader.Reset();
                while (reader.Read())
                {
                    countRecordsStepByStepAfterReset += 1;
                }

                reader.Reset();
                EventLogPosition position = reader.GetCurrentPosition();
                while (reader.Read());
                reader.SetCurrentPosition(position);
                while (reader.Read())
                {
                    countRecordsStepByStepAfterSetPosition += 1;
                }

                reader.Reset();
                EventLogLGFReader readerLGF = (EventLogLGFReader)reader;
                while (readerLGF.CurrentFile != null)
                {
                    reader.NextFile();
                    countLogFiles += 1;
                }
            }

            Assert.NotEqual(0, countLogFiles);
            Assert.NotEqual(0, countRecords);
            Assert.NotEqual(0, countRecordsStepByStep);
            Assert.NotEqual(0, countRecordsStepByStepAfterReset);
            Assert.NotEqual(0, countRecordsStepByStepAfterSetPosition);
            Assert.Equal(countRecords, countRecordsStepByStep);
            Assert.Equal(countRecords, countRecordsStepByStepAfterReset);
            Assert.Equal(countRecords, countRecordsStepByStepAfterSetPosition);
        }

        [Fact]
        public void ReadEventLogEvents_NewFormat_LGD_Test()
        {
            long countRecords = 0;
            long countRecordsStepByStep = 0;
            long countRecordsStepByStepAfterReset = 0;
            long countRecordsStepByStepAfterSetPosition = 0;

            using (EventLogReader reader = EventLogReader.CreateReader(sampleDatabaseFileLGD))
            {
                countRecords = reader.Count();

                while (reader.Read())
                {
                    countRecordsStepByStep += 1;
                }

                reader.Reset();
                while (reader.Read())
                {
                    countRecordsStepByStepAfterReset += 1;
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
            Assert.NotEqual(0, countRecordsStepByStepAfterReset);
            Assert.NotEqual(0, countRecordsStepByStepAfterSetPosition);
            Assert.Equal(countRecords, countRecordsStepByStep);
            Assert.Equal(countRecords, countRecordsStepByStepAfterReset);
            Assert.Equal(countRecords, countRecordsStepByStepAfterSetPosition);
        }

        #endregion
    }
}
