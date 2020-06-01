using System;

namespace YY.EventLogReaderAssistant.EventArguments
{
    public sealed class OnErrorEventArgs : EventArgs
    {
        public OnErrorEventArgs(Exception excepton, string sourceData, bool critical)
        {
            Exception = excepton;
            SourceData = sourceData;
            Critical = critical;
        }

        public Exception Exception { get; }
        public string SourceData { get; }
        public bool Critical { get; }
    }    
}
