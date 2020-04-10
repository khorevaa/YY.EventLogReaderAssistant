using YY.EventLogAssistant.Models;
using System;

namespace YY.EventLogAssistant
{
    public sealed class AfterReadEventArgs : EventArgs
    {
        public AfterReadEventArgs(EventLogRowData rowData, long eventNumber)
        {
            RowData = rowData;
            EventNumber = eventNumber;
        }

        public EventLogRowData RowData { get; }
        public long EventNumber { get; }
    }    
}
