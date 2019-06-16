using System;
using System.IO;

using Cursively.Internal;

namespace Cursively
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CsvInput
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        protected CsvInput(byte delimiter)
        {
            Delimiter = delimiter;
        }

        /// <summary>
        /// 
        /// </summary>
        protected byte Delimiter { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csvStream"></param>
        /// <returns></returns>
        public static CsvAsyncInput ForStream(Stream csvStream) =>
            ForStream(csvStream, 81920);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csvStream"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public static CsvAsyncInput ForStream(Stream csvStream, int bufferSize)
        {
            if (csvStream is null)
            {
                throw new ArgumentNullException(nameof(csvStream));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Must be greater than zero.");
            }

            if (!csvStream.CanRead)
            {
                throw new ArgumentException("Stream does not support reading.", nameof(csvStream));
            }

            return new CsvStreamInput((byte)',', csvStream, bufferSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csvFilePath"></param>
        /// <returns></returns>
        public static CsvInput ForFile(string csvFilePath)
        {
            return new CsvMemoryMappedFileInput((byte)',', csvFilePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static CsvInput ForBytes(ReadOnlyMemory<byte> bytes)
        {
            return new CsvBytesInput((byte)',', bytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static CsvInput ForChars(ReadOnlyMemory<char> chars)
        {
            return new CsvCharsInput((byte)',', chars);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static CsvInput ForString(string str)
        {
            return new CsvCharsInput((byte)',', str.AsMemory());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public abstract CsvInput WithDelimiter(byte delimiter);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visitor"></param>
        public void Process(CsvReaderVisitorBase visitor)
        {
            Process(new CsvTokenizer(Delimiter), visitor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <param name="visitor"></param>
        protected abstract void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor);
    }
}
