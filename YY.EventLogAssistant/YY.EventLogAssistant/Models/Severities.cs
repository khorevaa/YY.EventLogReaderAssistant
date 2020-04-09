using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YY.LogReader.Models.EventLog
{
    public enum Severity
    {
        Unknown = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
        Note = 4
    }
}
