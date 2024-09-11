using System;
using System.Collections.Generic;
using System.Linq;

namespace MssqlToMysqlMigrator.Structure
{
    public class DatabaseStructure
    {
        public DatabaseStructure()
        {
            Schemas = new List<Schema>();
        }

        public List<Schema> Schemas { get; set; }

        public Schema AppendSchema(string schema)
        {
            var sh = Schemas.FirstOrDefault(x => x.Name.ToLower() == schema.ToLower());
            if (sh == null)
            {
                sh = new Schema() { Name = schema };
                Schemas.Add(sh);
            }

            return sh;
        }

        public Schema GetSchema(string name)
        {
            return Schemas.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        }
    }

    public class Schema
    {
        public Schema()
        {
            Tables = new List<Table>();
            Views = new List<View>();
        }

        public string Name { get; set; }
        public List<Table> Tables { get; set; }
        public List<View> Views { get; set; }

        public Table AppendTable(string table, string comment)
        {
            var t = Tables.FirstOrDefault(x => x.Name.ToLower() == table.ToLower());
            if (t == null)
            {
                t = new Table() { Name = table, Comment = comment };
                t.Schema = this;
                Tables.Add(t);
            }

            return t;
        }

        public View AppendView(string name, string definition)
        {
            var t = Views.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            if (t == null)
            {
                t = new View() { Name = name, Definition = definition };
                t.Schema = this;
                Views.Add(t);
            }

            return t;
        }

        public Table GetTable(string name)
        {
            return Tables.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        }

        public View GetView(string name)
        {
            return Views.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Table : SchemaObject
    {
        public Table()
        {
            Columns = new List<Column>();
            Indexes = new List<Index>();
        }

        public List<Column> Columns { get; set; }
        public List<Index> Indexes { get; set; }

        public string Comment { get; set; }

        public Column AppendColumn(string column, string comment, string columnDefinition)
        {
            var t = Columns.FirstOrDefault(x => x.Name.ToLower() == column.ToLower());
            if (t == null)
            {
                t = new Column() { Name = column, Comment = comment, ColumnDefinition = columnDefinition };
                t.Table = this;
                Columns.Add(t);
            }

            return t;
        }

        public Index AppendIndex(string name, int type, bool isPrimary, bool isUnique)
        {
            var index = new Index(name, type, isPrimary, isUnique);
            index.Table = this;
            Indexes.Add(index);
            return index;
        }

        public Column GetColumn(string name)
        {
            return Columns.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class View : SchemaObject
    {
        public View()
        {
            ReferencedViews = new List<View>();
        }

        public string Definition { get; set; }

        public List<View> ReferencedViews { get; set; }

        public View AppendReferencedView(View view)
        {
            ReferencedViews.Add(view);
            return this;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class SchemaObject
    {
        public Schema Schema { get; set; }

        public string Name { get; set; }

        public string FullName => Schema?.Name + "." + Name;

        public string SqlFullName => "["+Schema?.Name + "].[" + Name + "]";
    }

    public class Column
    {
        public Table Table { get; set; }

        /// <summary>
        /// этот столбец ссылается на когото 
        /// </summary>
        public List<ForeignKey> ForeignKeys { get; set; }

        /// <summary>
        /// те кто ссылаются на этот столбец
        /// </summary>
        public List<ForeignKey> ReferenceForeignKeys { get; set; }

        public string Name { get; set; }
        public string Comment { get; set; }
        public string ColumnDefinition { get; set; }
        public ColumnTypes Type { get; set; }
        public int MaxLength { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public string CollationName { get; set; }
        public string FullName => Table?.Schema?.Name + "." + Table?.Name + "." + Name;

        public Column SetType(
            int type,
            int maxLength,
            int precision,
            int scale,
            bool isNullable,
            bool isIdentity,
            string collationName
            )
        {
            if (!Enum.IsDefined(typeof(ColumnTypes), type))
            {
                throw new Exception("type " + type + " undefined in " + FullName);
            }
            Type = (ColumnTypes)type;
            MaxLength = maxLength;
            Precision = precision;
            Scale = scale;
            IsNullable = isNullable;
            IsIdentity = isIdentity;
            CollationName = collationName;
            return this;
        }

        public void AppendForeignKey(
            Column referenceColumn,
            string name,
            int deleteAction,
            int updateAction,
            bool isDisabled)
        {

            if (!Enum.IsDefined(typeof(ForeignKeyActions), deleteAction))
            {
                throw new Exception("fk deleteAction " + deleteAction + " undefined in " + FullName + " fk: " + name);
            }
            if (!Enum.IsDefined(typeof(ForeignKeyActions), updateAction))
            {
                throw new Exception("fk updateAction " + updateAction + " undefined in " + FullName + " fk: " + name);
            }

            var fk = new ForeignKey();
            fk.Name = name;
            fk.Column = this;
            fk.ReferenceColumn = referenceColumn;
            fk.DeleteAction = (ForeignKeyActions)deleteAction;
            fk.UpdateAction = (ForeignKeyActions)updateAction;
            fk.IsDisabled = isDisabled;

            if (ForeignKeys == null)
            {
                ForeignKeys = new List<ForeignKey>();
            }

            ForeignKeys.Add(fk);

            if (referenceColumn.ReferenceForeignKeys == null)
            {
                referenceColumn.ReferenceForeignKeys = new List<ForeignKey>();
            }

            referenceColumn.ReferenceForeignKeys.Add(fk);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ForeignKey
    {
        public string Name { get; set; }

        /// <summary>
        /// Откуда.
        /// </summary>
        public Column Column { get; set; }

        /// <summary>
        /// Куда.
        /// </summary>
        public Column ReferenceColumn { get; set; }
        public ForeignKeyActions DeleteAction { get; set; }
        public ForeignKeyActions UpdateAction { get; set; }
        public bool IsDisabled { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Index
    {
        public Index(string name, int type, bool isPrimary, bool isUnique)
        {
            if (!Enum.IsDefined(typeof(IndexTypes), type))
            {
                throw new Exception("index type " + type + " undefined in" + name);
            }

            Name = name;
            Type = (IndexTypes)type;
            IsPrimary = isPrimary;
            IsUnique = isUnique;
            Columns = new List<IndexColumn>();
        }

        public string Name { get; set; }
        public IndexTypes Type { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsUnique { get; set; }

        public List<IndexColumn> Columns { get; set; }
        public Table Table { get; set; }

        public void AppendColumn(Column column, bool isIncluded, bool isDescending)
        {
            Columns.Add(new IndexColumn { Column = column, IsIncluded = isIncluded, IsDescending = isDescending });
        }

        public class IndexColumn
        {
            public Column Column { get; set; }
            public bool IsIncluded { get; set; }
            public bool IsDescending { get; set; }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum ColumnTypes
    {
        Int = 56,
        Bigint = 127,
        Nvarchar = 231,
        Varchar = 167,
        Decimal = 106,
        Bit = 104,
        Uniqueidentifier = 36,
        Date = 40,
        Datetime = 61,
        Float = 62,
        Char = 175,
        Text = 35,
        Image = 34,
        Time = 41,
    }

    public enum ForeignKeyActions
    {
        NoAction = 0,
        Cascade = 1,
        SetNull = 2,
        SetDefault = 3,
    }

    public enum IndexTypes
    {
        Clustered = 1,
        NonclusteredUnique = 2,
        Xml = 3,
        Spatial = 4,
        ClusteredColumnStore = 5,
        NonClusteredColumnStore = 6,
        NonClusteredHash = 7,
    }
}
