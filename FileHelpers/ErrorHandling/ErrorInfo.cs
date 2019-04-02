using System;
using System.Diagnostics;
using FileHelpers.Helpers;

namespace FileHelpers
{
    /// <summary>
    /// Contains error information of the <see cref="FileHelperEngine"/> class.
    /// </summary>
    [DelimitedRecord("|")]
    [IgnoreFirst(2)]
    [DebuggerDisplay("Line: {LineNumber}. Error: {ExceptionInfo.Message}.")]
    public sealed class ErrorInfo
    {
        public enum ErrorTypeEnum
        {
            Unknown,
            Master,
            Detail
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal ErrorTypeEnum mErrorType;
        public ErrorTypeEnum ErrorType
        {
            get { return mErrorType; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal string mFieldName;
        public string FieldName
        {
            get { return mFieldName; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal Type mFailedOnType;
        /// <summary>The type of record/subrecord that caused the error</summary>
        public Type FailedOnType
        {
            get { return mFailedOnType; }
        }

        /// <summary>
        /// Contains error information of the <see cref="FileHelperEngine"/> class.
        /// </summary>
        internal ErrorInfo() {}

        /// <summary>
        /// Line number of the error
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal int mLineNumber;
        /// <summary>The line number of the error</summary>
        public int LineNumber
        {
            get { return mLineNumber; }
        }

        internal int mDetailIndex = -1;
        public int DetailIndex
        {
            get { return mDetailIndex; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal int mStart;
        /// <summary>The start position of record/subrecord that caused the error</summary>
        public int Start
        {
            get { return mStart; }
        }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal int mLength;
        /// <summary>The length of record/subrecord that caused the error</summary>
        public int Length
        {
            get { return mLength; }
        }

        /// <summary>The string of the record of the error.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [FieldQuoted(QuoteMode.OptionalForBoth)]
        internal string mRecordString = string.Empty;

        /// <summary>The string of the record of the error.</summary>
        public string RecordString
        {
            get { return mRecordString; }
        }

        /// <summary>The name of the record type being used.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [FieldQuoted(QuoteMode.OptionalForBoth)]
        internal string mRecordTypeName = string.Empty;

        /// <summary>>The name of the record type being used.</summary>
        public string RecordTypeName
        {
            get { return mRecordTypeName; }
        }

        /// <summary>The exception that indicates the error.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [FieldConverter(typeof (ExceptionConverter))]
        [FieldQuoted(QuoteMode.OptionalForBoth)]
        internal Exception mExceptionInfo;

        /// <summary>The exception that indicates the error.</summary>
        public Exception ExceptionInfo
        {
            get { return mExceptionInfo; }
        }

        /// <summary>
        /// Converter exception
        /// </summary>
        internal class ExceptionConverter : ConverterBase
        {
            /// <summary>
            /// Convert a field definition to a string
            /// </summary>
            /// <param name="from">Convert exception object</param>
            /// <returns>Field as a string or null</returns>
            public override string FieldToString(object from)
            {
                if (from == null)
                    return String.Empty;
                else {
                    if (from is ConvertException) {
                        return "In the field '" + ((ConvertException) from).FieldName + "': " +
                               ((ConvertException) from).Message.Replace(StringHelper.NewLine, " -> ");
                    }
                    else
                        return ((Exception) from).Message.Replace(StringHelper.NewLine, " -> ");
                }
            }

            /// <summary>
            /// Convert a general exception to a string
            /// </summary>
            /// <param name="from">exception to convert</param>
            /// <returns>Exception from field</returns>
            public override object StringToField(string from)
            {
                return new Exception(from);
            }
        }
    }
}