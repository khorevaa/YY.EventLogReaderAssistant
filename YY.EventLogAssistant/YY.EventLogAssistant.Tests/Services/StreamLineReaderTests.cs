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
        private string[] sampleFilesLGP;

        #endregion

        #region Constructor

        public StreamLineReaderTests()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            sampleDataDirectory = Path.Combine(currentDirectory, "SampleData");
            sampleDatabaseFile = Path.Combine(sampleDataDirectory, "LGFFormatEventLog", "1Cv8.lgf");

            string dataDirectoryLGF = Path.Combine(sampleDataDirectory, "LGFFormatEventLog");
            sampleFilesLGP = Directory.GetFiles(dataDirectoryLGF, "*.lgp");
        }

        #endregion

        #region Public Methods

        [Fact]
        public void GoToLine_Test()
        {
            string lineContent = string.Empty;
            using (StreamReader reader = new StreamReader(sampleDatabaseFile))
            {
                using(StreamLineReader lineReader = new StreamLineReader(reader.BaseStream, reader.CurrentEncoding))
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
                using (StreamLineReader lineReader = new StreamLineReader(reader.BaseStream, reader.CurrentEncoding))
                {
                    lineCounterLibrary = lineReader.GetCount();
                }
            }

            Assert.Equal(lineCounterNative, lineCounterLibrary);
        }

        [Fact]
        public void ReadLine_LGP_Test()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding utf8 = Encoding.UTF8;

            List<string> correctLines = new List<string>();
            using (var reader = new StreamReader(sampleDatabaseFile, utf8))
            {
                string currentLine = reader.ReadLine();
                do
                {
                    correctLines.Add(currentLine);
                    currentLine = reader.ReadLine();
                } while (currentLine != null);
            }

            List<string> resultLines = new List<string>();
            using (StreamReader reader = new StreamReader(sampleDatabaseFile, utf8))
            {
                using (StreamLineReader lineReader = new StreamLineReader(reader.BaseStream, reader.CurrentEncoding))
                {
                    string currentLine = lineReader.ReadLine();
                    do
                    {
                        resultLines.Add(currentLine);
                        currentLine = lineReader.ReadLine();
                    } while (currentLine != null);
                }
            }

            Assert.Equal(correctLines, resultLines);
        }

        [Fact]
        public void ReadLine_LGFs_Test()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding utf8 = Encoding.UTF8;

            List<string> correctLines = new List<string>();
            List<string> resultLines = new List<string>();

            foreach (string fileLGP in sampleFilesLGP)
            {               
                using (var reader = new StreamReader(fileLGP, utf8))
                {
                    string currentLine = reader.ReadLine();
                    while (currentLine != null)
                    {
                        correctLines.Add(currentLine);
                        currentLine = reader.ReadLine();
                    }
                }
                
                using (StreamReader reader = new StreamReader(fileLGP, utf8))
                {
                    using (StreamLineReader lineReader = new StreamLineReader(reader.BaseStream, reader.CurrentEncoding))
                    {
                        string currentLine = lineReader.ReadLine();
                        while (currentLine != null)
                        {
                            resultLines.Add(currentLine);

                            int cnt = resultLines.Count - 1;
                            if (correctLines[cnt] != resultLines[cnt])
                            {
                                int fff = 1;
                            } else if(cnt == 173)
                            {
                                int sdafdfa = 1;
                            }

                            currentLine = lineReader.ReadLine();
                        }
                    }
                }
            }

            //for (int i = 0; i < correctLines.Count; i++)
            //{
            //    if(correctLines[i] != resultLines[i])
            //    {
            //        int fff = 1;
            //    }
            //}

            Assert.Equal(correctLines, resultLines);
        }

        #endregion
    }
}
