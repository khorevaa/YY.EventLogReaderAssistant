using System;
using System.Data.SQLite;

namespace YY.EventLogReaderAssistant.Models
{
    [Serializable]
    public class WorkServers : IReferenceObject
    {
        #region Public Members

        public long Code { get; set; }
        public string Name { get; set; }

        #endregion

        #region Public Methods

        public void FillBySqliteReader(SQLiteDataReader reader)
        {
            Code = reader.GetInt64(0);
            Name = reader.GetString(1);
        }
        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
