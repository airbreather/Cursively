using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public abstract class CsvInputTestBase
    {
        protected void RunUTF16Test(CsvInput sut, string filePath, byte delimiter, bool ignoreByteOrderMark)
        {
            // arrange
            var encoding = new UTF8Encoding(false, false);
            byte[] fileBytes = File.ReadAllBytes(filePath);
            string fileData = encoding.GetString(fileBytes);
            int offset = 0;
            if (ignoreByteOrderMark && fileData.Length != 0 && fileData[0] == '\uFEFF')
            {
                offset = 1;
            }

            var expected = TokenizeCsvFileUsingCursively(encoding.GetBytes(fileData, offset, fileData.Length - offset), fileData.Length, delimiter);

            for (int i = 0; i < 3; i++)
            {
                var inputVisitor = new StringBufferingVisitor(fileBytes.Length);

                // act
                sut.Process(inputVisitor);

                // assert
                Assert.Equal(expected, inputVisitor.Records);

                if (!sut.TryReset())
                {
                    break;
                }
            }
        }

        protected async Task RunUTF16TestAsync(CsvAsyncInput sut, string filePath, byte delimiter, bool ignoreByteOrderMark)
        {
            // arrange
            var encoding = new UTF8Encoding(false, false);
            byte[] fileBytes = File.ReadAllBytes(filePath);
            string fileData = encoding.GetString(fileBytes);
            int offset = 0;
            if (ignoreByteOrderMark && fileData.Length != 0 && fileData[0] == '\uFEFF')
            {
                offset = 1;
            }

            var expected = TokenizeCsvFileUsingCursively(encoding.GetBytes(fileData, offset, fileData.Length - offset), fileData.Length, delimiter);

            var readSoFar = new List<int>();
            var progress = new ImmediateProgress<int>(readSoFar.Add);
            for (int i = 0; i < 3; i++)
            {
                // arrange
                var inputVisitor = new StringBufferingVisitor(fileBytes.Length);
                readSoFar.Clear();

                // act
                await sut.ProcessAsync(inputVisitor, progress).ConfigureAwait(true);

                // assert
                Assert.Equal(expected, inputVisitor.Records);

                if (i < 2)
                {
                    Assert.Equal(fileData.Length, readSoFar.Sum());
                    Assert.Equal(0, readSoFar.Last());

                    if (i == 1)
                    {
                        // now do one without progress reporting
                        progress = null;
                    }

                    if (!sut.TryReset())
                    {
                        break;
                    }
                }
            }
        }

        protected void RunBinaryTest(CsvInput sut, string filePath, byte delimiter, bool ignoreUTF8ByteOrderMark)
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(filePath);
            int offset = 0;
            if (ignoreUTF8ByteOrderMark)
            {
                if (fileData.Length > 0)
                {
                    if (fileData[0] == 0xEF)
                    {
                        offset = 1;
                        if (fileData.Length > 1)
                        {
                            if (fileData[1] == 0xBB)
                            {
                                offset = 2;
                                if (fileData.Length > 2)
                                {
                                    if (fileData[2] == 0xBF)
                                    {
                                        offset = 3;
                                    }
                                    else
                                    {
                                        offset = 0;
                                    }
                                }
                            }
                            else
                            {
                                offset = 0;
                            }
                        }
                    }
                    else
                    {
                        offset = 0;
                    }
                }
            }

            var expected = TokenizeCsvFileUsingCursively(new ReadOnlySpan<byte>(fileData, offset, fileData.Length - offset), fileData.Length, delimiter);

            for (int i = 0; i < 3; i++)
            {
                var inputVisitor = new StringBufferingVisitor(fileData.Length);

                // act
                sut.Process(inputVisitor);

                // assert
                Assert.Equal(expected, inputVisitor.Records);

                if (!sut.TryReset())
                {
                    break;
                }
            }
        }

        protected async Task RunBinaryTestAsync(CsvAsyncInput sut, string filePath, byte delimiter, bool ignoreUTF8ByteOrderMark)
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(filePath);
            int offset = 0;
            if (ignoreUTF8ByteOrderMark)
            {
                if (fileData.Length > 0)
                {
                    if (fileData[0] == 0xEF)
                    {
                        offset = 1;
                        if (fileData.Length > 1)
                        {
                            if (fileData[1] == 0xBB)
                            {
                                offset = 2;
                                if (fileData.Length > 2)
                                {
                                    if (fileData[2] == 0xBF)
                                    {
                                        offset = 3;
                                    }
                                    else
                                    {
                                        offset = 0;
                                    }
                                }
                            }
                            else
                            {
                                offset = 0;
                            }
                        }
                    }
                    else
                    {
                        offset = 0;
                    }
                }
            }

            var expected = TokenizeCsvFileUsingCursively(new ReadOnlySpan<byte>(fileData, offset, fileData.Length - offset), fileData.Length, delimiter);

            var readSoFar = new List<int>();
            var progress = new ImmediateProgress<int>(readSoFar.Add);
            for (int i = 0; i < 3; i++)
            {
                // arrange
                var inputVisitor = new StringBufferingVisitor(fileData.Length);
                readSoFar.Clear();

                // act
                await sut.ProcessAsync(inputVisitor, progress).ConfigureAwait(true);

                // assert
                Assert.Equal(expected, inputVisitor.Records);

                if (i < 2)
                {
                    Assert.Equal(fileData.Length, readSoFar.Sum());
                    Assert.Equal(0, readSoFar.Last());

                    if (i == 1)
                    {
                        // now do one without progress reporting
                        progress = null;
                    }

                    if (!sut.TryReset())
                    {
                        break;
                    }
                }
            }
        }

        private sealed class ImmediateProgress<T> : IProgress<T>
        {
            private readonly Action<T> _onReport;

            public ImmediateProgress(Action<T> onReport) =>
                _onReport = onReport;

            public void Report(T value) => _onReport(value);
        }
    }
}
