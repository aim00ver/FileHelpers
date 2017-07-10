using System;
using System.Collections;
using System.Collections.Generic;

namespace FileHelpers
{
    /// <summary>
    /// add validation exceptions
    /// </summary>
    internal static class ExHelper
    {
        /// <summary>
        /// Check the string is null or empty and throw an exception
        /// </summary>
        /// <param name="val">value to test</param>
        /// <param name="paramName">name of parameter to check</param>
        public static void CheckNullOrEmpty(string val, string paramName)
        {
            if (string.IsNullOrEmpty(val))
                //?ValueNullOrEmpty"Value can't be null or empty"
                throw new FileHelpersException("FileHelperMsg_ValueNullOrEmpty", null);
        }

        /// <summary>
        /// Check that parameter is not null or empty and throw an exception
        /// </summary>
        /// <param name="param">value to check</param>
        /// <param name="paramName">parameter name</param>
        public static void CheckNullParam(string param, string paramName)
        {
            if (string.IsNullOrEmpty(param))
                //?NeitherNullOrEmpty"{0} can't be neither null nor empty"
                throw new FileHelpersException("FileHelperMsg_NeitherNullOrEmpty", new List<string>() { paramName });
        }

        /// <summary>
        /// Check that parameter is not null and throw an exception
        /// </summary>
        /// <param name="param">value to check</param>
        /// <param name="paramName">parameter name</param>
        public static void CheckNullParam(object param, string paramName)
        {
            if (param == null)
                //?CantBeNull"{0} can't be null"
                throw new FileHelpersException("FileHelperMsg_CantBeNull", new List<string>() { paramName });
        }

        /// <summary>
        /// check that parameter 1 is different from parameter 2
        /// </summary>
        /// <param name="param1">value 1 to test</param>
        /// <param name="param1Name">name of value 1</param>
        /// <param name="param2">value 2 to test</param>
        /// <param name="param2Name">name of vlaue 2</param>
        public static void CheckDifferentsParams(object param1, string param1Name, object param2, string param2Name)
        {
            if (param1 == param2) {
                //?CantBeSame"{0} can't be the same as {1}"
                throw new FileHelpersException("FileHelperMsg_CantBeSame", new List<string>() { param1Name, param2Name });
            }
        }

        /// <summary>
        /// Check an integer value is positive (0 or greater)
        /// </summary>
        /// <param name="val">Integer to test</param>
        public static void PositiveValue(int val)
        {
            if (val < 0)
                //?ValueMustBeGreater"The value must be greater than or equal to 0."
                throw new FileHelpersException("FileHelperMsg_ValueMustBeGreater", null);
        }
    }
}