using System;
using System.Collections;
using System.Collections.Generic;

namespace FileHelpers
{
    /// <summary>
    /// Indicates that a string value can't be converted to a dest type.
    /// </summary>
    [Serializable]
    public sealed class ConvertException : FileHelpersException
    {
        #region "  Fields & Property  "

        /// <summary>The destination type.</summary>
        public Type FieldType { get; private set; }

        /// <summary>The value that can't be converted. (null for unknown)</summary>
        public string FieldStringValue { get; private set; }

        /// <summary>Extra info about the error.</summary>
        public Func<string, string> MessageExtra { get; private set; }

        /// <summary>The message without the Line, Column and FieldName.</summary>
        //public string MessageOriginal { get; private set; }

        /// <summary>The name of the field related to the exception. (null for unknown)</summary>
        public string FieldName { get; internal set; }

        /// <summary>The line where the error was found. (-1 is unknown)</summary>
        public int LineNumber { get; internal set; }

        /// <summary>The estimate column where the error was found. (-1 is unknown)</summary>
        public int ColumnNumber { get; internal set; }
        public string CodeExtra { get; private set; }

        #endregion

        #region "  Constructors  "

        /// <summary>
        /// Create a new ConvertException object
        /// </summary>
        /// <param name="origValue">The value to convert.</param>
        /// <param name="destType">The destination Type.</param>
        public ConvertException(string origValue, Type destType)
            : this(origValue, destType, "", FileHelpersException.SimpleMessageFunc) { }


        /// <summary>
        /// Create a new ConvertException object
        /// </summary>
        /// <param name="origValue">The value to convert.</param>
        /// <param name="destType">The destination Type.</param>
        /// <param name="extraInfo">Additional info of the error.</param>
        public ConvertException(string origValue, Type destType, string extraCode, Func<string, string> extraInfo)
            : this(origValue, destType, string.Empty, -1, -1, extraCode, extraInfo, null) { }

        /// <summary>
        /// Create a new ConvertException object
        /// </summary>
        /// <param name="origValue">The value to convert.</param>
        /// <param name="destType">The destination Type.</param>
        /// <param name="extraInfo">Additional info of the error.</param>
        /// <param name="columnNumber">The estimated column number.</param>
        /// <param name="lineNumber">The line where the error was found.</param>
        /// <param name="fieldName">The name of the field with the error</param>
        /// <param name="innerEx">The Inner Exception</param>
        public ConvertException(string origValue,
            Type destType,
            string fieldName,
            int lineNumber,
            int columnNumber,
            string extraCode,
            Func<string, string> extraInfo,
            Exception innerEx)
            : base("FileHelperMsg_ConversionError", MessageBuilder(origValue, destType, fieldName, lineNumber, columnNumber, extraInfo), innerEx)
        {
            //MessageOriginal = string.Empty;
            FieldStringValue = origValue;
            FieldType = destType;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            FieldName = fieldName;
            CodeExtra = extraCode;
            MessageExtra = extraInfo;

            //if (origValue != null && destType != null)
            //   MessageOriginal = MessageBuilder(origValue, destType, fieldName, lineNumber, columnNumber, extraInfo)("Line: {0}. Column: {1}. Field: {2}. Error Converting '{3}' to type: '{4}'.");
        }

        private static Func<string, string> MessageBuilder(string origValue,
            Type destType,
            string fieldName,
            int lineNumber,
            int columnNumber,
            Func<string, string> extraInfo)
        {
            /*var res = "Line: {0}. Column: {1}. Field: {2}. Error Converting '{3}' to type: '{4}'.";
            if (lineNumber >= 0)
                res += "Line: " + lineNumber.ToString() + ". ";

            if (columnNumber >= 0)
                res += "Column: " + columnNumber.ToString() + ". ";

            if (!string.IsNullOrEmpty(fieldName))
                res += "Field: " + fieldName + ". ";

            if (origValue != null && destType != null)
                res += "Error Converting '" + origValue + "' to type: '" + destType.Name + "'. ";
            
            res += extraInfo;

            return res;*/
            //!"Line: {0}. Column: {1}. Field: {2}. Error Converting '{3}' to type: '{4}'."
            return (s) => { return String.Format(s, /*lineNumber.ToString(), columnNumber.ToString(), fieldName,*/ origValue, destType?.Name); };
        }

        #endregion
    }
}