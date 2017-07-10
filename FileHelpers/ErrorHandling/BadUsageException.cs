using System;
using System.Collections;
using System.Collections.Generic;

namespace FileHelpers
{
    /// <summary>Indicates the wrong usage of the library.</summary>
    [Serializable]
    public class BadUsageException : FileHelpersException
    {/*
        /// <summary>Creates an instance of an BadUsageException.</summary>
        /// <param name="message">The exception Message</param>
        protected internal BadUsageException(string message)
            : base(message) {}
        */
        /// <summary>Creates an instance of an BadUsageException.</summary>
        /// <param name="message">The exception Message</param>
        /// <param name="line">The line number where the problem was found</param>
        /// <param name="column">The column number where the problem was found</param>
        protected internal BadUsageException(int line, int column, string messageCode, List<string> messageParams)
            : base(line, column, messageCode, messageParams) {}

        /// <summary>Creates an instance of an BadUsageException.</summary>
        /// <param name="message">The exception Message</param>
        /// <param name="line">Line to display in message</param>
        internal BadUsageException(LineInfo line, string messageCode, List<string> messageParams)
            : this(line.mReader.LineNumber, line.mCurrentPos, messageCode, messageParams) {}

        protected internal BadUsageException(string messageCode, List<string> messageParams, string fieldName = null)
            : base(0, 0, messageCode, messageParams, fieldName) { }
    }
}