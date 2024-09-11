using Extentions;
using MssqlToMysqlMigrator.Helpers;
using MssqlToMysqlMigrator.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MssqlToMysqlMigrator
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private bool getStructure = true;
        public bool ShowMessageBox = true;
        private DatabaseStructure _structure;

        private void MainForm_Load(object sender, EventArgs e)
        {
            //connectionMssqlTextBox.Text = "data source=localhost\\sqlexpress;initial catalog=money-dev;persist security info=True;user id=money;password=money;App=MssqlToMysqlMigrator;";
            //connectionMysqlTextBox.Text = $"Host=localhost;Username=postgres;Password=RjirfLeyz;Database=money-dev";


            uiTableCheckBox.Checked = false;
            uiViewCheckBox.Checked = true;
            uiFkCheckBox.Checked = false;
            uiDataCheckBox.Checked = false;
            uiDataLogCheckBox.Checked = false;
            uiShowInConsoleCheckBox.Checked = true;
            uiCreateDbCheckBox.Checked = false;
        }

        public void SetParameters(string mssql, string postgre, bool structure, bool table, bool view, bool fk, bool data, bool dataLog, bool console, bool run)
        {
            connectionMssqlTextBox.Text = mssql;
            connectionMysqlTextBox.Text = postgre;

            getStructure = structure;
            uiTableCheckBox.Checked = table;
            uiViewCheckBox.Checked = view;
            uiFkCheckBox.Checked = fk;
            uiDataCheckBox.Checked = data;
            uiDataLogCheckBox.Checked = dataLog;
            uiShowInConsoleCheckBox.Checked = console;
            uiCreateDbCheckBox.Checked = run;
        }

        public string GetLog()
        {
            return textBox1.Text;
        }

        private void uiRunButton_Click(object sender, EventArgs e)
        {
#if DEBUG
            Run();
#else
            var th = new Thread(Run);
            th.Start();
#endif
        }

        private void Run()
        {
            uiRunButton.Enabled = false;
            Run2();
            uiRunButton.Enabled = true;
        }

        public void Run2()
        {

#if !DEBUG
            try
            {
#endif
            var mssqlCs = connectionMssqlTextBox.Text;
            var mysqlCs = connectionMysqlTextBox.Text;
            var startDate = DateTime.Now;
            var getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
            var structure = _structure;


            //using (var sqlProvider = new SqlProvider(mssqlCs))
            //{
            //    sqlProvider.ExecuteQuery("SELECT 1 FROM dbo.VersionInfo WHERE Version = '202012311217'");
            //    if (sqlProvider.Rows.Count == 0)
            //    {
            //        throw new Exception("Promigriruy db mssql migraciyami");
            //    }
            //}

            if (getStructure)
            {
                structure = new DatabaseStructure();

                label1.Text = "Получение структуры mssqlDB";

                using (var sqlProvider = new SqlProvider(mssqlCs))
                {
                    sqlProvider.ExecuteQuery("SELECT 1 FROM sys.schemas AS s");

                    getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                    startDate = DateTime.Now;
                    ConsoleWriteLine("-- Установка соединения: " + getStructureTime + "ms");

                    var tableQuery = @"SELECT 
s.name AS SchemaName,
t.name AS TableName,
tep.value AS TableComment,
c.name AS ColumnName,
ep.value as ColumnComent,
object_definition(c.default_object_id) AS ColumnDefinition,
c.system_type_id as ColumnTypeId,
c.max_length as ColumnMaxLength,
c.precision as ColumnPrecision,
c.scale as ColumnScale,
c.is_nullable As ColumnIsNullable,
c.is_identity as ColumnIsIdentity,
c.collation_name AS CollationName
FROM sys.schemas AS s
    INNER JOIN sys.tables AS t ON t.schema_id = s.schema_id
    INNER JOIN sys.columns c ON t.OBJECT_ID = c.OBJECT_ID
    LEFT JOIN sys.extended_properties ep on t.object_id = ep.major_id
                                         and c.column_id = ep.minor_id
                                         and ep.name = 'MS_Description'
    LEFT JOIN sys.extended_properties tep on t.object_id = tep.major_id
                                         and tep.minor_id = 0
                                         and tep.name = 'MS_Description'";
                    sqlProvider.ExecuteQuery(tableQuery);

                    foreach (var row in sqlProvider.Rows)
                    {
                        structure
                            .AppendSchema(row.Field<string>("SchemaName"))
                            .AppendTable(row.Field<string>("TableName"), row.Field<string>("TableComment"))
                            .AppendColumn(row.Field<string>("ColumnName"), row.Field<string>("ColumnComent"), row.Field<string>("ColumnDefinition"))
                            .SetType
                                (
                                    row.Field<int>("ColumnTypeId"),
                                    row.Field<int>("ColumnMaxLength"),
                                    row.Field<int>("ColumnPrecision"),
                                    row.Field<int>("ColumnScale"),
                                    row.Field<bool>("ColumnIsNullable"),
                                    row.Field<bool>("ColumnIsIdentity"),
                                    row.Field<string>("CollationName")
                                );
                    }

                    getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                    startDate = DateTime.Now;
                    ConsoleWriteLine("-- Получение текущей структуры\\таблицы: " + getStructureTime + "ms");

                    var viewQuery = @"select schema_name(v.schema_id) AS SchemaName,
       v.name as ViewName,
       v.create_date as created,
       v.modify_date as last_modified,
       m.definition AS ViewDefinition
from sys.views v
join sys.sql_modules m 
     on m.object_id = v.object_id";

                    sqlProvider.ExecuteQuery(viewQuery);

                    foreach (var row in sqlProvider.Rows)
                    {
                        structure
                            .AppendSchema(row.Field<string>("SchemaName"))
                            .AppendView(row.Field<string>("ViewName"), row.Field<string>("ViewDefinition"));
                    }

                    getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                    startDate = DateTime.Now;
                    ConsoleWriteLine("-- Получение текущей структуры\\представления: " + getStructureTime + "ms");

                    var viewDependencedQuery = @"SELECT 
schema_name(v.schema_id) AS SchemaName,
v.name AS ViewName, 
d.referenced_schema_name AS ReferencedSchemaName, 
d.referenced_entity_name AS ReferencedViewName
FROM sys.views AS V
	INNER JOIN sys.sql_expression_dependencies AS D ON D.referencing_id = V.object_id
	INNER JOIN sys.views AS RV ON RV.object_id = D.referenced_id";

                    sqlProvider.ExecuteQuery(viewDependencedQuery);

                    foreach (var row in sqlProvider.Rows)
                    {
                        var view = structure
                            .GetSchema(row.Field<string>("SchemaName"))
                            .GetView(row.Field<string>("ViewName"));

                        var refView = structure
                            .GetSchema(row.Field<string>("ReferencedSchemaName"))
                            .GetView(row.Field<string>("ReferencedViewName"));

                        view.AppendReferencedView(refView);
                    }

                    getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                    startDate = DateTime.Now;
                    ConsoleWriteLine("-- Получение текущей структуры\\зависимости представлений: " + getStructureTime + "ms");

                    var fkQuery = @"select o.name AS Name,
	s.name as SchemaName,
    t.name as TableName, 
	c.name as ColumnName,
	rs.name as ReferenceSchemaName,
	r.name AS ReferenceTableName,
	rc.name AS ReferenceColumnName,
	f.delete_referential_action as DeleteAction,
	f.update_referential_action As UpdateAction,
	f.is_disabled as IsDisabled
from sys.foreign_key_columns as fk
inner join sys.foreign_keys as f on f.object_id = fk.constraint_object_id
inner join sys.objects as o on o.object_id = fk.constraint_object_id
inner join sys.tables as t on fk.parent_object_id = t.object_id
inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id
inner join sys.schemas AS s on s.schema_id = t.schema_id
inner join sys.tables as r on fk.referenced_object_id = r.object_id
inner join sys.columns as rc on fk.referenced_object_id = rc.object_id and fk.referenced_column_id = rc.column_id
inner join sys.schemas AS rs on rs.schema_id = r.schema_id";

                    sqlProvider.ExecuteQuery(fkQuery);

                    foreach (var row in sqlProvider.Rows)
                    {
                        var column = structure
                            .GetSchema(row.Field<string>("SchemaName"))
                            .GetTable(row.Field<string>("TableName"))
                            .GetColumn(row.Field<string>("ColumnName"));

                        var referencedColumn = structure
                            .GetSchema(row.Field<string>("ReferenceSchemaName"))
                            .GetTable(row.Field<string>("ReferenceTableName"))
                            .GetColumn(row.Field<string>("ReferenceColumnName"));

                        column.AppendForeignKey
                            (
                                referencedColumn,
                                row.Field<string>("Name"),
                                row.Field<int>("DeleteAction"),
                                row.Field<int>("UpdateAction"),
                                row.Field<bool>("IsDisabled")
                            );
                    }

                    getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                    startDate = DateTime.Now;
                    ConsoleWriteLine("-- Получение текущей структуры\\ключи: " + getStructureTime + "ms");

                    var pkQuery = @"select i.[name] as IndexName,
	i.type as IndexType,
    i.is_primary_key AS IsPrimary,
    i.is_unique as IsUnique,
    substring(column_names, 1, len(column_names)-1) as [IndexColumns],
    case when i.[type] = 1 then 'Clustered index'
        when i.[type] = 2 then 'Nonclustered unique index'
        when i.[type] = 3 then 'XML index'
        when i.[type] = 4 then 'Spatial index'
        when i.[type] = 5 then 'Clustered columnstore index'
        when i.[type] = 6 then 'Nonclustered columnstore index'
        when i.[type] = 7 then 'Nonclustered hash index'
        end as index_type,
    case when i.is_unique = 1 then 'Unique'
        else 'Not unique' end as [unique],
	schema_name(t.schema_id) as SchemaName,
    t.[name] as TableName, 
    case when t.[type] = 'U' then 'Table'
        when t.[type] = 'V' then 'View'
        end as [object_type]
from sys.objects t
    inner join sys.indexes i
        on t.object_id = i.object_id
    cross apply (select col.[name] + ' ' + CAST(ic.is_included_column as nvarchar) + ' ' +  CAST(ic.is_descending_key as nvarchar)+ ','
                    from sys.index_columns ic
                        inner join sys.columns col
                            on ic.object_id = col.object_id
                            and ic.column_id = col.column_id
                    where ic.object_id = t.object_id
                        and ic.index_id = i.index_id
                            order by key_ordinal
                            for xml path ('') ) D (column_names)
where t.is_ms_shipped <> 1
and index_id > 0
order by i.[name]";

                    sqlProvider.ExecuteQuery(pkQuery);

                    foreach (var row in sqlProvider.Rows)
                    {
                        row.Field<string>("IndexName");
                        var type = row.Field<string>("IndexType");
                        var columns = row.Field<string>("IndexColumns");
                        var columnsList = columns.Split(',');


                        var index = structure
                            .GetSchema(row.Field<string>("SchemaName"))
                            .GetTable(row.Field<string>("TableName"))
                            .AppendIndex(row.Field<string>("IndexName"),
                            row.Field<int>("IndexType"),
                            row.Field<bool>("IsPrimary"),
                            row.Field<bool>("IsUnique"));
                        foreach (var column in columnsList)
                        {
                            var name = column.Substring(0, column.Length - 4);
                            var isIncluded = column.Substring(column.Length - 3, 1) == "1";
                            var isDescending = column.Substring(column.Length - 1, 1) == "1";
                            var tableColumn = index.Table.Columns.First(x => x.Name == name);
                            index.AppendColumn(tableColumn, isIncluded, isDescending);
                        }
                    }

                    getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                    startDate = DateTime.Now;
                    ConsoleWriteLine("-- Получение текущей структуры\\индексы: " + getStructureTime + "ms");
                }

                _structure = structure;
            }
            if (1 == 0)
            {
                foreach (var x in structure.Schemas)
                {
                    ConsoleWriteLine(x.Name);

                    foreach (var t in x.Tables)
                    {
                        foreach (var c in t.Columns)
                        {
                            if (c.Type == ColumnTypes.Image)
                            {
                                ConsoleWriteLine(c.FullName + " as ImageColumn");
                            }
                            if (c.Type == ColumnTypes.Char)
                            {
                                ConsoleWriteLine(c.FullName + " as CharColumn");
                            }
                        }
                    }
                }
            }

            label1.Text = "Формирование скриптов создания mysqlDB";
            var createSqript = new StringBuilder();
            var mtest = new StringBuilder();
            var isPostgress = true;
            var pkIndexPrefix = 0;
            if (uiTableCheckBox.Checked)
            {

                createSqript.AppendLine("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

                foreach (var schema in structure.Schemas.OrderBy(x => x.Name))
                {
                    createSqript.AppendLine($"CREATE SCHEMA IF NOT EXISTS {schema.Name.GetPostgresName()};");
                    createSqript.AppendLine();

                    foreach (var table in schema.Tables.OrderBy(x => x.Name))
                    {
                        if (isPostgress)
                        {
                            var tableScripts = new StringBuilder();
                            var commentScripts = new StringBuilder();
                            var sequenceScripts = new StringBuilder();
                            string stringSequenceName = null;

                            tableScripts.AppendLine($"CREATE TABLE IF NOT EXISTS {table.GetPostgresName()} (");
                            foreach (var column in table.Columns)
                            {
                                if (column.IsIdentity)
                                {
                                    stringSequenceName = table.Schema.Name.GetPostgresName() + "." + (table.Name + "_seq").GetPostgresName();
                                    sequenceScripts.Append($"CREATE SEQUENCE {stringSequenceName};");
                                    //mtest.Append($"SELECT setval('{stringSequenceName}', (SELECT MAX() FROM \"{table.Schema}\".\"{table.Name}\"");
                                    sequenceScripts.AppendLine();
                                    //mtest.Append($"SELECT setval('{stringSequenceName}', (SELECT max(\"{column.Name}\") FROM \"{table.Schema}\".\"{table.Name}\"));");
                                    //mtest.Append(Environment.NewLine);
                                }
                                var columnDefault = column.ColumnDefinition;
                                if (!string.IsNullOrEmpty(column.ColumnDefinition))
                                {
                                    columnDefault = columnDefault.Substring(1, columnDefault.Length - 2);
                                    if (column.Type == ColumnTypes.Uniqueidentifier)
                                    {
                                        if (columnDefault == "newid()")
                                        {
                                            columnDefault = "uuid_generate_v4()";
                                        }
                                        else
                                        {
                                            throw new Exception("blah ColumnTypes.Uniqueidentifier");
                                        }
                                    }
                                    else if (column.Type == ColumnTypes.Bit)
                                    {
                                        if (columnDefault == "(1)")
                                        {
                                            columnDefault = "true";
                                        }
                                        else if (columnDefault == "(0)")
                                        {
                                            columnDefault = "false";
                                        }
                                        else if (columnDefault == "'1'")
                                        {
                                            columnDefault = "false";
                                        }
                                        else if (columnDefault == "'0'")
                                        {
                                            columnDefault = "false";
                                        }
                                        else
                                        {
                                            throw new Exception("blah ColumnTypes.Bit");
                                        }
                                    }
                                    else if (column.Type == ColumnTypes.Int)
                                    {
                                        columnDefault = columnDefault.Trim('(').Trim(')');
                                    }
                                    else if (column.Type == ColumnTypes.Varchar)
                                    {

                                    }
                                    else if (column.Type == ColumnTypes.Datetime)
                                    {
                                        columnDefault = null;
                                    }
                                    else
                                    {
                                        throw new Exception("blah ColumnTypes");
                                    }
                                }
                                tableScripts.AppendLine($"    {column.Name.GetPostgresName()} {column.GetPostgresType()} " +
                                    (column.IsIdentity ? $" DEFAULT NEXTVAL('{stringSequenceName}')" : "") +
                                    (!column.IsIdentity && !string.IsNullOrEmpty(columnDefault) ? " DEFAULT " + columnDefault : "") +
                                    $"{(column.IsNullable ? " NULL" : " NOT NULL")},");

                                if (!string.IsNullOrEmpty(column.Comment))
                                {
                                    commentScripts.AppendLine($@"COMMENT ON COLUMN {table.GetPostgresName()}.{column.Name.GetPostgresName()}
IS '{column.Comment}'; ");

                                }
                            }
                            var pk = table.Indexes.FirstOrDefault(x => x.IsPrimary);
                            if (pk != null)
                            {
                                // переименуем все, при переезде по людски
                                pk.Name = table.Name + "_pk";
                                var pkName = pk.Name;
                                if (pk.Name.Length > 55 && (schema.Name == "Reference" || schema.Name == "Result"))
                                {
                                    var indexName = "0000000" + pkIndexPrefix;
                                    var indexName6 = indexName.Substring(indexName.Length - 6);
                                    pkName = pkName.Substring(0, 20) + "_" + indexName6 + "_" + pk.Name.Substring(pk.Name.Length - 25);
                                    pkIndexPrefix++;
                                }

                                var pkColumns = pk.Columns;
                                if (table.Schema.Name.ToLower() == "result")
                                {
                                    pkColumns = pkColumns.OrderBy(x => x.Column.Name.ToLower() != "recordguid").ThenBy(x => x.Column.Name.ToLower() != "versionid").ToList();
                                }
                                var indexColumns = pkColumns.Select(x => x.Column.Name.GetPostgresName()).ToList().Aggregate(", ");
                                tableScripts.AppendLine($"CONSTRAINT {pkName.GetPostgresName()} PRIMARY KEY ({indexColumns}),");
                            }
                            ////else
                            ////{
                            ////    var identity = table.Columns.FirstOrDefault(x => x.IsIdentity);
                            ////    if (identity != null)
                            ////    {
                            ////        createSqript.AppendLine($"PRIMARY KEY (`{identity.Name}`),");
                            ////    }
                            ////}
                            var uqs = table.Indexes.Where(x => x.IsUnique && x.IsPrimary == false);
                            foreach (var uq in uqs)
                            {
                                var indexColumns = uq.Columns.Select(x => x.Column.Name.GetPostgresName()).ToList().Aggregate(",");
                                tableScripts.AppendLine($"CONSTRAINT {uq.Name} UNIQUE ({indexColumns}),");
                            }


                            if (!string.IsNullOrEmpty(table.Comment))
                            {
                                commentScripts.AppendLine($@"COMMENT ON TABLE {table.GetPostgresName()}
IS '{table.Comment}'; ");

                            }

                            createSqript.AppendLine(sequenceScripts.ToString());
                            var ts = tableScripts.ToString();
                            ts = ts.Substring(0, ts.Length - 3) + ");";
                            createSqript.Append(ts);
                            createSqript.AppendLine();
                            createSqript.Append(commentScripts.ToString());
                        }
                        else
                        {
                            GetTableScriptMysql(createSqript, table);
                        }
                        createSqript.AppendLine();
                        createSqript.AppendLine();

                    }

                    createSqript.AppendLine();
                    createSqript.AppendLine();

                }

                createSqript.AppendLine("-- migrations block START");
                if (!isPostgress)
                {
                    createSqript.AppendLine(@"DROP FUNCTION IF EXISTS PatIndex;

DELIMITER $$

CREATE FUNCTION PatIndex(pattern VARCHAR(255), tblString VARCHAR(255)) RETURNS INTEGER
    DETERMINISTIC
BEGIN
    DECLARE i INTEGER;
    SET i = 1;
    myloop: WHILE(i <= LENGTH(tblString)) DO
        IF SUBSTRING(tblString, i, 1) REGEXP pattern THEN
            RETURN(i);
            LEAVE myloop;
        END IF;
        SET i = i + 1;
    END WHILE;
    RETURN(0);
END");

                    createSqript.AppendLine(@"DELIMITER $$
CREATE FUNCTION TruncateTime(dateValue DateTime) RETURNS date 
DETERMINISTIC
BEGIN
    RETURN Date(dateValue);
END");

                }
                else
                {
                    createSqript.AppendLine(@"CREATE or replace FUNCTION iIF(
    condition boolean,       -- IF condition
    true_result anyelement,  -- THEN
    false_result anyelement  -- ELSE
) RETURNS anyelement AS $f$
  SELECT CASE WHEN condition THEN true_result ELSE false_result END
$f$  LANGUAGE SQL IMMUTABLE;");


                }
                createSqript.AppendLine("-- migrations block END");

                createSqript.AppendLine();
                createSqript.AppendLine();

                getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                startDate = DateTime.Now;
                ConsoleWriteLine("-- Формирование скриптов\\таблицы: " + getStructureTime + "ms");
            }

                if (uiFkCheckBox.Checked)
                {
                    foreach (var schema in structure.Schemas)
                    {
                        foreach (var table in schema.Tables)
                        {
                            foreach (var colId in table.Columns.Where(x => x.IsIdentity))
                            {
                                var stringSequenceName = table.Schema.Name.GetPostgresName() + "." + (table.Name + "_seq").GetPostgresName();
                                mtest.Append($"SELECT setval('{stringSequenceName}', (SELECT max(\"{colId.Name}\") FROM \"{table.Schema}\".\"{table.Name}\"));");
                                mtest.Append(Environment.NewLine);
                            }
                            foreach (var column in table.Columns.Where(x => x.ForeignKeys != null))
                            {
                                foreach (var fk in column.ForeignKeys)
                                {
                                    if (isPostgress)
                                    {
                                        // переименуем все, при переезде по людски
                                        fk.Name = table.Name + "_" + column.Name + "_fk";
                                        createSqript.AppendLine($"ALTER TABLE {table.GetPostgresName()}");
                                        createSqript.AppendLine($"ADD CONSTRAINT " + fk.Name);
                                        createSqript.Append($"FOREIGN KEY({column.Name.GetPostgresName()}) REFERENCES {fk.ReferenceColumn.Table.GetPostgresName()}({fk.ReferenceColumn.Name.GetPostgresName()})");
                                    }
                                    else
                                    {
                                        createSqript.AppendLine($"ALTER TABLE `{table.GetMysqlName()}`");
                                        createSqript.AppendLine($"ADD CONSTRAINT " + fk.Name);
                                        createSqript.Append($"FOREIGN KEY({column.Name}) REFERENCES {fk.ReferenceColumn.Table.GetMysqlName()}({fk.ReferenceColumn.Name})");
                                    }
                                    if (fk.UpdateAction == ForeignKeyActions.Cascade)
                                    {
                                        createSqript.AppendLine();
                                        createSqript.Append($"ON UPDATE CASCADE");
                                    }
                                    else if (fk.UpdateAction == ForeignKeyActions.SetDefault)
                                    {
                                        createSqript.AppendLine();
                                        createSqript.Append($"ON UPDATE SET DEFAULT");
                                    }
                                    else if (fk.UpdateAction == ForeignKeyActions.SetNull)
                                    {
                                        createSqript.AppendLine();
                                        createSqript.Append($"ON UPDATE SET NULL");
                                    }
                                    if (fk.DeleteAction == ForeignKeyActions.Cascade)
                                    {
                                        createSqript.AppendLine();
                                        createSqript.Append($"ON DELETE CASCADE");
                                    }
                                    else if (fk.DeleteAction == ForeignKeyActions.SetDefault)
                                    {
                                        createSqript.AppendLine();
                                        createSqript.Append($"ON DELETE SET DEFAULT");
                                    }
                                    else if (fk.DeleteAction == ForeignKeyActions.SetNull)
                                    {
                                        createSqript.AppendLine();
                                        createSqript.Append($"ON DELETE SET NULL");
                                    }
                                    createSqript.Append($";");
                                    createSqript.AppendLine();
                                    createSqript.AppendLine();

                                }
                            }
                        }
                    }

                    createSqript.AppendLine();
                    createSqript.AppendLine();
                    createSqript.Append(mtest.ToString());
                    createSqript.AppendLine();
                    createSqript.AppendLine();
                    if (mtest.Length == 0)
                    {
                        var msg = "ERROR!!GENERATING DATA WITHOUT CORRECT IDENTITY!!GENERATE SCRIPT FOR TABLES FIRSTLY!!";
                        if (uiDataLogCheckBox.Checked)
                        {
                            ConsoleWriteLine(msg);
                        }
                        else
                        {
                            throw new Exception(msg);
                        }

                    }
                    getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                    startDate = DateTime.Now;
                    ConsoleWriteLine("-- Формирование скриптов\\ключи: " + getStructureTime + "ms");

                }

            if (uiViewCheckBox.Checked)
            {

                var viewReplacer = new Dictionary<string, string>();
                var columnReplacer = new Dictionary<string, string>();
                foreach (var table in structure.Schemas.SelectMany(x => x.Tables))
                {
                    foreach (var replacedValue in table.GetUsingsInView())
                    {
                        var mysqlTableName = isPostgress ? table.GetPostgresName() : table.GetMysqlName();
                        viewReplacer.Add(replacedValue, mysqlTableName);
                    }
                    if (isPostgress)
                    {
                        foreach (var col in table.Columns)
                        {
                            if (!columnReplacer.Values.Any(x => x == ".\"" + col.Name + "\""))
                            {
                                columnReplacer.Add("." + col.Name, ".\"" + col.Name + "\"");
                                columnReplacer.Add(".[" + col.Name + "]", ".\"" + col.Name + "\"");
                            }
                        }
                    }
                }

                foreach (var view in structure.Schemas.SelectMany(x => x.Views))
                {
                    foreach (var replacedValue in view.GetUsingsInView())
                    {
                        var mysqlTableName = isPostgress ? view.GetPostgresName() : view.GetMysqlName();
                        viewReplacer.Add(replacedValue, mysqlTableName);
                    }
                }

                viewReplacer = viewReplacer.OrderByDescending(x => x.Key.Length).ToDictionary(x => x.Key, x => x.Value);
                columnReplacer = columnReplacer.OrderByDescending(x => x.Key.Length).ToDictionary(x => x.Key, x => x.Value);

                var views = structure.Schemas.SelectMany(x => x.Views).OrderBy(x => x.FullName).ToList();
                var orderedViews = new List<Structure.View>();
                var orderedViewFullNames = new List<string>();
                while (true)
                {
                    for (int i = 0; i < views.Count; i++)
                    {
                        Structure.View view = views[i];
                        if (view.ReferencedViews.Count == 0
                            || view.ReferencedViews.All(x1 => orderedViewFullNames.Contains(x1.FullName.ToLower())))
                        {
                            orderedViews.Add(view);
                            orderedViewFullNames.Add(view.FullName.ToLower());
                            views.RemoveAt(i);
                            i--;
                        }
                    }

                    if (views.Count == 0)
                    {
                        break;
                    }
                }

                foreach (var view in orderedViews)
                {
                    var newDefenition = view.Definition;

                    if (newDefenition.Contains("заменил SELECT * на перечисление"))
                    {
                        var a = 1;
                    }

                    newDefenition = newDefenition.TrimStart("--заменил SELECT * на перечисление\n");
                    newDefenition = newDefenition.TrimStart("-- \n");
                    newDefenition = newDefenition.TrimStart("-- GO\n");
                    newDefenition = newDefenition.TrimStart("--GO\n");
                    newDefenition = newDefenition.TrimStart("--DROP VIEW [Reference].[ViewWorkplace]\n");
                    newDefenition = newDefenition.TrimStart("-- DROP VIEW [Reference].[ViewDolzhnostj]\n");
                    newDefenition = newDefenition.TrimStart("--DROP VIEW [Reference].[ViewDolzhnostj]\n");
                    newDefenition = newDefenition.TrimStart("-- DROP VIEW [Reference].[ViewProfessiya]\n");
                    newDefenition = newDefenition.TrimStart("-- GO\n");
                    newDefenition = newDefenition.TrimStart("--GO\n");
                    newDefenition = newDefenition.TrimStart('-').TrimStart(' ').TrimStart('\n').TrimStart('\r').TrimStart('\n');

                    int index = newDefenition.IndexOf('\n');
                    var nameBlock = newDefenition.Substring(0, index);
                    newDefenition = newDefenition.Substring(index + 1);

                    nameBlock = nameBlock
                         .Replace("CREATE VIEW", "CREATE OR REPLACE VIEW")
                         .Replace("CREATE View", "CREATE OR REPLACE VIEW");

                    if (isPostgress)
                    {
                        // синтаксис вьюх индивидуален и не совместим, так что весь этот шлак ниже сугубо для одной древней БД и можно удолить
                        nameBlock = nameBlock.Replace("[", "\"").Replace("]", "\"");
                        nameBlock = nameBlock.Trim(' ');
                        var parts = nameBlock.Split(' ');
                        var repalced2 = parts[4];
                        var replaced = repalced2.Trim('\r').Trim('\n').Trim('\r');
                        var newParts = replaced.Split('.');
                        var new2 = "\"" + newParts[0].Trim('"') + "\"" + "." + "\"" + newParts[1].Trim('"') + "\"";
                        nameBlock = nameBlock.Replace(replaced, new2);
                        ////nameBlock = nameBlock.Replace("\"Document\".DocumentAllSaveTableColumn", "\"Document\".\"DocumentAllSaveTableColumn\"");
                        ////nameBlock = nameBlock.Replace("\"Examination\".ExamProfession", "\"Examination\".\"ExamProfession\"");
                        nameBlock = nameBlock.Replace("\"Examination\".ExamProfession", "\"Examination\".\"ExamProfession\"");



                        newDefenition = newDefenition.Replace("CAST(MSP.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MSP.Identifikаtor_medprogrаmmy");
                        newDefenition = newDefenition.Replace("CAST(MSP.Identifikаtor_tipа_tseny AS uniqueidentifier)", "MSP.Identifikаtor_tipа_tseny");
                        newDefenition = newDefenition.Replace("CAST(SP.Identifikаtor_tipа_tseny AS uniqueidentifier)", "SP.Identifikаtor_tipа_tseny");
                        newDefenition = newDefenition.Replace("CAST([Price].Identifikаtor_tipа_tseny AS uniqueidentifier)", "[Price].Identifikаtor_tipа_tseny");
                        newDefenition = newDefenition.Replace("CAST(MedPat.Identifikаtor_tipа_tseny AS uniqueidentifier)", "MedPat.Identifikаtor_tipа_tseny");
                        newDefenition = newDefenition.Replace("CAST(MedPat.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MedPat.Identifikаtor_medprogrаmmy");
                        newDefenition = newDefenition.Replace("CAST(Main.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "Main.Identifikаtor_medprogrаmmy");
                        newDefenition = newDefenition.Replace("CONVERT(DECIMAL(18,2), Main.Summа_predoplаty)", "Main.Summа_predoplаty");
                        newDefenition = newDefenition.Replace("CONVERT(DECIMAL(18,2), Main.Summa_franshizy)", "Main.Summa_franshizy");
                        newDefenition = newDefenition.Replace("CAST(Price.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "Price.Identifikаtor_medprogrаmmy");
                        newDefenition = newDefenition.Replace("CAST(Price.Identifikаtor_tipа_tseny AS uniqueidentifier)", "Price.Identifikаtor_tipа_tseny");
                        newDefenition = newDefenition.Replace("CAST(MPP.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MPP.Identifikаtor_medprogrаmmy");
                        newDefenition = newDefenition.Replace("CAST(MIV2.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MIV2.Identifikаtor_medprogrаmmy");

                        newDefenition = newDefenition.Replace("CAST(JB.IDENTIFIKATOR_ORG AS uniqueidentifier)", "JB.IDENTIFIKATOR_ORG");
                        newDefenition = newDefenition.Replace("TOP (SELECT COUNT (*) FROM [Reference].[HarmfulFactorGroup])  ", "");
                        newDefenition = newDefenition.Replace("TOP (SELECT COUNT (*) FROM [Reference].[HarmfulFactor])", "");

                        foreach (var r in viewReplacer)
                        {
                            newDefenition = newDefenition.Replace(r.Key, r.Value);
                        }
                        foreach (var r in columnReplacer)
                        {
                            newDefenition = newDefenition.Replace(r.Key, r.Value);
                        }

                        //newDefenition = newDefenition.Replace("\"Record\".\"Record\"StatPart")
                        newDefenition = newDefenition.Replace("CONVERT(bit, main.\"Pervichnaya_registratsiya_otpechatkov_paljtsev\")"
                            , "main.\"Pervichnaya_registratsiya_otpechatkov_paljtsev\"::BOOLEAN");
                        newDefenition = newDefenition.Replace("CONVERT(bit, main.\"Zakreplenie_identificacionnoy_kartochki\")"
                            , "main.\"Zakreplenie_identificacionnoy_kartochki\"::BOOLEAN");

                        newDefenition = newDefenition.Replace("camera.number", "camera.\"Number\"");
                        newDefenition = newDefenition.Replace(" IsNull(", " COALESCE(");
                        newDefenition = newDefenition.Replace(" ISNULL(", " COALESCE(");
                        newDefenition = newDefenition.Replace(",ISNULL(", ",COALESCE(");
                        newDefenition = newDefenition.Replace("ISNULL(", "COALESCE(");
                        newDefenition = newDefenition.Replace("Guid AS", "\"Guid\" AS");
                        newDefenition = newDefenition.Replace("SELECT DocumentGuid", "SELECT \"DocumentGuid\"");
                        newDefenition = newDefenition.Replace(",DocumentChildGuid", ",DocumentChildGuid");
                        newDefenition = newDefenition.Replace(",EmbeddingTypeId", ",EmbeddingTypeId");
                        newDefenition = newDefenition.Replace('[', '"').Replace(']', '"');
                        newDefenition = newDefenition.Replace("WITH \"parent\" AS", "WITH recursive \"parent\" AS");
                        newDefenition = newDefenition.Replace("ProvidedServ.\"SumForPay\"Total", "ProvidedServ.SumForPayTotal");
                        newDefenition = newDefenition.Replace("Comments.\"Comment\"sCount", "Comments.CommentsCount");
                        newDefenition = newDefenition.Replace("Record\"Guid\"", "\"RecordGuid\"");
                        newDefenition = newDefenition.Replace(",VersionId", ",\"VersionId\"");
                        newDefenition = newDefenition.Replace(",Identifikаtor_vidа_oplаty", ",\"Identifikаtor_vidа_oplаty\"");
                        newDefenition = newDefenition.Replace(",Organisation\"Guid\"", ",\"OrganisationGuid\"");
                        newDefenition = newDefenition.Replace(",T.\"D\"epartment", ",T.Department");
                        newDefenition = newDefenition.Replace("e.FirstVersionCreator\"Guid\"", "e.\"FirstVersionCreatorGuid\"");
                        newDefenition = newDefenition.Replace("MP.\"D\"eductibleValue", "MP.\"DeductibleValue\"");
                        newDefenition = newDefenition.Replace("T.\"Price\"Number", "T.\"PriceNumber\"");
                        newDefenition = newDefenition.Replace(",DocumentChildGuid", ",\"DocumentChildGuid\"");
                        newDefenition = newDefenition.Replace(",EmbeddingTypeId", ",\"EmbeddingTypeId\"");
                        newDefenition = newDefenition.Replace(",DocumentChildGuid", ",DocumentChildGuid");
                        newDefenition = newDefenition.Replace(",DocumentChildGuid", ",DocumentChildGuid");

                        newDefenition = newDefenition.Replace("TND.IsMainName = 1", "TND.\"IsMainName\" = true");
                        newDefenition = newDefenition.Replace("DN.IsMainName = 1", "DN.\"IsMainName\" = true");
                        newDefenition = newDefenition.Replace("MD.\"IsFinal\" = 1", "MD.\"IsFinal\" = true");
                        newDefenition = newDefenition.Replace("d.\"Vynesti_v_list_utochnennyh_diаgnozov\" = 1", "d.\"Vynesti_v_list_utochnennyh_diаgnozov\" = true");
                        newDefenition = newDefenition.Replace("a.\"Otobrazitj_v_signaljnoy_informatsii\" = 1", "a.\"Otobrazitj_v_signaljnoy_informatsii\" = true");

                        newDefenition = newDefenition.Replace("ORDER BY Disc.\"Value\" DESC, Disc.\"Percent\" DESC"
, "ORDER BY Disc.\"Value\" DESC, Disc.\"Percent\" DESC LIMIT 1");
                        newDefenition = newDefenition.Replace("SELECT TOP 1 Id", "SELECT Id");
                        newDefenition = newDefenition.Replace("SELECT TOP (10000)", "SELECT");
                        newDefenition = newDefenition.Replace("SELECT Guid", "SELECT \"Guid\"");
                        newDefenition = newDefenition.Replace("SELECT RecordGuid, Max(Version) AS Version", "SELECT \"RecordGuid\", Max(\"Version\") AS \"Version\"");
                        newDefenition = newDefenition.Replace("SELECT RecordGuid, Max(VersionId) AS Version", "SELECT \"RecordGuid\", Max(\"VersionId\") AS \"Version\"");
                        newDefenition = newDefenition.Replace("GROUP BY RecordGuid", "GROUP BY \"RecordGuid\"");

                        newDefenition = newDefenition.Replace("PaymentMedProgGuid", "PaymentMedprogGuid");
                        newDefenition = newDefenition.Replace("MedProgGuid", "MedprogGuid");
                        newDefenition = newDefenition.Replace("medProgGuid", "MedprogGuid");
                        newDefenition = newDefenition.Replace(".MedprogGuid", ".\"MedprogGuid\"");
                        newDefenition = newDefenition.Replace(",D.Savetable", ",D.\"SaveTable\"");

                        newDefenition = newDefenition.Replace(".IsDiagnosis", ".\"IsDiagnosis\"");
                        newDefenition = newDefenition.Replace(".IsLifeAnamnesis", ".\"IsLifeAnamnesis\"");
                        newDefenition = newDefenition.Replace(".IsAlergyAnamnesis", ".\"IsAlergyAnamnesis\"");
                        newDefenition = newDefenition.Replace(".IsBlood", ".\"IsBlood\"");
                        newDefenition = newDefenition.Replace(".PaySum", ".\"PaySum\"");

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Document\".\"DocumentAllChield\""))
                        {
                            newDefenition = newDefenition.Replace(",DocumentGuid", ",\"DocumentGuid\"");
                            newDefenition = newDefenition.Replace(",DocumentChildGuid", ",\"DocumentChildGuid\"");
                            newDefenition = newDefenition.Replace(", DocumentChildGuid", ", \"DocumentChildGuid\"");
                            newDefenition = newDefenition.Replace(",EmbeddingTypeId", ",\"EmbeddingTypeId\"");
                            newDefenition = newDefenition.Replace("RowNumber", "\"RowNumber\"");
                        }

                        newDefenition = newDefenition.Replace("ORDER BY DocumentGuid", "ORDER BY \"DocumentGuid\"");

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Document\".\"DocumentAllSaveTableColumn\""))
                        {
                            newDefenition = newDefenition.Replace("DocumentGuid", "\"DocumentGuid\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Epmz\".\"EpmzWithSignalInfo\""))
                        {
                            newDefenition = newDefenition.Replace("THEN 0", "THEN false");
                            newDefenition = newDefenition.Replace("THEN 1 ELSE 0", "THEN true ELSE false");
                            newDefenition = newDefenition.Replace("AS BIT", "AS boolean");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Examination\".\"Exam\""))
                        {
                            newDefenition = newDefenition.Replace("AS BIT", "AS boolean");
                        }

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Examination\".\"ExamTemplateFullInfo\""))
                        {
                            newDefenition = newDefenition.Replace("AS NeedAlco", "AS \"NeedAlco\"");
                        }

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Document\".\"DocumentAllServs\""))
                        {
                            newDefenition = newDefenition.Replace("1 AS IsIncludeServicesToBill,", "true AS IsIncludeServicesToBill,");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Document\".\"DocumentClassAllParent\""))
                        {
                            newDefenition = newDefenition.Replace("        Guid", "        \"Guid\"");
                            newDefenition = newDefenition.Replace("        ,ParentClassGuid", "        ,\"ParentClassGuid\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Document\".\"DocumentGroupAllParent\""))
                        {
                            newDefenition = newDefenition.Replace("    SELECT Guid", "    SELECT \"Guid\"");
                            newDefenition = newDefenition.Replace("        ,ParentGuid", "        ,\"ParentGuid\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Reference\".\"ViewDolzhnostj\""))
                        {
                            //STUFF pcode был удалён, но пусть останется, впадлу проверять
                            newDefenition = newDefenition.Replace("	ID,", "	\"ID\",");
                            newDefenition = newDefenition.Replace("	P_ID,", "	\"P_ID\",");
                            newDefenition = newDefenition.Replace("CONCAT(", "");
                            newDefenition = newDefenition.Replace("		Name,", "		\"Name\" ||");
                            newDefenition = newDefenition.Replace("STUFF(", "");
                            newDefenition = newDefenition.Replace("', '+P.Code", "array_to_string(array_agg(P.\"Code\"), ' ')");
                            newDefenition = newDefenition.Replace("FOR XML PATH('')", "");
                            newDefenition = newDefenition.Replace("), 1, 1, '')", "");

                            newDefenition = newDefenition.Replace("	OKPDTR,", "	\"OKPDTR\",");
                            newDefenition = newDefenition.Replace("	OKZ,", "	\"OKZ\",");
                            newDefenition = newDefenition.Replace("	KCS,", "	\"KCS\",");
                            newDefenition = newDefenition.Replace("	TARIFF,", "	\"TARIFF\",");
                            newDefenition = newDefenition.Replace("	ETKS,", "	\"ETKS\",");
                            newDefenition = newDefenition.Replace("	CATEGOGY", "	\"CATEGOGY\"");
                            newDefenition = newDefenition.Replace("WHERE P_id", "WHERE \"P_ID\"");
                            newDefenition = newDefenition.Replace("\"Name\" ||", "\"Name\",");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Reference\".\"ViewProfessiya\""))
                        {
                            //STUFF pcode был удалён, но пусть останется, впадлу проверять
                            newDefenition = newDefenition.Replace("	ID,", "	\"ID\",");
                            newDefenition = newDefenition.Replace("	P_ID,", "	\"P_ID\",");
                            newDefenition = newDefenition.Replace("CONCAT(", "");
                            newDefenition = newDefenition.Replace("		Name,", "		\"Name\" ||");
                            newDefenition = newDefenition.Replace("STUFF(", "");
                            newDefenition = newDefenition.Replace("', '+P.Code", "array_to_string(array_agg(P.\"Code\"), ' ')");
                            newDefenition = newDefenition.Replace("FOR XML PATH('')", "");
                            newDefenition = newDefenition.Replace("), 1, 1, '')", "");

                            newDefenition = newDefenition.Replace("	OKPDTR,", "	\"OKPDTR\",");
                            newDefenition = newDefenition.Replace("	OKZ,", "	\"OKZ\",");
                            newDefenition = newDefenition.Replace("	KCS,", "	\"KCS\",");
                            newDefenition = newDefenition.Replace("	TARIFF,", "	\"TARIFF\",");
                            newDefenition = newDefenition.Replace("	ETKS,", "	\"ETKS\",");
                            newDefenition = newDefenition.Replace("	CATEGOGY", "	\"CATEGOGY\"");
                            newDefenition = newDefenition.Replace("WHERE P_id", "WHERE \"P_ID\"");
                            newDefenition = newDefenition.Replace("\"Name\" ||", "\"Name\",");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Reference\".\"ViewWorkplace\""))
                        {
                            newDefenition = newDefenition.Replace("	ID,", "	\"ID\",");
                            newDefenition = newDefenition.Replace("	P_ID,", "	\"P_ID\",");
                            newDefenition = newDefenition.Replace("CONCAT(Name, STUFF", "\"Name\" ||");
                            newDefenition = newDefenition.Replace("((SELECT ', ' + P.\"Code\"", "(SELECT array_to_string(array_agg(P.\"Code\"),' ')");
                            newDefenition = newDefenition.Replace("WHERE        P.\"PostId\" = ID FOR XML PATH('')), 1, 1, '')) AS Name,",
                                "WHERE        P.\"PostId\" = ID) AS \"Name\",");
                            newDefenition = newDefenition.Replace("	OKPDTR,", "	\"OKPDTR\",");
                            newDefenition = newDefenition.Replace("	OKZ,", "	\"OKZ\",");
                            newDefenition = newDefenition.Replace("	KCS,", "	\"KCS\",");
                            newDefenition = newDefenition.Replace("	TARIFF,", "	\"TARIFF\",");
                            newDefenition = newDefenition.Replace("	ETKS,", "	\"ETKS\",");
                            newDefenition = newDefenition.Replace("	CATEGOGY", "	\"CATEGOGY\"");
                            newDefenition = newDefenition.Replace("WHERE P_id", "WHERE \"P_ID\"");
                            newDefenition = newDefenition.Replace("OR P_id", "OR \"P_ID\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Examination\".\"ExamProfession\""))
                        {
                            newDefenition = newDefenition.Replace("SELECT ID AS Id", "SELECT \"ID\" AS \"Id\"");
                            newDefenition = newDefenition.Replace(",P_ID AS ParentId", ",\"P_ID\" AS \"ParentId\"");
                            newDefenition = newDefenition.Replace(",Name", ",\"Name\"");
                            newDefenition = newDefenition.Replace("P_id", "\"P_ID\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Examination\".\"ExamStatistic\""))
                        {
                            newDefenition = newDefenition.Replace(", 1, 0) AS", ", true, false) AS");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"UserSystem\".\"UserInfo\""))
                        {
                            newDefenition = newDefenition.Replace("E.Inn", "E.\"INN\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Organisation\".\"EmployeeFullInfo\""))
                        {
                            newDefenition = newDefenition.Replace("isPasswordUnlimited", "\"IsPasswordUnlimited\"");
                        }

                        newDefenition = newDefenition.Replace(",COALESCE(E.\"PaymentMedprogGuid\", cast(cast(0 as binary) as uniqueidentifier)) AS PaymentMedprogGuid"
   , ",E.\"PaymentMedprogGuid\" AS \"PaymentMedprogGuid\"");

                        newDefenition = newDefenition.Replace("COALESCE(CONVERT(uniqueidentifier, \"cnt\".\"Identifikаtor_dokumentа_dlya_nаprаvleniy\"),CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)) AS DocumentGuid"
                            , "\"cnt\".\"Identifikаtor_dokumentа_dlya_nаprаvleniy\" AS \"DocumentGuid\"");
                        ////newDefenition = newDefenition.Replace("IIf(", "IF(");
                        ////newDefenition = newDefenition.Replace("IIF(", "IF(");
                        ////////newDefenition = newDefenition.Replace("AS ServGuid,", "AS \"ServGuid\",");

                        newDefenition = newDefenition.Replace("\"\"DocumentGuid\"\"", "\"DocumentGuid\"");

                        newDefenition = newDefenition.Replace("CONVERT(INT, sanc.\"Poryadok_prohozhdeniya\")", "sanc.\"Poryadok_prohozhdeniya\"");
                        newDefenition = newDefenition.Replace("CONVERT(INT, sa.\"Poryadok_prohozhdeniya\")", "sa.\"Poryadok_prohozhdeniya\"");
                        newDefenition = newDefenition.Replace("CONVERT(INT, sbp.\"Poryadok_prohozhdeniya\")", "sbp.\"Poryadok_prohozhdeniya\"");
                        newDefenition = newDefenition.Replace("CONVERT(INT, st.\"Poryadok_prohozhdeniya\")", "st.\"Poryadok_prohozhdeniya\"");
                        newDefenition = newDefenition.Replace("CONVERT(INT, satt.\"Poryadok_prohozhdeniya\")", "satt.\"Poryadok_prohozhdeniya\"");
                        newDefenition = newDefenition.Replace("CONVERT(varchar, sa.\"Alkogolj_Max\")", "sa.\"Alkogolj_Max\"");
                        newDefenition = newDefenition.Replace("CONVERT(varchar, sa.\"Alkogolj_Min\")", "sa.\"Alkogolj_Min\"");
                        newDefenition = newDefenition.Replace("CONVERT(varchar, sa.\"Alkogolj_AbsMax\")", "sa.\"Alkogolj_AbsMax\"");
                        newDefenition = newDefenition.Replace(",\"Main\".", ",Main.");
                        newDefenition = newDefenition.Replace("+ ' ' +", "|| ' ' ||");
                        newDefenition = newDefenition.Replace("+ '.' +", "|| '.' ||");
                        newDefenition = newDefenition.Replace("+'.' +", "|| '.' ||");
                        newDefenition = newDefenition.Replace("+'.'", "|| '.'");
                        newDefenition = newDefenition.Replace("+ '.'", "|| '.'");
                        newDefenition = newDefenition.Replace("|| '.'+", "|| '.' ||");
                        newDefenition = newDefenition.Replace(",ps.\"ID\"", ",ps.\"Id\"");
                        newDefenition = newDefenition.Replace(",\"Price\".\"", ",Price.\"");
                        newDefenition = newDefenition.Replace(",\"Serv\".\"", ",Serv.\"");

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Finance\".\"MedprogType\""))
                        {
                            newDefenition = newDefenition.Replace("SELECT Id", "SELECT \"ID\"");
                            newDefenition = newDefenition.Replace(",Name", ",\"Name\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Finance\".\"PatientDebts\""))
                        {
                            newDefenition = newDefenition.Replace("BillCancelledPaymentId", "\"BillCancelledPaymentId\"");
                            newDefenition = newDefenition.Replace("WHERE SumToPay", "WHERE \"SumToPay\"");
                            newDefenition = newDefenition.Replace("AND Status", "AND \"Status\"");
                        }
                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Finance\".\"SavingsDiscountPatientPercent\""))
                        {
                            newDefenition = newDefenition.Replace("SELECT Id", "SELECT \"Id\"");
                        }
                        newDefenition = newDefenition.Replace(".PAtientGuid", ".\"PatientGuid\"");
                        newDefenition = newDefenition.Replace(".PaymentMedprogGuid", ".\"PaymentMedprogGuid\"");
                        newDefenition = newDefenition.Replace(".number", ".\"Number\"");
                        newDefenition = newDefenition.Replace(".Meditsinskаya_dolzhnostj", ".\"Meditsinskаya_dolzhnostj\"");
                        newDefenition = newDefenition.Replace(",Meditsinskаya_dolzhnostj", ",\"Meditsinskаya_dolzhnostj\"");
                        newDefenition = newDefenition.Replace("Identifikator_meditsinskoy_dolzhnosti", "\"Identifikator_meditsinskoy_dolzhnosti\"");
                        newDefenition = newDefenition.Replace(".TSEH", ".\"TSeh\"");
                        newDefenition = newDefenition.Replace("CONVERT(int,NS.\"Identifikator_tipa_uvedomleniya\")", "NS.\"Identifikator_tipa_uvedomleniya\"");
                        newDefenition = newDefenition.Replace(", '') + ", ", '') || ");
                        newDefenition = newDefenition.Replace("RecordGuid, VersionId", "\"RecordGuid\", \"VersionId\"");
                        newDefenition = newDefenition.Replace(", VersionId)", ", \"VersionId\")");
                        newDefenition = newDefenition.Replace("Sms AS INT", "\"Sms\" AS INT");
                        ////newDefenition = newDefenition.Replace("AS Sms", "AS \"Sms\"");
                        ////newDefenition = newDefenition.Replace("AS Pochta", "AS \"Pochta\"");
                        newDefenition = newDefenition.Replace("Pochta AS INT", "\"Pochta\" AS INT");
                        newDefenition = newDefenition.Replace("WHEN LEN(", "WHEN LENGTH(");
                        newDefenition = newDefenition.Replace("' ' +", "' ' ||");
                        newDefenition = newDefenition.Replace("END + CASE", "END || CASE");
                        newDefenition = newDefenition.Replace("END +", "END ||");
                        newDefenition = newDefenition.Replace("MD.\"FAMILIYA\" +", "MD.\"FAMILIYA\" ||");

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Patient\".\"PatientFullInfo\""))
                        {
                            newDefenition = newDefenition.Replace("CASE WHEN DATEPART(DY, MD.\"DATA_ROZHDENIYA\") <= DATEPART(DY, GETDATE())",
                                "CASE WHEN date_part('doy', MD.\"DATA_ROZHDENIYA\") <= date_part('doy', now()) ");
                            newDefenition = newDefenition.Replace("THEN DATEDIFF(YY, MD.\"DATA_ROZHDENIYA\", GETDATE()) ",
                                "THEN DATE_PART('year', now()) - DATE_PART('year', MD.\"DATA_ROZHDENIYA\")");
                            newDefenition = newDefenition.Replace("WHEN DATEDIFF(YY, MD.\"DATA_ROZHDENIYA\", DATEADD(yy, - 1, GETDATE())) = - 1",
                                "ELSE DATE_PART('year', now()) - DATE_PART('year', MD.\"DATA_ROZHDENIYA\") -1 END AS Years,");
                            newDefenition = newDefenition.Replace("THEN 0 ELSE DATEDIFF(YY, MD.\"DATA_ROZHDENIYA\", DATEADD(yy, - 1, GETDATE())) END AS Years,",
                                "");
                        }
                        newDefenition = newDefenition.Replace("COALESCE(MC.\"Meditsinskoe_soglasie_dano\", 0)", "COALESCE(MC.\"Meditsinskoe_soglasie_dano\", false)");
                        newDefenition = newDefenition.Replace("CONVERT(varchar, PRV.\"Alkogolj_Max\")", "PRV.\"Alkogolj_Max\"");
                        newDefenition = newDefenition.Replace("CONVERT(varchar, PRV.\"Alkogolj_Min\")", "PRV.\"Alkogolj_Min\"");
                        newDefenition = newDefenition.Replace("CONVERT(varchar, sa.\"Alkogolj_AbsMin\")", "sa.\"Alkogolj_AbsMin\"");
                        newDefenition = newDefenition.Replace("SELECT RecordGuid, MAX(Version) AS Version", "SELECT \"RecordGuid\", MAX(\"Version\") AS \"Version\"");
                        newDefenition = newDefenition.Replace("RecordGuid, MAX(Version) AS Version", "\"RecordGuid\", MAX(\"Version\") AS \"Version\"");

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Record\".\"DirectionPatternDocuments\""))
                        {
                            newDefenition = newDefenition.Replace(", COALESCE(",
                                "");
                            newDefenition = newDefenition.Replace("CAST(DFD.\"Identifikаtor_dokumentа_dlya_nаprаvleniy\" AS uniqueidentifier),",
                                "");
                            newDefenition = newDefenition.Replace("CAST(CAST(0 as binary) as uniqueidentifier)",
                                "");
                            newDefenition = newDefenition.Replace(")  AS DocumentGuid",
                                ", DFD.\"Identifikаtor_dokumentа_dlya_nаprаvleniy\" AS \"DocumentGuid\"");
                        }
                        ////newDefenition = newDefenition.Replace("AS RowNumber", "AS \"RowNumber\"");
                        newDefenition = newDefenition.Replace(",Guid", ",\"Guid\"");
                        newDefenition = newDefenition.Replace(",Name", ",\"Name\"");
                        newDefenition = newDefenition.Replace(",ParentGuid", ",\"ParentGuid\"");
                        newDefenition = newDefenition.Replace("ORDER BY Guid", "ORDER BY \"Guid\"");
                        ////newDefenition = newDefenition.Replace("AS Version", "AS \"Version\"");
                        newDefenition = newDefenition.Replace(" as ", " AS ");
                        newDefenition = newDefenition.Replace(" As ", " AS ").Replace(" aS ", " AS ");
                        newDefenition = newDefenition.Replace("  AS", " AS").Replace("AS  ", "AS ");
                        newDefenition = newDefenition.Replace("  AS", " AS").Replace("AS  ", "AS ");
                        newDefenition = newDefenition.Replace("  AS", " AS").Replace("AS  ", "AS ");
                        newDefenition = newDefenition.Replace("  AS", " AS").Replace("AS  ", "AS ");
                        newDefenition = newDefenition.Replace("  AS", " AS").Replace("AS  ", "AS ");
                        newDefenition = newDefenition.Replace(" As ", " AS ").Replace(" aS ", " AS ");

                        var columnsFuck = newDefenition.Replace("\r\n", "\n").Split('\n');
                        var newLines = "";
                        if (nameBlock.Contains("DirectionDocumentFields"))
                        {
                            var a = 1;
                        }
                        foreach (var columnF in columnsFuck)
                        {
                            if (columnF.Contains(" AS ")
                                    //&& !columnF.ToUpper().Contains(" ON ") 
                                    && !columnF.ToUpper().Contains("FROM ")
                                    && !columnF.ToUpper().Contains(" JOIN "))
                            {
                                var i = columnF.LastIndexOf(" AS ");
                                var rreepl = columnF.Substring(i);
                                var parts2a = rreepl.Split(' ', ')', ',', '\n', '\t');
                                if (parts2a[2] != ""
                                        && !parts2a[2].ToUpper().StartsWith("NVARCHAR")
                                        && (!parts2a[2].ToUpper().StartsWith("INT") || parts2a[2].ToUpper().StartsWith("INTERVAL"))
                                        && !parts2a[2].ToUpper().StartsWith("BOOLEAN")
                                        )
                                {
                                    var oldV = " " + parts2a[1] + " " + parts2a[2];
                                    var newV = " " + parts2a[1] + " \"" + parts2a[2].Trim('"') + "\"";
                                    var newLine = columnF.Replace(oldV, newV);
                                    newLines += newLine + "\n";
                                }
                                else
                                {
                                    newLines += columnF + "\n";
                                }
                            }
                            else
                            {
                                newLines += columnF + "\n";
                            }
                        }
                        newDefenition = newLines.Trim('\n');

                        newDefenition = newDefenition.Replace("\"Device\".\"Device\" AS \"D\"", "\"Device\".\"Device\" AS D");
                        newDefenition = newDefenition.Replace("\"Document\".\"DocumentAllChield\" AS \"DC\"", "\"Document\".\"DocumentAllChield\" AS DC");
                        newDefenition = newDefenition.Replace("\"Document\".\"DocumentClass\" AS \"docChield\"", "\"Document\".\"DocumentClass\" AS docChield");
                        newDefenition = newDefenition.Replace("\"Examination\".\"Examination\" AS \"Exam\"", "\"Examination\".\"Examination\" AS Exam");
                        newDefenition = newDefenition.Replace("\"Examination\".\"ExamTemplate\" AS \"T\"", "\"Examination\".\"ExamTemplate\" AS T");
                        newDefenition = newDefenition.Replace("\"Patient\".\"Patient\" AS \"P\"", "\"Patient\".\"Patient\" AS P");
                        newDefenition = newDefenition.Replace("\"Patient\".\"PatientFullInfo\" AS \"P\"", "\"Patient\".\"PatientFullInfo\" AS P");
                        newDefenition = newDefenition.Replace("\"Device\".\"Device\" AS \"D\"", "\"Device\".\"Device\" AS D");
                        newDefenition = newDefenition.Replace(") AS \"X\"", ") AS X");
                        newDefenition = newDefenition.Replace(") AS \"x\"", ") AS x");
                        newDefenition = newDefenition.Replace("On \"x\".", "ON x.");
                        newDefenition = newDefenition.Replace("\"Organisation\".\"Organisation\" AS \"O\"", "\"Organisation\".\"Organisation\" AS O");
                        newDefenition = newDefenition.Replace(") AS \"LastSP\" ON", ") AS LastSP ON");
                        newDefenition = newDefenition.Replace(") AS \"ProvidedServ\" ON", ") AS ProvidedServ ON");
                        newDefenition = newDefenition.Replace(") AS \"Comments\" ON", ") AS Comments ON");
                        newDefenition = newDefenition.Replace(") AS \"BP\" ON", ") AS BP ON");
                        newDefenition = newDefenition.Replace(") AS \"TN\" ON", ") AS TN ON");
                        newDefenition = newDefenition.Replace(") AS \"V\" ON", ") AS V ON");
                        newDefenition = newDefenition.Replace(" AS \"HF\" ON", " AS HF ON");
                        newDefenition = newDefenition.Replace(" AS \"RHF\" ON", " AS RHF ON");
                        newDefenition = newDefenition.Replace(" AS \"HFG\" ON", " AS HFG ON");
                        newDefenition = newDefenition.Replace(") AS \"T\"", ") AS T");
                        newDefenition = newDefenition.Replace(") AS \"InsF\"", ") AS InsF");
                        newDefenition = newDefenition.Replace(" AS \"D\" ON", " AS D ON");
                        newDefenition = newDefenition.Replace(" AS \"ET\" ON", " AS ET ON");

                        newDefenition = newDefenition.Replace(".ServCount", ".\"ServCount\"");
                        newDefenition = newDefenition.Replace(".InBillCount", ".\"InBillCount\"");
                        newDefenition = newDefenition.Replace(".PaydSum", ".\"PaydSum\"");
                        newDefenition = newDefenition.Replace(".InPayedBillCount", ".\"InPayedBillCount\"");
                        newDefenition = newDefenition.Replace(".SumForPayTotal", ".\"SumForPayTotal\"");
                        newDefenition = newDefenition.Replace(".CommentsCount", ".\"CommentsCount\"");
                        newDefenition = newDefenition.Replace(") InPayedBillCount", ") AS \"InPayedBillCount\"");
                        newDefenition = newDefenition.Replace(") AS Sms", ") AS \"Sms\"");
                        newDefenition = newDefenition.Replace(") AS Sms", ") AS \"Sms\"");
                        newDefenition = newDefenition.Replace("NVARCHAR(max)", "text");
                        newDefenition = newDefenition.Replace("VP.\"IsVisible\" = 1", "VP.\"IsVisible\" = true");
                        newDefenition = newDefenition.Replace("DocN.\"IsMainName\" = 1", "DocN.\"IsMainName\" = true");
                        newDefenition = newDefenition.Replace("TND.\"IsMainName\" = 1", "TND.\"IsMainName\" = true");
                        newDefenition = newDefenition.Replace("DN.\"IsMainName\" = 1", "DN.\"IsMainName\" = true");
                        newDefenition = newDefenition.Replace("Lab.\"IsArchive\" = 0", "Lab.\"IsArchive\" = false");

                        newDefenition = newDefenition.Replace(",UserId", ",\"UserId\"");
                        newDefenition = newDefenition.Replace(",FullName As Name", ",\"FullName\" AS \"Name\"");
                        newDefenition = newDefenition.Replace(",FullName AS", ",\"FullName\" AS");
                        newDefenition = newDefenition.Replace(",LastName", ",\"LastName\"");
                        newDefenition = newDefenition.Replace(",FirstName", ",\"FirstName\"");
                        newDefenition = newDefenition.Replace(",MiddleName", ",\"MiddleName\"");
                        newDefenition = newDefenition.Replace(",BirthDate", ",\"BirthDate\"");
                        newDefenition = newDefenition.Replace(",SEX", ",\"SEX\"");
                        newDefenition = newDefenition.Replace(",JobMedicalPost", ",\"JobMedicalPost\"");
                        newDefenition = newDefenition.Replace("|| FirstName", "|| \"FirstName\"");
                        newDefenition = newDefenition.Replace("|| MiddleName", "|| \"MiddleName\"");
                        newDefenition = newDefenition.Replace("+ ', ' +", "|| ', ' ||");
                        newDefenition = newDefenition.Replace("+', '+", "|| ', ' ||");
                        newDefenition = newDefenition.Replace("\" + r.", "\" || r.");
                        newDefenition = newDefenition.Replace("JobMedicalPost As", "\"JobMedicalPost\" As");
                        newDefenition = newDefenition.Replace("JobMedicalPost AS", "\"JobMedicalPost\" AS");
                        newDefenition = newDefenition.Replace("As NameWithJobMedicalPost", "AS \"NameWithJobMedicalPost\"");
                        newDefenition = newDefenition.Replace("WHERE JobMedicalPost", "WHERE \"JobMedicalPost\"");
                        newDefenition = newDefenition.Replace(",\"SEX\"", ",\"Sex\"");
                        newDefenition = newDefenition.Replace("(StatusTypeId", "(\"StatusTypeId\"");
                        newDefenition = newDefenition.Replace("OR StatusTypeId", "OR \"StatusTypeId\"");
                        newDefenition = newDefenition.Replace("AND Id", "AND \"Id\"");
                        newDefenition = newDefenition.Replace(",PatientGuid", ",\"PatientGuid\"");
                        newDefenition = newDefenition.Replace(",RecordVersion", ",\"RecordVersion\"");
                        newDefenition = newDefenition.Replace(",Number", ",\"Number\"");
                        newDefenition = newDefenition.Replace(",OpenDate", ",\"OpenDate\"");
                        newDefenition = newDefenition.Replace(",CloseDate", ",\"CloseDate\"");
                        newDefenition = newDefenition.Replace(",Result", ",\"Result\"");
                        newDefenition = newDefenition.Replace(",MD.Savetable", ",MD.\"SaveTable\"");
                        newDefenition = newDefenition.Replace(",Code", ",\"Code\"");
                        newDefenition = newDefenition.Replace(",MedprogType", ",\"MedprogType\"");
                        newDefenition = newDefenition.Replace(",MedprogTypeId", ",\"MedprogTypeId\"");
                        newDefenition = newDefenition.Replace(",IsArchive", ",\"IsArchive\"");
                        newDefenition = newDefenition.Replace("IsArchive = 0", "\"IsArchive\" = false");
                        newDefenition = newDefenition.Replace("IsArchive = 1", "\"IsArchive\" = true");
                        newDefenition = newDefenition.Replace("WHERE ParentId", "WHERE \"ParentId\"");
                        newDefenition = newDefenition.Replace(".Department", ".\"Department\"");
                        newDefenition = newDefenition.Replace("ORDER BY Department", "ORDER BY \"Department\"");
                        newDefenition = newDefenition.Replace("Id, Name", "\"Id\", \"Name\"");
                        newDefenition = newDefenition.Replace("WHERE Guid", "WHERE \"Guid\"");
                        newDefenition = newDefenition.Replace("AND Guid", "AND \"Guid\"");
                        newDefenition = newDefenition.Replace("SELECT Id", "SELECT \"Id\"");
                        newDefenition = newDefenition.Replace("ParentId IS NULL", "\"ParentId\" IS NULL");
                        newDefenition = newDefenition.Replace("WHERE ParentId", "WHERE \"ParentId\"");
                        newDefenition = newDefenition.Replace("SELECT \"Id\" from \"Reference\".\"ServGroup\"", "SELECT \"ID\" from \"Reference\".\"ServGroup\"");
                        newDefenition = newDefenition.Replace(".ServGroupId", ".\"ServGroupId\"");
                        newDefenition = newDefenition.Replace("\"Record\".\"PostHarmfulFactor\" AS \"P\"", "\"Record\".\"PostHarmfulFactor\" AS P");
                        newDefenition = newDefenition.Replace("', '+", "', ' ||");
                        newDefenition = newDefenition.Replace("P.\"PostId\" = ID", "P.\"PostId\" = \"ID\"");

                        newDefenition = newDefenition.Replace("As PatientGuid,", "AS \"PatientGuid\",");

                        newDefenition = newDefenition.Replace("RecordGuid,", "\"RecordGuid\",");
                        newDefenition = newDefenition.Replace("VersionId,", "\"VersionId\",");
                        newDefenition = newDefenition.Replace("EstabOGRN,", "\"EstabOGRN\",");
                        newDefenition = newDefenition.Replace("EstabFax,", "\"EstabFax\",");
                        newDefenition = newDefenition.Replace("PropertyType,", "\"PropertyType\",");
                        newDefenition = newDefenition.Replace("EditDate,", "\"EditDate\",");
                        newDefenition = newDefenition.Replace("EstabRegPhoneNumber,", "\"EstabRegPhoneNumber\",");
                        newDefenition = newDefenition.Replace("EstabCode,", "\"EstabCode\",");
                        newDefenition = newDefenition.Replace("ShortName,", "\"ShortName\",");
                        newDefenition = newDefenition.Replace("EstabWebsite,", "\"EstabWebsite\",");
                        newDefenition = newDefenition.Replace("EstabEmail,", "\"EstabEmail\",");
                        newDefenition = newDefenition.Replace("EstabINN,", "\"EstabINN\",");
                        newDefenition = newDefenition.Replace("Teritory,", "\"Teritory\",");
                        newDefenition = newDefenition.Replace("EstabBeginDate,", "\"EstabBeginDate\",");
                        newDefenition = newDefenition.Replace("EstabEndDate,", "\"EstabEndDate\",");
                        newDefenition = newDefenition.Replace("EstabInnerCode,", "\"EstabInnerCode\",");
                        newDefenition = newDefenition.Replace("EstabIsStom,", "\"EstabIsStom\",");
                        newDefenition = newDefenition.Replace("EstabIsGin,", "\"EstabIsGin\",");
                        newDefenition = newDefenition.Replace("EstabIsClinic,", "\"EstabIsClinic\",");
                        newDefenition = newDefenition.Replace("EstabInEstab,", "\"EstabInEstab\",");
                        newDefenition = newDefenition.Replace("EstabType,", "\"EstabType\",");
                        newDefenition = newDefenition.Replace("EstabOwn,", "\"EstabOwn\",");
                        newDefenition = newDefenition.Replace("OKVED,", "\"OKVED\",");
                        newDefenition = newDefenition.Replace("ApprovalPhoneNumber,", "\"ApprovalPhoneNumber\",");
                        newDefenition = newDefenition.Replace("HRPhone,", "\"HRPhone\",");
                        newDefenition = newDefenition.Replace("ShortNameEstabBookKeeper,", "\"ShortNameEstabBookKeeper\",");
                        newDefenition = newDefenition.Replace("IsArchive,", "\"IsArchive\",");
                        newDefenition = newDefenition.Replace("KPP,", "\"KPP\",");
                        newDefenition = newDefenition.Replace("BankRecipient,", "\"BankRecipient\",");
                        newDefenition = newDefenition.Replace("CheckingAccount,", "\"CheckingAccount\",");
                        newDefenition = newDefenition.Replace("CorporateAccount,", "\"CorporateAccount\",");
                        newDefenition = newDefenition.Replace("BIK,", "\"BIK\",");
                        newDefenition = newDefenition.Replace("EstabLeaderPost, ", "\"EstabLeaderPost\",");
                        newDefenition = newDefenition.Replace("OKPO, ", "\"OKPO\",");
                        newDefenition = newDefenition.Replace("EstabTypeId, ", "\"EstabTypeId\",");
                        newDefenition = newDefenition.Replace("LicenseNumber, ", "\"LicenseNumber\",");
                        newDefenition = newDefenition.Replace("	LicenseDate", "	\"LicenseDate\"");
                        newDefenition = newDefenition.Replace("Name,", "\"Name\",");
                        newDefenition = newDefenition.Replace("Guid,", "\"Guid\",");

                        newDefenition = newDefenition.Replace("EstabType", "\"EstabType\"");
                        newDefenition = newDefenition.Replace("\"IsArchive\" = 0", "\"IsArchive\" = false");

                        if (nameBlock.Contains("CREATE OR REPLACE VIEW \"Reference\".\"ServPaymentKind\""))
                        {
                            newDefenition = newDefenition.Replace("SELECT \"Id\"", "SELECT \"ID\"");
                        }

                        newDefenition = newDefenition.Replace("DISTINCT Name", "DISTINCT \"Name\"");
                        newDefenition = newDefenition.Replace("ORDER BY  NLeft", "ORDER BY  \"NLeft\"");
                        newDefenition = newDefenition.Replace(",Icon", ",\"Icon\"");
                        newDefenition = newDefenition.Replace("DISTINCT Name", "DISTINCT \"Name\"");

                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HI.\"SYSTOLIC_BLOOD_PRESSURE_MIN\") AS \"SystolicPressureMinHyper1\"",
                            "HI.\"SYSTOLIC_BLOOD_PRESSURE_MIN\" AS \"SystolicPressureMinHyper1\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HI.\"SYSTOLIC_BLOOD_PRESSURE_MAX\") AS \"SystolicPressureMaxHyper1\"",
                            "HI.\"SYSTOLIC_BLOOD_PRESSURE_MAX\" AS \"SystolicPressureMaxHyper1\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HI.\"DISTOLIC_BLOOD_PRESSURE_MIN\") AS \"DistolicPressureMinHyper1\"",
                            "HI.\"DISTOLIC_BLOOD_PRESSURE_MIN\" AS \"DistolicPressureMinHyper1\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HI.\"DISTOLIC_BLOOD_PRESSURE_MAX\") AS \"DistolicPressureMaxHyper1\"",
                            "HI.\"DISTOLIC_BLOOD_PRESSURE_MAX\" AS \"DistolicPressureMaxHyper1\"");

                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HII.\"SYSTOLIC_BLOOD_PRESSURE_MIN\") AS \"SystolicPressureMinHyper2\"",
                            "HII.\"SYSTOLIC_BLOOD_PRESSURE_MIN\" AS \"SystolicPressureMinHyper2\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HII.\"SYSTOLIC_BLOOD_PRESSURE_MAX\") AS \"SystolicPressureMaxHyper2\"",
                            "HII.\"SYSTOLIC_BLOOD_PRESSURE_MAX\" AS \"SystolicPressureMaxHyper2\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HII.\"DISTOLIC_BLOOD_PRESSURE_MIN\") AS \"DistolicPressureMinHyper2\"",
                            "HII.\"DISTOLIC_BLOOD_PRESSURE_MIN\" AS \"DistolicPressureMinHyper2\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HII.\"DISTOLIC_BLOOD_PRESSURE_MAX\") AS \"DistolicPressureMaxHyper2\"",
                            "HII.\"DISTOLIC_BLOOD_PRESSURE_MAX\" AS \"DistolicPressureMaxHyper2\"");

                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HIII.\"SYSTOLIC_BLOOD_PRESSURE_MIN\") AS \"SystolicPressureMinHyper3\"",
                            "HIII.\"SYSTOLIC_BLOOD_PRESSURE_MIN\" AS \"SystolicPressureMinHyper3\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HIII.\"SYSTOLIC_BLOOD_PRESSURE_MAX\") AS \"SystolicPressureMaxHyper3\"",
                            "HIII.\"SYSTOLIC_BLOOD_PRESSURE_MAX\" AS \"SystolicPressureMaxHyper3\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HIII.\"DISTOLIC_BLOOD_PRESSURE_MIN\") AS \"DistolicPressureMinHyper3\"",
                            "HIII.\"DISTOLIC_BLOOD_PRESSURE_MIN\" AS \"DistolicPressureMinHyper3\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, HIII.\"DISTOLIC_BLOOD_PRESSURE_MAX\") AS \"DistolicPressureMaxHyper3\"",
                            "HIII.\"DISTOLIC_BLOOD_PRESSURE_MAX\" AS \"DistolicPressureMaxHyper3\"");

                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, Hyp.\"SYSTOLIC_BLOOD_PRESSURE_MIN\") AS \"SystolicPressureMinHypo\"",
                            "Hyp.\"SYSTOLIC_BLOOD_PRESSURE_MIN\" AS \"SystolicPressureMinHypo\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, Hyp.\"SYSTOLIC_BLOOD_PRESSURE_MAX\") AS \"SystolicPressureMaxHypo\"",
                            "Hyp.\"SYSTOLIC_BLOOD_PRESSURE_MAX\" AS \"SystolicPressureMaxHypo\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, Hyp.\"DISTOLIC_BLOOD_PRESSURE_MIN\") AS \"DistolicPressureMinHypo\"",
                            "Hyp.\"DISTOLIC_BLOOD_PRESSURE_MIN\" AS \"DistolicPressureMinHypo\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, Hyp.\"DISTOLIC_BLOOD_PRESSURE_MAX\") AS \"DistolicPressureMaxHypo\"",
                            "Hyp.\"DISTOLIC_BLOOD_PRESSURE_MAX\" AS \"DistolicPressureMaxHypo\"");
                        newDefenition = newDefenition.Replace("CONVERT(FLOAT, Pulse.\"HEART_RATE_MIN\") AS \"HeartRateMin\"",
                            "Pulse.\"HEART_RATE_MIN\" AS \"HeartRateMin\"");

                        newDefenition = newDefenition.Replace("\"\"EstabType\"\"", "\"EstabType\"");
                        newDefenition = newDefenition.Replace("\"\"EstabType\"Id\"", "\"EstabTypeId\"");
                        newDefenition = newDefenition.Replace("E.Inn as INN", "E.\"INN\" AS \"INN\"");
                        newDefenition = newDefenition.Replace(".FileCount", ".\"FileCount\"");
                        newDefenition = newDefenition.Replace(".PaidSum", ".\"PaidSum\"");
                        newDefenition = newDefenition.Replace("GETDATE()", "NOW()");
                        newDefenition = newDefenition.Replace("DATEADD(day, 1, D.\"EndDate\") >= NOW()", "D.\"EndDate\" + INTERVAL '1 day' >= NOW()");
                        newDefenition = newDefenition.Replace("DATEADD(day, 1, SD.\"EndDate\") >= NOW()", "SD.\"EndDate\" + INTERVAL '1 day' >= NOW()");
                        newDefenition = newDefenition.Replace(".MkbId", ".\"MkbId\"");
                        newDefenition = newDefenition.Replace(".MkbClass", ".\"MkbClass\"");
                        newDefenition = newDefenition.Replace(".\"IsMainName\" = 1", ".\"IsMainName\" = true");
                        newDefenition = newDefenition.Replace(".MkbId", ".\"MkbId\"");
                        newDefenition = newDefenition.Replace("AS p ON \"PS\"", "AS p ON \"ps\"");
                        newDefenition = newDefenition.Replace("MAX(Id) AS Id", "MAX(\"Id\") AS \"Id\"");
                        newDefenition = newDefenition.Replace("\"es\".\"id\"", "\"es\".\"Id\"");
                        newDefenition = newDefenition.Replace(".SystemNumber", ".\"SystemNumber\"");
                        newDefenition = newDefenition.Replace(".NeedConfirm", ".\"NeedConfirm\"");
                        newDefenition = newDefenition.Replace(".\"Price\"Min", ".\"PriceMin\"");
                        newDefenition = newDefenition.Replace(".\"Price\"Max", ".\"PriceMax\"");
                        newDefenition = newDefenition.Replace(".StartDate", ".\"StartDate\"");
                        newDefenition = newDefenition.Replace(",StartDate", ",\"StartDate\"");
                        newDefenition = newDefenition.Replace(",ParentId", ",\"ParentId\"");
                        newDefenition = newDefenition.Replace(",MKBCode", ",\"MKBCode\"");
                        newDefenition = newDefenition.Replace("MKBName AS", "\"MKBName\" AS");
                        newDefenition = newDefenition.Replace("SELECT  Id", "SELECT  \"Id\"");
                        newDefenition = newDefenition.Replace("Last\"Name\"", "\"LastName\"");
                        newDefenition = newDefenition.Replace("First\"Name\"", "\"FirstName\"");
                        newDefenition = newDefenition.Replace("Middle\"Name\"", "\"MiddleName\"");
                        newDefenition = newDefenition.Replace("LastName +", "\"LastName\" ||");
                        newDefenition = newDefenition.Replace("SELECT OrganisationGuid", "SELECT \"OrganisationGuid\"");
                        newDefenition = newDefenition.Replace(",DiscountId", ",\"DiscountId\"");
                        newDefenition = newDefenition.Replace(",DiscountName", ",\"DiscountName\"");
                        newDefenition = newDefenition.Replace(",BeginDate", ",\"BeginDate\"");
                        newDefenition = newDefenition.Replace(",EndDate", ",\"EndDate\"");
                        newDefenition = newDefenition.Replace(",DiscountPercent", ",\"DiscountPercent\"");
                        newDefenition = newDefenition.Replace(",ServGuid", ",\"ServGuid\"");
                        newDefenition = newDefenition.Replace(",ServName", ",\"ServName\"");
                        newDefenition = newDefenition.Replace(",DiscountTypeId", ",\"DiscountTypeId\"");
                        newDefenition = newDefenition.Replace("NULL AS \"FieldGroupGuid\",", "NULL::uuid AS \"FieldGroupGuid\",");
                        newDefenition = newDefenition.Replace("NULL AS \"FieldGuid\",", "NULL::uuid AS \"FieldGuid\",");
                        newDefenition = newDefenition.Replace("CAST(MAX(CAST(\"Sms\" AS INT)) AS BIT) AS \"Sms\"", "bool_or(s.\"Sms\") AS \"Sms\"");
                        newDefenition = newDefenition.Replace("CAST(MAX(CAST(\"Pochta\" AS INT)) AS BIT) AS \"Pochta\"", "bool_or(s.\"Pochta\") AS \"Pochta\"");
                        newDefenition = newDefenition.Replace("housing,", "\"Housing\",");
                        newDefenition = newDefenition.Replace("Housing,", "\"Housing\",");
                        newDefenition = newDefenition.Replace("datefrom,", "\"DateFrom\",");
                        newDefenition = newDefenition.Replace("AS DateFrom", "AS \"DateFrom\"");
                    }
                    else
                    {
                        MysqlViewReplaceShit(viewReplacer, ref newDefenition, ref nameBlock);
                    }

                    createSqript.AppendLine(nameBlock);
                    createSqript.AppendLine(newDefenition);
                    createSqript.Append($";");
                    createSqript.AppendLine();
                }

                createSqript.AppendLine();
                createSqript.AppendLine();

                getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                startDate = DateTime.Now;
                ConsoleWriteLine("-- Формирование скриптов\\представления: " + getStructureTime + "ms");
            }

            var createMySqlDbScript = createSqript.ToString();
            if (uiShowInConsoleCheckBox.Checked)
            {
                ConsoleWriteLine(createMySqlDbScript);
            }

            if (uiCreateDbCheckBox.Checked)
            {
                label1.Text = "Создание mysqlDB";
                using (var sqlProvider = new PostgreSqlProvider(mysqlCs))// : new MySqlProvider(mysqlCs)) ну вообще можно было в ООП но тяп ляп время деньги
                {
                    sqlProvider.ExecuteNonQuery(createMySqlDbScript);
                }

                getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                startDate = DateTime.Now;
                ConsoleWriteLine("-- Создание MysqlDB: " + getStructureTime + "ms");
            }

            if (uiDataCheckBox.Checked)
            {
                progressBar1.Maximum = structure.Schemas.Sum(x => x.Tables.Count);
                progressBar1.Value = 0;

                var tables = structure.Schemas.SelectMany(x => x.Tables).OrderBy(x => x.FullName).ToList();

                var parts = 5000;
                var insertLogStrBuilder = new StringBuilder();
                using (var sqlProvider = new SqlProvider(mssqlCs))
                {
                    using (var mysqlProvider = new PostgreSqlProvider(mysqlCs))//new MySqlProvider(mysqlCs))
                    {
                        if (isPostgress)
                        {
                        }
                        else
                        {
                            mysqlProvider.ExecuteNonQuery(@"SET FOREIGN_KEY_CHECKS=0;");
                            mysqlProvider.ExecuteNonQuery(@"SET GLOBAL max_allowed_packet=1024*1024*1024;");
                        }

                        foreach (var table in tables)
                        {
                            var d = DateTime.Now;
                            progressBar1.Value++;
                            if (isPostgress)
                            {
                                label1.Text = table.FullName + " -> " + table.GetPostgresName();
                            }
                            else
                            {
                                label1.Text = table.FullName + " -> " + table.GetMysqlName();
                            }

                            var offet = 0;
                            if (isPostgress)
                            {
                                mysqlProvider.ExecuteQuery($"SELECT 1 FROM {table.GetPostgresName()} LIMIT 1");
                            }
                            else
                            {
                                mysqlProvider.ExecuteQuery($"SELECT 1 FROM `{table.GetMysqlName()}` LIMIT 1");
                            }
                            if (mysqlProvider.Rows.Count > 0)
                            {
                                if (uiDataLogCheckBox.Checked)
                                {
                                    var insertTime2 = (DateTime.Now - d).TotalMilliseconds;
                                    insertLogStrBuilder.AppendLine("-- Перенос данных\\" + table.FullName + ": " + insertTime2 + "ms <skip(target not empty)>");
                                }
                                continue;
                            }
                            var empty = false;
                            while (true)
                            {
                                var tablePk = table.Indexes.FirstOrDefault(x => x.IsPrimary);
                                string orderColumn;
                                if (tablePk != null)
                                {
                                    string indexColumns;
                                    if (isPostgress)
                                    {
                                        indexColumns = tablePk.Columns.Select(x => "\"" + x.Column.Name + "\"").ToList().Aggregate(", ");
                                    }
                                    else
                                    {
                                        indexColumns = tablePk.Columns.Select(x => x.Column.Name).ToList().Aggregate(", ");
                                    }
                                    orderColumn = "ORDER BY " + indexColumns;
                                }
                                else
                                {
                                    if (isPostgress)
                                    {
                                        orderColumn = "ORDER BY \"" + ((table.Columns.FirstOrDefault(x => x.Name.ToLower() == "id")
                                        ?? table.Columns.FirstOrDefault(x => x.Name.ToLower() == "guid")
                                        ?? table.Columns.FirstOrDefault()).Name) + "\"";
                                    }
                                    else
                                    {
                                        orderColumn = "ORDER BY " + (table.Columns.FirstOrDefault(x => x.Name.ToLower() == "id")
                                        ?? table.Columns.FirstOrDefault(x => x.Name.ToLower() == "guid")
                                        ?? table.Columns.FirstOrDefault()).Name;
                                    }
                                }
                                sqlProvider.ExecuteQuery($@"SELECT * FROM {table.SqlFullName} 
{orderColumn}
OFFSET     {offet * parts} ROWS
FETCH NEXT {parts} ROWS ONLY;");

                                if (sqlProvider.Rows.Count > 0)
                                {
                                    var strBuilder = new StringBuilder();
                                    string keyStr;
                                    if (isPostgress)
                                    {
                                        keyStr = sqlProvider.Columns.Select(x => "\"" + x.Name + "\"").ToList().Aggregate(",");
                                    }
                                    else
                                    {
                                        keyStr = sqlProvider.Columns.Select(x => "`" + x.Name + "`").ToList().Aggregate(",");
                                    }
                                    for (var j = 0; j < sqlProvider.Rows.Count; j++)
                                    {
                                        string valuesStr;
                                        if (isPostgress)
                                        {
                                            valuesStr = sqlProvider.Rows[j].Fields.Select((x, i) => x.Value.GetPostgresScriptValue(sqlProvider.Columns[i].Type)).ToList().Aggregate(",");
                                        }
                                        else
                                        {
                                            valuesStr = sqlProvider.Rows[j].Fields.Select((x, i) => x.Value.GetMysqlScriptValue(sqlProvider.Columns[i].Type)).ToList().Aggregate(",");
                                        }
                                        if (j == 0)
                                        {
                                            if (isPostgress)
                                            {
                                                strBuilder.Append($"INSERT INTO {table.GetPostgresName()} ({keyStr}) VALUES\r\n" +
                                                    $" ({valuesStr})");
                                            }
                                            else
                                            {
                                                strBuilder.Append($"INSERT INTO `{table.GetMysqlName()}`({keyStr}) VALUES\r\n" +
                                                    $" ({valuesStr})");
                                            }
                                        }
                                        else
                                        {
                                            strBuilder.Append($"\r\n,({valuesStr})");
                                        }
                                    }
                                    var query = strBuilder.ToString();
                                    //ConsoleWriteLine(query);

                                    try
                                    {
                                        mysqlProvider.ExecuteNonQuery(query);
                                    }
                                    catch (Exception ex)
                                    {
                                        ConsoleWriteLine(insertLogStrBuilder.ToString());
                                        ConsoleWriteLine("query: ");
                                        ConsoleWriteLine(query);
                                        throw new Exception("insert into " + table.GetMysqlName() + " error: " + ex.Message, ex);
                                    }
                                }
                                else
                                {
                                    if (offet == 0)
                                    {
                                        empty = true;
                                        //ConsoleWriteLine(table.SqlFullName + " empty");
                                    }
                                }

                                offet++;
                                if (sqlProvider.Rows.Count < parts)
                                {
                                    break;
                                }
                            }

                            if (uiDataLogCheckBox.Checked)
                            {
                                var insertTime = (DateTime.Now - d).TotalMilliseconds;
                                insertLogStrBuilder.AppendLine("-- Перенос данных\\" + table.FullName + ": " + insertTime + "ms" + (empty ? " <empty>" : ""));
                            }
                        }
                    }
                }

                getStructureTime = (DateTime.Now - startDate).TotalMilliseconds;
                startDate = DateTime.Now;
                if (uiDataLogCheckBox.Checked)
                {
                    ConsoleWriteLine(insertLogStrBuilder.ToString());
                }
                ConsoleWriteLine("-- Перенос данных\\Всего: " + getStructureTime + "ms");
            }

            label1.Text = "Конец";
#if !DEBUG
            }
            catch (Exception ex)
            {
                ConsoleWriteLine(ex.ToString());
                if (ShowMessageBox)
                {
                    MessageBox.Show(ex.Message, "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
#endif
        }

        private static void MysqlViewReplaceShit(Dictionary<string, string> viewReplacer, ref string newDefenition, ref string nameBlock)
        {
            nameBlock = nameBlock.Replace("[", "").Replace("]", "").Replace(".", "_");

            newDefenition = newDefenition.Replace("TOP (SELECT COUNT (*) FROM [Reference].[HarmfulFactorGroup])  ", "");
            newDefenition = newDefenition.Replace("TOP (SELECT COUNT (*) FROM [Reference].[HarmfulFactor])", "");

            foreach (var r in viewReplacer)
            {
                newDefenition = newDefenition.Replace(r.Key, r.Value);
            }

            newDefenition = newDefenition.Replace('[', '`').Replace(']', '`');
            newDefenition = newDefenition.Replace(" IsNull(", " IFNULL(");
            newDefenition = newDefenition.Replace(" ISNULL(", " IFNULL(");
            newDefenition = newDefenition.Replace(",ISNULL(", ",IFNULL(");
            newDefenition = newDefenition.Replace("ISNULL(", "IFNULL(");
            newDefenition = newDefenition.Replace("SUM (", "SUM(");

            newDefenition = newDefenition.Replace("AS int)", "AS signed integer)");
            newDefenition = newDefenition.Replace("AS INT)", "AS signed integer)");
            newDefenition = newDefenition.Replace("AS bit)", "AS binary)");
            newDefenition = newDefenition.Replace("AS BIT)", "AS binary)");
            newDefenition = newDefenition.Replace("CAST(MSP.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MSP.Identifikаtor_medprogrаmmy");
            newDefenition = newDefenition.Replace("CAST(MSP.Identifikаtor_tipа_tseny AS uniqueidentifier)", "MSP.Identifikаtor_tipа_tseny");
            newDefenition = newDefenition.Replace("CAST(SP.Identifikаtor_tipа_tseny AS uniqueidentifier)", "SP.Identifikаtor_tipа_tseny");
            newDefenition = newDefenition.Replace("CAST(`Price`.Identifikаtor_tipа_tseny AS uniqueidentifier)", "`Price`.Identifikаtor_tipа_tseny");
            newDefenition = newDefenition.Replace("CAST(MedPat.Identifikаtor_tipа_tseny AS uniqueidentifier)", "MedPat.Identifikаtor_tipа_tseny");
            newDefenition = newDefenition.Replace("CAST(MedPat.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MedPat.Identifikаtor_medprogrаmmy");
            newDefenition = newDefenition.Replace("CAST(Main.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "Main.Identifikаtor_medprogrаmmy");
            newDefenition = newDefenition.Replace("CONVERT(DECIMAL(18,2), Main.Summа_predoplаty)", "Main.Summа_predoplаty");
            newDefenition = newDefenition.Replace("CONVERT(DECIMAL(18,2), Main.Summa_franshizy)", "Main.Summa_franshizy");
            newDefenition = newDefenition.Replace("CAST(Price.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "Price.Identifikаtor_medprogrаmmy");
            newDefenition = newDefenition.Replace("CAST(Price.Identifikаtor_tipа_tseny AS uniqueidentifier)", "Price.Identifikаtor_tipа_tseny");
            newDefenition = newDefenition.Replace("CAST(MPP.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MPP.Identifikаtor_medprogrаmmy");
            newDefenition = newDefenition.Replace("CAST(MIV2.Identifikаtor_medprogrаmmy AS uniqueidentifier)", "MIV2.Identifikаtor_medprogrаmmy");
            newDefenition = newDefenition.Replace("CONVERT(INT, sanc.`Poryadok_prohozhdeniya`)", "sanc.`Poryadok_prohozhdeniya`");
            newDefenition = newDefenition.Replace("CAST(JB.IDENTIFIKATOR_ORG AS uniqueidentifier)", "JB.IDENTIFIKATOR_ORG");


            newDefenition = newDefenition.Replace("IIf(", "IF(");
            newDefenition = newDefenition.Replace("IIF(", "IF(");
            newDefenition = newDefenition.Replace("WHEN LEN(", "WHEN LENGTH(");
            newDefenition = newDefenition.Replace("DATEADD(day, 1, SD.EndDate)", "DATE_ADD(SD.EndDate, interval 1 day)");
            newDefenition = newDefenition.Replace("DATEADD(day, 1, D.EndDate)", "DATE_ADD(D.EndDate, interval 1 day)");
            newDefenition = newDefenition.Replace("DATEDIFF(YY", "TIMESTAMPDIFF(year");

            newDefenition = newDefenition.Replace("WITH `parent` AS", "WITH recursive `parent` AS");
            newDefenition = newDefenition.Replace("CONVERT(bit, main.Pervichnaya_registratsiya_otpechatkov_paljtsev)"
                , "CONVERT(main.Pervichnaya_registratsiya_otpechatkov_paljtsev, binary)");
            newDefenition = newDefenition.Replace("CONVERT(bit, main.Zakreplenie_identificacionnoy_kartochki)"
                , "CONVERT(main.Zakreplenie_identificacionnoy_kartochki, binary)");
            newDefenition = newDefenition.Replace("CONVERT(int,NS.`Identifikator_tipa_uvedomleniya`)"
                , "CONVERT(NS.`Identifikator_tipa_uvedomleniya`, signed integer)");

            newDefenition = newDefenition.Replace(",IFNULL(CONVERT(uniqueidentifier, `cnt`.`Identifikаtor_dokumentа_dlya_nаprаvleniy`),CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)) AS DocumentGuid"
                , ",`cnt`.`Identifikаtor_dokumentа_dlya_nаprаvleniy` AS DocumentGuid");
            newDefenition = newDefenition.Replace(",IFNULL(E.PaymentMedprogGuid, cast(cast(0 as binary) as uniqueidentifier)) AS PaymentMedprogGuid"
                , ",E.PaymentMedprogGuid AS PaymentMedprogGuid");

            newDefenition = newDefenition.Replace("`Interval_v_minutah` AS Interval", "`Interval_v_minutah` AS `Interval`");

            newDefenition = newDefenition.Replace("CONVERT(INT, st.Poryadok_prohozhdeniya)", "CONVERT(st.Poryadok_prohozhdeniya, signed integer)");
            newDefenition = newDefenition.Replace("CONVERT(INT, sbp.Poryadok_prohozhdeniya)", "CONVERT(sbp.Poryadok_prohozhdeniya, signed integer)");
            newDefenition = newDefenition.Replace("CONVERT(INT, satt.Poryadok_prohozhdeniya)", "CONVERT(satt.Poryadok_prohozhdeniya, signed integer)");
            newDefenition = newDefenition.Replace("CONVERT(INT, sanc.Poryadok_prohozhdeniya)", "CONVERT(sanc.Poryadok_prohozhdeniya, signed integer)");
            newDefenition = newDefenition.Replace("CONVERT(INT, sa.Poryadok_prohozhdeniya)", "CONVERT(sa.Poryadok_prohozhdeniya, signed integer)");

            newDefenition = newDefenition.Replace("CONVERT(varchar, sa.Alkogolj_Max)", " sa.Alkogolj_Max");
            newDefenition = newDefenition.Replace("CONVERT(varchar, sa.Alkogolj_Min)", " sa.Alkogolj_Min");
            newDefenition = newDefenition.Replace("CONVERT(varchar, sa.Alkogolj_AbsMax)", " sa.Alkogolj_AbsMax");
            newDefenition = newDefenition.Replace("CONVERT(varchar, sa.Alkogolj_AbsMin)", " sa.Alkogolj_AbsMin");
            newDefenition = newDefenition.Replace("CONVERT(varchar, PRV.Alkogolj_Max)", " PRV.Alkogolj_Max");
            newDefenition = newDefenition.Replace("CONVERT(varchar, PRV.Alkogolj_Min)", " PRV.Alkogolj_Min");


            newDefenition = newDefenition.Replace("ORDER BY Disc.`Value` DESC, Disc.`Percent` DESC"
                , "ORDER BY Disc.`Value` DESC, Disc.`Percent` DESC LIMIT 1");
            newDefenition = newDefenition.Replace("SELECT TOP 1 Id", "SELECT Id");
            newDefenition = newDefenition.Replace("SELECT TOP (10000)", "SELECT");

            newDefenition = newDefenition.Replace("GETDATE()", "NOW()");
            newDefenition = newDefenition.Replace("--придумать", "-- придумать");
            newDefenition = newDefenition.Replace("--OtherPriceTypeGuid", "-- OtherPriceTypeGuid");
            newDefenition = newDefenition.Replace("--MedProgPriceTypeName", "-- MedProgPriceTypeName");
            newDefenition = newDefenition.Replace("--ДМС", "-- ДМС");
            newDefenition = newDefenition.Replace("CAST(M.Guid AS NVARCHAR(max)) AS Id", "M.Guid AS Id");
            newDefenition = newDefenition.Replace("CAST(P.Id AS NVARCHAR(max)) AS Id", "P.Id AS Id");


            if (nameBlock.Contains("CREATE OR REPLACE VIEW Record_DirectionPatternDocuments"))
            {
                newDefenition = newDefenition.Replace(", IFNULL(", ", DFD.Identifikаtor_dokumentа_dlya_nаprаvleniy AS DocumentGuid");
                newDefenition = newDefenition.Replace("CAST(DFD.Identifikаtor_dokumentа_dlya_nаprаvleniy AS uniqueidentifier),", "");
                newDefenition = newDefenition.Replace("CAST(CAST(0 as binary) as uniqueidentifier)", "");
                newDefenition = newDefenition.Replace(")  AS DocumentGuid", "");
                newDefenition = newDefenition.Replace("END AS bit", "END AS binary");
            }
            if (nameBlock.Contains("CREATE OR REPLACE VIEW RiskFactor_RiskFactorSettings"))
            {
                newDefenition = newDefenition.Replace("CONVERT(FLOAT, ", "");
                newDefenition = newDefenition.Replace("BLOOD_PRESSURE_MIN) AS", "BLOOD_PRESSURE_MIN AS");
                newDefenition = newDefenition.Replace("BLOOD_PRESSURE_MAX) AS", "BLOOD_PRESSURE_MAX AS");
                newDefenition = newDefenition.Replace("HEART_RATE_MIN) AS", "HEART_RATE_MIN AS");
            }
            if (nameBlock.Contains("CREATE OR REPLACE VIEW Patient_PatientFullInfo"))
            {
                newDefenition = newDefenition.Replace("CASE WHEN DATEPART(DY, MD.DATA_ROZHDENIYA) <= DATEPART(DY, NOW())",
                    "YEAR(CURDATE()) - YEAR(MD.DATA_ROZHDENIYA) - IF(STR_TO_DATE(CONCAT(YEAR(CURDATE()), '-', MONTH(MD.DATA_ROZHDENIYA), '-', DAY(MD.DATA_ROZHDENIYA)), '%Y-%c-%e') > CURDATE(), 1, 0) AS Years,");
                newDefenition = newDefenition.Replace("THEN TIMESTAMPDIFF(year, MD.DATA_ROZHDENIYA, NOW())", "");
                newDefenition = newDefenition.Replace("WHEN TIMESTAMPDIFF(year, MD.DATA_ROZHDENIYA, DATEADD(yy, - 1, NOW())) = - 1 ", "");
                newDefenition = newDefenition.Replace("THEN 0 ELSE TIMESTAMPDIFF(year, MD.DATA_ROZHDENIYA, DATEADD(yy, - 1, NOW())) END AS Years, ", "");
                newDefenition = newDefenition.Replace("MD.FAMILIYA + CASE WHEN LENGTH(MD.IMYA) > 0 THEN ' ' + MD.IMYA ELSE '' END + CASE WHEN LENGTH(MD.OTCHESTVO) > 0 THEN ' ' + MD.OTCHESTVO ELSE '' END AS FullName,",
                    "CONCAT(MD.FAMILIYA, CASE WHEN LENGTH(MD.IMYA) > 0 THEN CONCAT(' ', MD.IMYA) ELSE '' END, CASE WHEN LENGTH(MD.OTCHESTVO) > 0 THEN CONCAT(' ', MD.OTCHESTVO) ELSE '' END)  AS FullName, ");
                newDefenition = newDefenition.Replace("MD.FAMILIYA +", "CONCAT(MD.FAMILIYA,");
                newDefenition = newDefenition.Replace("END +", "END ,");
                newDefenition = newDefenition.Replace("SUBSTRING(MD.IMYA, 1, 1) + '.'", "CONCAT(SUBSTRING(MD.IMYA, 1, 1), '.')");
                newDefenition = newDefenition.Replace("SUBSTRING(MD.OTCHESTVO, 1, 1) + '.'", "CONCAT(SUBSTRING(MD.OTCHESTVO, 1, 1), '.')");
                newDefenition = newDefenition.Replace("AS ShortName, ", ")AS ShortName, ");

            }
            if (nameBlock.Contains("CREATE OR REPLACE VIEW Reference_ViewWorkplace"))
            {
                newDefenition = newDefenition.Replace("CONCAT(Name, STUFF", "Name +");
                newDefenition = newDefenition.Replace("((SELECT ', ' + P.Code", "(SELECT GROUP_CONCAT(' ', P.Code)");
                newDefenition = newDefenition.Replace("WHEN TIMESTAMPDIFF(year, MD.DATA_ROZHDENIYA, DATEADD(yy, - 1, NOW())) = - 1 ", "");
                newDefenition = newDefenition.Replace("WHERE        P.PostId = ID FOR XML PATH('')), 1, 1, '')) AS Name,",
                    "WHERE        P.PostId = ID) AS Name,");
            }
            if (nameBlock.Contains("CREATE OR REPLACE VIEW Reference_ViewDolzhnostj"))
            {
                newDefenition = newDefenition.Replace("CONCAT(", "");
                newDefenition = newDefenition.Replace("		Name,", "		Name +");
                newDefenition = newDefenition.Replace("STUFF(", "");
                newDefenition = newDefenition.Replace("', '+P.Code", "GROUP_CONCAT(' ', P.Code)");
                newDefenition = newDefenition.Replace("FOR XML PATH('')", "");
                newDefenition = newDefenition.Replace("), 1, 1, '')", "");
            }
            if (nameBlock.Contains("CREATE OR REPLACE VIEW Reference_ViewProfessiya"))
            {
                newDefenition = newDefenition.Replace("CONCAT(", "");
                newDefenition = newDefenition.Replace("		Name,", "		Name +");
                newDefenition = newDefenition.Replace("STUFF(", "");
                newDefenition = newDefenition.Replace("', '+P.Code", "GROUP_CONCAT(' ', P.Code)");
                newDefenition = newDefenition.Replace("FOR XML PATH('')", "");
                newDefenition = newDefenition.Replace("), 1, 1, '')", "");
            }

        }

        private static void GetTableScriptMysql(StringBuilder createSqript, Table table)
        {
            createSqript.AppendLine($"CREATE TABLE IF NOT EXISTS `{table.GetMysqlName()}` (");
            foreach (var column in table.Columns)
            {
                createSqript.AppendLine($"    `{column.Name}` {column.GetMysqlType()} " +
                    $"{(column.IsNullable ? "NULL" : "NOT NULL")}" +
                    $"{(column.IsIdentity ? " AUTO_INCREMENT" : "")}" +
                    $"{(string.IsNullOrEmpty(column.Comment) ? "" : " COMMENT '" + column.Comment + "'")},");
            }
            var pk = table.Indexes.FirstOrDefault(x => x.IsPrimary);
            if (pk != null)
            {
                var indexColumns = pk.Columns.Select(x => "`" + x.Column.Name + "`").ToList().Aggregate(",");
                createSqript.AppendLine($"PRIMARY KEY ({indexColumns}),");
            }
            else
            {
                var identity = table.Columns.FirstOrDefault(x => x.IsIdentity);
                if (identity != null)
                {
                    createSqript.AppendLine($"PRIMARY KEY (`{identity.Name}`),");
                }
            }
            var uqs = table.Indexes.Where(x => x.IsUnique && x.IsPrimary == false);
            foreach (var uq in uqs)
            {
                var indexColumns = uq.Columns.Select(x => "`" + x.Column.Name + "` " + (x.IsDescending ? "DESC" : "ASC")).ToList().Aggregate(",");
                createSqript.AppendLine($"UNIQUE INDEX `{uq.Name}` ({indexColumns}) VISIBLE,");
            }

            createSqript.Remove(createSqript.Length - 3, 3);
            createSqript.Append($")");
            if (!string.IsNullOrEmpty(table.Comment))
            {
                createSqript.AppendLine();
                createSqript.Append($"COMMENT '{table.Comment}'");
            }
            createSqript.Append($";");
        }

        private void ConsoleWriteLine(string v)
        {
            //textBox1.Text = textBox1.Text.Insert(0, v + Environment.NewLine);
            textBox1.Text += v + Environment.NewLine;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = string.Empty;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            var mssql = connectionMssqlTextBox.Text;
            var postgre = connectionMysqlTextBox.Text;

            bool structure = true;
            bool table = false;
            bool view = false;
            bool fk = false;
            bool data = false;
            bool dataLog = true;
            bool console = true;
            bool run = true;

            var form = this;
            form.ShowMessageBox = false;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            structure = false;
            table = true;
            data = true;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            table = false;
            data = false;
            view = true;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            view = false;
            fk = true;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            textBox1.Text += "\n\n Перенос базы данных выполнен";
        }

        private void uiMaxvalToFile_Click(object sender, EventArgs e)
        {
            var mtest = new StringBuilder();
            var mssqlCs = connectionMssqlTextBox.Text;
            var structure = new DatabaseStructure();

            label1.Text = "Получение структуры mssqlDB";

            using (var sqlProvider = new SqlProvider(mssqlCs))
            {
                sqlProvider.ExecuteQuery("SELECT 1 FROM sys.schemas AS s");

                var tableQuery = @"SELECT 
s.name AS SchemaName,
t.name AS TableName,
tep.value AS TableComment,
c.name AS ColumnName,
ep.value as ColumnComent,
object_definition(c.default_object_id) AS ColumnDefinition,
c.system_type_id as ColumnTypeId,
c.max_length as ColumnMaxLength,
c.precision as ColumnPrecision,
c.scale as ColumnScale,
c.is_nullable As ColumnIsNullable,
c.is_identity as ColumnIsIdentity,
c.collation_name AS CollationName
FROM sys.schemas AS s
    INNER JOIN sys.tables AS t ON t.schema_id = s.schema_id
    INNER JOIN sys.columns c ON t.OBJECT_ID = c.OBJECT_ID
    LEFT JOIN sys.extended_properties ep on t.object_id = ep.major_id
                                         and c.column_id = ep.minor_id
                                         and ep.name = 'MS_Description'
    LEFT JOIN sys.extended_properties tep on t.object_id = tep.major_id
                                         and tep.minor_id = 0
                                         and tep.name = 'MS_Description'";
                sqlProvider.ExecuteQuery(tableQuery);

                foreach (var row in sqlProvider.Rows)
                {
                    structure
                        .AppendSchema(row.Field<string>("SchemaName"))
                        .AppendTable(row.Field<string>("TableName"), row.Field<string>("TableComment"))
                        .AppendColumn(row.Field<string>("ColumnName"), row.Field<string>("ColumnComent"), row.Field<string>("ColumnDefinition"))
                        .SetType
                            (
                                row.Field<int>("ColumnTypeId"),
                                row.Field<int>("ColumnMaxLength"),
                                row.Field<int>("ColumnPrecision"),
                                row.Field<int>("ColumnScale"),
                                row.Field<bool>("ColumnIsNullable"),
                                row.Field<bool>("ColumnIsIdentity"),
                                row.Field<string>("CollationName")
                            );
                }

                var viewQuery = @"select schema_name(v.schema_id) AS SchemaName,
       v.name as ViewName,
       v.create_date as created,
       v.modify_date as last_modified,
       m.definition AS ViewDefinition
from sys.views v
join sys.sql_modules m 
     on m.object_id = v.object_id";

                sqlProvider.ExecuteQuery(viewQuery);

                foreach (var row in sqlProvider.Rows)
                {
                    structure
                        .AppendSchema(row.Field<string>("SchemaName"))
                        .AppendView(row.Field<string>("ViewName"), row.Field<string>("ViewDefinition"));
                }


                var viewDependencedQuery = @"SELECT 
schema_name(v.schema_id) AS SchemaName,
v.name AS ViewName, 
d.referenced_schema_name AS ReferencedSchemaName, 
d.referenced_entity_name AS ReferencedViewName
FROM sys.views AS V
	INNER JOIN sys.sql_expression_dependencies AS D ON D.referencing_id = V.object_id
	INNER JOIN sys.views AS RV ON RV.object_id = D.referenced_id";

                sqlProvider.ExecuteQuery(viewDependencedQuery);

                foreach (var row in sqlProvider.Rows)
                {
                    var view = structure
                        .GetSchema(row.Field<string>("SchemaName"))
                        .GetView(row.Field<string>("ViewName"));

                    var refView = structure
                        .GetSchema(row.Field<string>("ReferencedSchemaName"))
                        .GetView(row.Field<string>("ReferencedViewName"));

                    view.AppendReferencedView(refView);
                }

                var fkQuery = @"select o.name AS Name,
	s.name as SchemaName,
    t.name as TableName, 
	c.name as ColumnName,
	rs.name as ReferenceSchemaName,
	r.name AS ReferenceTableName,
	rc.name AS ReferenceColumnName,
	f.delete_referential_action as DeleteAction,
	f.update_referential_action As UpdateAction,
	f.is_disabled as IsDisabled
from sys.foreign_key_columns as fk
inner join sys.foreign_keys as f on f.object_id = fk.constraint_object_id
inner join sys.objects as o on o.object_id = fk.constraint_object_id
inner join sys.tables as t on fk.parent_object_id = t.object_id
inner join sys.columns as c on fk.parent_object_id = c.object_id and fk.parent_column_id = c.column_id
inner join sys.schemas AS s on s.schema_id = t.schema_id
inner join sys.tables as r on fk.referenced_object_id = r.object_id
inner join sys.columns as rc on fk.referenced_object_id = rc.object_id and fk.referenced_column_id = rc.column_id
inner join sys.schemas AS rs on rs.schema_id = r.schema_id";

                sqlProvider.ExecuteQuery(fkQuery);

                foreach (var row in sqlProvider.Rows)
                {
                    var column = structure
                        .GetSchema(row.Field<string>("SchemaName"))
                        .GetTable(row.Field<string>("TableName"))
                        .GetColumn(row.Field<string>("ColumnName"));

                    var referencedColumn = structure
                        .GetSchema(row.Field<string>("ReferenceSchemaName"))
                        .GetTable(row.Field<string>("ReferenceTableName"))
                        .GetColumn(row.Field<string>("ReferenceColumnName"));

                    column.AppendForeignKey
                        (
                            referencedColumn,
                            row.Field<string>("Name"),
                            row.Field<int>("DeleteAction"),
                            row.Field<int>("UpdateAction"),
                            row.Field<bool>("IsDisabled")
                        );
                }

                var pkQuery = @"select i.[name] as IndexName,
	i.type as IndexType,
    i.is_primary_key AS IsPrimary,
    i.is_unique as IsUnique,
    substring(column_names, 1, len(column_names)-1) as [IndexColumns],
    case when i.[type] = 1 then 'Clustered index'
        when i.[type] = 2 then 'Nonclustered unique index'
        when i.[type] = 3 then 'XML index'
        when i.[type] = 4 then 'Spatial index'
        when i.[type] = 5 then 'Clustered columnstore index'
        when i.[type] = 6 then 'Nonclustered columnstore index'
        when i.[type] = 7 then 'Nonclustered hash index'
        end as index_type,
    case when i.is_unique = 1 then 'Unique'
        else 'Not unique' end as [unique],
	schema_name(t.schema_id) as SchemaName,
    t.[name] as TableName, 
    case when t.[type] = 'U' then 'Table'
        when t.[type] = 'V' then 'View'
        end as [object_type]
from sys.objects t
    inner join sys.indexes i
        on t.object_id = i.object_id
    cross apply (select col.[name] + ' ' + CAST(ic.is_included_column as nvarchar) + ' ' +  CAST(ic.is_descending_key as nvarchar)+ ','
                    from sys.index_columns ic
                        inner join sys.columns col
                            on ic.object_id = col.object_id
                            and ic.column_id = col.column_id
                    where ic.object_id = t.object_id
                        and ic.index_id = i.index_id
                            order by key_ordinal
                            for xml path ('') ) D (column_names)
where t.is_ms_shipped <> 1
and index_id > 0
order by i.[name]";

                sqlProvider.ExecuteQuery(pkQuery);

                foreach (var row in sqlProvider.Rows)
                {
                    row.Field<string>("IndexName");
                    var type = row.Field<string>("IndexType");
                    var columns = row.Field<string>("IndexColumns");
                    var columnsList = columns.Split(',');


                    var index = structure
                        .GetSchema(row.Field<string>("SchemaName"))
                        .GetTable(row.Field<string>("TableName"))
                        .AppendIndex(row.Field<string>("IndexName"),
                        row.Field<int>("IndexType"),
                        row.Field<bool>("IsPrimary"),
                        row.Field<bool>("IsUnique"));
                    foreach (var column in columnsList)
                    {
                        var name = column.Substring(0, column.Length - 4);
                        var isIncluded = column.Substring(column.Length - 3, 1) == "1";
                        var isDescending = column.Substring(column.Length - 1, 1) == "1";
                        var tableColumn = index.Table.Columns.First(x => x.Name == name);
                        index.AppendColumn(tableColumn, isIncluded, isDescending);
                    }
                }
            }
      
            foreach (var schema in structure.Schemas)
            {
                foreach (var table in schema.Tables)
                {
                    foreach (var colId in table.Columns.Where(x => x.IsIdentity))
                    {
                        var stringSequenceName = table.Schema.Name.GetPostgresName() + "." + (table.Name + "_seq").GetPostgresName();
                        mtest.Append($"SELECT setval('{stringSequenceName}', (SELECT max(\"{colId.Name}\") FROM \"{table.Schema}\".\"{table.Name}\"));");
                        mtest.Append(Environment.NewLine);
                    }
                }
            }

            var str = mtest.ToString();
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keys.txt");
            File.WriteAllText(path, str);
            MessageBox.Show("Успех! Файл keys.txt лежит в корне приложения!");
        }
    }
}
