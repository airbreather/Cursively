using System;
using System.Buffers;
using System.IO;

using Cursively.Inputs;

namespace Cursively
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CsvInput
    {
        private readonly bool _mustResetAfterProcessing;

        private bool _alreadyProcessed;

        /// <summary>
        /// 
        /// </summary>
        protected CsvInput()
            : this((byte)',', true)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="mustResetAfterProcessing"></param>
        protected CsvInput(byte delimiter, bool mustResetAfterProcessing)
        {
            _mustResetAfterProcessing = mustResetAfterProcessing;
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
        public static CsvStreamInput ForStream(Stream csvStream) =>
            ForStream(csvStream, 81920);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csvStream"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public static CsvStreamInput ForStream(Stream csvStream, int bufferSize)
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
        public static CsvMemoryMappedFileInput ForFile(string csvFilePath)
        {
            return new CsvMemoryMappedFileInput((byte)',', csvFilePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static CsvBytesInput ForBytes(ReadOnlyMemory<byte> bytes)
        {
            return new CsvBytesInput((byte)',', bytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static CsvCharsInput ForString(string str) =>
            ForChars(str.AsMemory());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="chunkCharCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static CsvCharsInput ForString(string str, int chunkCharCount) =>
            ForChars(str.AsMemory(), chunkCharCount);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static CsvCharsInput ForChars(ReadOnlyMemory<char> chars) =>
            ForChars(chars, 341);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="chunkCharCount"></param>
        /// <returns></returns>
        public static CsvCharsInput ForChars(ReadOnlyMemory<char> chars, int chunkCharCount)
        {
            if (chunkCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkCharCount), chunkCharCount, "Must be greater than zero.");
            }

            return new CsvCharsInput((byte)',', chars, chunkCharCount);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static CsvCharSequenceInput ForChars(ReadOnlySequence<char> chars) =>
            ForChars(chars, 341);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="chunkCharCount"></param>
        /// <returns></returns>
        public static CsvCharSequenceInput ForChars(ReadOnlySequence<char> chars, int chunkCharCount)
        {
            if (chunkCharCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkCharCount), chunkCharCount, "Must be greater than zero.");
            }

            return new CsvCharSequenceInput((byte)',', chars, chunkCharCount);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visitor"></param>
        public void Process(CsvReaderVisitorBase visitor)
        {
            if (_alreadyProcessed && _mustResetAfterProcessing)
            {
                throw new InvalidOperationException("Input was already processed.  Call TryReset() first to try to reset this input.  If that method returns false, then this input will not work.");
            }

            Process(new CsvTokenizer(Delimiter), visitor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool TryReset()
        {
            if (_alreadyProcessed)
            {
                if (!TryResetCore())
                {
                    return false;
                }

                _alreadyProcessed = false;
            }

            return !_alreadyProcessed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <param name="visitor"></param>
        protected abstract void Process(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected virtual bool TryResetCore() => true;
    }
}
