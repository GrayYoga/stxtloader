using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
namespace stxtloader
{
    class Program
    {
        
	
        static void Main(string[] args)
        {
            WriteLog("Старт программы.");
            SqlConnection sc = new SqlConnection();
            DataSet ds = new DataSet();
            ComandLineParams clp = new ComandLineParams(args);
            if (clp.bParseSuccess)
            {
                // получаем список заданий не на контроле
                if (GetTasksList(clp.strServer, clp.strUser, clp.strPassword, clp.bAllTasks, false, sc, ds) == false) {
                    Console.WriteLine("Ошибка получения списка заданий.");
                    return;
                }
                DataSet ds2 = GetStenogrammTasksList(ds, clp.strPathToOutDir, clp.bAllTasks);
                SaveStenogramms(sc,ds2,clp.strPathToOutDir);
                DataSet ds3 = GetOrientationTasksList(ds, clp.strPathToOutDir, clp.bAllTasks);
                SaveOrientation(sc, ds3, clp.strPathToOutDir);

            }
            else
            {
                Console.WriteLine("Ошибка чтения параметров командной строки.");
            }
            WriteLog("Работа программы окончена.");
        }
        // получение списка заданий
        private static bool GetTasksList(string server, string user, string pass,
                                                bool bAllTasks, bool IntAuth, SqlConnection sqlConn, DataSet fullinfo)
        {
            WriteLog("Получаем список заданий");
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder {
					ConnectionString = "server=" + server + 
					";Initial Catalog=MagTalksBase;user id=" + 
					user + 
					";password=" + pass + ";"
			};
			// Integrated Security=true;
			sqlConn.ConnectionString = builder.ConnectionString;
			try
			{
				sqlConn.Open(); 
				// выборка только заданий не на контроле ( в таких уже точно обработаны все заявки и составлены все сводки)
				SqlCommand command = new SqlCommand(string.Concat("SELECT obj_shifr,task_id  FROM [MagTalksBase].[dbo].[dt_Tasks]",
													bAllTasks?"":"WHERE ctrl_allowed = 0" ," ORDER BY task_begin "), sqlConn);
				SqlDataAdapter adapter = new SqlDataAdapter(command.CommandText, sqlConn);
				
				adapter.Fill(fullinfo);
                WriteLog(String.Concat("Получено ",fullinfo.Tables[0].Rows.Count.ToString()," заданий (не на контроле)."));
                return true;
			}
			catch (SqlException exception)
			{
				string text = "Ошибка соединения с базой данных.\n";
                WriteLog(text);
                for (int i = 0; i < exception.Errors.Count; i++)
				{
                    WriteLog(string.Concat(exception.Errors[i].Message, "\n"));
                    text = string.Concat(text, exception.Errors[i].Message, "\n");
				}
				WriteLog("Сбой аутентификации.");
                return false;
			}
        }
        
        // получаем список заданий, стенограммы по которым не сохранялись
        private static DataSet GetStenogrammTasksList(DataSet ds, string Path, bool bAllTasks)
        {
            WriteLog("Получаем список заданий, стенограммы по которым не сохранены.");
            DataSet filtredDs = new DataSet();
            filtredDs.Tables.Add(new DataTable());

            int lenHeader = 0;//("ПТП-75-").Length;
            foreach (DataColumn col in ds.Tables[0].Columns) {
                filtredDs.Tables[0].Columns.Add(col.ToString());
            }
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                string strShipher = row["obj_shifr"].ToString();
                lenHeader = strShipher.IndexOf('-') - 1; // буква П или С попадут в имя
                string filename = strShipher.Substring(lenHeader);
                if (bAllTasks || !File.Exists(CombinePathToStenogrammFile(Path, filename)))
                {
                    filtredDs.Tables[0].ImportRow(row);
                }
            }
            // отрапортовать о количестве таких заданий
            WriteLog(string.Concat("Получено заданий: ", filtredDs.Tables[0].Rows.Count.ToString()));
            return filtredDs;
        }
        
        // получаем список заданий, стенограммы по которым не сохранялись
        private static DataSet GetOrientationTasksList(DataSet ds, string Path, bool bAllTasks)
        {
            WriteLog("Получаем список заданий, ориентировки по которым не сохранены.");
            DataSet filtredDs = new DataSet();
            filtredDs.Tables.Add(new DataTable());

            int lenHeader = 0;//("ПТП-75-").Length;
            foreach (DataColumn col in ds.Tables[0].Columns)
            {
                filtredDs.Tables[0].Columns.Add(col.ToString());
            }
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                string strShipher = row["obj_shifr"].ToString();
                lenHeader = strShipher.IndexOf('-') - 1; // буква П или С попадут в имя
                string filename = strShipher.Substring(lenHeader);
                if (bAllTasks || !File.Exists(CombinePathToOrientationFile(Path, filename)))
                {
                    filtredDs.Tables[0].ImportRow(row);
                }
            }
            WriteLog(string.Concat("Получено заданий: ", filtredDs.Tables[0].Rows.Count.ToString()));
            return filtredDs;
        }
        
        // сохранение стенограмм по списку заданий
        private static bool SaveStenogramms(SqlConnection sqlConn, DataSet ds, string strPathToSave){
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                DataSet dataSet = new DataSet();

                WriteLog(string.Concat("Сохранение стенограмм: task_id=", row["task_id"], " ", row["obj_shifr"]));
                SqlCommand command = new SqlCommand(string.Concat("SELECT t.[talk_id], t.[task_auto_id], t.[tbegin],t.[direction],",
                                                                  " tx.Phone, tx.Stenogram",
                                                                  " FROM [MagTalksBase].[dbo].[dt_Talks] t",
                                                                    " INNER JOIN [MagTalksBase].[dbo].[sm_dt_Texts] tx ON t.talk_id = tx.talk_id",
                                                                    " where tx.task_id = ", row["task_id"], " order by tx.tbegin"), sqlConn);
                new SqlDataAdapter(command.CommandText, sqlConn).Fill(dataSet);

                string str2 = "";

                foreach (DataRow row3 in dataSet.Tables[0].Rows)
                {
                    string strPhone = row3["Phone"].ToString();

                    strPhone = strPhone.Replace("(", ""); strPhone = strPhone.Replace(")", "");
                    strPhone = strPhone.Replace("-", ""); strPhone = strPhone.Replace(" ", "");

                    str2 = string.Concat(str2,
                                         row3["tbegin"], " ", strPhone, " ",
                                         (row3["direction"].ToString().Contains("1") ? "Входящий" : "Исходящий"),
                                         "\r\n", row3["Stenogram"],
                                         "\r\n");

                }
                int lenHeader = 0;//("ПТП-75-").Length;
                string shipher = row["obj_shifr"].ToString();
                lenHeader = shipher.IndexOf('-') - 1; // буква П или С попадут в имя
                string filename = shipher.Substring(lenHeader);
                
                if (!System.IO.Directory.Exists(strPathToSave)) { Directory.CreateDirectory(strPathToSave); }

                StreamWriter writer = new StreamWriter(CombinePathToStenogrammFile(strPathToSave, filename), false, Encoding.Unicode);
                writer.Write(str2);
                writer.Close();
                WriteLog(string.Concat("Файл ", CombinePathToStenogrammFile(strPathToSave, filename), " сохранен."));
            }
            return true;
        }
        
        // сохранение ориентировок по списку заданий
        private static bool SaveOrientation(SqlConnection sqlConn, DataSet ds, string strPathToSave)
        {
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                DataSet dataSet = new DataSet();

                WriteLog(string.Concat("Сохранение ориентировки: task_id=", row["task_id"], " ", row["obj_shifr"]));
                SqlCommand command = new SqlCommand(string.Concat("SELECT orientation, task_id",
                                                                  //" tx.Phone, tx.Stenogram",
                                                                  " FROM [MagTalksBase].[dbo].[dt_Tasks] ",
                                                                    //" INNER JOIN [MagTalksBase].[dbo].[sm_dt_Texts] tx ON t.talk_id = tx.talk_id",
                                                                    " where task_id = ", row["task_id"]), sqlConn);
                new SqlDataAdapter(command.CommandText, sqlConn).Fill(dataSet);

                string str2 = "";

                foreach (DataRow row3 in dataSet.Tables[0].Rows)
                {
                    str2 += row3["orientation"].ToString();
                }
                int lenHeader = 0;//("ПТП-75-").Length;
                string shipher = row["obj_shifr"].ToString();
                lenHeader = shipher.IndexOf('-') - 1; // буква П или С попадут в имя
                string filename = shipher.Substring(lenHeader);
                
                if (!System.IO.Directory.Exists(strPathToSave)) { Directory.CreateDirectory(strPathToSave); }

                StreamWriter writer = new StreamWriter(CombinePathToOrientationFile(strPathToSave, filename), false, Encoding.Unicode);
                writer.Write(str2);
                writer.Close();
                WriteLog(string.Concat("Файл ", CombinePathToOrientationFile(strPathToSave, filename), " сохранен."));
            }
            return true;
        }
        private static void CreateDir(string s) { 
            if (!System.IO.Directory.Exists(s)) System.IO.Directory.CreateDirectory(s);
        }
        private static string CombinePathToStenogrammFile(string Path, string filename)
        {
            string strYear = string.Concat(filename.Substring(filename.Length - 4), "\\");
            CreateDir(System.IO.Path.Combine(Path, string.Concat("stenogramms\\", strYear)));
            return System.IO.Path.Combine(Path, string.Concat("stenogramms\\", strYear, filename, "_stn.txt"));
        }
        
        private static string CombinePathToOrientationFile(string Path, string filename)
        {
            string strYear = string.Concat(filename.Substring(filename.Length - 4), "\\" );
            CreateDir(System.IO.Path.Combine(Path, string.Concat("orientations\\", strYear)));
            return System.IO.Path.Combine(Path, string.Concat("orientations\\", strYear, filename, "_ort.txt"));
        }
        
        static void WriteLog(string str){
            string contents = string.Format("[{0:dd.MM.yyyy HH:mm:ss.fff}] {1}\r\n", DateTime.Now, str);
            File.AppendAllText("events.log", contents, Encoding.GetEncoding("Windows-1251"));
        }
    }
}
