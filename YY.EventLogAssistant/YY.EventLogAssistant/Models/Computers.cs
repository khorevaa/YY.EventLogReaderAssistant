using System.ComponentModel.DataAnnotations;

namespace YY.EventLogReaderAssistant.Models
{
    public class Computers
    {
        [Key]
        public long Code { get; set; }
        [MaxLength(250)]
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
