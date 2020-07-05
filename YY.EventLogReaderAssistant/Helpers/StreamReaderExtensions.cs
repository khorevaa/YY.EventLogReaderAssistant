using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogReaderAssistant.Tests")]
namespace YY.EventLogReaderAssistant.Helpers
{
    internal static class StreamReaderExtensions
    {
        #region Private Member Variables

        readonly static FieldInfo CharPosField = typeof(StreamReader).GetField("_charPos", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        static readonly FieldInfo ByteLenField = typeof(StreamReader).GetField("_byteLen", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        static readonly FieldInfo CharBufferField = typeof(StreamReader).GetField("_charBuffer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        #endregion

        #region Public Methods

        public static long GetPosition(this StreamReader reader)
        {
            int byteLen = (int)ByteLenField.GetValue(reader);
            var position = reader.BaseStream.Position - byteLen;

            int charPos = (int)CharPosField.GetValue(reader);
            if (charPos > 0)
            {
                var charBuffer = (char[])CharBufferField.GetValue(reader);
                var encoding = reader.CurrentEncoding;
                var bytesConsumed = encoding.GetBytes(charBuffer, 0, charPos).Length;
                position += bytesConsumed;
            }

            return position;
        }

        public static void SetPosition(this StreamReader reader, long position)
        {
            reader.DiscardBufferedData();
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }

        public static void SkipLine(this StreamReader stream, long numberToSkip)
        {
            for (int i = 0; i < numberToSkip; i++)
            {
                stream.ReadLine();
            }
        }

        #endregion
    }
}
