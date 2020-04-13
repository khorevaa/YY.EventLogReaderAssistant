using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogAssistant.Tests")]
namespace YY.EventLogAssistant
{
    internal interface IEventLogReader
    {
        bool Read();
        bool GoToEvent(long eventNumber);
        EventLogPosition GetCurrentPosition();
        void SetCurrentPosition(EventLogPosition newPosition);
        long Count();
        void Reset();
        void NextFile();
    }
}
