using System;
using System.IO;
using Xunit;

namespace YY.EventLogAssistant.Services.Tests
{
    public class StreamReaderExtensionsTests
    {
        #region Private Member Variables

        private string sampleDataDirectory;
        private string sampleDatabaseFile;

        #endregion

        #region Constructor

        public StreamReaderExtensionsTests()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            sampleDataDirectory = Path.Combine(currentDirectory, "SampleData");
            sampleDatabaseFile = Path.Combine(sampleDataDirectory, "LGFFormatEventLog", "1Cv8.lgf");
        }

        #endregion

        #region Public Methods

        [Fact]
        public void GetAndSetPosition_Test()
        {
            string checkString = string.Empty;
            string resultString = string.Empty;
            long position = 0;

            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                reader.ReadLine();
                position = reader.GetPosition();
                checkString = reader.ReadLine();
            }

            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                reader.SetPosition(position);
                resultString = reader.ReadLine();
            }

            Assert.Equal(checkString, resultString);
        }

        [Fact]
        public void SkipLine_Test()
        {
            string checkString = string.Empty;
            string resultString = string.Empty;

            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                reader.SkipLine(10);
                checkString = reader.ReadLine();
            }

            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                reader.SkipLine(10);
                resultString = reader.ReadLine();
            }

            Assert.Equal(checkString, resultString);
        }

        #endregion
    }
}
