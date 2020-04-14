using System;

namespace YY.EventLogReaderAssistant
{
    public sealed class AfterReadFileEventArgs : EventArgs
    {
        public AfterReadFileEventArgs(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }
    }
}
