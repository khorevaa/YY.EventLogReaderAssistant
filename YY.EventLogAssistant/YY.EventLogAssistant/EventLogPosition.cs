namespace YY.EventLogAssistant
{
    public sealed class EventLogPosition
    {
        #region Constructor

        public EventLogPosition(long EventNumber, string CurrentFileReferences, string CurrentFileData, long? StreamPosition)
        {
            this.EventNumber = EventNumber;
            this.CurrentFileReferences = CurrentFileReferences;
            this.CurrentFileData = CurrentFileData;
            this.StreamPosition = StreamPosition;
        }

        #endregion

        #region Public Properties

        public long EventNumber { get; }
        public string CurrentFileReferences { get; }
        public string CurrentFileData { get; }
        public long? StreamPosition { get; }

        #endregion
    }
}
