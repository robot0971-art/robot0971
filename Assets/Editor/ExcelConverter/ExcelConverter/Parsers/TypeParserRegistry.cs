using System;
using System.Collections.Generic;
using System.Globalization;
using ExcelConverter.Exceptions;
using ExcelConverter.Interfaces;

namespace ExcelConverter.Parsers
{
    /// <summary>
    /// 타입 파서를 등록하고 관리하는 레지스트리입니다.
    /// </summary>
    public class TypeParserRegistry
    {
        private readonly List<ICustomTypeParser> _customParsers = new List<ICustomTypeParser>();
        private readonly Dictionary<Type, Func<string, object>> _defaultParsers = new Dictionary<Type, Func<string, object>>();

        public TypeParserRegistry()
        {
            RegisterDefaultParsers();
        }

        private void RegisterDefaultParsers()
        {
            _defaultParsers[typeof(string)] = v => v;
            _defaultParsers[typeof(int)] = v => int.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(int?)] = v => string.IsNullOrWhiteSpace(v) ? (int?)null : int.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(float)] = v => float.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(float?)] = v => string.IsNullOrWhiteSpace(v) ? (float?)null : float.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(double)] = v => double.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(double?)] = v => string.IsNullOrWhiteSpace(v) ? (double?)null : double.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(long)] = v => long.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(long?)] = v => string.IsNullOrWhiteSpace(v) ? (long?)null : long.Parse(v, CultureInfo.InvariantCulture);
            _defaultParsers[typeof(bool)] = v => ParseBool(v);
            _defaultParsers[typeof(bool?)] = v => string.IsNullOrWhiteSpace(v) ? (bool?)null : ParseBool(v);
        }

        private static bool ParseBool(string value)
        {
            var lowerValue = value.ToLowerInvariant().Trim();
            return lowerValue switch
            {
                "1" or "true" or "yes" or "y" => true,
                "0" or "false" or "no" or "n" => false,
                _ => bool.Parse(value)
            };
        }

        /// <summary>
        /// 커스텀 타입 파서를 등록합니다.
        /// </summary>
        public void RegisterParser(ICustomTypeParser parser)
        {
            _customParsers.Add(parser);
        }

        /// <summary>
        /// 값을 지정된 타입으로 파싱합니다.
        /// </summary>
        public object Parse(string value, Type targetType, string columnName)
        {
            if (targetType.IsEnum)
            {
                return ParseEnum(value, targetType, columnName);
            }

            // 커스텀 파서 확인
            foreach (var parser in _customParsers)
            {
                if (parser.CanParse(targetType))
                {
                    try
                    {
                        return parser.Parse(value, targetType);
                    }
                    catch (Exception ex)
                    {
                        throw new ParseException(value, targetType, columnName, ex);
                    }
                }
            }

            // 기본 파서 확인
            if (_defaultParsers.TryGetValue(targetType, out var defaultParser))
            {
                try
                {
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return null;
                    }
                    return defaultParser(value);
                }
                catch (Exception ex)
                {
                    throw new ParseException(value, targetType, columnName, ex);
                }
            }

            throw new TypeNotSupportedException(targetType);
        }

        private object ParseEnum(string value, Type enumType, string columnName)
        {
            try
            {
                if (int.TryParse(value, out var intValue))
                {
                    return Enum.ToObject(enumType, intValue);
                }
                return Enum.Parse(enumType, value, true);
            }
            catch (Exception ex)
            {
                throw new ParseException(value, enumType, columnName, ex);
            }
        }

        /// <summary>
        /// 해당 타입을 파싱할 수 있는지 확인합니다.
        /// </summary>
        public bool CanParse(Type targetType)
        {
            if (targetType.IsEnum)
                return true;

            if (_defaultParsers.ContainsKey(targetType))
                return true;

            foreach (var parser in _customParsers)
            {
                if (parser.CanParse(targetType))
                    return true;
            }

            return false;
        }
    }
}
