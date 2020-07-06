using System.Data.SQLite;

namespace YY.EventLogReaderAssistant.Models
{
    public interface IReferenceObject
    {
        void FillBySqliteReader(SQLiteDataReader reader);
        void FillByStringParsedData(string[] parsedEventData);
    }
}
