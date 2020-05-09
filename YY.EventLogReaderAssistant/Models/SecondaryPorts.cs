using System;

namespace YY.EventLogReaderAssistant.Models
{
    [Serializable]
    public class SecondaryPorts
    {
        public long Code { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
