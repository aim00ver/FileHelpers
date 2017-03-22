using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FileHelpers
{
    /// <summary>
    /// Fixed length field that has length and alignment
    /// </summary>
    public sealed class FixedLengthField
        : FieldBase
    {
        #region "  Properties  "

        /// <summary>
        /// Field length of this field in the record
        /// </summary>
        internal int FieldLength { get; private set; }

        /// <summary>
        /// Alignment of this record
        /// </summary>
        internal FieldAlignAttribute Align { get; private set; }

        /// <summary>
        /// Whether we allow more or less characters to be handled
        /// </summary>
        internal FixedMode FixedMode { get; set; }

        #endregion

        #region "  Constructor  "

        /// <summary>
        /// Simple fixed length field constructor
        /// </summary>
        private FixedLengthField() {}

        /// <summary>
        /// Create a fixed length field from field information
        /// </summary>
        /// <param name="fi">Field definitions</param>
        /// <param name="length">Length of this field</param>
        /// <param name="align">Alignment, left or right</param>
        internal FixedLengthField(FieldInfo fi, int length, FieldAlignAttribute align)
            : base(fi)
        {
            FixedMode = FixedMode.ExactLength;
            Align = new FieldAlignAttribute(AlignMode.Left, ' ');
            this.FieldLength = length;

            if (align != null)
                this.Align = align;
            else {
                if (TypeHelper.IsNumericType(fi.FieldType))
                    Align = new FieldAlignAttribute(AlignMode.Right, ' ');
            }
        }

        #endregion

        #region "  Overrides String Handling  "

        /// <summary>
        /// Get the value from the record
        /// </summary>
        /// <param name="line">line to extract from</param>
        /// <returns>Information extracted from record</returns>
        internal override ExtractedInfo ExtractFieldString(LineInfo line)
        {
            if (line.CurrentLength == 0) {
                if (IsOptional)
                    return ExtractedInfo.Empty;
                else {
                    //?EOLInsideField"End Of Line found processing the field: {0} at line {1}. (You need to mark it as [FieldOptional] if you want to avoid this exception)"
                    throw new BadUsageException("FileHelperMsg_EOLInsideField", (s) => { return String.Format(s, FieldInfo.Name, line.mReader.LineNumber.ToString()); });
                }
            }

            //ExtractedInfo res;

            if (line.CurrentLength < this.FieldLength) {
                if (FixedMode == FixedMode.AllowLessChars ||
                    FixedMode == FixedMode.AllowVariableLength)
                    return new ExtractedInfo(line);
                else {
                    //?StringLenLessThanDefined"The string '{0}' (length {1}) at line {2} has less chars than the defined for {3} ({4}). You can use the [FixedLengthRecord(FixedMode.AllowLessChars)] to avoid this problem."
                    throw new BadUsageException("FileHelperMsg_StringLenLessThanDefined", (s) => { return String.Format(s, line.CurrentString, line.CurrentLength.ToString(), line.mReader.LineNumber.ToString(), FieldInfo.Name, FieldLength.ToString()); });
                }
            }
            else if (line.CurrentLength > FieldLength &&
                     IsArray == false &&
                     IsLast &&
                     FixedMode != FixedMode.AllowMoreChars &&
                     FixedMode != FixedMode.AllowVariableLength) {
                //?StringLenGreaterThanDefined"The string '{0}' (length {1}) at line {2} has more chars than the defined for the last field {3} ({4}).You can use the [FixedLengthRecord(FixedMode.AllowMoreChars)] to avoid this problem."
                throw new BadUsageException("FileHelperMsg_StringLenGreaterThanDefined", (s) => { return String.Format(s, line.CurrentString, line.CurrentLength.ToString(), line.mReader.LineNumber.ToString(), FieldInfo.Name, FieldLength.ToString()); });
            }
            else
                return new ExtractedInfo(line, line.mCurrentPos + FieldLength);
        }

        /// <summary>
        /// Create a fixed length string representation (pad it out or truncate it)
        /// </summary>
        /// <param name="sb">buffer to add field to</param>
        /// <param name="fieldValue">value we are updating with</param>
        /// <param name="isLast">Indicates if we are processing last field</param>
        internal override void CreateFieldString(StringBuilder sb, object fieldValue, bool isLast)
        {
            string field = base.CreateFieldString(fieldValue);

            // Discard longer field values
            if (field.Length > FieldLength)
                field = field.Substring(0, FieldLength);

            if (Align.Align == AlignMode.Left) {
                sb.Append(field);
                sb.Append(Align.AlignChar, FieldLength - field.Length);
            }
            else if (Align.Align == AlignMode.Right) {
                sb.Append(Align.AlignChar, FieldLength - field.Length);
                sb.Append(field);
            }
            else {
                int middle = (FieldLength - field.Length)/2;

                sb.Append(Align.AlignChar, middle);
                sb.Append(field);
                sb.Append(Align.AlignChar, FieldLength - field.Length - middle);
//				if (middle > 0)
//					res = res.PadLeft(mFieldLength - middle, mAlign.AlignChar).PadRight(mFieldLength, mAlign.AlignChar);
            }
        }

        /// <summary>
        /// Create a clone of the fixed length record ready to get updated by
        /// the base settings
        /// </summary>
        /// <returns>new fixed length field definition just like this one minus
        /// the base settings</returns>
        protected override FieldBase CreateClone()
        {
            var res = new FixedLengthField {
                Align = Align,
                FieldLength = FieldLength,
                FixedMode = FixedMode
            };
            return res;
        }

        #endregion
    }
}