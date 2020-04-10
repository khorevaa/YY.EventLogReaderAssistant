using System;
using System.ComponentModel.DataAnnotations;

namespace YY.EventLogAssistant.Models
{
    public class Metadata
    {
        [Key]
        public long Code { get; set; }
        public Guid Uuid { get; set; }
        [MaxLength(250)]
        public string Name { get; set; }        

        public override string ToString()
        {
            return Name;
        }
    }
}
