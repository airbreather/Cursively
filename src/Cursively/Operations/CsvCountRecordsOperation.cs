namespace Cursively.Operations
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CsvCountRecordsOperation : CsvOperationBase<long>
    {
        private RecordCountingVisitor _visitor;

        internal CsvCountRecordsOperation()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override long Result => _visitor.RecordCount;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override CsvReaderVisitorBase CreateVisitor() =>
            _visitor = new RecordCountingVisitor();
    }
}
