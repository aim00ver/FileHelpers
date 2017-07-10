using System;
using System.Collections.Generic;

namespace FileHelpers.DataLink
{
    /// <summary>Indicates the wrong usage of the ExcelStorage of the library.</summary>
    [Serializable]
    public sealed class ExcelBadUsageException : BadUsageException
    {
        /// <summary>Creates an instance of an ExcelBadUsageException.</summary>
        /// <param name="message">The exception Message</param>
        internal ExcelBadUsageException(string messageCode, List<string> messageParams)
            : base(messageCode, messageParams) { }

//		/// <summary>Creates an instance of an ExcelBadUsageException.</summary>
//		/// <param name="message">The exception Message</param>
//		/// <param name="innerEx">The inner Exception.</param>
//		internal ExcelBadUsageException(string message, Exception innerEx) : base(message, innerEx)
//		{
//		}
    }
}