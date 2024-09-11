using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Extentions
{
    public static class CodeHelper
    {
        private static Random _random;

        public static int Random(this int data)
        {
            if (_random == null)
            {
                _random = new Random();
            }
            var randomPos = _random.Next(0, data);
            return randomPos;
        }

        public static double RandomDouble(this double data)
        {
            if (_random == null)
            {
                _random = new Random();
            }
            var randomPos = data * _random.NextDouble();
            return randomPos;
        }

        public static T Random<T>(this IList<T> data)
        {
            if (data.Any())
            {
                if (_random == null)
                {
                    _random = new Random();
                }
                var randomPos = _random.Next(0, data.Count());
                return data[randomPos];
            }
            return data.FirstOrDefault();
        }
        public static List<Guid> KickOneEmpty(this List<Guid> data)
        {
            data = (data != null && data.Count() == 1 && data.First() == Guid.Empty) ? null : data;
            return data;
        }

        public static string Aggregate<T>(this List<T> source, string delimeter = " / ")
        {
            if (source == null)
            {
                return "";
            }
            var s = source.Aggregate("", (current, i) => current + (delimeter + i));
            if (s.Length > delimeter.Length)
            {
                s = s.Substring(delimeter.Length);
            }
            return s;
        }

        public static Guid ToGuid(this string source)
        {
            return Guid.Parse(source);
        }

        public static Guid ToGuid(this string source, Guid defaultValue)
        {
            Guid i;
            if (Guid.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static Guid? ToGuid(this string source, Guid? defaultValue)
        {
            Guid i;
            if (Guid.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static Guid? ToNullableGuid(this string source)
        {
            if (String.IsNullOrEmpty(source))
            {
                return null;
            }
            return Guid.Parse(source);
        }

        public static bool ToBool(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            return Convert.ToBoolean(source);
        }

        public static bool ToBool(this bool? source)
        {
            if (source == null)
            {
                return false;
            }
            return (bool)source;
        }

        public static bool ToBool(this string source, bool defaultValue)
        {
            bool i;
            if (Boolean.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static bool ToBoolAndOneZero(this string source, bool defaultValue)
        {
            if (String.IsNullOrEmpty(source))
            {
                return defaultValue;
            }
            if (source == "1")
            {
                return true;
            }
            if (source == "0")
            {
                return false;
            }
            bool i;
            if (Boolean.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static bool? ToBool(this string source, bool? defaultValue)
        {
            if (source == null)
            {
                return null;
            }
            bool i;
            if (Boolean.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static int ToInt(this string source)
        {
            return Convert.ToInt32(source);
        }

        public static int ToInt(this bool source)
        {
            return source ? 1 : 0;
        }

        public static int ToInt(this string source, int defaultValue)
        {
            int i;
            if (Int32.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static int? ToNullableInt(this string s)
        {
            if (s == null)
            {
                return null;
            }
            int i;
            if (Int32.TryParse(s, out i)) return i;
            return null;
        }

        public static int? ToInt(this string source, int? defaultValue)
        {
            int i;
            if (Int32.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static decimal? ToNullableDecimal(this string s)
        {
            if (s == null)
            {
                return null;
            }
            decimal i;
            if (Decimal.TryParse(s, out i))
            {
                return i;
            }
            return null;
        }

        //Todo когда дату конвертируем в стринг, всегда пишем ToString(CultureInfo.InvariantCulture)
        [Obsolete]
        public static DateTime ToDateTime(this string source)
        {
            try
            {
                return DateTime.Parse(source);
            }
            catch (Exception e)
            {
                try
                {
                    var str = new[]
                    {
                        CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern,
                        CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
                        CultureInfo.InvariantCulture.DateTimeFormat.FullDateTimePattern,
                        CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern,
                        CultureInfo.InvariantCulture.DateTimeFormat.LongDatePattern,
                        CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern,
                        CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.InvariantCulture.DateTimeFormat.LongTimePattern,
                        CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern,
                        CultureInfo.InvariantCulture.DateTimeFormat.LongDatePattern + " " + CultureInfo.InvariantCulture.DateTimeFormat.LongTimePattern,
                        CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern,
                    };
                    return DateTime.ParseExact(source, str, CultureInfo.CurrentCulture, DateTimeStyles.None);
                }
                catch
                {
                    return new DateTime();
                }
            }
        }

        [Obsolete]
        //Todo когда дату конвертируем в стринг, всегда пишем ToString(CultureInfo.InvariantCulture)
        public static DateTime ToLicenseDateTime(this string source)
        {
            var invariantDate = Convert.ToDateTime(source, CultureInfo.InvariantCulture);
            var utcDate = DateTime.SpecifyKind(invariantDate, DateTimeKind.Utc);
            var dateTime = Convert.ToDateTime(utcDate, CultureInfo.CurrentCulture).ToLocalTime();
            return dateTime;
        }

        //Todo когда дату конвертируем в стринг, всегда пишем ToString(CultureInfo.InvariantCulture)
        [Obsolete]
        public static DateTime? ToDateTime(this string source, DateTime? defaultValue)
        {
            if (source == null)
            {
                return null;
            }
            DateTime i;
            if (DateTime.TryParse(source, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static T ToEnum<T>(this string source) where T : struct
        {
            return (T)Enum.Parse(typeof(T), source);
        }


        public static double ToDouble(this string source)
        {
            return Convert.ToDouble(source);
        }

        public static double? ToDouble(this string source, double? defaultValue)
        {
            double i;
            var parsingSource = source;
            if (!String.IsNullOrEmpty(parsingSource))
            {
                parsingSource = parsingSource.Replace(".", ",");
            }
            if (double.TryParse(parsingSource, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static double ToNotNullableDouble(this string source, double defaultValue)
        {
            double i;
            var parsingSource = source;
            if (!String.IsNullOrEmpty(parsingSource))
            {
                parsingSource = parsingSource.Replace(".", ",");
            }
            if (double.TryParse(parsingSource, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static float ToFloat(this string source, float defaultValue = 0)
        {
            float i;
            var parsingSource = source;
            if (!String.IsNullOrEmpty(parsingSource))
            {
                parsingSource = parsingSource.Replace(".", ",");
            }
            else
            {
                return defaultValue;
            }
            if (float.TryParse(parsingSource, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static float? ToNullableFloat(this string source, float? defaultValue = null)
        {
            float i;
            var parsingSource = source;
            if (!String.IsNullOrEmpty(parsingSource))
            {
                parsingSource = parsingSource.Replace(".", ",");
            }
            else
            {
                return defaultValue;
            }
            if (float.TryParse(parsingSource, out i))
            {
                return i;
            }
            return defaultValue;
        }

        public static float ToFloat(this double? source, float defaultValue)
        {
            if (source == null)
                return defaultValue;

            return (float)source;
        }

        public static float ToFloat(this int? source, float defaultValue)
        {
            if (source == null)
                return defaultValue;

            return (float)source;
        }

        public static string ToShortDateString(this DateTime? source)
        {
            if (source == null)
            {
                return null;
            }
            return ((DateTime)source).ToShortDateString();
        }

        public static string ToSpecialFormatDate(this DateTime date)
        {
            var month = "";
            switch (date.Month)
            {
                case 1:
                    month = "января";
                    break;
                case 2:
                    month = "февраля";
                    break;
                case 3:
                    month = "марта";
                    break;
                case 4:
                    month = "апреля";
                    break;
                case 5:
                    month = "мая";
                    break;
                case 6:
                    month = "июня";
                    break;
                case 7:
                    month = "июля";
                    break;
                case 8:
                    month = "августа";
                    break;
                case 9:
                    month = "сентября";
                    break;
                case 10:
                    month = "октября";
                    break;
                case 11:
                    month = "ноября";
                    break;
                case 12:
                    month = "декабря";
                    break;
            }

            return date.ToString("dd ") + month;
        }

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static List<Guid> ToList(this Guid source)
        {
            return new List<Guid> { source };
        }

        public static string ToExcelExportType(this DateTime date)
        {
            return date.ToString("yyyyMMddHHmmssfff");
        }

        public static string ToUnitTestValue(this DateTime date)
        {
            return date.ToString("yMMddHHmmssfff");
        }

        public static List<string> ToWords(this string str)
        {
            if (String.IsNullOrEmpty(str))
            {
                return new List<string>();
            }
            return str.Trim(' ').Split(';').Select(x1 => x1.Trim(' ')).ToList();
        }

        public static string ToUnitTestValueWithRandom(this DateTime date, int randNumbers = 3)
        {
            if (_random == null)
            {
                _random = new Random();
            }
            var maxValue = (int)Math.Pow(10, randNumbers);
            var randomPos = _random.Next(0, maxValue).ToString();
            while (randomPos.Length < randNumbers)
            {
                randomPos = "0" + randomPos;
            }
            return date.ToString("yMMddHHmmss") + randomPos;
        }

        public static string SurroundByBrackets(this string source)
        {
            return String.Format("[{0}]", source);
        }

        public static string ToLowerOrNull(this string source)
        {
            var x = String.IsNullOrEmpty(source) ? null : source.ToLower();
            return x;
        }

        public static string FirstUpper(this string source)
        {
            var x = String.IsNullOrEmpty(source) ? null : Char.ToUpper(source[0]) + source.Substring(1, source.Length - 1).ToLower();
            return x;
        }

        public static string OnlyFirstUpper(this string source)
        {
            var x = String.IsNullOrEmpty(source) ? null : Char.ToUpper(source[0]) + source.Substring(1, source.Length - 1);
            return x;
        }

        public static string OnlyFirstLower(this string source)
        {
            var x = String.IsNullOrEmpty(source) ? null : Char.ToLower(source[0]) + source.Substring(1, source.Length - 1);
            return x;
        }

        public static string GetLowerSubstring(this string source, int chars = 1)
        {
            if (String.IsNullOrEmpty(source))
            {
                return null;
            }
            if (source.Length < chars)
            {
                return source.ToLower();
            }
            return source.Substring(0, chars).ToLower();
        }

        public static bool Between(this DateTime date, DateTime start, DateTime end)
        {
            return start.CompareTo(date) <= 0 && end.CompareTo(date) >= 0;
        }

        public static string ToUpper(this string value, int index = 0, int? length = null)
        {
            if (length == null || length > value.Length)
            {
                length = value.Length;
            }
            value = value.Substring(index, (int)length).ToUpper() + value.Substring((int)length);
            return value;
        }

        public static string TrimValue(this string value, char c = ' ')
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Trim(c);
        }

        public static string GetEmptyStringIfNull(this string value)
        {
            if (value == null)
            {
                return String.Empty;
            }
            return value;
        }

        public static bool IsHasValue(this string value)
        {
            return !String.IsNullOrEmpty(value);
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return String.IsNullOrEmpty(value);
        }

        public static string TrimStart(this string value, string c)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            if (value.StartsWith(c))
            {
                value = value.Substring(c.Length);
            }
            return value;
        }

        public static string TrimEnd(this string value, string c)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            if (value.EndsWith(c))
            {
                value = value.Substring(0, value.Length - c.Length);
            }
            return value;
        }

        public static string ToValueIfNullOrEmpty(this string value, string defaultValue = null)
        {
            if (String.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value;
        }

        public static string NameOf<T, TT>(this T obj, Expression<Func<T, TT>> propertyAccessor)
        {
            var expr = GetExpression(propertyAccessor.Body);
            return expr;
        }

        private static string GetExpression(Expression memberExpression)
        {
            if (memberExpression.NodeType == ExpressionType.MemberAccess)
            {
                var member = memberExpression as MemberExpression;
                if (member == null)
                {
                    return null;
                }
                var exp = "";
                if (member.Expression != null)
                {
                    exp = GetExpression(member.Expression);
                    if (exp == null)
                    {
                        exp = "";
                    }
                    else
                    {
                        exp += ".";
                    }
                }
                return exp + member.Member.Name;
            }
            return null;
        }

        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();

            // Где-то этот метод используется так, что список меняется внутри цикла по результату, потому добавил 
            // ToArray прямо сюда, чтобы избежать таких проблем
            foreach (TSource element in source.ToArray())
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static string ToLowerOrEmpty(this string source)
        {
            if (source == null)
            {
                return String.Empty;
            }
            return source.ToLower();
        }

        public static bool ContainsIfNotNull(this string source, string search)
        {
            if (source == null)
            {
                return false;
            }

            return source.Contains(search);
        }

        public static string ToSearchWord(this string source)
        {
            if (source == null)
            {
                return "";
            }
            return source.Trim().ToLower().Replace('ё', 'е').Replace('й', 'и').Replace('э', 'е').Replace("(", "").Replace(")", "").Replace("-", "");
        }

        public static string GetFullName(string lastName, string firstName, string middleName)
        {
            var first = String.IsNullOrWhiteSpace(firstName) ? "" : " " + firstName;
            var middle = String.IsNullOrWhiteSpace(middleName) ? "" : " " + middleName;
            var full = lastName + first + middle;
            return full;
        }

        public static string GetShortName(string lastName, string firstName, string middleName)
        {
            if (!String.IsNullOrWhiteSpace(firstName) || !String.IsNullOrWhiteSpace(middleName))
            {
                lastName = lastName + " ";
            }

            var first = String.IsNullOrWhiteSpace(firstName) ? "" : firstName.Substring(0, 1) + ".";
            var middle = String.IsNullOrWhiteSpace(middleName) ? "" : middleName.Substring(0, 1) + ".";
            var full = lastName + first + middle;
            return full;
        }

        public static bool IsFileLocked(this FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        public static string UrlFormated(this string value)
        {
            if (value == null)
            {
                return null;
            }
            return value.Replace("\\", "/").Replace("//", "/");
        }

        public static string GetDeclension(this int number, string nominativ, string genetiv, string plural)
        {
            number = number % 100;
            if (number >= 11 && number <= 19)
            {
                return plural;
            }
            var i = number % 10;
            switch (i)
            {
                case 1:
                    return nominativ;
                case 2:
                case 3:
                case 4:
                    return genetiv;
                default:
                    return plural;
            }
        }

        public static bool RegexIsMatch(string pattern, string input)
        {
            return new Regex(pattern).IsMatch(input);
        }

        public static DateTime AddPeriod(this DateTime source, int? sourcePeriod, string sourceUnit)
        {
            DateTime result = source;

            int period = sourcePeriod ?? 1;
            period = period >= 0 ? period : period * -1;

            string unit = (sourceUnit.IsHasValue() ? sourceUnit : "месяц").ToLower();

            switch (unit)
            {
                case "день":
                    result = source.AddDays(period);
                    break;
                case "неделя":
                    result = source.AddDays(period * 7);
                    break;
                case "месяц":
                    result = source.AddMonths(period);
                    break;
                case "год":
                    result = source.AddYears(period);
                    break;
                default:
                    result = source.AddMonths(period);
                    break;
            }

            return result;
        }

        public static DateTime SubPeriod(this DateTime source, int? sourcePeriod, string sourceUnit)
        {
            DateTime result = source;

            int period = sourcePeriod ?? 1;
            period = period <= 0 ? period : period * -1;

            string unit = (sourceUnit.IsHasValue() ? sourceUnit : "месяц").ToLower();

            switch (unit)
            {
                case "день":
                    result = source.AddDays(period);
                    break;
                case "неделя":
                    result = source.AddDays(period * 7);
                    break;
                case "месяц":
                    result = source.AddMonths(period);
                    break;
                case "год":
                    result = source.AddYears(period);
                    break;
                default:
                    result = source.AddMonths(period);
                    break;
            }

            return result;
        }

        public static bool IsEmail(this string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Возвращает результат логического сложения предикатов
        /// </summary>
        public static bool Or<T>(this IEnumerable<Predicate<T>> predicates, T value)
        {
            foreach (var predicate in predicates)
            {
                if (predicate(value))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Возвращает результат логического умножения предикатов
        /// </summary>
        public static bool And<T>(this IEnumerable<Predicate<T>> predicates, T value)
        {
            foreach (var predicate in predicates)
            {
                if (!predicate(value))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
