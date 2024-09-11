using System;
using System.Collections.Generic;

namespace MssqlToMysqlMigrator.Structure
{
    public static class MysqlScriptExtentions
    {
        public static string GetMysqlName(this SchemaObject table)
        {
            return table.Schema.Name.ToLower() + "_" + table.Name.ToLower();
        }

        public static List<string> GetUsingsInView(this SchemaObject table)
        {
            var tableName = table.Name;
            var schemaName = table.Schema.Name;
            var additional = new List<string>();
            
            if (table.Schema.Name == "Result")
            {
                if (table.Name == "building")
                {
                    tableName = "Building";
                }
            }
            if (table.Schema.Name == "Result")
            {
                if (table.Name == "itog")
                {
                    tableName = "Itog";
                }
                if(table.Name == "blood_pressure")
                {
                    tableName = "Blood_pressure";
                }
                if (table.Name == "inval")
                {
                    tableName = "Inval";
                }
            }
            if (table.Schema.Name == "Finance")
            {
                if (table.Name == "MedprogFullInfo")
                {
                    additional.Add("FInance.MedprogFullInfo");
                }
            }
            if (table.Schema.Name == "Finance")
            {
                if (table.Name == "MedprogDocument")
                {
                    additional.Add("Finance.MedProgDocument");
                }
            }
            if (table.Schema.Name == "Reference")
            {
                if (table.Name == "MKBAllClasses")
                {
                    additional.Add("[Reference].MkbAllClasses");
                }
            }


            var t1 = schemaName + "." + tableName;
            var t2 = "[" + schemaName + "].[" + tableName + "]";
            var t3 = "[" + schemaName + "]." + tableName;
            var t4 = schemaName + ".[" + tableName + "]";

            var list = new List<string> { t1, t2, t3, t4 };
            list.AddRange(additional);
            return list;
        }

        public static string GetMysqlType(this Column column)
        {
            if (column.Type == ColumnTypes.Nvarchar)
            {
                if (column.MaxLength == -1)
                {
                    column.CollationName.ToMysqlCollation();
                    return "LONGTEXT CHARACTER SET 'utf8mb4' COLLATE 'utf8mb4_ru_0900_ai_ci'";
                }

                if (column.MaxLength <= 0 || column.MaxLength > 8000)
                {
                    throw new Exception();
                }

                return $"VARCHAR({column.MaxLength / 2}) CHARACTER SET 'utf8mb4' COLLATE 'utf8mb4_ru_0900_ai_ci'";
            }
            if (column.Type == ColumnTypes.Varchar)
            {
                if (column.MaxLength == -1)
                {
                    return "LONGTEXT CHARACTER SET 'utf8mb4' COLLATE 'utf8mb4_ru_0900_ai_ci'";
                }
                if (column.MaxLength <= 0 || column.MaxLength > 4000)
                {
                    throw new Exception();
                }

                return $"VARCHAR({column.MaxLength}) CHARACTER SET 'utf8mb4' COLLATE 'utf8mb4_ru_0900_ai_ci'";
            }
            if (column.Type == ColumnTypes.Char)
            {
                return $"VARCHAR({column.MaxLength}) CHARACTER SET 'utf8mb4' COLLATE 'utf8mb4_ru_0900_ai_ci'";
            }

            if (column.Type == ColumnTypes.Decimal)
            {
                return $"DECIMAL({column.Precision},{column.Scale})";
            }

            return MysqlTypes[column.Type];
        }
        public static string ToMysqlCollation(this string collation)
        {
            if(collation == "Cyrillic_General_CI_AS")
            {
                return "utf8mb4_ru_0900_ai_ci";
            }
            throw new Exception("unrecognize collation " + collation);
        }


        public static Dictionary<ColumnTypes, string> MysqlTypes => mysqlTypes ?? (mysqlTypes = GetMysqlTypes());

        private static Dictionary<ColumnTypes, string> GetMysqlTypes()
        {
            var dict = new Dictionary<ColumnTypes, string>();
            dict.Add(ColumnTypes.Int, "INT");
            dict.Add(ColumnTypes.Bigint, "BIGINT");
            // dict.Add(ColumnTypes.Nvarchar, "INT");
            // dict.Add(ColumnTypes.Varchar, "INT");
            // dict.Add(ColumnTypes.Decimal, "INT");
            dict.Add(ColumnTypes.Bit, "TINYINT(1)");
            dict.Add(ColumnTypes.Uniqueidentifier, "VARCHAR(64)");
            dict.Add(ColumnTypes.Date, "DATE");
            dict.Add(ColumnTypes.Datetime, "DATETIME(6)");
            dict.Add(ColumnTypes.Float, "double precision");
            // dict.Add(ColumnTypes.Char , "INT");
            dict.Add(ColumnTypes.Text, "LONGTEXT");
            dict.Add(ColumnTypes.Image, "LONGBLOB");
            dict.Add(ColumnTypes.Time, "TIME");
            return dict;
        }

        private static Dictionary<ColumnTypes, string> mysqlTypes;

        public static string GetMysqlScriptValue(this Object value, Type type)
        {
            if (value is System.DBNull)
            {
                return "null";
            }

            if (type == typeof(Boolean))
            {
                return (bool)value == true ? "1" : "0";
            }
            if(type == typeof(Int32))
            {
                return value.ToString();
            }
            if (type == typeof(double))
            {
                return value.ToString().Replace(',','.');
            }
            if (type == typeof(float))
            {
                return value.ToString().Replace(',', '.');
            }
            if (type == typeof(decimal))
            {
                return value.ToString().Replace(',', '.');
            }
            if (type == typeof(DateTime))
            {
                return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "'";
            }

            return "'" +value.ToString().Replace(@"\",@"\\").Replace(@"'", @"''") + "'";
        }

        public static string GetPostgresScriptValue(this Object value, Type type)
        {
            if (value is System.DBNull)
            {
                return "null";
            }
            ////if (type == typeof(Boolean))
            ////{
            ////    return (bool)value == true ? "1" : "0";
            ////}
            if (type == typeof(Int32))
            {
                return value.ToString();
            }
            if (type == typeof(double))
            {
                return value.ToString().Replace(',', '.');
            }
            if (type == typeof(float))
            {
                return value.ToString().Replace(',', '.');
            }
            if (type == typeof(decimal))
            {
                return value.ToString().Replace(',', '.');
            }
            if (type == typeof(DateTime))
            {
                return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "'";
            }

            return "'" + value.ToString().Replace(@"\", @"\\").Replace(@"'", @"''") + "'";
        }

        private static bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

    }
}
