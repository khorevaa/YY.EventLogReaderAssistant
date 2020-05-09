using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YY.EventLogReaderAssistant.Services;
using YY.EventLogReaderAssistant.Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant
{
    public abstract partial class EventLogReader : IEventLogReader, IDisposable
    {
        #region Static Methods

        public static EventLogReader CreateReader(string pathLogFile)
        {
            FileAttributes attr = File.GetAttributes(pathLogFile);

            FileInfo logFileInfo = null;
            string logFileWithReferences;
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string currentLogFilesPath = pathLogFile;
                logFileWithReferences = string.Format("{0}{1}{2}", currentLogFilesPath, Path.DirectorySeparatorChar, @"1Cv8.lgf");
            }
            else
            {
                logFileInfo = new FileInfo(pathLogFile);
                logFileWithReferences = logFileInfo.FullName;
            }

            if (!File.Exists(logFileWithReferences))
                logFileWithReferences = string.Format("{0}{1}{2}", pathLogFile, Path.DirectorySeparatorChar, @"1Cv8.lgd");

            if (File.Exists(logFileWithReferences))
            {
                if (logFileInfo == null) logFileInfo = new FileInfo(logFileWithReferences);

                string logFileExtension = logFileInfo.Extension.ToUpper();
                if (logFileExtension.EndsWith("LGF"))
                    return new EventLogLGFReader(logFileInfo.FullName);
                else if (logFileExtension.EndsWith("LGD"))
                    return new EventLogLGDReader(logFileInfo.FullName);
            }

            throw new ArgumentException("Invalid log file path");
        }

        #endregion

        #region Private Member Variables

        protected string _logFilePath;
        protected string _logFileDirectoryPath;
        protected long _currentFileEventNumber;

        protected DateTime _referencesReadDate;
        protected string _referencesHash;
        protected List<Applications> _applications;
        protected List<Computers> _computers;
        protected List<Metadata> _metadata;
        protected List<Events> _events;
        protected List<PrimaryPorts> _primaryPorts;
        protected List<SecondaryPorts> _secondaryPorts;
        protected List<Users> _users;
        protected List<WorkServers> _workServers;
        protected RowData _currentRow;

        #endregion

        #region Constructor

        internal EventLogReader() : base() { }
        internal EventLogReader(string logFilePath)
        {
            _logFilePath = logFilePath;
            _logFileDirectoryPath = new FileInfo(_logFilePath).Directory.FullName;

            _applications = new List<Applications>();
            _computers = new List<Computers>();
            _metadata = new List<Metadata>();
            _events = new List<Events>();
            _primaryPorts = new List<PrimaryPorts>();
            _secondaryPorts = new List<SecondaryPorts>();
            _users = new List<Users>();
            _workServers = new List<WorkServers>();

            _referencesReadDate = DateTime.MinValue;
            ReadEventLogReferences();
        }

        #endregion

        #region Public Properties

        public DateTime ReferencesReadDate => _referencesReadDate;
        public string ReferencesHash => _referencesHash;
        public IReadOnlyList<Applications> Applications => _applications;
        public IReadOnlyList<Computers> Computers => _computers;
        public IReadOnlyList<Metadata> Metadata => _metadata;
        public IReadOnlyList<Events> Events => _events;
        public IReadOnlyList<PrimaryPorts> PrimaryPorts => _primaryPorts;
        public IReadOnlyList<SecondaryPorts> SecondaryPorts => _secondaryPorts;
        public IReadOnlyList<Users> Users => _users;
        public IReadOnlyList<WorkServers> WorkServers => _workServers;
        public RowData CurrentRow => _currentRow;
        public long CurrentFileEventNumber => _currentFileEventNumber;
        public string LogFilePath => _logFilePath;
        public string LogFileDirectoryPath => _logFileDirectoryPath;
        public virtual string CurrentFile
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Public Methods

        public virtual bool Read()
        {
            throw new NotImplementedException();
        }
        public virtual bool GoToEvent(long eventNumber)
        {
            throw new NotImplementedException();
        }
        public virtual EventLogPosition GetCurrentPosition()
        {
            throw new NotImplementedException();
        }
        public virtual void SetCurrentPosition(EventLogPosition newPosition)
        {
            throw new NotImplementedException();
        }
        public virtual long Count()
        {
            throw new NotImplementedException();
        }
        public virtual void Reset()
        {
            throw new NotImplementedException();
        }
        public virtual void NextFile()
        {
            throw new NotImplementedException();
        }
        public virtual void Dispose()
        {
            _applications.Clear();
            _computers.Clear();
            _metadata.Clear();
            _events.Clear();
            _primaryPorts.Clear();
            _secondaryPorts.Clear();
            _users.Clear();
            _workServers.Clear();
            _currentRow = null;
        }

        public Users GetUserByCode(string code)
        {
            return GetUserByCode(code.ToInt64());
        }
        public Users GetUserByCode(long code)
        {
            return _users.Where(i => i.Code == code).FirstOrDefault();
        }

        public Computers GetComputerByCode(string code)
        {
            return GetComputerByCode(code.ToInt64());
        }
        public Computers GetComputerByCode(long code)
        {
            return _computers.Where(i => i.Code == code).FirstOrDefault();
        }

        public Applications GetApplicationByCode(string code)
        {
            return GetApplicationByCode(code.ToInt64());
        }
        public Applications GetApplicationByCode(long code)
        {
            return _applications.Where(i => i.Code == code).FirstOrDefault();
        }

        public Events GetEventByCode(string code)
        {
            return GetEventByCode(code.ToInt64());
        }
        public Events GetEventByCode(long code)
        {
            return _events.Where(i => i.Code == code).FirstOrDefault();
        }

        public Severity GetSeverityByCode(string code)
        {
            Severity severity;

            switch (code.Trim())
            {
                case "I":
                    severity = Severity.Information;
                    break;
                case "W":
                    severity = Severity.Warning;
                    break;
                case "E":
                    severity = Severity.Error;
                    break;
                case "N":
                    severity = Severity.Note;
                    break;
                default:
                    severity = Severity.Unknown;
                    break;
            }

            return severity;
        }
        public Severity GetSeverityByCode(long code)
        {
            try
            {
                return (Severity)code;
            } catch
            {
                return Severity.Unknown;
            }
        }

        public TransactionStatus GetTransactionStatus(string code)
        {
            TransactionStatus transactionStatus;

            if (code == "R")
                transactionStatus = TransactionStatus.Unfinished;
            else if (code == "N")
                transactionStatus = TransactionStatus.NotApplicable;
            else if (code == "U")
                transactionStatus = TransactionStatus.Committed;
            else if (code == "C")
                transactionStatus = TransactionStatus.RolledBack;
            else
                transactionStatus = TransactionStatus.Unknown;

            return transactionStatus;
        }
        public TransactionStatus GetTransactionStatus(long code)
        {
            try
            {
                return (TransactionStatus)code;
            } catch
            {
                return TransactionStatus.Unknown;
            }
        }

        public Metadata GetMetadataByCode(string code)
        {
            return GetMetadataByCode(code.ToInt64());
        }
        public Metadata GetMetadataByCode(long code)
        {
            return _metadata.Where(i => i.Code == code).FirstOrDefault();
        }

        public WorkServers GetWorkServerByCode(string code)
        {
            return GetWorkServerByCode(code.ToInt64());
        }
        public WorkServers GetWorkServerByCode(long code)
        {
            return _workServers.Where(i => i.Code == code).FirstOrDefault();
        }

        public PrimaryPorts GetPrimaryPortByCode(string code)
        {
            return GetPrimaryPortByCode(code.ToInt64());
        }
        public PrimaryPorts GetPrimaryPortByCode(long code)
        {
            return _primaryPorts.Where(i => i.Code == code).FirstOrDefault();
        }

        public SecondaryPorts GetSecondaryPortByCode(string code)
        {
            return GetSecondaryPortByCode(code.ToInt64());
        }
        public SecondaryPorts GetSecondaryPortByCode(long code)
        {
            return _secondaryPorts.Where(i => i.Code == code).FirstOrDefault();
        }

        #endregion

        #region Private Methods

        protected virtual void ReadEventLogReferences()
        {
            ReferencesDataHash data = ReferencesDataHash.CreateromReader(this);
            _referencesHash = MD5HashGenerator.GetMD5Hash<ReferencesDataHash>(data);
        }

        #endregion

        #region Events

        public delegate void BeforeReadFileHandler(EventLogReader sender, BeforeReadFileEventArgs args);
        public delegate void AfterReadFileHandler(EventLogReader sender, AfterReadFileEventArgs args);
        public delegate void BeforeReadEventHandler(EventLogReader sender, BeforeReadEventArgs args);
        public delegate void AfterReadEventHandler(EventLogReader sender, AfterReadEventArgs args);
        public delegate void OnErrorEventHandler(EventLogReader sender, OnErrorEventArgs args);

        public event BeforeReadFileHandler BeforeReadFile;
        public event AfterReadFileHandler AfterReadFile;
        public event BeforeReadEventHandler BeforeReadEvent;
        public event AfterReadEventHandler AfterReadEvent;
        public event OnErrorEventHandler OnErrorEvent;

        protected void RaiseBeforeReadFile(BeforeReadFileEventArgs args)
        {
            BeforeReadFile?.Invoke(this, args);
        }
        protected void RaiseAfterReadFile(AfterReadFileEventArgs args)
        {
            AfterReadFile?.Invoke(this, args);
        }
        protected void RaiseBeforeRead(BeforeReadEventArgs args)
        {
            BeforeReadEvent?.Invoke(this, args);
        }
        protected void RaiseAfterRead(AfterReadEventArgs args)
        {
            AfterReadEvent?.Invoke(this, args);
        }
        protected void RaiseOnError(OnErrorEventArgs args)
        {
            OnErrorEvent?.Invoke(this, args);
        }

        #endregion

        #region Service

        [Serializable]
        private class ReferencesDataHash
        {
            public static ReferencesDataHash CreateromReader(EventLogReader reader)
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

                ReferencesDataHash referenceData;

                referenceData = new ReferencesDataHash()
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
