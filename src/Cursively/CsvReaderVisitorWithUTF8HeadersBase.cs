using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cursively
{
    /// <summary>
    /// <para>
    /// Intermediate base class for CSV reader visitors that don't want to have to implement header
    /// handling by themselves.
    /// </para>
    /// <para>
    /// Instances of this class are tied to a single CSV stream and cannot be reused or reset for
    /// use with other CSV streams.
    /// </para>
    /// <para>
    /// Each instance of this visitor has an upper-bound on the maximum number of headers and on the
    /// maximum length of each header.  CSV streams that exceed these limits will cause this class
    /// to throw exceptions, and behavior of a particular instance is undefined once this happens.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The following input-dependent exceptions may get thrown when using this visitor, all of
    /// which inherit from <see cref="CursivelyDataStreamException"/>:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="CursivelyHeadersAreNotUTF8Exception"/> if <see cref="DefaultDecoderFallback"/> is
    /// being used and the CSV stream contains a sequence of invalid UTF-8 bytes.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="CursivelyHeaderIsTooLongException"/> if the CSV stream contains one or more
    /// headers that are longer than the configured maximum.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="CursivelyTooManyHeadersException"/> if the CSV stream contains more headers than
    /// the configured maximum.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="CursivelyMissingDataFieldsException"/>, by default, if a data record contains more
    /// fields than the header record.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="CursivelyExtraDataFieldsException"/>, by default, if a data record contains more
    /// fields than the header record.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public abstract class CsvReaderVisitorWithUTF8HeadersBase : CsvReaderVisitorBase
    {
        /// <summary>
        /// The value used by <see cref="CsvReaderVisitorWithUTF8HeadersBase()"/> to initialize the
        /// maximum number of headers (1,000).
        /// </summary>
        protected static readonly int DefaultMaxHeaderCount = 1_000;

        /// <summary>
        /// The value used by <see cref="CsvReaderVisitorWithUTF8HeadersBase()"/> to initialize the
        /// maximum length, in UTF-16 code units, of a single header (100).
        /// </summary>
        protected static readonly int DefaultMaxHeaderLength = 100;

        /// <summary>
        /// The value used by <see cref="CsvReaderVisitorWithUTF8HeadersBase()"/> to initialize the
        /// value indicating whether or not to ignore a leading UTF-8 BOM (true).
        /// </summary>
        protected static readonly bool DefaultIgnoreUTF8IdentifierOnFirstHeaderField = true;

        /// <summary>
        /// The value used by <see cref="CsvReaderVisitorWithUTF8HeadersBase()"/> to initialize the
        /// fallback logic when the decoder encounters invalid UTF-8 bytes (throw an exception).
        /// </summary>
        protected static readonly DecoderFallback DefaultDecoderFallback = new CursivelyDecoderExceptionFallback();

        private static readonly UTF8Encoding EncodingToUse = new UTF8Encoding(false, false);

        private readonly Decoder _headerDecoder;

        private readonly ImmutableArray<string>.Builder _headersBuilder;

        private readonly bool _ignoreUTF8IdentifierOnFirstHeaderField;

        private char[] _headerBuffer;

        private ImmutableArray<string> _headers;

        private int _headerBufferConsumed;

        private int _currentFieldIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvReaderVisitorWithUTF8HeadersBase"/> class.
        /// </summary>
        protected CsvReaderVisitorWithUTF8HeadersBase()
            : this(maxHeaderCount: DefaultMaxHeaderCount,
                   maxHeaderLength: DefaultMaxHeaderLength,
                   ignoreUTF8IdentifierOnFirstHeaderField: DefaultIgnoreUTF8IdentifierOnFirstHeaderField,
                   decoderFallback: DefaultDecoderFallback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvReaderVisitorWithUTF8HeadersBase"/> class.
        /// </summary>
        /// <param name="maxHeaderCount">
        /// The maximum number of headers to allow.
        /// Default: <see cref="DefaultMaxHeaderCount"/>.
        /// </param>
        /// <param name="maxHeaderLength">
        /// The maximum length, in UTF-16 code units, of any particular header.
        /// Default: <see cref="DefaultMaxHeaderLength"/>.
        /// </param>
        /// <param name="ignoreUTF8IdentifierOnFirstHeaderField">
        /// A value indicating whether or not to ignore a leading UTF-8 BOM.
        /// Default: <see cref="DefaultIgnoreUTF8IdentifierOnFirstHeaderField"/>.
        /// </param>
        /// <param name="decoderFallback">
        /// The fallback logic used when the decoder encounters invalid UTF-8 bytes.
        /// Default: <see cref="DefaultDecoderFallback"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="decoderFallback"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxHeaderCount"/> or <paramref name="maxHeaderLength"/> is
        /// less than 1.
        /// </exception>
        protected CsvReaderVisitorWithUTF8HeadersBase(int maxHeaderCount, int maxHeaderLength, bool ignoreUTF8IdentifierOnFirstHeaderField, DecoderFallback decoderFallback)
        {
            if (maxHeaderCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHeaderCount), maxHeaderCount, "Must be greater than zero.");
            }

            if (maxHeaderLength < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHeaderLength), maxHeaderLength, "Must be greater than zero.");
            }

            if (decoderFallback is null)
            {
                throw new ArgumentNullException(nameof(decoderFallback));
            }

            _ignoreUTF8IdentifierOnFirstHeaderField = ignoreUTF8IdentifierOnFirstHeaderField;

            _headersBuilder = ImmutableArray.CreateBuilder<string>(maxHeaderCount);

            _headerBuffer = new char[maxHeaderLength];

            _headerDecoder = EncodingToUse.GetDecoder();
            _headerDecoder.Fallback = decoderFallback;
        }

        /// <summary>
        /// <para>
        /// Gets the headers of the CSV stream.
        /// </para>
        /// <para>
        /// Only valid after <see cref="VisitEndOfHeaderRecord"/> has been called.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when trying to access this value before <see cref="VisitEndOfHeaderRecord"/> has
        /// been called.
        /// </exception>
        /// <remarks>
        /// Once initialized, the value will remain the same for as long as this object instance
        /// stays alive.
        /// </remarks>
        protected ImmutableArray<string> Headers
        {
            get
            {
                if (_headers.IsDefault)
                {
                    ThrowExceptionWhenHeadersAreStillBeingBuilt();
                }

                return _headers;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowExceptionWhenHeadersAreStillBeingBuilt() =>
            throw new InvalidOperationException("Headers are still being built.");

        /// <summary>
        /// Gets the zero-based index of the field that is currently being read.  The value should
        /// be the length of <see cref="Headers"/> during <see cref="VisitEndOfHeaderRecord"/> and
        /// <see cref="VisitEndOfDataRecord"/>, except after <see cref="VisitMissingDataFields"/> or
        /// <see cref="VisitUnexpectedDataField"/> has been called.
        /// </summary>
        protected int CurrentFieldIndex => _currentFieldIndex;

        /// <inheritdoc />
        public sealed override void VisitPartialFieldContents(ReadOnlySpan<byte> chunk)
        {
            if (_headers.IsDefault || _currentFieldIndex >= _headers.Length)
            {
                VisitPartialFieldContentsSlow(chunk);
            }
            else
            {
                VisitPartialDataFieldContents(chunk);
            }
        }

        /// <inheritdoc />
        public sealed override void VisitEndOfField(ReadOnlySpan<byte> chunk)
        {
            if (_headers.IsDefault || _currentFieldIndex >= _headers.Length)
            {
                VisitEndOfFieldSlow(chunk);
            }
            else
            {
                VisitEndOfDataField(chunk);
                ++_currentFieldIndex;
            }
        }

        /// <inheritdoc />
        public sealed override void VisitEndOfRecord()
        {
            if (_headers.IsDefault || _currentFieldIndex != _headers.Length)
            {
                VisitEndOfRecordSlow();
            }
            else
            {
                VisitEndOfDataRecord();
                _currentFieldIndex = 0;
            }
        }

        /// <summary>
        /// <para>
        /// Notifies that all headers have been read and <see cref="Headers"/> is safe to read.
        /// </para>
        /// <para>
        /// The default behavior is to do nothing.
        /// </para>
        /// </summary>
        protected virtual void VisitEndOfHeaderRecord() { }

        /// <summary>
        /// Visits part of a non-header field's data.
        /// </summary>
        /// <param name="chunk">
        /// The data from this part of the field.
        /// </param>
        /// <remarks>
        /// See documentation for <see cref="CsvReaderVisitorBase.VisitPartialFieldContents"/> for
        /// details about when and how this method will be called.
        /// </remarks>
        protected abstract void VisitPartialDataFieldContents(ReadOnlySpan<byte> chunk);

        /// <summary>
        /// Visits the last part of a non-header field's data.
        /// </summary>
        /// <param name="chunk">
        /// The data from the last part of the field.
        /// </param>
        /// <remarks>
        /// See documentation for <see cref="CsvReaderVisitorBase.VisitEndOfField"/> for
        /// details about when and how this method will be called.
        /// </remarks>
        protected abstract void VisitEndOfDataField(ReadOnlySpan<byte> chunk);

        /// <summary>
        /// Notifies that all fields in the current non-header record have been visited.
        /// </summary>
        /// <remarks>
        /// See documentation for <see cref="CsvReaderVisitorBase.VisitEndOfRecord"/> for
        /// details about when and how this method will be called.
        /// </remarks>
        protected abstract void VisitEndOfDataRecord();

        /// <summary>
        /// <para>
        /// Notifies that the current non-header record is about to be terminated without reading
        /// all the fields that were identified in the header record.
        /// </para>
        /// <para>
        /// The default behavior is to throw <see cref="CursivelyMissingDataFieldsException"/>.
        /// </para>
        /// </summary>
        protected virtual void VisitMissingDataFields()
        {
            if (_headers.IsDefault)
            {
                // we will never do this, but a cheeky subclass might.
                throw new InvalidOperationException("This method is only intended to be called by the base class.");
            }

            throw new CursivelyMissingDataFieldsException(_headers.Length, _currentFieldIndex);
        }

        /// <summary>
        /// <para>
        /// Notifies that data for a field is about to be read on a non-header record, but all the
        /// fields that were identified in the header record have already been read.
        /// </para>
        /// <para>
        /// This method is called before every single <see cref="VisitPartialDataFieldContents"/> or
        /// <see cref="VisitEndOfDataField"/> call for fields not present in the header record.
        /// </para>
        /// <para>
        /// The default behavior is to throw <see cref="CursivelyExtraDataFieldsException"/>.
        /// </para>
        /// </summary>
        protected virtual void VisitUnexpectedDataField()
        {
            if (_headers.IsDefault)
            {
                // we will never do this, but a cheeky subclass might.
                throw new InvalidOperationException("This method is only intended to be called by the base class.");
            }

            throw new CursivelyExtraDataFieldsException(_headers.Length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void VisitPartialFieldContentsSlow(ReadOnlySpan<byte> chunk)
        {
            if (_headers.IsDefault)
            {
                if (_headersBuilder.Capacity == _headersBuilder.Count)
                {
                    throw new CursivelyTooManyHeadersException(_headersBuilder.Capacity);
                }

                if (chunk.IsEmpty)
                {
                    // the tokenizer will never do this, but an external caller might.
                    return;
                }

                fixed (byte* b = &chunk[0])
                {
                    VisitHeaderChunk(b, chunk.Length, false);
                }
            }
            else
            {
                Debug.Assert(_currentFieldIndex >= _headers.Length, "Another condition brought us into VisitPartialFieldContentsSlow without updating this bit.");
                VisitUnexpectedDataField();
                VisitPartialDataFieldContents(chunk);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void VisitEndOfFieldSlow(ReadOnlySpan<byte> chunk)
        {
            if (_headers.IsDefault)
            {
                if (_headersBuilder.Capacity == _headersBuilder.Count)
                {
                    throw new CursivelyTooManyHeadersException(_headersBuilder.Capacity);
                }

                if (chunk.IsEmpty)
                {
                    // the tokenizer will never do this, but an external caller might.  note that
                    // the Decoder methods require a non-null pointer, even if the length is zero.
                    byte b = 0xFF;
                    VisitHeaderChunk(&b, 0, true);
                }
                else
                {
                    fixed (byte* b = &chunk[0])
                    {
                        VisitHeaderChunk(b, chunk.Length, true);
                    }
                }

                int headerBufferOffset = 0;

                if (_headersBuilder.Count == 0 &&
                    _ignoreUTF8IdentifierOnFirstHeaderField &&
                    _headerBufferConsumed > 0 &&
                    _headerBuffer[0] == '\uFEFF')
                {
                    headerBufferOffset = 1;
                }

                _headersBuilder.Add(new string(_headerBuffer, headerBufferOffset, _headerBufferConsumed - headerBufferOffset));
                _headerBufferConsumed = 0;
                ++_currentFieldIndex;
            }
            else
            {
                Debug.Assert(_currentFieldIndex >= _headers.Length, "Another condition brought us into VisitEndOfFieldSlow without updating this bit.");
                VisitUnexpectedDataField();
                VisitEndOfDataField(chunk);
                _currentFieldIndex = checked(_currentFieldIndex + 1);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void VisitEndOfRecordSlow()
        {
            if (_headers.IsDefault)
            {
                if (_headersBuilder.Count == 0)
                {
                    // the tokenizer will never do this, but an external caller might.
                    throw new InvalidOperationException("No fields were present in the header record.");
                }

                // this is almost equivalent to setting _headers = _headersBuilder.ToImmutable(),
                // but this does a better job rewarding people for setting the max field count to
                // the actual field count, which will often be the case.
                _headersBuilder.Capacity = _headersBuilder.Count;
                _headers = _headersBuilder.MoveToImmutable();
                _currentFieldIndex = _headers.Length;

                // we're done building headers, so free up our buffer.
                _headerBuffer = null;

                // let the subclass know that the headers are ready, in case it wants to set up some
                // stuff before the field data starts rolling in.
                VisitEndOfHeaderRecord();
            }
            else
            {
                Debug.Assert(_currentFieldIndex != _headers.Length, "Another condition brought us into VisitEndOfRecordSlow without updating this bit.");
                if (_currentFieldIndex < _headers.Length)
                {
                    VisitMissingDataFields();
                }

                VisitEndOfDataRecord();
            }

            _currentFieldIndex = 0;
        }

        private unsafe void VisitHeaderChunk(byte* b, int byteCount, bool flush)
        {
            int charCount = _headerDecoder.GetCharCount(b, byteCount, flush);
            if (_headerBufferConsumed + charCount <= _headerBuffer.Length)
            {
                fixed (char* c = &_headerBuffer[_headerBufferConsumed])
                {
                    _headerDecoder.GetChars(b, byteCount, c, charCount, flush);
                }
            }
            else
            {
                throw new CursivelyHeaderIsTooLongException(_headerBuffer.Length);
            }

            _headerBufferConsumed += charCount;
        }
    }
}
