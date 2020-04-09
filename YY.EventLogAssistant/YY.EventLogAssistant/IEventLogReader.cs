using System;
using System.Collections.Generic;
using System.Text;
using YY.LogReader.Models.EventLog;

namespace YY.LogReader.EventLog
{
    internal interface IEventLogReader
    {
        bool Read(out EventLogRowData rowData);
        bool GoToEvent(long eventNumber);
        EventLogPosition GetCurrentPosition();
        void SetCurrentPosition(EventLogPosition newPosition);
        long Count();
        void Reset();
        void NextFile();
    }
}
