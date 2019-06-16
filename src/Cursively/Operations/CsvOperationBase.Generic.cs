using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cursively.Operations
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CsvOperationBase<T> : CsvOperationBase
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract T Result { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public new T Run(CsvInput input)
        {
            base.Run(input);
            return Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public new async ValueTask<T> RunAsync(CsvAsyncInput input, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            await base.RunAsync(input, progress, cancellationToken).ConfigureAwait(false);
            return Result;
        }
    }
}
