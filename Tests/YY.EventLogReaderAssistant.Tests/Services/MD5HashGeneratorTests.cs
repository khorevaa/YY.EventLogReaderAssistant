using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using YY.EventLogReaderAssistant.Models;
using YY.EventLogReaderAssistant;
using System.IO;
using System.Linq;

namespace YY.EventLogReaderAssistant.Services.Tests
{
    public class MD5HashGeneratorTests
    {
        #region Private Member Variables

        private readonly string sampleDataDirectory;
        private readonly string sampleDatabaseFileLGF;
        private readonly string sampleDatabaseFileLGD;
        private readonly string sampleDatabaseFileLGD_ReadRefferences_IfChanged;

        #endregion

        #region Constructor

        public MD5HashGeneratorTests()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            sampleDataDirectory = Path.Combine(currentDirectory, "SampleData");
            sampleDatabaseFileLGF = Path.Combine(sampleDataDirectory, "LGFFormatEventLog", "1Cv8.lgf");
            sampleDatabaseFileLGD = Path.Combine(sampleDataDirectory, "SQLiteFormatEventLog", "1Cv8.lgd");
            sampleDatabaseFileLGD_ReadRefferences_IfChanged = Path.Combine(
                sampleDataDirectory, "SQLiteFormatEventLog", "1Cv8_ReadRefferences_IfChanged_Test.lgd");
        }

        #endregion

        #region Public Methods

        [Fact]
        public void GetMD5Hash_Test()
        {
            ReferencesData data1;
            ReferencesData data2;

            using(EventLogReader reader = EventLogReader.CreateReader(sampleDatabaseFileLGF))
            {
                data1 = ReferencesData.CreateromReader(reader);
            }
            using (EventLogReader reader = EventLogReader.CreateReader(sampleDatabaseFileLGF))
            {
                data2 = ReferencesData.CreateromReader(reader);
            }

            string hashMD5_1 = MD5HashGenerator.GetMD5Hash<ReferencesData>(data1);
            string hashMD5_2 = MD5HashGenerator.GetMD5Hash<ReferencesData>(data2);

            Assert.Equal(hashMD5_1, hashMD5_2);

        }

        #endregion

        #region Service

        [Serializable]
        private class ReferencesData
        {
            public static ReferencesData CreateromReader(EventLogReader reader)
            {
                List<Severity> severities = new List<Severity>();
                severities.Add(Severity.Error);
                severities.Add(Severity.Information);
                severities.Add(Severity.Note);
                severities.Add(Severity.Unknown);
                severities.Add(Severity.Warning);

                List<TransactionStatus> transactionStatuses = new List<TransactionStatus>();
                transactionStatuses.Add(TransactionStatus.Committed);
                transactionStatuses.Add(TransactionStatus.NotApplicable);
                transactionStatuses.Add(TransactionStatus.RolledBack);
                transactionStatuses.Add(TransactionStatus.Unfinished);
                transactionStatuses.Add(TransactionStatus.Unknown);

                ReferencesData referenceData;

                referenceData = new ReferencesData()
                {
                    Applications = reader.Applications.ToList().AsReadOnly(),
                    Computers = reader.Computers.ToList().AsReadOnly(),
                    Events = reader.Events.ToList().AsReadOnly(),
                    Metadata = reader.Metadata.ToList().AsReadOnly(),
                    PrimaryPorts = reader.PrimaryPorts.ToList().AsReadOnly(),
                    SecondaryPorts = reader.SecondaryPorts.ToList().AsReadOnly(),
                    Users = reader.Users.ToList().AsReadOnly(),
                    WorkServers = reader.WorkServers.ToList().AsReadOnly(),
                    Severities = severities.ToList().AsReadOnly(),
                    TransactionStatuses = transactionStatuses.ToList().AsReadOnly()
                };

                return referenceData;
            }

            public IReadOnlyList<Applications> Applications;
            public IReadOnlyList<Computers> Computers;
            public IReadOnlyList<Events> Events;
            public IReadOnlyList<Metadata> Metadata;
            public IReadOnlyList<PrimaryPorts> PrimaryPorts;
            public IReadOnlyList<SecondaryPorts> SecondaryPorts;
            public IReadOnlyList<Severity> Severities;
            public IReadOnlyList<TransactionStatus> TransactionStatuses;
            public IReadOnlyList<Users> Users;
            public IReadOnlyList<WorkServers> WorkServers;
        }

        #endregion
    }
}
