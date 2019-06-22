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

        private protected bool _alreadyProcessed;

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
            csvStream = csvStream ?? Stream.Null;
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
            if (csvFilePath is null)
            {
                throw new ArgumentNullException(nameof(csvFilePath));
            }

            if (string.IsNullOrWhiteSpace(csvFilePath))
            {
                throw new ArgumentException("Cannot be blank", nameof(csvFilePath));
            }

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
            if (!TryReset())
            {
                throw new InvalidOperationException("Input has already been consumed and cannot be reset.");
            }

            Process(new CsvTokenizer(Delimiter), visitor);
            _alreadyProcessed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool TryReset()
        {
            if (_alreadyProcessed)
            {
                if (_mustResetAfterProcessing && !TryResetCore())
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
        protected virtual bool TryResetCore() => false;
    }
}
