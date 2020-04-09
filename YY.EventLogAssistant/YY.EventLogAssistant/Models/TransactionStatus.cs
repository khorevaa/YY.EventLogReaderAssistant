using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YY.LogReader.Models.EventLog
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
