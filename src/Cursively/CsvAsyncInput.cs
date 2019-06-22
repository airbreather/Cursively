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
        /// <param name="requiresExplicitReset"></param>
        protected CsvAsyncInput(byte delimiter, bool requiresExplicitReset)
            : base(delimiter, requiresExplicitReset)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask ProcessAsync(CsvReaderVisitorBase visitor, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (!TryReset())
            {
                throw new InvalidOperationException("Input has already been consumed and cannot be reset.");
            }

            await ProcessAsync(new CsvTokenizer(Delimiter), visitor, progress, cancellationToken).ConfigureAwait(false);
            _alreadyProcessed = true;
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
