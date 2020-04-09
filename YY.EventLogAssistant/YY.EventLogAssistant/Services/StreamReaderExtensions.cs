using System.IO;
using System.Reflection;

namespace YY.LogReader.Services
{
    internal static class StreamReaderExtensions
    {
        readonly static FieldInfo charPosField = typeof(StreamReader).GetField("_charPos", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        readonly static FieldInfo byteLenField = typeof(StreamReader).GetField("_byteLen", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        readonly static FieldInfo charBufferField = typeof(StreamReader).GetField("_charBuffer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        public static long GetPosition(this StreamReader reader)
        {
            int byteLen = (int)byteLenField.GetValue(reader);
            var position = reader.BaseStream.Position - byteLen;

            int charPos = (int)charPosField.GetValue(reader);
            if (charPos > 0)
            {
                var charBuffer = (char[])charBufferField.GetValue(reader);
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
    }

}
