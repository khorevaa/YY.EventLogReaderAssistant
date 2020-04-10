using YY.EventLogAssistant.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace YY.EventLogAssistant
{
    public abstract partial class EventLogReader : IEventLogReader, IDisposable
    {
        public static EventLogReader CreateReader(string pathLogFile)
        {
            FileAttributes attr = File.GetAttributes(pathLogFile);

            FileInfo logFileInfo = null;
            string currentLogFilesPath;
            string logFileWithReferences;
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                currentLogFilesPath = pathLogFile;
                logFileWithReferences = string.Format("{0}{1}{2}", currentLogFilesPath, Path.DirectorySeparatorChar, @"1Cv8.lgf");
            }
            else
            {
                logFileInfo = new FileInfo(pathLogFile);
                currentLogFilesPath = logFileInfo.Directory.FullName;
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

        protected string _logFilePath;
        protected string _logFileDirectoryPath;
        protected long _currentFileEventNumber;
        public long CurrentFileEventNumber { get { return _currentFileEventNumber; } }
        public string LogFilePath { get { return _logFilePath; } }
        public string LogFileDirectoryPath { get { return _logFileDirectoryPath; } }

        protected List<Applications> _applications;
        protected List<Computers> _computers;
        protected List<Metadata> _metadata;
        protected List<Events> _events;
        protected List<PrimaryPorts> _primaryPorts;
        protected List<SecondaryPorts> _secondaryPorts;
        protected List<Users> _users;
        protected List<WorkServers> _workServers;

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

            ReadEventLogReferences();
        }

        public IReadOnlyList<Applications> Applications { get { return _applications; } }
        public IReadOnlyList<Computers> Computers { get { return _computers; } }
        public IReadOnlyList<Metadata> Metadata { get { return _metadata; } }
        public IReadOnlyList<Events> Events { get { return _events; } }
        public IReadOnlyList<PrimaryPorts> PrimaryPorts { get { return _primaryPorts; } }
        public IReadOnlyList<SecondaryPorts> SecondaryPorts { get { return _secondaryPorts; } }
        public IReadOnlyList<Users> Users { get { return _users; } }
        public IReadOnlyList<WorkServers> WorkServers { get { return _workServers; } }

        protected virtual void ReadEventLogReferences() { }

        public virtual bool Read(out EventLogRowData rowData)
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
        }
    }
}
