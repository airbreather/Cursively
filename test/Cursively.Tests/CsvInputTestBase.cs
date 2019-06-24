using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using static Cursively.Tests.TestHelpers;

namespace Cursively.Tests
{
    public abstract class CsvInputTestBase
    {
        protected void RunTest(CsvInput sut, string filePath, byte delimiter, bool ignoreByteOrderMark)
        {
            for (int i = 0; i < 3; i++)
            {
                // arrange
                byte[] fileData = File.ReadAllBytes(filePath);
                var inputVisitor = new StringBufferingVisitor(fileData.Length);

                // act
                sut.Process(inputVisitor);

                // assert
                if (ignoreByteOrderMark && fileData.Length >= 3 && fileData[0] == 0xEF && fileData[1] == 0xBB && fileData[2] == 0xBF)
                {
                    byte[] newFileData = new byte[fileData.Length - 3];
                    Buffer.BlockCopy(fileData, 3, newFileData, 0, newFileData.Length);
                    fileData = newFileData;
                }

                var expected = TokenizeCsvFileUsingCursively(fileData, fileData.Length, delimiter);
                Assert.Equal(expected, inputVisitor.Records);

                if (!sut.TryReset())
                {
                    break;
                }
            }
        }

        protected async ValueTask RunTestAsync(CsvAsyncInput sut, string filePath, byte delimiter, bool ignoreByteOrderMark, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < 3; i++)
            {
                // arrange
                byte[] fileData = File.ReadAllBytes(filePath);
                var inputVisitor = new StringBufferingVisitor(fileData.Length);

                // act
                await sut.ProcessAsync(inputVisitor, progress, cancellationToken).ConfigureAwait(false);

                // assert
                if (ignoreByteOrderMark && fileData.Length >= 3 && fileData[0] == 0xEF && fileData[1] == 0xBB && fileData[2] == 0xBF)
                {
                    byte[] newFileData = new byte[fileData.Length - 3];
                    Buffer.BlockCopy(fileData, 3, newFileData, 0, newFileData.Length);
                    fileData = newFileData;
                }

                var expected = TokenizeCsvFileUsingCursively(fileData, fileData.Length, delimiter);
                Assert.Equal(expected, inputVisitor.Records);

                if (!sut.TryReset())
                {
                    break;
                }
            }
        }
    }
}
