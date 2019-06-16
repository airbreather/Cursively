namespace Cursively.Processing
{
    /// <summary>
    /// 
    /// </summary>
    public static class CsvOperation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static CsvCountRecordsOperation CountRecords() => new CsvCountRecordsOperation();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static CsvWriteFlattenedOperation WriteFlattened() => new CsvWriteFlattenedOperation();
    }
}
