using System;
using System.IO;

namespace CheckService
{
    class Log
    {
        public static void Write(string str)
        {
            string logname = "CheckService";
            string EntryTime = DateTime.Now.ToLongTimeString();
            string EntryDate = DateTime.Today.ToShortDateString();
            string fileName = logname + ".log";  //log + data +logname ? 

//             if (!Directory.Exists(Environment.CurrentDirectory + "/log/"))
//                 Directory.CreateDirectory((Environment.CurrentDirectory + "/log/"));

            try
            {
                StreamWriter sw = new StreamWriter(fileName, true, System.Text.Encoding.UTF8);
                sw.WriteLine("[" + EntryDate + "][" + EntryTime + "]" + " " + str);
                sw.Close();
                //check this
                sw.Dispose();

                Color.WriteLineColor(str, ConsoleColor.Green);
            }
            catch (Exception ex) { Write(ex.Message); }
        }

        public static void ExcWrite(string text)
        {
            Write(text);
        }
    }
}
