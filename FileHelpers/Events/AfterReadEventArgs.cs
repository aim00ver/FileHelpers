using System;
using System.Collections.Generic;

namespace FileHelpers.Events
{
    /// <summary>Arguments for the <see cref="AfterReadHandler{T}"/></summary>
    public class AfterReadEventArgs
        : ReadEventArgs
        
    {
        /// <summary>
        /// After the record is read,  allow details to be inspected.
        /// </summary>
        /// <param name="engine">Engine that parsed the record</param>
        /// <param name="line">Record that was analysed</param>
        /// <param name="lineChanged">Was it changed before</param>
        /// <param name="lineNumber">Record number read</param>
        internal AfterReadEventArgs(EngineBase engine,
            string line,
            bool lineChanged,
            int lineNumber)
            : base(engine, line, lineNumber)
        {
            SkipThisRecord = false;
            RecordLineChanged = lineChanged;
        }

    }

    /// <summary>Arguments for the <see cref="AfterReadHandler{T}"/></summary>
    public class AfterReadEventArgs<T>
        : AfterReadEventArgs
        where T : class
    {
        /// <summary>
        /// After the record is read,  allow details to be inspected.
        /// </summary>
        /// <param name="engine">Engine that parsed the record</param>
        /// <param name="line">Record that was analysed</param>
        /// <param name="lineChanged">Was it changed before</param>
        /// <param name="newRecord">Object created</param>
        /// <param name="lineNumber">Record number read</param>
        internal AfterReadEventArgs(EventEngineBase<T> engine,
            string line,
            bool lineChanged,
            T newRecord,
            Tuple<int, int>[] valuesPosition,
            int lineNumber)
            : base(engine, line, lineChanged, lineNumber)
        {
            Record = newRecord;
            ValuesPosition = valuesPosition;
        }

        /// <summary>The current record.</summary>
        public T Record { get; set; }
        public Tuple<int, int>[] ValuesPosition { get; set; }
    }

    public class AfterReadWithDetailsEventArgs<T> : AfterReadEventArgs<T> where T : class
    {
        internal AfterReadWithDetailsEventArgs(EventEngineBase<T> engine,
            string line,
            bool lineChanged,
            T newRecord,
            int lineNumber,
            Tuple<int, int>[] masterValuesPosition,
            Dictionary<Type, List<Tuple<int, int>>> detailsValuesPosition) : base(engine, line, lineChanged, newRecord, masterValuesPosition, lineNumber)
        {
            //MasterLocation = masterLocation;
            DetailsLocations = detailsValuesPosition;
        }
        //public Tuple<int, int> MasterLocation { get; set; }
        public Dictionary<Type, List<Tuple<int, int>>> DetailsLocations { get; set; }
    }
}