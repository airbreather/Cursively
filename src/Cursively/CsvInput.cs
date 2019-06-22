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
        public static CsvStreamInput ForStream(Stream csvStream)
        {
            if (csvStream is null)
            {
                throw new ArgumentNullException(nameof(csvStream));
            }

            if (!csvStream.CanRead)
            {
                throw new ArgumentException("Stream does not support reading.", nameof(csvStream));
            }

            return new CsvStreamInput((byte)',', csvStream, 65536, ArrayPool<byte>.Shared, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csvFilePath"></param>
        /// <returns></returns>
        public static CsvMemoryMappedFileInput ForFile(string csvFilePath)
        {
            return new CsvMemoryMappedFileInput((byte)',', csvFilePath, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static CsvBytesInput ForBytes(ReadOnlyMemory<byte> bytes)
        {
            return new CsvBytesInput((byte)',', bytes, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static CsvCharsInput ForString(string str)
        {
            return ForChars(str.AsMemory());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static CsvCharsInput ForChars(ReadOnlyMemory<char> chars)
        {
            return new CsvCharsInput((byte)',', chars, 340, MemoryPool<byte>.Shared, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static CsvCharSequenceInput ForChars(ReadOnlySequence<char> chars)
        {
            return new CsvCharSequenceInput((byte)',', chars, 340, MemoryPool<byte>.Shared, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textReader"></param>
        /// <returns></returns>
        public static CsvTextReaderInput ForTextReader(TextReader textReader)
        {
            return new CsvTextReaderInput((byte)',', textReader ?? TextReader.Null, 1024, ArrayPool<char>.Shared, 340, MemoryPool<byte>.Shared, true);
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
