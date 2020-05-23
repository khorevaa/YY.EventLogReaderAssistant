using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant
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
        void SetReadDelay(double milliseconds);
    }
}
