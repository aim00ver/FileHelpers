using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace FileHelpers.Dynamic
{
    /// <summary>
    /// Exception with error information of the run time compilation.
    /// </summary>
    [Serializable]
    public sealed class DynamicCompilationException : FileHelpersException
    {
        /// <summary>
        /// Compilation exception happen loading a dynamic class
        /// </summary>
        /// <param name="message">Message for the error</param>
        /// <param name="sourceCode">Source code reference???</param>
        /// <param name="errors">Errors from compiler</param>
        internal DynamicCompilationException(string messageCode, List<string> messageParams, string sourceCode, CompilerErrorCollection errors)//TODO: CompilerErrorCollection not supported by standard 2.0
            : base(messageCode, messageParams)
        {
            mSourceCode = sourceCode;
            mCompilerErrors = errors;
        }

        private readonly string mSourceCode;

        /// <summary>
        /// The source code that generates the Exception
        /// </summary>
        public string SourceCode
        {
            get { return mSourceCode; }
        }

        private readonly CompilerErrorCollection mCompilerErrors;

        /// <summary>
        /// The errors returned from the compiler.
        /// </summary>
        public CompilerErrorCollection CompilerErrors
        {
            get { return mCompilerErrors; }
        }
    }
}