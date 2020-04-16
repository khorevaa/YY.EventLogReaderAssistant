using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Xunit;

namespace YY.EventLogReaderAssistant.Services.Tests
{
    [Collection("SQLite Event Log Test")]
    public class SQLiteExtensionsTests
    {
        #region Private Member Variables

        private readonly string sampleDataDirectory;
        private readonly string sampleDatabaseFile;

        #endregion

        #region Constructor

        public SQLiteExtensionsTests()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            sampleDataDirectory = Path.Combine(currentDirectory, "SampleData");
            sampleDatabaseFile = Path.Combine(sampleDataDirectory, "SQLiteFormatEventLog", "1Cv8.lgd");
        }

        #endregion

        #region Public Methods

        [Fact]
        public void GetStringOrDefault_Test()
        {
            string connectionString = SQLiteExtensions.GetConnectionString(sampleDatabaseFile);

            string queryText = string.Format(
                "Select\n" +
                "    \"Hello, world!\" AS DataPresentation,\n" +
                "    null AS DataPresentationEmpty\n" +
                "From\n" +
                "    EventLog el\n" +
                "Where RowID > {0}\n" +
                "Order By rowID\n" +
                "Limit {1}\n", 0, 1);

            string DataPresentation = null;
            string DataPresentationEmpty = null;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using SQLiteCommand command = new SQLiteCommand(queryText, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    DataPresentation = SQLiteExtensions.GetStringOrDefault(reader, 0);
                    DataPresentationEmpty = SQLiteExtensions.GetStringOrDefault(reader, 1);
                }
            }

            Assert.Equal("Hello, world!", DataPresentation);
            Assert.Equal(String.Empty, DataPresentationEmpty);
        }

        [Fact]
        public void GetInt64OrDefault_Test()
        {
            string connectionString = SQLiteExtensions.GetConnectionString(sampleDatabaseFile);

            string queryText = String.Format(
                "Select\n" +
                "    null AS ConnectId,\n" +
                "    777 AS Session\n" +
                "From\n" +
                "    EventLog el\n" +
                "Where RowID > {0}\n" +
                "Order By rowID\n" +
                "Limit {1}\n", 0, 1);

            long connectionId = 0;
            long sessionId = 0;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(queryText, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            connectionId = SQLiteExtensions.GetInt64OrDefault(reader, 0);
                            sessionId = SQLiteExtensions.GetInt64OrDefault(reader, 1);
                        }
                    }
                }
            }
            Assert.Equal(0, connectionId);
            Assert.Equal(777, sessionId);
        }

        [Fact]
        public void GetRowAsString_Test()
        {
            string connectionString = SQLiteExtensions.GetConnectionString(sampleDatabaseFile);

            string queryText = String.Format(
                "Select\n" +
                "    el.RowId AS RowId,\n" +
                "    el.Date AS Date,\n" +
                "    el.ConnectId AS ConnectId,\n" +
                "    el.Session AS Session\n" +
                "From\n" +
                "    EventLog el\n" +
                "Where RowID > {0}\n" +
                "Order By rowID\n" +
                "Limit {1}\n", 0, 1);

            string rowAsString = String.Empty;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using SQLiteCommand command = new SQLiteCommand(queryText, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    rowAsString = reader.GetRowAsString();
                }
            }
            int countLines = rowAsString.Split('\n').Where(str => str != string.Empty).Count();

            Assert.NotEqual(String.Empty, rowAsString);
            Assert.Equal(4, countLines);
            Assert.Contains("RowId", rowAsString);
            Assert.Contains("Date", rowAsString);
            Assert.Contains("ConnectId", rowAsString);
            Assert.Contains("Session", rowAsString);
        }

        [Fact]
        public void GetConnectionString_Test()
        {            
            string connectionString = SQLiteExtensions.GetConnectionString(sampleDatabaseFile);

            string queryText = String.Format(
                    "Select\n" +
                    "    COUNT(el.RowId) CNT\n" +
                    "From\n" +
                    "    EventLog el\n");

            long rowsCount = 0;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using SQLiteCommand command = new SQLiteCommand(queryText, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    object rowsCountObject = reader.GetValue(0);
                    if (rowsCountObject is long)
                    {
                        rowsCount = Convert.ToInt64(rowsCountObject);
                    }
                }
            }

            Assert.NotEqual(0, rowsCount);
        }

        #endregion
    }
}
