using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FileHelpers.Helpers;
using FileHelpers.Options;

namespace FileHelpers
{
    /// <summary>
    /// Base class for all Field Types.
    /// Implements all the basic functionality of a field in a typed file.
    /// </summary>
    public abstract class FieldBase
    {
        #region "  Private & Internal Fields  "

        // --------------------------------------------------------------
        // WARNING !!!
        //    Remember to add each of these fields to the clone method !!
        // --------------------------------------------------------------

        /// <summary>
        /// type of object to be created,  eg DateTime
        /// </summary>
        public Type FieldType { get; private set; }

        /// <summary>
        /// Provider to convert to and from text
        /// </summary>
        public ConverterBase Converter { get; private set; }

        /// <summary>
        /// Number of extra characters used,  delimiters and quote characters
        /// </summary>
        internal virtual int CharsToDiscard => 0;

        /// <summary>
        /// Field type of an array or it is just fieldType.
        /// What actual object will be created
        /// </summary>
        internal Type FieldTypeInternal { get; private set; }

        /// <summary>
        /// Is this field an array?
        /// </summary>
        public bool IsArray { get; private set; }

        /// <summary>
        /// Array must have this many entries
        /// </summary>
        public int ArrayMinLength { get; set; }

        /// <summary>
        /// Array may have this many entries,  if equal to ArrayMinLength then
        /// it is a fixed length array
        /// </summary>
        public int ArrayMaxLength { get; set; }

        /// <summary>
        /// Seems to be duplicate of FieldTypeInternal except it is ONLY set
        /// for an array
        /// </summary>
        private Type ArrayType { get; set; }

        /// <summary>
        /// Do we process this field but not store the value
        /// </summary>
        public bool Discarded { get; set; }

        /// <summary>
        /// Value to use if input is null or empty
        /// </summary>
        internal object NullValue { get; private set; }

        /// <summary>
        /// Are we a simple string field we can just assign to
        /// </summary>
        private bool IsStringField { get; set; }

        /// <summary>
        /// Details about the extraction criteria
        /// </summary>
        internal FieldInfo FieldInfo { get; private set; }

        /// <summary>
        /// indicates whether we trim leading and/or trailing whitespace
        /// </summary>
        public TrimMode TrimMode { get; set; }

        /// <summary>
        /// Character to chop off front and / rear of the string
        /// </summary>
        internal char[] TrimChars { get; set; }

        /// <summary>
        /// The field may not be present on the input data (line not long enough)
        /// </summary>
        public bool IsOptional
        {
            get; set;
        }

        /// <summary>
        /// The next field along is optional,  optimise processing next records
        /// </summary>
        internal bool NextIsOptional
        {
            get
            {
                if (Parent.FieldCount > ParentIndex + 1)
                    return Parent.Fields[ParentIndex + 1].IsOptional;
                return false;
            }
        }


        /// <summary>
        /// Am I the first field in an array list
        /// </summary>
        internal bool IsFirst => ParentIndex == 0;

        /// <summary>
        /// Am I the last field in the array list
        /// </summary>
        internal bool IsLast => ParentIndex == Parent.FieldCount - 1;

        /// <summary>
        /// Set from the FieldInNewLIneAtribute.  This field begins on a new
        /// line of the file
        /// </summary>
        internal bool InNewLine { get; private set; }

        /// <summary>
        /// Order of the field in the file layout
        /// </summary>
        internal int? FieldOrder { get; private set; }

        /// <summary>
        /// Can null be assigned to this value type, for example not int or
        /// DateTime
        /// </summary>
        internal bool IsNullableType { get; private set; }

        /// <summary>
        /// Name of the field without extra characters (eg property)
        /// </summary>
        internal string FieldFriendlyName { get; private set; }

        /// <summary>
        /// The field must be not be empty
        /// </summary>
        public bool IsNotEmpty { get; set; }

        /// <summary>
        /// Do not include the field value to the output
        /// </summary>
        public bool IsNotIncluded { get; set; }

        /// <summary>
        /// Caption of the field displayed in header row (see EngineBase.GetFileHeader)
        /// </summary>
        internal string FieldCaption { get; private set; }

        // --------------------------------------------------------------
        // WARNING !!!
        //    Remember to add each of these fields to the clone method !!
        // --------------------------------------------------------------

        /// <summary>
        /// Fieldname of the field we are storing
        /// </summary>
        internal string FieldName => FieldInfo.Name;

        #endregion

        #region "  CreateField  "

        /// <summary>
        /// Check the Attributes on the field and return a structure containing
        /// the settings for this file.
        /// </summary>
        /// <param name="fi">Information about this field</param>
        /// <param name="recordAttribute">Type of record we are reading</param>
        /// <returns>Null if not used</returns>
        public static FieldBase CreateField(FieldInfo fi, TypedRecordAttribute recordAttribute)
        {
            FieldBase res = null;
            MemberInfo mi = fi;
            //bool isField = true;
            var memberName = /*"The field: '" + */fi.Name;
            Type fieldType = fi.FieldType;             
            string fieldFriendlyName = AutoPropertyName(fi);
            if (string.IsNullOrEmpty(fieldFriendlyName)==false)
            {                
                var prop = fi.DeclaringType.GetProperty(fieldFriendlyName);
                if (prop != null)
                {
                    //isField = false;
                    memberName = /*"The property: '" + */prop.Name;
                    mi = prop;
                }
                else
                {
                    fieldFriendlyName = null;
                }
            }
            // If ignored, return null
#pragma warning disable 612,618 // disable obsolete warning
            if (mi.IsDefined(typeof (FieldNotInFileAttribute), true) ||
                mi.IsDefined(typeof (FieldIgnoredAttribute), true) ||
                mi.IsDefined(typeof (FieldHiddenAttribute), true))
#pragma warning restore 612,618
                return null;

            
            
            var attributes = (FieldAttribute[]) mi.GetCustomAttributes(typeof (FieldAttribute), true);

            // CHECK USAGE ERRORS !!!

            // Fixed length record and no attributes at all
            if (recordAttribute is FixedLengthRecordAttribute &&
                attributes.Length == 0) 
            {
                //?NoFieldFixedLengthAttr"The field or property '{0}' must be marked the FieldFixedLength attribute because the record class is marked with FixedLengthRecord."
                throw new BadUsageException("FileHelperMsg_NoFieldFixedLengthAttr", new List<string>() { memberName });
            }

            if (attributes.Length > 1) 
            {
                //?FieldFixedLengthAttrConflict"The field or property '{0}' has a FieldFixedLength and a FieldDelimiter attribute."
                throw new BadUsageException("FileHelperMsg_FieldFixedLengthAttrConflict", new List<string>() { memberName });
            }

            if (recordAttribute is DelimitedRecordAttribute &&
                mi.IsDefined(typeof (FieldAlignAttribute), false)) 
            {
                //?FieldAlignAttrIsWrong"The field or property '{0}' can't be marked with FieldAlign attribute, it is only valid for fixed length records and are used only for write purpose."
                throw new BadUsageException("FileHelperMsg_FieldAlignAttrIsWrong", new List<string>() { memberName });
            }

            if (fieldType.IsArray == false &&
                mi.IsDefined(typeof (FieldArrayLengthAttribute), false)) 
            {
                //?FieldArrayLengthAttrIsWrong"The field or property '{0}' can't be marked with FieldArrayLength attribute is only valid for array fields."
                throw new BadUsageException("FileHelperMsg_FieldArrayLengthAttrIsWrong", new List<string>() { memberName });
            }

            // PROCESS IN NORMAL CONDITIONS
            if (attributes.Length > 0) 
            {
                FieldAttribute fieldAttb = attributes[0];

                if (fieldAttb is FieldFixedLengthAttribute)
                {
                    // Fixed Field
                    if (recordAttribute is DelimitedRecordAttribute) 
                    {
                        //?FieldFixedLengthAttrIsWrong"The field or property '{0}' can't be marked with FieldFixedLength attribute, it is only for the FixedLengthRecords not for delimited ones."
                        throw new BadUsageException("FileHelperMsg_FieldFixedLengthAttrIsWrong", new List<string>() { memberName });
                    }

                    var attbFixedLength = (FieldFixedLengthAttribute) fieldAttb;
                    var attbAlign = Attributes.GetFirst<FieldAlignAttribute>(mi);

                    res = new FixedLengthField(fi,
                        attbFixedLength.Length,
                        attbAlign,
                        recordAttribute.DefaultCultureName);
                    ((FixedLengthField) res).FixedMode = ((FixedLengthRecordAttribute) recordAttribute).FixedMode;
                }
                else if (fieldAttb is FieldDelimiterAttribute)
                {
                    // Delimited Field
                    if (recordAttribute is FixedLengthRecordAttribute) 
                    {
                        //?FieldDelimiterAttrIsWrong"The field or property '{0}' can't be marked with FieldDelimiter attribute, it is only for DelimitedRecords not for fixed ones."
                        throw new BadUsageException("FileHelperMsg_FieldDelimiterAttrIsWrong", new List<string>() { memberName });
                    }

                    res = new DelimitedField(fi,
                        ((FieldDelimiterAttribute)fieldAttb).Delimiter,
                        recordAttribute.DefaultCultureName);
                }
                else 
                {
                    //?CustomAttrNotSupported"Custom field attributes are not currently supported. Unknown attribute: {0} on field: {1}"
                    throw new BadUsageException("FileHelperMsg_CustomAttrNotSupported", new List<string>() { fieldAttb.GetType().Name, fi.Name });
                }
            }
            else // attributes.Length == 0
            {
                var delimitedRecordAttribute = recordAttribute as DelimitedRecordAttribute;

                if (delimitedRecordAttribute != null)
                {
                    res = new DelimitedField(fi,
                        delimitedRecordAttribute.Separator,
                        recordAttribute.DefaultCultureName);
                }
            }

            if (res != null)
            {
                // FieldDiscarded
                res.Discarded = mi.IsDefined(typeof (FieldValueDiscardedAttribute), false);

                // FieldTrim
                Attributes.WorkWithFirst<FieldTrimAttribute>(mi,
                    (x) =>
                    {
                        res.TrimMode = x.TrimMode;
                        res.TrimChars = x.TrimChars;
                    });

                // FieldQuoted
                Attributes.WorkWithFirst<FieldQuotedAttribute>(mi,
                    (x) => 
                    {
                        if (res is FixedLengthField) 
                        {
                            //?FieldQuotedAttrIsWrong"The field or property '{0}' can't be marked with FieldQuoted attribute, it is only for the delimited records."
                            throw new BadUsageException("FileHelperMsg_FieldQuotedAttrIsWrong", new List<string>() { memberName });
                        }

                        ((DelimitedField) res).QuoteChar =
                            x.QuoteChar;
                        ((DelimitedField) res).QuoteMode =
                            x.QuoteMode;
                        ((DelimitedField) res).QuoteMultiline =
                            x.QuoteMultiline;
                    });

                // FieldOrder
                Attributes.WorkWithFirst<FieldOrderAttribute>(mi, x => res.FieldOrder = x.Order);

                // FieldCaption
                Attributes.WorkWithFirst<FieldCaptionAttribute>(mi, x => res.FieldCaption = x.Caption);

                // FieldOptional
                res.IsOptional = mi.IsDefined(typeof(FieldOptionalAttribute), false);

                // FieldInNewLine
                res.InNewLine = mi.IsDefined(typeof(FieldInNewLineAttribute), false);

                // FieldNotEmpty
                res.IsNotEmpty = mi.IsDefined(typeof(FieldNotEmptyAttribute), false);

                // FieldNotIncluded
                res.IsNotIncluded = mi.IsDefined(typeof(FieldNotIncludedAttribute), false);

                // FieldArrayLength
                if (fieldType.IsArray) 
                {
                    res.IsArray = true;
                    res.ArrayType = fieldType.GetElementType();

                    // MinValue indicates that there is no FieldArrayLength in the array
                    res.ArrayMinLength = int.MinValue;
                    res.ArrayMaxLength = int.MaxValue;

                    Attributes.WorkWithFirst<FieldArrayLengthAttribute>(mi,
                        (x) => 
                        {
                            res.ArrayMinLength = x.MinLength;
                            res.ArrayMaxLength = x.MaxLength;

                            if (res.ArrayMaxLength < res.ArrayMinLength ||
                                res.ArrayMinLength < 0 ||
                                res.ArrayMaxLength <= 0) 
                            {
                                //?FieldArrayLengthAttrIsInvalid"The field or property '{0}' has invalid length values in the [FieldArrayLength] attribute."
                                throw new BadUsageException("FileHelperMsg_FieldArrayLengthAttrIsInvalid", new List<string>() { memberName });
                            }
                        });
                }
            }

            if (string.IsNullOrEmpty(res.FieldFriendlyName))
                res.FieldFriendlyName = res.FieldName;

            return res;
        }

        internal RecordOptions Parent { private get; set; }
        internal int ParentIndex { private get; set; }

        internal static string AutoPropertyName(FieldInfo fi)
        {
            if (fi.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                if (fi.Name.EndsWith("__BackingField") &&
                    fi.Name.StartsWith("<") &&
                    fi.Name.Contains(">"))
                    return fi.Name.Substring(1, fi.Name.IndexOf(">") - 1);
                
            }

            if (fi.IsDefined(typeof(DebuggerBrowsableAttribute), false))
            {
                if (fi.Name.EndsWith("@") && !fi.IsPublic)
                {
                    var name = fi.Name.Substring(0, fi.Name.Length - 1);

                    if (fi.DeclaringType?.GetProperty(name) != null)
                        return name;
                }
            }

            return "";
        }

        internal bool IsAutoProperty { get; set; }

        #endregion

        #region "  Constructor  "

        /// <summary>
        /// Create a field base without any configuration
        /// </summary>
        internal FieldBase()
        {
            IsNullableType = false;
            TrimMode = TrimMode.None;
            FieldOrder = null;
            InNewLine = false;
            IsOptional = false;
            TrimChars = null;
            NullValue = null;
            IsArray = false;
            IsNotEmpty = false;
            IsNotIncluded = false;
        }

        /// <summary>
        /// Create a field base from a fieldinfo object
        /// Verify the settings against the actual field to ensure it will work.
        /// </summary>
        /// <param name="fi">Field Info Object</param>
        /// <param name="defaultCultureName">Default culture name used for each properties if no converter is specified otherwise. If null, the default decimal separator (".") will be used.</param>
        internal FieldBase(FieldInfo fi, string defaultCultureName = null)
            : this()
        {
         
            FieldInfo = fi;
            FieldType = FieldInfo.FieldType;
            MemberInfo attibuteTarget = fi;
            FieldFriendlyName = AutoPropertyName(fi);
            if (string.IsNullOrEmpty(FieldFriendlyName) == false)
            {
                var prop = fi.DeclaringType.GetProperty(FieldFriendlyName);
                if (prop == null)
                {
                    FieldFriendlyName = null;
                }
                else
                {
                    this.IsAutoProperty = true;
                    attibuteTarget = prop;
                }
            }

            if (FieldType.IsArray)
                FieldTypeInternal = FieldType.GetElementType();
            else
                FieldTypeInternal = FieldType;

            IsStringField = FieldTypeInternal == typeof (string);

            object[] attribs = attibuteTarget.GetCustomAttributes(typeof (FieldConverterAttribute), true);

            if (attribs.Length > 0)
            {
                var conv = (FieldConverterAttribute) attribs[0];
                Converter = conv.Converter;
                conv.ValidateTypes(FieldInfo);
            }
            else
                Converter = ConvertHelpers.GetDefaultConverter(FieldFriendlyName ?? fi.Name,
                    FieldType,
                    defaultCultureName: defaultCultureName);

            if (Converter != null)
                Converter.mDestinationType = FieldTypeInternal;

            attribs = attibuteTarget.GetCustomAttributes(typeof (FieldNullValueAttribute), true);

            if (attribs.Length > 0)
            {
                NullValue = ((FieldNullValueAttribute) attribs[0]).NullValue;

                if (NullValue != null) 
                {
                    if (!FieldTypeInternal.IsAssignableFrom(NullValue.GetType())) 
                    {
                        //?NullValueWrongType"The NullValue is of type: {0} that is not asignable to the field {1} of type: {2}"
                        throw new BadUsageException("FileHelperMsg_NullValueWrongType", 
                            new List<string>() { NullValue.GetType().Name, FieldInfo.Name, FieldTypeInternal.Name },
                            FieldInfo.Name);
                    }
                }
            }

            IsNullableType = FieldTypeInternal.IsValueType &&
                             FieldTypeInternal.IsGenericType &&
                             FieldTypeInternal.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        #endregion

        #region "  MustOverride (String Handling)  "

        /// <summary>
        /// Extract the string from the underlying data, removes quotes
        /// characters for example
        /// </summary>
        /// <param name="line">Line to parse data from</param>
        /// <returns>Slightly processed string from the data</returns>
        internal abstract ExtractedInfo ExtractFieldString(LineInfo line);

        /// <summary>
        /// Create a text block containing the field from definition
        /// </summary>
        /// <param name="sb">Append string to output</param>
        /// <param name="fieldValue">Field we are adding</param>
        /// <param name="isLast">Indicates if we are processing last field</param>
        internal abstract void CreateFieldString(StringBuilder sb, object fieldValue, bool isLast);

        /// <summary>
        /// Convert a field value to a string representation
        /// </summary>
        /// <param name="fieldValue">Object containing data</param>
        /// <returns>String representation of field</returns>
        internal string CreateFieldString(object fieldValue)
        {
            if (IsNotIncluded)
                return string.Empty;

            if (Converter == null) 
            {
                if (fieldValue == null)
                    return string.Empty;
                else
                    return fieldValue.ToString();
            }
            else
                return Converter.FieldToString(fieldValue);
        }

        #endregion

        #region "  ExtractValue  "

        /// <summary>
        /// Get the data out of the records
        /// </summary>
        /// <param name="line">Line handler containing text</param>
        /// <returns></returns>
        internal object ExtractFieldValue(LineInfo line, ErrorManager ErrorManager, int detailIndex, out Tuple<int, int> valuePosition)
        {
            valuePosition = new Tuple<int, int>(line.mCurrentPos, 0);
            //-> extract only what I need
            try
            {
                if (InNewLine) 
                {
                    // Any trailing characters, terminate
                    if (line.EmptyFromPos() == false) 
                    {
                        //?TextBeforeNewLine"Text '{0}' found before the new line of the field: {1} (this is not allowed when you use [FieldInNewLine])"
                        throw new BadUsageException(line, "FileHelperMsg_TextBeforeNewLine", new List<string>() { line.CurrentString, FieldInfo.Name });
                    }

                    line.ReLoad(line.mReader.ReadNextLine());

                    if (line.mLineStr == null) 
                    {
                        //?EndOfStreamInsideField"End of stream found parsing the field {0}. Please check the class record."
                        throw new BadUsageException(line, "FileHelperMsg_EndOfStreamInsideField", new List<string>() { FieldInfo.Name });
                    }
                }

                if (IsArray == false) 
                {
                    if (line.IsEOL())
                    {
                        ExtractedInfo info = new ExtractedInfo(line);
                        return AssignFromString(info, line, ErrorManager, detailIndex).Value;
                    }
                    else
                    {
                        ExtractedInfo info = ExtractFieldString(line);
                        if (info.mCustomExtractedString == null)
                            line.mCurrentPos = info.ExtractedTo + 1;

                        line.mCurrentPos += CharsToDiscard; //total;

                        if (Discarded)
                            return GetDiscardedNullValue();
                        else
                            return AssignFromString(info, line, ErrorManager, detailIndex).Value;
                    }
                }
                else 
                {
                    if (ArrayMinLength <= 0)
                        ArrayMinLength = 0;

                    int i = 0;

                    var res = new ArrayList(Math.Max(ArrayMinLength, 10));

                    while (line.mCurrentPos - CharsToDiscard < line.mLineStr.Length &&
                       i < ArrayMaxLength)
                {
                        ExtractedInfo info = ExtractFieldString(line);
                        if (info.mCustomExtractedString == null)
                            line.mCurrentPos = info.ExtractedTo + 1;

                        line.mCurrentPos += CharsToDiscard;

                        try {
                            var value = AssignFromString(info, line, ErrorManager, detailIndex);

                            if (value.NullValueUsed &&
                                i == 0 &&
                                line.IsEOL())
                                break;

                            res.Add(value.Value);
                        }
                    catch (NullValueNotFoundException)
                    {
                            if (i == 0)
                                break;
                            else
                                throw;
                        }
                        i++;
                    }

                    if (res.Count < ArrayMinLength) 
                    {
                        //?ArrayLengthLessThanMin"Line: {0} Column: {1} Field: {2}. The array has only {3} values, less than the minimum length of {4}"
                        throw new FileHelpersException("FileHelperMsg_ArrayLengthLessThanMin", new List<string>() { line.mReader.LineNumber.ToString(), line.mCurrentPos.ToString(), FieldInfo.Name, res.Count.ToString(), ArrayMinLength.ToString() });
                    }
                    else if (IsLast && line.IsEOL() == false) 
                    {
                        //?ArrayLengthGreaterThanMin"Line: {0} Column: {1} Field: {2}. The array has more values than the maximum length of {3}"
                        throw new FileHelpersException("FileHelperMsg_ArrayLengthGreaterThanMin", new List<string>() { line.mReader.LineNumber.ToString(), line.mCurrentPos.ToString(), FieldInfo.Name, ArrayMaxLength.ToString() });
                    }

                    // TODO:   is there a reason we go through all the array processing then discard it
                    if (Discarded)
                        return null;
                    else
                        return res.ToArray(ArrayType);
                }
            }
            finally
            {
                valuePosition = new Tuple<int, int>(valuePosition.Item1, line.mCurrentPos - valuePosition.Item1);
            }
        }

        #region "  AssignFromString  "

        private struct AssignResult
        {
            public object Value;
            public bool NullValueUsed;
        }

        /// <summary>
        /// Create field object after extracting the string from the underlying
        /// input data
        /// </summary>
        /// <param name="fieldString">Information extracted?</param>
        /// <param name="line">Underlying input data</param>
        /// <returns>Object to assign to field</returns>
        private AssignResult AssignFromString(ExtractedInfo fieldString, LineInfo line, ErrorManager ErrorManager, int detailIndex)
        {
            object val;

            var extractedString = fieldString.ExtractedString();
            try
            {
                try
                {
                    /*
                    if (IsNotEmpty && String.IsNullOrEmpty(extractedString))
                    {
                        //?ValueNonPopulated"The value is empty and must be populated."
                        throw new FileHelpersException("FileHelperMsg_ValueNonPopulated", null);
                    }
                    else */
                    if (IsStringField)
                    {
                        if (IsNotEmpty && String.IsNullOrEmpty(extractedString))
                        {
                            var nullVal = GetNullValue(line);
                            if ((nullVal == null) || (string.IsNullOrEmpty(nullVal.ToString())))
                                throw new FileHelpersException("FileHelperMsg_ValueNonPopulated", null);
                            else
                                return new AssignResult
                                {
                                    Value = nullVal.ToString(),
                                    NullValueUsed = true
                                };
                        }
                        else if (this.Converter != null)
                            val = this.Converter.StringToField(TrimString(extractedString));
                        else
                            val = TrimString(extractedString);
                    }
                    else if (this.Converter == null)
                    {
                        extractedString = extractedString.Trim();

                        if (extractedString.Length == 0)
                        {
                            var nullVal = GetNullValue(line);
                            if ((nullVal == null) && (IsNotEmpty))
                                throw new FileHelpersException("FileHelperMsg_ValueNonPopulated", null);

                            return new AssignResult
                            {
                                Value = nullVal,
                                NullValueUsed = true
                            };
                        }
                        else
                            val = Convert.ChangeType(extractedString, FieldTypeInternal, null);
                    }
                    else
                    {
                        var trimmedString = extractedString.Trim();

                        if (this.Converter.CustomNullHandling == false &&
                            trimmedString.Length == 0)
                        {
                            var nullVal = GetNullValue(line);
                            if ((nullVal == null) && (IsNotEmpty))
                                throw new FileHelpersException("FileHelperMsg_ValueNonPopulated", null);

                            return new AssignResult
                            {
                                Value = nullVal,
                                NullValueUsed = true
                            };
                        }
                        else
                        {
                            if (TrimMode == TrimMode.Both)
                                val = this.Converter.StringToField(trimmedString);
                            else
                                val = this.Converter.StringToField(TrimString(extractedString));

                            if (val == null)
                            {
                                return new AssignResult
                                {
                                    Value = GetNullValue(line),
                                    NullValueUsed = true
                                };
                            }
                        }
                    }

                    return new AssignResult
                    {
                        Value = val
                    };
                }
                catch (ConvertException ex)
                {
                    ex.FieldName = FieldInfo.Name;
                    ex.LineNumber = line.mReader.LineNumber;
                    ex.ColumnNumber = fieldString.ExtractedFrom + 1;
                    throw;
                }
                catch (BadUsageException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                if (Converter == null ||
                    Converter.GetType().Assembly == typeof(FieldBase).Assembly)
                    {
                        throw new ConvertException(extractedString,
                            FieldTypeInternal,
                            FieldInfo.Name,
                            line.mReader.LineNumber,
                            fieldString.ExtractedFrom + 1,
                            ex.Message,//FileHelpersException.NonLocalizedCode,
                            null,
                            ex);
                    }
                    else
                    {
                        throw new ConvertException(extractedString,
                            FieldTypeInternal,
                            FieldInfo.Name,
                            line.mReader.LineNumber,
                            fieldString.ExtractedFrom + 1,
                            "FileHelperMsg_ConvertExceptionCustomConverterException",
                            //!"Your custom converter: {0} throws an {1} with the message: {2}"
                            new List<string>() { this.Converter.GetType().Name, ex.GetType().Name, ex.Message },
                            ex);
                    }
                }
            }
            catch (Exception exception)
            {
                if (ErrorManager.ErrorMode == ErrorMode.ThrowException)
                    throw;
                else
                {
                    ErrorManager.AddError(new ErrorInfo()
                    {
                        mErrorType = ErrorInfo.ErrorTypeEnum.Unknown,
                        mExceptionInfo = exception,
                        mFailedOnType = FieldInfo.DeclaringType,
                        //mRecordString = extractedString,
                        mRecordString = line.mLineStr,
                        mLineNumber = line.mReader.LineNumber,
                        mDetailIndex = detailIndex,
                        mStart = fieldString.ExtractedFrom + 1,
                        mLength = fieldString.Length,
                        mFieldName = FieldInfo.Name
                    });
                    return new AssignResult
                    {
                        Value = FieldInfo.FieldType.IsValueType ? Activator.CreateInstance(FieldInfo.FieldType) : null,
                        NullValueUsed = !FieldInfo.FieldType.IsValueType
                    };
                }
            }
        }

        private String TrimString(string extractedString)
        {
            switch (TrimMode)
            {
                case TrimMode.None:
                    return extractedString;

                case TrimMode.Both:
                    return extractedString.Trim();

                case TrimMode.Left:
                    return extractedString.TrimStart();

                case TrimMode.Right:
                    return extractedString.TrimEnd();

                default:
                    //?TrimModeIsInvalid"Trim mode invalid in FieldBase.TrimString"
                    throw new FileHelpersException("FileHelperMsg_TrimModeIsInvalid", new List<string>() { TrimMode.ToString() });
            }
        }

        /// <summary>
        /// Convert a null value into a representation,
        /// allows for a null value override
        /// </summary>
        /// <param name="line">input line to read, used for error messages</param>
        /// <returns>Null value for object</returns>
        private object GetNullValue(LineInfo line)
        {
            if (NullValue == null)
            {
                if (FieldTypeInternal.IsValueType)
                {
                    if (IsNullableType)
                        return null;

                    //?FieldNullValueIsMissing"No value found for the value type field: '{0}' Class: '{1}'. {2}You must use the [FieldNullValue] attribute because this is a value type and can't be null or use a Nullable Type instead of the current type.";
                    //new List<string>() { FieldInfo.Name, FieldInfo.DeclaringType.Name, Environment.NewLine); }
                    throw new NullValueNotFoundException(line, "FileHelperMsg_FieldNullValueIsMissing", null);
                }
                else
                    return null;
            }
            else
                return NullValue;
        }

        /// <summary>
        /// Get the null value that represent a discarded value
        /// </summary>
        /// <returns>null value of discard?</returns>
        private object GetDiscardedNullValue()
        {
            if (NullValue == null)
            {
                if (FieldTypeInternal.IsValueType)
                {
                    if (IsNullableType)
                        return null;

                    //?ValueIsDiscarded"The field: '{0}' Class: '{1}' is from a value type: {2} and is discarded (null) you must provide a [FieldNullValue] attribute.";
                    throw new BadUsageException("FileHelperMsg_ValueIsDiscarded", new List<string>() { FieldInfo.Name, FieldInfo.DeclaringType.Name, FieldInfo.FieldType.Name });
                }
                else
                    return null;
            }
            else
                return NullValue;
        }

        #endregion

        #region "  CreateValueForField  "

        /// <summary>
        /// Convert a field value into a write able value
        /// </summary>
        /// <param name="fieldValue">object value to convert</param>
        /// <returns>converted value</returns>
        public object CreateValueForField(object fieldValue)
        {
            object val = null;

            if (fieldValue == null) 
            {
                if (NullValue == null) 
                {
                    if (FieldTypeInternal.IsValueType &&
                        Nullable.GetUnderlyingType(FieldTypeInternal) == null) {
                        //?NullValueFoundNoFieldNullValueAttribute"Null Value found. You must specify a FieldNullValueAttribute in the {0} field of type {1}, because this is a ValueType."
                        throw new BadUsageException("FileHelperMsg_NullValueFoundNoFieldNullValueAttribute", new List<string>() { FieldInfo.Name, FieldTypeInternal.Name });
                    }
                    else
                        val = null;
                }
                else
                    val = NullValue;
            }
            else if (FieldTypeInternal == fieldValue.GetType())
                val = fieldValue;
            else 
            {
                if (Converter == null)
                    val = Convert.ChangeType(fieldValue, FieldTypeInternal, null);
                else
                {
                    try
                    {
                        if (Nullable.GetUnderlyingType(FieldTypeInternal) != null &&
                            Nullable.GetUnderlyingType(FieldTypeInternal) == fieldValue.GetType())
                            val = fieldValue;
                        else
                            val = Convert.ChangeType(fieldValue, FieldTypeInternal, null);
                    }
                    catch
                    {
                        val = Converter.StringToField(fieldValue.ToString());
                    }
                }
            }

            return val;
        }

        #endregion

        #endregion

        #region "  AssignToString  "

        /// <summary>
        /// convert field to string value and assign to a string builder
        /// buffer for output
        /// </summary>
        /// <param name="sb">buffer to collect record</param>
        /// <param name="fieldValue">value to convert</param>
        internal void AssignToString(StringBuilder sb, object fieldValue)
        {
            if (InNewLine)
                sb.Append(StringHelper.NewLine);

            if (IsArray) 
            {
                if (fieldValue == null) 
                {
                    if (0 < ArrayMinLength)
                    {
                        //?ArrayIsNullMinLen"Field: {0}. The array is null, but the minimum length is {1}"
                        throw new FileHelpersException("FileHelperMsg_ArrayIsNullMinLen", new List<string>() { FieldInfo.Name, ArrayMinLength.ToString() });
                    }

                    return;
                }

                var array = (IList) fieldValue;

                if (array.Count < ArrayMinLength)
                {
                    //?ArrayLenWrongMin"Field: {0}. The array has {1} values, but the minimum length is {2}"
                    throw new FileHelpersException("FileHelperMsg_ArrayLenWrongMin", new List<string>() { FieldInfo.Name, array.Count.ToString(), ArrayMinLength.ToString() });
                }

                if (array.Count > ArrayMaxLength)
                {
                    //?ArrayLenWrongMax"Field: {0}. The array has {1} values, but the maximum length is {2}"
                    throw new FileHelpersException("FileHelperMsg_ArrayLenWrongMax", new List<string>() { FieldInfo.Name, array.Count.ToString(), ArrayMaxLength.ToString() });
                }
                
                for (int i = 0; i < array.Count; i++) 
                {
                    object val = array[i];
                    CreateFieldString(sb, val, IsLast && i == array.Count - 1);
                }
            }
            else
                CreateFieldString(sb, fieldValue, IsLast);
        }

        #endregion

        /// <summary>
        /// Copy the field object
        /// </summary>
        /// <returns>a complete copy of the Field object</returns>
        internal FieldBase Clone()
        {
            var res = CreateClone();

            res.FieldType = FieldType;
            res.Converter = Converter;
            res.FieldTypeInternal = FieldTypeInternal;
            res.IsArray = IsArray;
            res.ArrayType = ArrayType;
            res.ArrayMinLength = ArrayMinLength;
            res.ArrayMaxLength = ArrayMaxLength;
            res.NullValue = NullValue;
            res.IsStringField = IsStringField;
            res.FieldInfo = FieldInfo;
            res.TrimMode = TrimMode;
            res.TrimChars = TrimChars;
            res.IsOptional = IsOptional;
            res.InNewLine = InNewLine;
            res.FieldOrder = FieldOrder;
            res.IsNullableType = IsNullableType;
            res.Discarded = Discarded;
            res.FieldFriendlyName = FieldFriendlyName;
            res.IsNotEmpty = IsNotEmpty;
            res.IsNotIncluded = IsNotIncluded;
            res.FieldCaption = FieldCaption;
            res.Parent = Parent;
            res.ParentIndex = ParentIndex;
            return res;
        }

        /// <summary>
        /// Add the extra details that derived classes create
        /// </summary>
        /// <returns>field clone of right type</returns>
        protected abstract FieldBase CreateClone();
    }
}
