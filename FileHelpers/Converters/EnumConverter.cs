using System;
using System.Collections.Generic;

namespace FileHelpers
{
    internal sealed class EnumConverter : ConverterBase
    {
        private readonly Type mEnumType;
        private readonly EnumFormat mFormat;
        /// <summary>
        /// whether to output enum as string or as integer
        /// </summary>
        internal enum EnumFormat
        {
            /// <summary>
            /// as string
            /// </summary>
            String,

            /// <summary>
            /// as integer
            /// </summary>
            Number,
        }

        public EnumConverter(Type sourceEnum, EnumFormat format = EnumFormat.String)
        {
            if (sourceEnum.IsEnum == false)
            {
                //?ImputIsNotEnum"The Input sourceType must be an Enum but is of type {0}" 
                throw new BadUsageException("FileHelperMsg_ImputIsNotEnum", new List<string>() { sourceEnum.Name });
            }
            mEnumType = sourceEnum;
            mFormat = format;
        }


        public EnumConverter(Type sourceEnum, string format) : this(sourceEnum, GetEnumFormat(format))
        {
        }

        public override object StringToField(string from)
        {
            try
            {
                return Enum.Parse(mEnumType, from.Trim(), true);
            }
            catch (ArgumentException)
            {
                //?EnumValueNotFound"The value {0} is not present in the Enum."
                throw new ConvertException(from, mEnumType, "FileHelperMsg_EnumValueNotFound", new List<string>() { from });
            }
        }

        public override string FieldToString(object from)
        {
            if (from == null)
                return string.Empty;

            switch (mFormat)
            {
                case EnumFormat.String:
                    return from.ToString();
                default:
                    {
                        int data = (int)from;
                        return data.ToString();
                    }
            }
        }

        private static EnumFormat GetEnumFormat(string format)
        {
            switch (format.Trim().ToLower())
            {
                case "n":
                    return EnumFormat.Number;
                case "s":
                    return EnumFormat.String;
                default:
                    return EnumFormat.String;

            }
        }
    }
}