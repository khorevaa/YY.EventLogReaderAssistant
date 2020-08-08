using System;
using System.Collections.Generic;
using System.Linq;
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

        private Dictionary<long, Applications> _applicationsDictionary;
        private Dictionary<long, Computers> _computersDictionary;
        private Dictionary<long, Events> _eventsDictionary;
        private Dictionary<long, Metadata> _metadataDictionary;
        private Dictionary<long, PrimaryPorts> _primaryPortsDictionary;
        private Dictionary<long, SecondaryPorts> _secondaryPortsDictionary;
        private Dictionary<long, Users> _usersDictionary;
        private Dictionary<long, WorkServers> _workServersDictionary;
        private Dictionary<long, Severity> _severityDictionary;
        private Dictionary<long, TransactionStatus> _transactionStatusDictionary;

        #endregion

        #region Public Members

        public IReadOnlyDictionary<long, Applications> ApplicationsDictionary =>  _applicationsDictionary ?? (_applicationsDictionary = ConvertListToDictionary(_applications));
        public IReadOnlyDictionary<long, Computers> ComputersDictionary => _computersDictionary ?? (_computersDictionary = ConvertListToDictionary(_computers));
        public IReadOnlyDictionary<long, Events> EventsDictionary => _eventsDictionary ?? (_eventsDictionary = ConvertListToDictionary(_events));
        public IReadOnlyDictionary<long, Metadata> MetadataDictionary => _metadataDictionary ?? (_metadataDictionary = ConvertListToDictionary(_metadata));
        public IReadOnlyDictionary<long, PrimaryPorts> PrimaryPortsDictionary => _primaryPortsDictionary ?? (_primaryPortsDictionary = ConvertListToDictionary(_primaryPorts));
        public IReadOnlyDictionary<long, SecondaryPorts> SecondaryPortsDictionary => _secondaryPortsDictionary ?? (_secondaryPortsDictionary = ConvertListToDictionary(_secondaryPorts));
        public IReadOnlyDictionary<long, Users> UsersDictionary => _usersDictionary ?? (_usersDictionary = ConvertListToDictionary(_users));
        public IReadOnlyDictionary<long, WorkServers> WorkServersDictionary => _workServersDictionary ?? (_workServersDictionary = ConvertListToDictionary(_workServers));
        public IReadOnlyDictionary<long, Severity> Severities => _severityDictionary ?? ( _severityDictionary = EnumToDictionary<Severity>());
        public IReadOnlyDictionary<long, TransactionStatus> TransactionStatuses => _transactionStatusDictionary ?? (_transactionStatusDictionary = EnumToDictionary<TransactionStatus>());

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
        private Dictionary<long, T> ConvertListToDictionary<T>(List<T> sourceList) where T : ReferenceObject, new()
        {
            Dictionary<long, T> resultDictionary = new Dictionary<long, T>();
            if (sourceList != null)
                resultDictionary = sourceList.ToDictionary(x => x.Code, x => x);
            return resultDictionary;
        }
        private Dictionary<long, T> EnumToDictionary<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .ToDictionary(t => (long)(object)t, t => t);
        }

        #endregion
    }
}
