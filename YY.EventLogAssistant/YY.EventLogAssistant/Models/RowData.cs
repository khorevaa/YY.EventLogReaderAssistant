using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YY.LogReader.Models.EventLog;

namespace YY.LogReader.Models
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
