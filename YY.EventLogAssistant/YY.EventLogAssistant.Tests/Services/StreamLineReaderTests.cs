using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace YY.EventLogAssistant.Services.Tests
{
    public class StreamLineReaderTests
    {
        #region Private Member Variables

        private string sampleDataDirectory;
        private string sampleDatabaseFile;

        #endregion

        #region Constructor

        public StreamLineReaderTests()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            sampleDataDirectory = Path.Combine(currentDirectory, "SampleData");
            sampleDatabaseFile = Path.Combine(sampleDataDirectory, "LGFFormatEventLog", "1Cv8.lgf");
        }

        #endregion

        #region Public Methods

        [Fact]
        public void GoToLine_Test()
        {
            string lineContent = string.Empty;
            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                using(StreamLineReader lineReader = new StreamLineReader(reader.BaseStream))
                {
                    if(lineReader.GoToLine(1))
                    {
                        lineContent = lineReader.ReadLine();
                    }
                }
            }

            Guid eventLogGuid = Guid.Empty;
            Guid.TryParse(lineContent, out eventLogGuid);

            Assert.NotEqual(Guid.Empty, eventLogGuid);
        }

        [Fact]
        public void GetCount_Test()
        {
            long lineCounterNative = 0;
            using (var reader = new StreamReader(sampleDatabaseFile))
            {
                while (reader.ReadLine() != null)
                {
                    lineCounterNative++;
                }
            }

            long lineCounterLibrary = 0;
            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                using (StreamLineReader lineReader = new StreamLineReader(reader.BaseStream))
                {
                    lineCounterLibrary = lineReader.GetCount();
                }
            }

            Assert.Equal(lineCounterNative, lineCounterLibrary);
        }

        [Fact]
        public void ReadLine_Test()
        {
            string correctFirstLine = string.Empty;
            using (var reader = new StreamReader(sampleDatabaseFile))
            {
                correctFirstLine = reader.ReadLine();                
            }

            string resultFirstLine = string.Empty;
            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                using (StreamLineReader lineReader = new StreamLineReader(reader.BaseStream))
                {
                    resultFirstLine = lineReader.ReadLine();
                }
            }

            Assert.Equal(correctFirstLine, resultFirstLine);
        }

        #endregion
    }
}
