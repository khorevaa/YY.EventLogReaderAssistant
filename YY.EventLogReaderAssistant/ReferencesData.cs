using System;
using System.Collections.Generic;
using YY.EventLogReaderAssistant.Helpers;
using YY.EventLogReaderAssistant.Models;

namespace YY.EventLogReaderAssistant
{
    [Serializable]
    public class ReferencesData
    {
        #region Constructor

        public ReferencesData()
        {
            FillDefaultReferences();

            _applications = new List<Applications>();
            _computers = new List<Computers>();
            _metadata = new List<Metadata>();
            _events = new List<Events>();
            _primaryPorts = new List<PrimaryPorts>();
            _secondaryPorts = new List<SecondaryPorts>();
            _users = new List<Users>();
            _workServers = new List<WorkServers>();
        }

        #endregion

        #region Private Members

        internal List<Severity> _severitiesData;
        internal List<TransactionStatus> _transactionStatusesData;
        internal List<Applications> _applications;
        internal List<Computers> _computers;
        internal List<Events> _events;
        internal List<Metadata> _metadata;
        internal List<PrimaryPorts> _primaryPorts;
        internal List<SecondaryPorts> _secondaryPorts;
        internal List<Users> _users;
        internal List<WorkServers> _workServers;

        #endregion

        #region Public Members

        public IReadOnlyList<Severity> Severities => _severitiesData;
        public IReadOnlyList<TransactionStatus> TransactionStatuses => _transactionStatusesData;
        public IReadOnlyList<Applications> Applications => _applications;
        public IReadOnlyList<Computers> Computers => _computers;
        public IReadOnlyList<Events> Events => _events;
        public IReadOnlyList<Metadata> Metadata => _metadata;
        public IReadOnlyList<PrimaryPorts> PrimaryPorts => _primaryPorts;
        public IReadOnlyList<SecondaryPorts> SecondaryPorts => _secondaryPorts;
        public IReadOnlyList<Users> Users => _users;
        public IReadOnlyList<WorkServers> WorkServers => _workServers;

        #endregion

        #region Public Methods

        public string GetReferencesHash()
        {
            return MD5HashGenerator.GetMd5Hash(this);
        }

        #endregion

        #region Private Methods

        private void FillDefaultReferences()
        {
            _severitiesData = new List<Severity>
            {
                Severity.Error,
                Severity.Information,
                Severity.Note,
                Severity.Unknown,
                Severity.Warning
            };
            _transactionStatusesData = new List<TransactionStatus>
            {
                TransactionStatus.Committed,
                TransactionStatus.NotApplicable,
                TransactionStatus.RolledBack,
                TransactionStatus.Unfinished,
                TransactionStatus.Unknown
            };
        }

        #endregion
    }
}
