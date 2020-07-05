using System;
using System.Data.SQLite;
using YY.EventLogReaderAssistant.Services;

namespace YY.EventLogReaderAssistant.Models
{
    [Serializable]
    public class Metadata : ReferenceObject
    {
        #region Public Members

        public Guid Uuid { get; set; }

        #endregion

        #region Public Methods

        public override void FillBySqliteReader(SQLiteDataReader reader)
        {
            base.FillBySqliteReader(reader);
            Uuid = reader.GetString(2).ToGuid();
        }

        #endregion
    }
}
