namespace YY.EventLogReaderAssistant.Models
{
    public class WorkServers
    {
        public long Code { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
