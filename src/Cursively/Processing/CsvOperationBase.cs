using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Processing
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CsvOperationBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <exception cref="ArgumentNullException"/>
        public void Run(CsvInput input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            input.Process(CreateVisitor());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="ObjectDisposedException"/>
        public async ValueTask RunAsync(CsvAsyncInput input, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            await input.ProcessAsync(CreateVisitor(), progress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected abstract CsvReaderVisitorBase CreateVisitor();
    }
}
