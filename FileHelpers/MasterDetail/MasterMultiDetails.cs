using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

//using System.ComponentModel.TypeConverter;

namespace FileHelpers.MasterDetail
{
    /// <summary>
    /// <para>This class contains information of a Master record and its Details records.</para>
    /// <para>This class is used for the Read and Write operations of the <see cref="MasterDetailEngine"/>.</para>
    /// </summary>
    public class MasterMultiDetails
    {
        /// <summary>Create an empty instance.</summary>
        public MasterMultiDetails()
        {
            Details = new Dictionary<Type, object[]>();
        }

        /// <summary>Create a new instance with the specified values.</summary>
        /// <param name="master">The master record.</param>
        /// <param name="details">The details record.</param>
        public MasterMultiDetails(object master, Dictionary<Type, object[]> details)
        {
            Master = master;
            Details = details;
        }


        /// <summary>The Master record.</summary>
        public object Master { get; set; }

        /// <summary>An Array with the Detail records.</summary>
        public Dictionary<Type, object[]> Details { get; set; }
    }
}