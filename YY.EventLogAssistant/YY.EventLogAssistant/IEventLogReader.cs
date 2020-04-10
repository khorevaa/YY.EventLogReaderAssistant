using YY.EventLogAssistant.Models;

namespace YY.EventLogAssistant
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
