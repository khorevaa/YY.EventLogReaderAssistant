namespace YY.EventLogReaderAssistant.Models
{
    public enum TransactionStatus
    {
        Unknown = 0,
        Committed = 1,
        Unfinished = 2,
        NotApplicable = 3,
        RolledBack = 4
    }
}
