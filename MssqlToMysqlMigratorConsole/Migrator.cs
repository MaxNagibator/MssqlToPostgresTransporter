using MssqlToMysqlMigrator;
using System;

namespace MssqlToMysqlMigratorConsole
{
    public static class Migrator
    {
        public static void MigrateFromMsToPg(Action<string> ConsoleWriteLine, string mssql, string postgre)
        {
            bool structure = true;
            bool table = false;
            bool view = false;
            bool fk = false;
            bool data = false;
            bool dataLog = true;
            bool console = true;
            bool run = true;

            ConsoleWriteLine("MssqlToMysqlMigratorConsole structure");
            var form = new MainForm();
            form.ShowMessageBox = false;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            ConsoleWriteLine("MssqlToMysqlMigratorConsole table & data");
            structure = false;
            table = true;
            data = true;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            ConsoleWriteLine("MssqlToMysqlMigratorConsole view");
            table = false;
            data = false;
            view = true;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            ////ConsoleWriteLine("MssqlToMysqlMigratorConsole data");
            ////view = false;
            ////data = true;
            ////form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            ////form.Run2();

            ConsoleWriteLine("MssqlToMysqlMigratorConsole fk");
            view = false;
            fk = true;
            form.SetParameters(mssql, postgre, structure, table, view, fk, data, dataLog, console, run);
            form.Run2();

            ConsoleWriteLine(form.GetLog());
        }
    }
}
