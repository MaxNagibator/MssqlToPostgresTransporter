using MssqlToMysqlMigrator;
using System;

namespace MssqlToMysqlMigratorConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //args = new string[] {
            //    "data source=localhost;initial catalog=postgres;persist security info=True;user id=ums;password=umsforadmin;App=MssqlToMysqlMigrator;" ,
            //    "Host=localhost;Username=postgres;Password=admin;Database=testo"
            // };
            string mssql = args[0];
            string postgre = args[1];

            Migrator.MigrateFromMsToPg(Console.WriteLine, mssql, postgre);
        }
    }
}
