using System;
using System.IO;
using System.Text;

using Cursively.Internal;

namespace Cursively
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvWriteFlattenedOperation : CsvOperationBase
    {
        private readonly int _maxHeaderCount;

        private readonly int _maxFieldLength;

        private readonly bool _ignoreUTF8IdentifierOnFirstHeaderField;

        private readonly DecoderFallback _decoderFallback;

        private readonly TextWriter _outputSink;

        internal CsvWriteFlattenedOperation()
            : this(CsvReaderVisitorWithUTF8HeadersBase.DefaultMaxHeaderCount,
                   CsvReaderVisitorWithUTF8HeadersBase.DefaultMaxHeaderLength,
                   CsvReaderVisitorWithUTF8HeadersBase.DefaultIgnoreUTF8IdentifierOnFirstHeaderField,
                   CsvReaderVisitorWithUTF8HeadersBase.DefaultDecoderFallback,
                   Console.Out)
        {
        }

        internal CsvWriteFlattenedOperation(int maxHeaderCount, int maxFieldLength, bool ignoreUTF8IdentifierOnFirstHeaderField, DecoderFallback decoderFallback, TextWriter outputSink)
        {
            _maxHeaderCount = maxHeaderCount;
            _maxFieldLength = maxFieldLength;
            _ignoreUTF8IdentifierOnFirstHeaderField = ignoreUTF8IdentifierOnFirstHeaderField;
            _decoderFallback = decoderFallback;
            _outputSink = outputSink;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxHeaderCount"></param>
        /// <returns></returns>
        public CsvWriteFlattenedOperation WithMaxHeaderCount(int maxHeaderCount) =>
            new CsvWriteFlattenedOperation(maxHeaderCount, _maxFieldLength, _ignoreUTF8IdentifierOnFirstHeaderField, _decoderFallback, _outputSink);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxFieldLength"></param>
        /// <returns></returns>
        public CsvWriteFlattenedOperation WithMaxFieldLength(int maxFieldLength) =>
            new CsvWriteFlattenedOperation(_maxHeaderCount, maxFieldLength, _ignoreUTF8IdentifierOnFirstHeaderField, _decoderFallback, _outputSink);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoreUTF8IdentifierOnFirstHeaderField"></param>
        /// <returns></returns>
        public CsvWriteFlattenedOperation WithIgnoreUTF8IdentifierOnFirstHeaderField(bool ignoreUTF8IdentifierOnFirstHeaderField) =>
            new CsvWriteFlattenedOperation(_maxHeaderCount, _maxFieldLength, ignoreUTF8IdentifierOnFirstHeaderField, _decoderFallback, _outputSink);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="decoderFallback"></param>
        /// <returns></returns>
        public CsvWriteFlattenedOperation WithDecoderFallback(DecoderFallback decoderFallback) =>
            new CsvWriteFlattenedOperation(_maxHeaderCount, _maxFieldLength, _ignoreUTF8IdentifierOnFirstHeaderField, decoderFallback, _outputSink);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputSink"></param>
        /// <returns></returns>
        public CsvWriteFlattenedOperation WithOutputSink(TextWriter outputSink) =>
            new CsvWriteFlattenedOperation(_maxHeaderCount, _maxFieldLength, _ignoreUTF8IdentifierOnFirstHeaderField, _decoderFallback, outputSink);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override CsvReaderVisitorBase CreateVisitor() =>
            new WriteFlattenedVisitor(_maxHeaderCount, _maxFieldLength, _ignoreUTF8IdentifierOnFirstHeaderField, _decoderFallback, _outputSink);
    }
}
