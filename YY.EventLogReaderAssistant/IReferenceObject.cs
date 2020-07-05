using System.Data.SQLite;

namespace YY.EventLogReaderAssistant
{
    public interface IReferenceObject
    {
        void FillBySqliteReader(SQLiteDataReader reader);
    }
}
