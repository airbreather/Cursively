using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively
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
        protected CsvAsyncInput(byte delimiter)
            : base(delimiter)
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
