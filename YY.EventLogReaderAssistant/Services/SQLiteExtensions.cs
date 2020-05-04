using System;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant.Services
{
    internal static class SQLiteExtensions
    {
        #region Public Methods

        public static string GetStringOrDefault(this SQLiteDataReader reader, int valueIndex)
        {
            try
            {
                return reader.GetString(valueIndex);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static long GetInt64OrDefault(this SQLiteDataReader reader, int valueIndex)
        {
            try
            {
                return reader.GetInt64(valueIndex);
            }
            catch
            {
                return 0;
            }
        }

        public static string GetRowAsString(this SQLiteDataReader reader)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                builder.Append(reader.GetName(i));
                builder.Append(" : ");
                builder.Append(Convert.ToString(reader.GetValue(i)));
                builder.AppendLine();
            }

            return builder.ToString();
        }

        public static string GetConnectionString(string dbFile, bool readOnly = true)
        {
            string readOnlyMode = readOnly ? "True" : "False";
            return string.Format("Data Source={0};Version=3;Read Only={1};", dbFile, readOnlyMode);
        }

        #endregion
    }
}
