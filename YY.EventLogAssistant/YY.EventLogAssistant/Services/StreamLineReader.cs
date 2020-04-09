using System;
using System.IO;
using System.Text;

namespace YY.LogReader.Services
{
    internal class StreamLineReader : IDisposable
    {
        const int BufferLength = 1024;

        Stream _Base;
        int _Read = 0, _Index = 0;
        byte[] _Bff = new byte[BufferLength];

        long _CurrentPosition = 0;
        long _CurrentLine = 0;

        public long CurrentPosition { get { return _CurrentPosition; } }

        public long CurrentLine { get { return _CurrentLine; } }

        public StreamLineReader(Stream stream) { _Base = stream; }

        public bool GoToLine(long goToLine) { return IGetCount(goToLine, true) == goToLine; }

        public long GetCount(long goToLine) { return IGetCount(goToLine, false); }

        long IGetCount(long goToLine, bool stopWhenLine)
        {
            _Base.Seek(0, SeekOrigin.Begin);
            _CurrentPosition = 0;
            _CurrentLine = 0;
            _Index = 0;
            _Read = 0;

            long savePosition = _Base.Length;

            do
            {
                if (_CurrentLine == goToLine)
                {
                    savePosition = _CurrentPosition;
                    if (stopWhenLine) return _CurrentLine;
                }
            }
            while (ReadLine() != null);

            long count = _CurrentLine;

            _CurrentLine = goToLine;
            _Base.Seek(savePosition, SeekOrigin.Begin);

            return count;
        }

        public string ReadLine()
        {
            bool found = false;

            StringBuilder sb = new StringBuilder();
            while (!found)
            {
                if (_Read <= 0)
                {
                    // Read next block
                    _Index = 0;
                    _Read = _Base.Read(_Bff, 0, BufferLength);
                    if (_Read == 0)
                    {
                        if (sb.Length > 0) break;
                        return null;
                    }
                }

                for (int max = _Index + _Read; _Index < max; )
                {
                    char ch = (char)_Bff[_Index];
                    _Read--; _Index++;
                    _CurrentPosition++;

                    if (ch == '\0' || ch == '\n')
                    {
                        found = true;
                        break;
                    }
                    else if (ch == '\r') continue;
                    else sb.Append(ch);
                }
            }

            _CurrentLine++;
            return sb.ToString();
        }

        public void Dispose()
        {
            if (_Base != null)
            {
                _Base.Close();
                _Base.Dispose();
                _Base = null;
            }
        }
    }
}
