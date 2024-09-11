using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace MssqlToMysqlMigrator.Helpers
{
    public class PostgreSqlProvider : IDisposable
    {
        private DbCommand _currentCommand;
        private ICollection<DbCommand> _commands;
        private DbTransaction _transaction;
        private DbConnection _connection;

        public string Database => _connection.Database;

        public PostgreSqlProvider(string connectionString)
        {
            _connection = new NpgsqlConnection(connectionString);
            SqlQueryConstructorInit();
        }

        private void SqlQueryConstructorInit()
        {
            _currentCommand = _connection.CreateCommand();

            if (_currentCommand.CommandTimeout < 600)
            {
                _currentCommand.CommandTimeout = 600;
            }

        }
        public void SetParameter(string parameterName, object value)
        {
            object sqlValue = (value == null || String.IsNullOrEmpty(value.ToString())) ? DBNull.Value : value;
            var param = _currentCommand.Parameters.OfType<SqlParameter>().FirstOrDefault(p => p.ParameterName == parameterName);
            if (param != null)
            {
                _currentCommand.Parameters.Remove(param);
            }
            param = new SqlParameter(parameterName, sqlValue);
            _currentCommand.Parameters.Add(param);
        }

        public void SetParameter(string parameterName, object value, SqlDbType sqlDbType)
        {
            object sqlValue = (value == null || String.IsNullOrEmpty(value.ToString())) ? DBNull.Value : value;
            var param = _currentCommand.Parameters.OfType<SqlParameter>().FirstOrDefault(p => p.ParameterName == parameterName);
            if (param != null)
            {
                _currentCommand.Parameters.Remove(param);
            }
            if (sqlDbType == SqlDbType.UniqueIdentifier)
            {
                sqlDbType = SqlDbType.NVarChar;
            }
            param = new SqlParameter(parameterName, sqlDbType);
            param.Value = sqlValue;
            _currentCommand.Parameters.Add(param);
        }

        private static IEnumerable<object[]> Read(DbDataReader reader)
        {
            while (reader.Read())
            {
                var values = new List<object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    values.Add(reader.GetValue(i));
                }
                yield return values.ToArray();
            }
        }

        public void ExecuteQuery(string query)
        {
            if (_connection.State == ConnectionState.Closed)
            {
               _connection.Open();
            }
            _currentCommand.CommandText = query;
            DbDataReader reader = _currentCommand.ExecuteReader();
            GenerateColumsFromReader(reader);
            GenerateRowsFromReader(reader);
            reader.Close();
            _connection.Close();
        }

        public int ExecuteNonQuery(string query)
        {
            if (_connection.State == ConnectionState.Closed)
            {
                    _connection.Open();
            }
            _currentCommand.CommandText = query;
            var numberOfRecords = _currentCommand.ExecuteNonQuery();
            _connection.Close();
            return numberOfRecords;
        }

        private readonly List<IQueryResultRow> _rows = new List<IQueryResultRow>();
        private readonly List<IQueryResultColumn> _columns = new List<IQueryResultColumn>();

        public List<IQueryResultRow> Rows
        {
            get { return _rows; }
        }

        public List<IQueryResultColumn> Columns
        {
            get { return _columns; }
        }

        public bool HasRows
        {
            get { return _rows.Any(); }
        }

        public DbCommand Command
        {
            get { return _currentCommand; }
        }

        private void GenerateColumsFromReader(DbDataReader reader)
        {
            _columns.Clear();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                var columnType = reader.GetFieldType(i);
                var column = new QueryColumn(columnName, columnType);
                _columns.Add(column);
            }
        }

        private void GenerateRowsFromReader(DbDataReader reader)
        {
            _rows.Clear();
            while (reader.Read())
            {
                var row = new QueryRow();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.AddField(reader.GetName(i), reader[i]);
                }
                _rows.Add(row);
            }
        }

        public interface IQueryResultRow
        {
            T Field<T>(string fieldName);
            T FieldOrDefault<T>(string fieldName);
            bool IsNull(string fieldName);
            Dictionary<string, object> Fields { get; }
        }

        public interface IQueryResultColumn
        {
            string Name { get; }
            Type Type { get; }
        }

        public class QueryRow : IQueryResultRow
        {
            private readonly Dictionary<string, object> _fields = new Dictionary<string, object>();
            public Dictionary<string, object> Fields => _fields;

            internal void AddField(string fieldName, object value)
            {
                var fn = fieldName.ToLower();
                if (_fields.ContainsKey(fn))
                {
                    throw new Exception("Поле " + fn + " уже существует в запросе");
                }
                _fields.Add(fn, value);
            }

            public T FieldOrDefault<T>(string fieldName)
            {
                try
                {
                    return Field<T>(fieldName);
                }
                catch
                {
                    return default(T);
                }
            }

            public dynamic Field(string fieldName)
            {
                var value = Field<object>(fieldName);
                return DBNull.Value.Equals(value) ? null : value;
            }

            public T Field<T>(string fieldName)
            {
                try
                {
                    object value;
                    if (_fields.Keys.Contains(fieldName.ToLower()))
                    {
                        value = _fields[fieldName.ToLower()];
                    }
                    else
                    {
                        value = _fields["[" + fieldName.ToLower() + "]"];
                    }

                    if (IsNullableType(typeof(T)))
                    {
                        return GetNullableValue<T>(value);
                    }

                    return GetNotNullableValue<T>(value);
                }
                catch (Exception e)
                {
                    e.Data.Add("fieldName", fieldName);
                    throw;
                }
            }

            private T GetNullableValue<T>(object value)
            {
                try
                {
                    Type nullableType = Nullable.GetUnderlyingType(typeof(T));
                    if (value is Guid?)
                    {
                        return DBNull.Value.Equals(value) ? default(T) : (T)Convert.ChangeType(value, typeof(Guid));
                    }

                    if (typeof(T) == typeof(Single?))
                    {
                        // todo .Replace('.', ',') так нельзя, но это уже не первое место, нужно это учитывать, так как могут быть косяки с форматами в других культурах :c
                        return DBNull.Value.Equals(value) ? default(T) : (T)Convert.ChangeType(value.ToString().Replace('.', ','), typeof(Single));
                    }

                    return DBNull.Value.Equals(value) ? default(T) : (T)Convert.ChangeType(value, nullableType);

                }
                catch (Exception e)
                {
                    e.Data.Add("type", typeof(T));
                    e.Data.Add("value", value);
                    throw;
                }
            }

            private T GetNotNullableValue<T>(object value)
            {
                try
                {
                    if (value is Guid && typeof(T) == typeof(Guid))
                    {
                        return (T)Convert.ChangeType(value, typeof(Guid));
                    }

                    if (value is Guid)
                    {
                        var x = Convert.ChangeType(value, typeof(Guid));
                        value = x.ToString();
                    }

                    if (value is TimeSpan)
                    {
                        var x = Convert.ChangeType(value, typeof(TimeSpan));
                        value = x.ToString();
                    }

                    return (T)Convert.ChangeType(value, typeof(T));

                }
                catch (Exception e)
                {
                    e.Data.Add("type", typeof(T));
                    e.Data.Add("value", value);
                    throw;
                }
            }

            private bool IsNullableType(Type type)
            {
                return Nullable.GetUnderlyingType(type) != null;
            }

            public bool IsNull(string fieldName)
            {
                return DBNull.Value.Equals(_fields[fieldName]);
            }
        }

        public class QueryColumn : IQueryResultColumn
        {
            internal QueryColumn(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; private set; }

            public Type Type { get; private set; }
        }

        public decimal ExecuteInt32WithNullAsZero(string sql)
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
            _currentCommand.CommandText = sql;
            object commandResult = _currentCommand.ExecuteScalar();
            if (commandResult is int?)
            {
                _connection.Close();
                return Convert.ToInt32(commandResult);
            }
            if (commandResult == DBNull.Value
                || commandResult == null)
            {
                _connection.Close();
                return 0;
            }
            throw new Exception("ResultIsNotInt32Exception");
        }


        public void AddCommand(string commandText)
        {
            var command = _connection.CreateCommand();
            command.CommandText = commandText;
            if (_commands == null)
            {
                _commands = new List<DbCommand>();
            }
            _commands.Add(command);
            _currentCommand = command;
        }

        public void UpdateCurrentCommand(string commandText)
        {
            _currentCommand.CommandText = commandText;
        }

        public void Commit()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }

            using (DbTransaction transaction = _connection.BeginTransaction())
            {
                try
                {
                    foreach (var command in _commands)
                    {
                        command.Transaction = transaction;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);// + " in command" + command.CommandText);
                }
            }
            _connection.Close();
            _commands.Clear();
        }

        public void Dispose()
        {
            _connection.Close();
        }
    }
}
