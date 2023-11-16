using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MySql.Data.MySqlClient;

namespace DevastorTradeSystem
{
    public class DevastorDatabaseController
    {
        private MySqlConnection DevastorMySQLConnection;
        private string DevastorDatabaseConnectionString = "";
        public DevastorDatabaseController(string connection_string)
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorDatabaseController");
            DevastorDatabaseConnectionString = connection_string;
            DevastorMySQLConnection = new MySqlConnection(DevastorDatabaseConnectionString);
            DevastorMySQLConnection.Open();
            Console.WriteLine($"MySQL version : {DevastorMySQLConnection.ServerVersion}");
        }
        // функция реалицзаии MYSQL запроса
        public void DevastorExecuteMySQLQuery(string query)
        {
            //Console.WriteLine("FUNCTION ==> " + "DevastorExecuteMySQLQuery");
            try
            {
                var cmd = new MySqlCommand();
                cmd.Connection = DevastorMySQLConnection;
                MySqlCommand command = new MySqlCommand(query, DevastorMySQLConnection);
                command.ExecuteNonQuery();
            }
            catch (Exception e) { Console.WriteLine("ERROR! Can't execute query! Reason: " + e.Message); };
        }
        // функция чтения  MYSQL запроса на чтение данных
        public List<List<string>> DevastorExecuteMySQLReader(string query, DateTime start, DateTime end, List<string> values = null)
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorExecuteMySQLReader");
            Console.WriteLine("Reading data from MySQL table...");
            Console.WriteLine("start: " + start);
            Console.WriteLine("end: " + end);
            List <List<string>> table_data = new List<List<string>>();
            MySqlDataReader DevastorMySQLReader = null;
            try
            {
                MySqlCommand DevastorMySQLCommand = new MySqlCommand(query, DevastorMySQLConnection);
                DevastorMySQLCommand.CommandTimeout = 360;
                DevastorMySQLReader = DevastorMySQLCommand.ExecuteReader();
                while (DevastorMySQLReader.Read())
                {
                    DateTime from_table = DevastorLongToDateTime(Convert.ToInt64(DevastorMySQLReader.GetValue(0)));
                    if (from_table >= start
                        && from_table <= end)
                    {
                        List<string> temp_list = new List<string>();
                        string output = "DATA: ";
                        for (int i = 0; i < values.Count; i++)
                        {
                            string value = DevastorMySQLReader.GetValue(i).ToString();
                            temp_list.Add(value);
                            output += value + " ";
                        }
                        table_data.Add(temp_list);
                        //Console.WriteLine(output);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine("ERROR! Can't execute reader! Reason: " + e.Message); };
            DevastorMySQLReader.Close();
            Console.WriteLine("Data reading finished!");
            //Console.WriteLine("query: " + query);
            //Console.WriteLine("result: " + table_data);
            return table_data;
        }
        // функция создания базы данных
        public void DevastorCreateDatabase(string db_name)
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorCreateDatabase");
            string query = "CREATE DATABASE If Not Exists " + db_name;
            DevastorExecuteMySQLQuery(query);
        }
        // функция исользования базы данных
        public void DevastorUseDatabase(string db_name)
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorUseDatabase");
            string query = "USE " + db_name;
            DevastorExecuteMySQLQuery(query);
        }
        // функция создания таблицы
        public void DevastorCreateTable(string table_name, List<string> values, List<string> types, string P_KEY = "")
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorCreateTable");
            string query = "CREATE TABLE If Not Exists " + table_name.ToLower() + " (";
            for (int i = 0; i < values.Count; i++)
            {
                if (P_KEY == values[i]) query += values[i] + " " + types[i].ToUpper() + " PRIMARY KEY, ";
                else query += values[i] + " " + types[i].ToUpper() + ", ";
            }
            query = query.Substring(0, query.Length - 2);
            query += ")";
            DevastorExecuteMySQLQuery(query);
        }
        // функция изменения строки
        public void DevastorUpdateRow(string table_name,
            string column_rule_name = null,
            string value_rule = null,
            string column_set_name = null,
            string value_set = null)
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorUpdateRow");
            string query = "UPDATE " +
                table_name.ToLower() +
                " SET " +
                column_set_name +
                " = " +
                value_set +
                " WHERE " +
                column_rule_name +
                " = " +
                value_rule;
            Console.WriteLine("UPDATE:: " + query);
            DevastorExecuteMySQLQuery(query);
        }
        // функция записи строки в таблицу
        public void DevastorInsertRow(string table_name, List<string> values, List<string> _values_names = null)
        {
            //Console.WriteLine("FUNCTION ==> " + "DevastorInsertRow");
            string values_names = "";
            if (values_names != null)
            {
                values_names += "(";
                foreach (string item in _values_names)
                {
                    values_names += item;
                    values_names += ", ";
                }
                values_names = values_names.Substring(0, values_names.Length - 2);
                values_names += ")";
            }
            string query = "INSERT INTO " + table_name.ToLower() + " " + values_names + " VALUES (";
            for (int i = 0; i < values.Count; i++)
            {
                query += values[i] + ", ";
            }
            query = query.Substring(0, query.Length - 2);
            query += ")";
            DevastorExecuteMySQLQuery(query);
        }
        // функция записи строк в таблицу
        public void DevastorInsertRows(string table_name, List<string> rows, string id_column_name=null, List<string> column_names = null)
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorInsertRows");
            List<long> MIN_MAX = DevastorGetBounds(table_name, id_column_name);
            Console.WriteLine("LOCAL MIN: " + MIN_MAX[0]);
            Console.WriteLine("LOCAL MAX: " + MIN_MAX[1]);
            Console.WriteLine("rows[MIN]: " + rows.Min().Split(',')[0]);
            Console.WriteLine("rows[MAX]: " + rows.Max().Split(',')[0]);
            //Environment.Exit(666);
            bool NEED_SET_NEW_FIRST_ID = false;
            bool NEED_SET_NEW_LAST_ID = false;
            // если данные в таблице присутствуют
            if (MIN_MAX[0] != -1 && MIN_MAX[1] != -1)
            {
                // если загружаемый левый конец имеет более ранний id
                if (Convert.ToInt64(rows.Min().Split(',')[0]) < MIN_MAX[0])
                {
                    DevastorUpdateRow(table_name, id_column_name, MIN_MAX[0].ToString(), "flag", "NULL");
                    NEED_SET_NEW_FIRST_ID = true;
                }
                // если загружаемый правый конец имеет более поздний id
                if (Convert.ToInt64(rows.Max().Split(',')[0]) > MIN_MAX[1])
                {
                    DevastorUpdateRow(table_name, id_column_name, MIN_MAX[1].ToString(), "flag", "NULL");
                    NEED_SET_NEW_LAST_ID = true;
                }
            }
            // если таблица пустая
            else
            {
                NEED_SET_NEW_FIRST_ID = true;
                NEED_SET_NEW_LAST_ID = true;
            }

            foreach (string row in rows)
            {
                List<string> rows_to_write = new List<string>(row.Split(','));
                DevastorInsertRow(table_name, rows_to_write, column_names);
            }
            Console.WriteLine("NEED_SET_NEW_FIRST_ID = " + NEED_SET_NEW_FIRST_ID);
            Console.WriteLine("NEED_SET_NEW_LAST_ID = " + NEED_SET_NEW_LAST_ID);

            if (NEED_SET_NEW_FIRST_ID)
                DevastorUpdateRow(table_name, id_column_name, rows.Min().Split(',')[0], "flag", "true");
            if (NEED_SET_NEW_LAST_ID)
                DevastorUpdateRow(table_name, id_column_name, rows.Max().Split(',')[0], "flag", "false");
            //Console.WriteLine("Press key...");
            //Console.ReadKey();
        }
        // функция чтения из базы данных
        public List<List<string>> DevastorReadMySQL(string table_name, DateTime start, DateTime end, int ADD_HOURS, List<string> values = null, int duration = 1)
        {
            ADD_HOURS = 0;
            List<List<string>> table_data = new List<List<string>>();
            string query = "SELECT ";
            Console.WriteLine();
            if (values != null)
            {
                foreach (string value in values)
                {
                    query += value + ", ";
                }
                query = query.Substring(0, query.Length - 2);
                query += " FROM " + table_name;
            }
            else query += " * FROM " + table_name;
            query += " WHERE time > " + DevastorDateTimeToMilliseconds(start.AddHours(ADD_HOURS)) + " AND time < " + DevastorDateTimeToMilliseconds(end.AddHours(ADD_HOURS));
            query += " AND time MOD " + (duration * 1000 * 60).ToString() + " = 0";
            return DevastorExecuteMySQLReader(query, start, end, values);
        }
        // функция чтения границ из базы данных
        public List<long> DevastorGetBounds(string table_name, string id)
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorGetBounds");
            DateTime _start = new DateTime(1987, 8, 17, 5, 0, 0);
            DateTime _end = new DateTime(2050, 8, 18, 5, 0, 0);
            List<List<string>> table_data = new List<List<string>>();
            long MAX = -1;
            long MIN = -1;
            try
            {
                string query = "SELECT " + id + " FROM " + table_name + " WHERE flag = TRUE OR flag = 1";
                table_data = DevastorExecuteMySQLReader(query, _start, _end, new List<string>() { id });
                Console.WriteLine("Минимум: " + table_data);
                Console.WriteLine("table_data.Count: " + table_data.Count);
                Console.WriteLine("table_data[0]: " + table_data[0]);
                Console.WriteLine("table_data[0][0]: " + table_data[0][0]);
                MIN = Convert.ToInt64(table_data[0][0]);
                query = "SELECT " + id + " FROM " + table_name + " WHERE flag = FALSE OR flag = 0";
                table_data = DevastorExecuteMySQLReader(query, _start, _end, new List<string>() { id });
                Console.WriteLine("Максимум: " + table_data);
                Console.WriteLine("table_data.Count: " + table_data.Count);
                Console.WriteLine("table_data[0]: " + table_data[0]);
                Console.WriteLine("table_data[0][0]: " + table_data[0][0]);
                MAX = Convert.ToInt64(table_data[0][0]);
            }
            catch (Exception e) { Console.WriteLine("ERROR min_max_read: " + e.Message); }
            // SELECT MIN(id), MAX(id) FROM tabla
            /*
            try
            {
                table_data = DevastorExecuteMySQLReader(query, _start, _end, new List<string>() { id });
                MAX = Convert.ToInt64(table_data[0][0]);
            }
            catch {}
            query = "SELECT MIN(" + id + ") FROM " + table_name;
            try
            {
                table_data = DevastorExecuteMySQLReader(query, _start, _end, new List<string>() { id });
                MIN = Convert.ToInt64(table_data[0][0]);
            }
            catch { }*/
            Console.WriteLine("MIN: " + MIN);
            Console.WriteLine("MAX: " + MAX);
            return new List<long>() {MIN, MAX};
        }
        // функция чтения таблиц из базы данных
        public List<string> DevastorGetTables()
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorGetTables");
            List<string> table_names = new List<string>();
            try
            {
                string query = "SHOW TABLES";
                MySqlDataReader DevastorMySQLReader = null;
                MySqlCommand DevastorMySQLCommand = new MySqlCommand(query, DevastorMySQLConnection);
                DevastorMySQLCommand.CommandTimeout = 360;
                DevastorMySQLReader = DevastorMySQLCommand.ExecuteReader();
                while (DevastorMySQLReader.Read())
                {
                    string value = DevastorMySQLReader.GetString(0);
                    Console.WriteLine("table name: " + value);
                    table_names.Add(value);
                }
                DevastorMySQLReader.Close();
                Console.WriteLine("Data reading finished!");
            }
            catch (Exception e) { Console.WriteLine("ERROR DevastorGetTables: " + e.Message); }
            return table_names;
        }
        // функция закрытия соединения с базой данных
        public void DevastorDatabaseConnectionClose()
        {
            Console.WriteLine("FUNCTION ==> " + "DevastorDatabaseConnectionClose");
            DevastorMySQLConnection.Close();
        }
        // функция перевода из UnixTime в DateTime
        public static DateTime DevastorLongToDateTime(long unixTimeStamp)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)((unixTimeStamp / 1000) * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks, System.DateTimeKind.Utc);
        }
        // функция перевода формата DateTime в миллисекунды с 1 января 1970 года
        public long DevastorDateTimeToMilliseconds(DateTime _DateTime)
        {
            //Console.WriteLine("FUNCTION ==> " + "DevastorDateTimeToMilliseconds");
            return new DateTimeOffset(_DateTime).ToUnixTimeMilliseconds();
        }
    }
}
