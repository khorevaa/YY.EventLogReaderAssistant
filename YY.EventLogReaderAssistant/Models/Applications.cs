namespace YY.EventLogReaderAssistant.Models
{
    public class Applications
    {
        public long Code { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
