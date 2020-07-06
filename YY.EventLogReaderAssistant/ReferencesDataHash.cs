using System;
using System.Collections.Generic;
using System.Linq;
using YY.EventLogReaderAssistant.Models;

namespace YY.EventLogReaderAssistant
{
    [Serializable]
    internal class ReferencesDataHash
    {
        #region Public Static Methods

        public static ReferencesDataHash CreateFromReader(EventLogReader reader)
        {
            var referenceData = new ReferencesDataHash()
            {
                Applications = reader.Applications.ToList().AsReadOnly(),
                Computers = reader.Computers.ToList().AsReadOnly(),
                Events = reader.Events.ToList().AsReadOnly(),
                Metadata = reader.Metadata.ToList().AsReadOnly(),
                PrimaryPorts = reader.PrimaryPorts.ToList().AsReadOnly(),
                SecondaryPorts = reader.SecondaryPorts.ToList().AsReadOnly(),
                Users = reader.Users.ToList().AsReadOnly(),
                WorkServers = reader.WorkServers.ToList().AsReadOnly()
            };

            return referenceData;
        }

        #endregion

        #region Constructor

        public ReferencesDataHash()
        {
            Severities = new List<Severity>
                {
                    Severity.Error,
                    Severity.Information,
                    Severity.Note,
                    Severity.Unknown,
                    Severity.Warning
                }.AsReadOnly();

            TransactionStatuses = new List<TransactionStatus>
                {
                    TransactionStatus.Committed,
                    TransactionStatus.NotApplicable,
                    TransactionStatus.RolledBack,
                    TransactionStatus.Unfinished,
                    TransactionStatus.Unknown
                }.AsReadOnly();
        }

        #endregion

        #region Public Members

        public IReadOnlyList<Applications> Applications { get; set; }
        public IReadOnlyList<Computers> Computers { get; set; }
        public IReadOnlyList<Events> Events { get; set; }
        public IReadOnlyList<Metadata> Metadata { get; set; }
        public IReadOnlyList<PrimaryPorts> PrimaryPorts { get; set; }
        public IReadOnlyList<SecondaryPorts> SecondaryPorts { get; set; }
        public IReadOnlyList<Severity> Severities { get; set; }
        public IReadOnlyList<TransactionStatus> TransactionStatuses { get; set; }
        public IReadOnlyList<Users> Users { get; set; }
        public IReadOnlyList<WorkServers> WorkServers { get; set; }

        #endregion
    }
}
