using System;
using System.Collections;
using System.Collections.Generic;

namespace FileHelpers
{
    /// <summary>Base class for all the library Exceptions.</summary>
    [Serializable]
    public class FileHelpersException : Exception
    {/*
        public const string NonLocalizedCode = "FileHelperMsg_NonLocalized";
        public static Func<string, string> SimpleMessageFunc
        {
            get
            {
                return (s) => { return s; };
            }
        }*/
           
        public FileHelpersException(string messageCode, List<string> messageParams)
            : base(messageCode)
        {
            MessageParams = messageParams;
        }

        public FileHelpersException(string messageCode, List<string> messageParams, Exception ex)
            : base(messageCode, ex)
        {
            MessageParams = messageParams;
        }
        /*
        /// <summary>Basic constructor of the exception.</summary>
        /// <param name="messageCode">Message code of the exception.</param>
        public FileHelpersException(string messageCode) : this(messageCode, SimpleMessageFunc)
        {
        }
        */
        /*
        /// <summary>Basic constructor of the exception.</summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerEx">The inner Exception.</param>
        public FileHelpersException(string message, Exception innerEx)
            : base(message, innerEx) {}

        /// <summary>Basic constructor of the exception.</summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="line">The line number where the problem was found</param>
        /// <param name="column">The column number where the problem was found</param>
        public FileHelpersException(int line, int column, string message)
            : base("Line: " + line.ToString() + " Column: " + column.ToString() + ". " + message)
        {
            Line = line;
            Column = column;
        }
        */
        public FileHelpersException(int line, int column, string messageCode, List<string> messageParams, string fieldName = null)
            : this(messageCode, messageParams)
        {
            Line = line;
            Column = column;
            FieldName = fieldName;
        }

        public string FieldName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public List<string> MessageParams;
    }
}