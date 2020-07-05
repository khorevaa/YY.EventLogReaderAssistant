using System;
using System.Data.SQLite;
using YY.EventLogReaderAssistant.Services;

namespace YY.EventLogReaderAssistant.Models
{
    [Serializable]
    public class Metadata : IReferenceObject
    {
        #region Public Members

        public long Code { get; set; }
        public Guid Uuid { get; set; }
        public string Name { get; set; }

        #endregion

        #region Public Methods

        public void FillBySqliteReader(SQLiteDataReader reader)
        {
            Code = reader.GetInt64(0);
            Name = reader.GetString(1);
            Uuid = reader.GetString(2).ToGuid();
        }
        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
