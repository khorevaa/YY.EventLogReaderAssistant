using System;

namespace YY.EventLogReaderAssistant.Models
{
    public class RowData
    {
        public InformationSystems InformationSystem { get; set; }
        public DateTimeOffset Period { get; set; }
        public long RowID { get; set; }
        public Severity Severity { get; set; }
        public long? ConnectId { get; set; }
        public long? Session { get; set; }
        public TransactionStatus TransactionStatus { get; set; }
        public DateTime? TransactionDate { get; set; }
        public long? TransactionId { get; set; }
        public Users User { get; set; }
        public Computers Computer { get; set; }
        public Applications Application { get; set; }
        public Events Event { get; set; }
        public string Comment { get; set; }
        public Metadata Metadata { get; set; }
        public string Data { get; set; }
        public string DataUUID { get; set; }
        public string DataPresentation { get; set; }
        public WorkServers WorkServer { get; set; }
        public PrimaryPorts PrimaryPort { get; set; }
        public SecondaryPorts SecondaryPort { get; set; }
    }
}
