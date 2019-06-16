using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Processing
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CsvAsyncInput : CsvInput
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="mustResetAfterProcessing"></param>
        protected CsvAsyncInput(byte delimiter, bool mustResetAfterProcessing)
            : base(delimiter, mustResetAfterProcessing)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask ProcessAsync(CsvReaderVisitorBase visitor, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return ProcessAsync(new CsvTokenizer(Delimiter), visitor, progress, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <param name="visitor"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract ValueTask ProcessAsync(CsvTokenizer tokenizer, CsvReaderVisitorBase visitor, IProgress<int> progress, CancellationToken cancellationToken);
    }
}
