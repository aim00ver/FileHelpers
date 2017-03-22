using System;
using System.Collections;
using System.Collections.Generic;

namespace FileHelpers
{
    internal sealed class EnumConverter : ConverterBase
    {
        private readonly Type mEnumType;

        public EnumConverter(Type sourceEnum)
        {
            if (sourceEnum.IsEnum == false)
                //?ImputIsNotEnum"The Input sourceType must be an Enum but is of type {0}" 
                throw new BadUsageException("FileHelperMsg_ImputIsNotEnum", (s) => { return String.Format(s, sourceEnum.Name); });

            mEnumType = sourceEnum;
        }

        public override object StringToField(string from)
        {
            try {
                return Enum.Parse(mEnumType, from.Trim(), true);
            }
            catch (ArgumentException) {
                //?EnumValueNotFound"The value {0} is not present in the Enum."
                throw new ConvertException(from, mEnumType, "FileHelperMsg_EnumValueNotFound", (s) => { return String.Format(s, from); });
            }
        }
    }
}