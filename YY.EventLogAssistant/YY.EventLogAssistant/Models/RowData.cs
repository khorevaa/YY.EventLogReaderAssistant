using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YY.EventLogAssistant.Models
{
    public abstract class RowData
    {
        [Key]
        [Column(Order = 1)]
        public InformationSystems InformationSystem { get; set; }
        [Key]
        [Column(Order = 2)]
        public DateTimeOffset Period { get; set; }
        [Key]
        [Column(Order = 3)]
        public long RowID { get; set; }
    }
}
