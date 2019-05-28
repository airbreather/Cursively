using System;
using System.Runtime.CompilerServices;

namespace Cursively
{
    /// <summary>
    /// Tokenizes a byte stream into CSV fields.  The processing follows the guidelines set out in
    /// RFC 4180 unless and until the stream proves to be in an incompatible format, in which case a
    /// set of additional rules kick in to ensure that all streams are still compatible.
    /// <para>
    /// The byte stream is tokenized according to the rules of the ASCII encoding.  This makes it
    /// compatible with any encoding that encodes 0x0A, 0x0D, 0x22, and 0x2C the same way that ASCII
    /// encodes them.  UTF-8 and Extended ASCII SBCS are notable examples of acceptable encodings.
    /// UTF-16 is a notable example of an unacceptable encoding; trying to use this class to process
    /// text encoded in an unacceptable encoding will yield undesirable results without any errors.
    /// </para>
    /// <para>
    /// All bytes that appear in the stream except 0x0A, 0x0D, 0x22, and 0x2C are unconditionally
    /// treated as data and passed through as-is.  It is the consumer's responsibility to handle (or
    /// not handle) NUL bytes, invalid UTF-8, leading UTF-8 BOM, or any other quirks that come with
    /// the territory of text processing.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each instance of this class expects to process all data from one stream, represented as zero
    /// or more <see cref="ProcessNextChunk"/> followed by one <see cref="ProcessEndOfStream"/>,
    /// before moving on to another stream.  An instance may be reused after a stream has been fully
    /// processed, but each instance is also <strong>very</strong> lightweight, so it is recommended
    /// that callers simply create a new instance for each stream that needs to be processed.
    /// </para>
    /// <para>
    /// RFC 4180 leaves a lot of wiggle room for implementers.  The following section explains how
    /// this implementation resolves ambiguities in the spec, explains where and why we deviate from
    /// it, and offers clarifying notes where the spec appears to have "gotchas", in the order that
    /// the relevant items appear in the spec, primarily modeled off of how Josh Close's CsvHelper
    /// library handles the same situations:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// The spec says that separate lines are delimited by CRLF line breaks.  This implementation
    /// accepts line breaks of any format (CRLF, LF, CR).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The spec says that there may or may not be a line break at the end of the last record in the
    /// stream.  This implementation does not require there to be a line break, and it would not
    /// hurt to add one either.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The spec refers to an optional header line at the beginning.  This implementation does not
    /// include any special treatment for the first line of fields; if they need to be treated as
    /// headers, then the consumer needs to know that and respond accordingly.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The spec says each record may contain "one or more fields".  This implementation interprets
    /// that to mean strictly that any number of consecutive newline characters in a row are treated
    /// as one.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Many implementations allow the delimiter character to be configured to be something else
    /// other than a comma.  This implementation does not currently offer that flexibility.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Many implementations allow automatically trimming whitespace at the beginning and/or end of
    /// each field (sometimes optionally).  The spec expressly advises against doing that, and this
    /// implementation follows suit.  It is our opinion that consumers ought to be more than capable
    /// of trimming spaces at the beginning or end as part of their processing if this is desired.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The spec says that the last field in a record must not be followed by a comma.  This
    /// implementation interprets that to mean that if we do see a comma followed immediately by a
    /// line ending character, then that represents the data for an empty field.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// Finally, the spec has a lot to say about double quotes.  This implementation follows the
    /// rules that it expressly lays out, but there are some "gotchas" that follow from the spec
    /// leaving it open-ended how implementations should deal with various streams that include
    /// double quotes which do not completely enclose fields, resolved as follows:
    /// </para>
    /// <para>
    /// If a double quote is encountered at the very beginning of a field, then all characters up
    /// until the next unescaped double quote or the end of the stream (whichever comes first) are
    /// considered to be part of the data for that field (we do translate escaped double quotes for
    /// convenience).  This includes line ending characters, even though Excel seems to only make
    /// that happen if the field counts matching up.  If parsing stopped at an unescaped double
    /// quote, but there are still more bytes after that double quote before the next delimiter,
    /// then all those bytes will be treated verbatim as part of the field's data (double quotes are
    /// no longer special at all for the remainder of the field).
    /// </para>
    /// <para>
    /// Double quotes encountered at any other point are included verbatim as part of the field with
    /// no special processing.
    /// </para>
    /// <para>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var visitor = new MyVisitorSubclass();
    /// var tokenizer = new CsvTokenizer();
    /// tokenizer.ProcessNextChunk(File.ReadAllBytes("..."), visitor);
    /// tokenizer.ProcessEndOfStream(visitor);
    /// ]]>
    /// </code>
    /// </example>
    /// </para>
    /// <para>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using (var stream = File.OpenRead("..."))
    /// {
    ///     var visitor = new MyVisitorSubclass();
    ///     var tokenizer = new CsvTokenizer();
    ///     var buffer = new byte[81920];
    ///     int lastRead;
    ///     while ((lastRead = stream.Read(buffer, 0, buffer.Length)) != 0)
    ///     {
    ///         tokenizer.ProcessNextChunk(new ReadOnlySpan<byte>(buffer, 0, lastRead), visitor);
    ///     }
    ///
    ///     tokenizer.ProcessEndOfStream(visitor);
    /// }
    /// ]]>
    /// </code>
    /// </example>
    /// </para>
    /// </remarks>
    public class CsvTokenizer
    {
        private const byte CR = (byte)'\r';

        private const byte LF = (byte)'\n';

        private const byte QUOTE = (byte)'"';

        private readonly byte _delimiter;

        private ParserFlags _parserFlags;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvTokenizer"/> class.
        /// </summary>
        public CsvTokenizer()
            : this((byte)',')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvTokenizer"/> class.
        /// </summary>
        /// <param name="delimiter">
        /// The single byte to expect to see between fields of the same record.  This may not be an
        /// end-of-line or double-quote character, as those have special meanings.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="delimiter"/> is <code>0x0A</code>, <code>0x0D</code>, or
        /// <code>0x22</code>.
        /// </exception>
        public CsvTokenizer(byte delimiter)
        {
            switch (delimiter)
            {
                case CR:
                case LF:
                case QUOTE:
                    throw new ArgumentException("Must not be a carriage return, linefeed, or double-quote.", nameof(delimiter));

                default:
                    _delimiter = delimiter;
                    break;
            }
        }

        [Flags]
        private enum ParserFlags : byte
        {
            None,
            ReadAnythingOnCurrentLine = 0b00000001,
            ReadAnythingInCurrentField = 0b00000010,
            CurrentFieldStartedWithQuote = 0b00000100,
            QuotedFieldDataEnded = 0b00001000,
            CutAtPotentiallyTerminalDoubleQuote = 0b00010000,
        }

        /// <summary>
        /// Accepts the next (or first) chunk of data in the CSV stream, and informs an instance of
        /// <see cref="CsvReaderVisitorBase"/> what it contains.
        /// </summary>
        /// <param name="chunk">
        /// A <see cref="ReadOnlySpan{T}"/> containing the next chunk of data.
        /// </param>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> to interact with, or <see langword="null"/> if we
        /// should simply advance the parser state.
        /// </param>
        /// <remarks>
        /// If <paramref name="chunk"/> is empty, this method will do nothing.
        /// </remarks>
        public void ProcessNextChunk(ReadOnlySpan<byte> chunk, CsvReaderVisitorBase visitor)
        {
            if (visitor is null)
            {
                // "null object" pattern.
                visitor = CsvReaderVisitorBase.Null;
            }

            byte delimiter = _delimiter;

            // we're going to consume the entire buffer that was handed to us.
            while (!chunk.IsEmpty)
            {
                if ((_parserFlags & ParserFlags.ReadAnythingInCurrentField) != 0)
                {
                    // most of the time, we should be able to fully process each field in the same
                    // loop iteration that we first start reading it.  the most prominent exception
                    // is when we encounter a quoted field.
                    PickUpFromLastTime(ref chunk, visitor);
                    continue;
                }

                for (int idx = 0; idx < chunk.Length; idx++)
                {
                    byte c = chunk[idx];
                    if (c == QUOTE)
                    {
                        if (idx == 0)
                        {
                            _parserFlags = ParserFlags.CurrentFieldStartedWithQuote | ParserFlags.ReadAnythingInCurrentField | ParserFlags.ReadAnythingOnCurrentLine;
                        }
                        else
                        {
                            // RFC 4180 forbids quotes that show up anywhere but the beginning of a
                            // field, so it's up to us to decide what we want to do about this.  We
                            // choose to treat all such quotes as just regular data.
                            visitor.VisitPartialFieldContents(chunk.Slice(0, idx + 1));
                            _parserFlags = ParserFlags.ReadAnythingInCurrentField | ParserFlags.ReadAnythingOnCurrentLine;
                        }
                    }
                    else if (c == delimiter)
                    {
                        visitor.VisitEndOfField(chunk.Slice(0, idx));
                        _parserFlags = ParserFlags.ReadAnythingOnCurrentLine;
                    }
                    else if (c == CR || c == LF)
                    {
                        ProcessEndOfLine(chunk.Slice(0, idx), visitor);
                    }
                    else
                    {
                        continue;
                    }

                    chunk = chunk.Slice(idx + 1);
                    goto nextLoop;
                }

                visitor.VisitPartialFieldContents(chunk);
                _parserFlags = ParserFlags.ReadAnythingInCurrentField | ParserFlags.ReadAnythingOnCurrentLine;
                break;

                nextLoop:;
            }
        }

        /// <summary>
        /// Informs this tokenizer that the last chunk of data in the stream has been read, and so
        /// we should make any final interactions with the <see cref="CsvReaderVisitorBase"/> and
        /// reset our state to prepare for the next stream.
        /// </summary>
        /// <param name="visitor">
        /// The <see cref="CsvReaderVisitorBase"/> to interact with, or <see langword="null"/> if we
        /// should simply advance the parser state.
        /// </param>
        /// <remarks>
        /// <para>
        /// If <see cref="ProcessNextChunk"/> has never been called (or has not been called since
        /// the last time that this method was called), then this method will do nothing.
        /// </para>
        /// </remarks>
        public void ProcessEndOfStream(CsvReaderVisitorBase visitor)
        {
            if (visitor is null)
            {
                visitor = CsvReaderVisitorBase.Null;
            }

            ProcessEndOfLine(default, visitor);
        }

        private void PickUpFromLastTime(ref ReadOnlySpan<byte> readBuffer, CsvReaderVisitorBase visitor)
        {
            if ((_parserFlags & ParserFlags.CutAtPotentiallyTerminalDoubleQuote) != 0)
            {
                HandleBufferCutAtPotentiallyTerminalDoubleQuote(ref readBuffer, visitor);
                return;
            }

            if ((_parserFlags & (ParserFlags.CurrentFieldStartedWithQuote | ParserFlags.QuotedFieldDataEnded)) == ParserFlags.CurrentFieldStartedWithQuote)
            {
                int idx = readBuffer.IndexOf(QUOTE);
                if (idx < 0)
                {
                    visitor.VisitPartialFieldContents(readBuffer);
                    readBuffer = default;
                    return;
                }

                // the double quote we stopped at was either escaping a literal double quote, or it
                // represented the end of a quoted field.  we will usually have at least one more
                // byte ready for us (except in contrived cases), and so it should almost always pay
                // off to try to look ahead by one more byte to see if we can avoid a Partial call.
                if (idx == readBuffer.Length - 1)
                {
                    // in fact, it should pay off so well in so many cases that we can probably even
                    // get away with making the other case really suboptimal, which is what it will
                    // do when we pick up where we leave off after setting this flag.
                    visitor.VisitPartialFieldContents(readBuffer.Slice(0, idx));
                    _parserFlags |= ParserFlags.CutAtPotentiallyTerminalDoubleQuote;
                    readBuffer = default;
                    return;
                }

                // we have at least one more byte, so let's see what the double quote actually means
                byte b = readBuffer[idx + 1];
                if (b == QUOTE)
                {
                    // the double quote we stopped at was escaping a literal double quote, so we
                    // send everything up to and including the escaping quote.
                    visitor.VisitPartialFieldContents(readBuffer.Slice(0, idx + 1));
                }
                else if (b == _delimiter)
                {
                    // the double quote was the end of a quoted field, so send the entire data from
                    // the beginning of this quoted field data chunk up to the double quote that
                    // terminated it (excluding, of course, the double quote itself).
                    visitor.VisitEndOfField(readBuffer.Slice(0, idx));
                    _parserFlags = ParserFlags.ReadAnythingOnCurrentLine;
                }
                else if (b == CR || b == LF)
                {
                    // same thing as the delimiter case, just the field ended at the end of a line
                    // instead of the end of a field on the current line.
                    ProcessEndOfLine(readBuffer.Slice(0, idx), visitor);
                }
                else
                {
                    // the double quote was the end of the quoted part of the field data, but then
                    // it continues on with more data; don't spend too much time optimizing this
                    // case since it's not RFC 4180, just do the parts we need to do in order to
                    // behave the way we said we would.
                    _parserFlags |= ParserFlags.QuotedFieldDataEnded;
                    visitor.VisitPartialFieldContents(readBuffer.Slice(0, idx));
                    visitor.VisitPartialFieldContents(readBuffer.Slice(idx + 1, 1));
                }

                // slice off the data up to the quote and the next byte that we read.
                readBuffer = readBuffer.Slice(idx + 2);
            }
            else
            {
                // this is expected to be rare: either we were cut between field reads, or we're
                // reading nonstandard field data where there's a quote that neither starts nor ends
                // the field.
                for (int idx = 0; idx < readBuffer.Length; idx++)
                {
                    byte b = readBuffer[idx];
                    if (b == _delimiter)
                    {
                        visitor.VisitEndOfField(readBuffer.Slice(0, idx));
                        _parserFlags = ParserFlags.ReadAnythingOnCurrentLine;
                    }
                    else if (b == CR || b == LF)
                    {
                        ProcessEndOfLine(readBuffer.Slice(0, idx), visitor);
                    }
                    else
                    {
                        continue;
                    }

                    readBuffer = readBuffer.Slice(idx + 1);
                    return;
                }

                visitor.VisitPartialFieldContents(readBuffer);
                readBuffer = default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleBufferCutAtPotentiallyTerminalDoubleQuote(ref ReadOnlySpan<byte> readBuffer, CsvReaderVisitorBase visitor)
        {
            // this method is only called in the rare case where the very last character of the last
            // read buffer was a stopping double quote while we were reading quoted field data, so
            // this method is expected to be called so rarely in performance-sensitive cases that I
            // don't think it will ever pay off to bother doing more processing here.  so we just do
            // the minimum amount that we need to do in order to clear this flag and get back into
            // the normal swing of things.
            _parserFlags &= ~ParserFlags.CutAtPotentiallyTerminalDoubleQuote;
            if (readBuffer[0] == QUOTE)
            {
                // the previous double quote was actually there to escape this double quote.  we
                // didn't visit the double-quote last time because we weren't sure.  well, we're
                // sure now, so go ahead and do it.
                visitor.VisitPartialFieldContents(readBuffer.Slice(0, 1));

                // we processed the double quote, so main loop should resume at the next byte.
                readBuffer = readBuffer.Slice(1);
            }
            else
            {
                // the previous double quote did in fact terminate the quoted part of the field
                // data, and so all we need to do is set this flag..  main loop will re-process this
                // buffer and go about its merry way.
                _parserFlags |= ParserFlags.QuotedFieldDataEnded;
            }
        }

        private void ProcessEndOfLine(ReadOnlySpan<byte> lastFieldDataChunk, CsvReaderVisitorBase visitor)
        {
            if (!lastFieldDataChunk.IsEmpty || (_parserFlags & ParserFlags.ReadAnythingOnCurrentLine) != 0)
            {
                // even if the last field data chunk is empty, we still need to send it: we might be
                // looking at a newline that immediately follows a comma, which is defined to mean
                // an empty field at the end of a line.
                visitor.VisitEndOfField(lastFieldDataChunk);
                visitor.VisitEndOfRecord();
            }

            _parserFlags = ParserFlags.None;
        }
    }
}
