using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public abstract class CsvInputTestBase
    {
        protected void RunTest(CsvInput sut, string filePath, byte delimiter, bool ignoreByteOrderMark)
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(filePath);
            int offset = 0;
            if (ignoreByteOrderMark)
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

        protected async ValueTask RunTestAsync(CsvAsyncInput sut, string filePath, byte delimiter, bool ignoreByteOrderMark)
        {
            // arrange
            byte[] fileData = File.ReadAllBytes(filePath);
            int offset = 0;
            if (ignoreByteOrderMark)
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
            var progress = new Progress<int>(readSoFar.Add);
            for (int i = 0; i < 3; i++)
            {
                // arrange
                var inputVisitor = new StringBufferingVisitor(fileData.Length);

                // act
                await sut.ProcessAsync(inputVisitor, progress).ConfigureAwait(false);

                // assert
                Assert.Equal(expected, inputVisitor.Records);

                if (i < 2)
                {
                    Assert.Equal(fileData.Length, readSoFar.Sum());
                    Assert.Equal(0, readSoFar.Last());

                    readSoFar.Clear();

                    if (i == 1)
                    {
                        // now do one without progress reporting
                        progress = null;
                    }

                    sut.TryReset();
                }
            }
        }
    }
}
