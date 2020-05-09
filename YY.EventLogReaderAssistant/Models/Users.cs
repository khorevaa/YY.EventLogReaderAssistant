using System;

namespace YY.EventLogReaderAssistant.Models
{
    [Serializable]
    public class Users
    {
        public long Code { get; set; }
        public Guid Uuid { get; set; }
        public string Name { get; set; }        

        public override string ToString()
        {
            return Name;
        }
    }
}
