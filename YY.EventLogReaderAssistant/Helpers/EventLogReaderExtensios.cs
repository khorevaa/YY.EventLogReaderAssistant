using System.Linq;
using YY.EventLogReaderAssistant.Models;

namespace YY.EventLogReaderAssistant.Helpers
{
    public static class EventLogReaderExtensions
    {
        public static Users GetUserByCode(this EventLogReader reader, string code)
        {
            return reader.GetUserByCode(code.ToInt64());
        }
        public static Users GetUserByCode(this EventLogReader reader, long code)
        {
            return reader.Users.FirstOrDefault(i => i.Code == code);
        }
        public static Computers GetComputerByCode(this EventLogReader reader, string code)
        {
            return reader.GetComputerByCode(code.ToInt64());
        }
        public static Computers GetComputerByCode(this EventLogReader reader, long code)
        {
            return reader.Computers.FirstOrDefault(i => i.Code == code);
        }
        public static Applications GetApplicationByCode(this EventLogReader reader, string code)
        {
            return reader.GetApplicationByCode(code.ToInt64());
        }
        public static Applications GetApplicationByCode(this EventLogReader reader, long code)
        {
            return reader.Applications.FirstOrDefault(i => i.Code == code);
        }
        public static Events GetEventByCode(this EventLogReader reader, string code)
        {
            return reader.GetEventByCode(code.ToInt64());
        }
        public static Events GetEventByCode(this EventLogReader reader, long code)
        {
            return reader.Events.FirstOrDefault(i => i.Code == code);
        }
        public static Severity GetSeverityByCode(this EventLogReader reader, string code)
        {
            Severity severity;

            switch (code.Trim())
            {
                case "I":
                    severity = Severity.Information;
                    break;
                case "W":
                    severity = Severity.Warning;
                    break;
                case "E":
                    severity = Severity.Error;
                    break;
                case "N":
                    severity = Severity.Note;
                    break;
                default:
                    severity = Severity.Unknown;
                    break;
            }

            return severity;
        }
        public static Severity GetSeverityByCode(this EventLogReader reader, long code)
        {
            try
            {
                return (Severity)code;
            }
            catch
            {
                return Severity.Unknown;
            }
        }
        public static TransactionStatus GetTransactionStatus(this EventLogReader reader, string code)
        {
            TransactionStatus transactionStatus;

            if (code == "R")
                transactionStatus = TransactionStatus.Unfinished;
            else if (code == "N")
                transactionStatus = TransactionStatus.NotApplicable;
            else if (code == "U")
                transactionStatus = TransactionStatus.Committed;
            else if (code == "C")
                transactionStatus = TransactionStatus.RolledBack;
            else
                transactionStatus = TransactionStatus.Unknown;

            return transactionStatus;
        }
        public static TransactionStatus GetTransactionStatus(this EventLogReader reader, long code)
        {
            try
            {
                return (TransactionStatus)code;
            }
            catch
            {
                return TransactionStatus.Unknown;
            }
        }
        public static Metadata GetMetadataByCode(this EventLogReader reader, string code)
        {
            return reader.GetMetadataByCode(code.ToInt64());
        }
        public static Metadata GetMetadataByCode(this EventLogReader reader, long code)
        {
            return reader.Metadata.FirstOrDefault(i => i.Code == code);
        }
        public static WorkServers GetWorkServerByCode(this EventLogReader reader, string code)
        {
            return reader.GetWorkServerByCode(code.ToInt64());
        }
        public static WorkServers GetWorkServerByCode(this EventLogReader reader, long code)
        {
            return reader.WorkServers.FirstOrDefault(i => i.Code == code);
        }
        public static PrimaryPorts GetPrimaryPortByCode(this EventLogReader reader, string code)
        {
            return reader.GetPrimaryPortByCode(code.ToInt64());
        }
        public static PrimaryPorts GetPrimaryPortByCode(this EventLogReader reader, long code)
        {
            return reader.PrimaryPorts.FirstOrDefault(i => i.Code == code);
        }
        public static SecondaryPorts GetSecondaryPortByCode(this EventLogReader reader, string code)
        {
            return reader.GetSecondaryPortByCode(code.ToInt64());
        }
        public static SecondaryPorts GetSecondaryPortByCode(this EventLogReader reader, long code)
        {
            return reader.SecondaryPorts.FirstOrDefault(i => i.Code == code);
        }
    }
}
