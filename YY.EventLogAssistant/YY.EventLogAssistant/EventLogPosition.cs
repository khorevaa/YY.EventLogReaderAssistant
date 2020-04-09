using System;
using System.Collections.Generic;
using System.Text;

namespace YY.LogReader.EventLog
{
    public sealed class EventLogPosition
    {
        private readonly long _eventNumber;
        private readonly string _currentFileReferences;
        private readonly string _currentFileData;
        private readonly long? _streamPosition;

        public EventLogPosition(long EventNumber, string CurrentFileReferences, string CurrentFileData, long? StreamPosition)
        {
            _eventNumber = EventNumber;
            _currentFileReferences = CurrentFileReferences;
            _currentFileData = CurrentFileData;
            _streamPosition = StreamPosition;
        }

        public long EventNumber { get { return _eventNumber; } }
        public string CurrentFileReferences { get { return _currentFileReferences; } }
        public string CurrentFileData { get { return _currentFileData; } }
        public long? StreamPosition { get { return _streamPosition; } }
    }
}
