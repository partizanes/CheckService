using System;
using System.Data.OleDb;
using System.ServiceProcess;
using System.Threading;

namespace CheckService
{
    class Program
    {
        public static string ConnectingString;
        private static string LsTradeDir;
        private static ServiceController TheServiceName = new ServiceController(Config.GetParametr("ServiceName"));

        static void Main(string[] args)
        {
            Color.WriteLineColor("Программа запущена.", ConsoleColor.Green);
            Thread myThreading = new Thread(CheckThread);
            myThreading.Start();
        }

        protected static void CheckThread()
        {
            LsTradeDir = Config.GetParametr("LsTradeDir");
            ConnectingString = "Provider=vfpoledb.1;Data Source=" + LsTradeDir + ";Mode=Read;Collating Sequence=MACHINE;CODEPAGE=1251";
            while (true)
            {
                Color.WriteLineColor("Начало проверки службы.", ConsoleColor.Green);
                CheckService();

                int timeBetweenChecks = 60000*(int.Parse(Config.GetParametr("timeBetweenChecks")));

                Color.WriteLineColor("Проверка отложена на 5 минут.", ConsoleColor.Green);
                Thread.Sleep(timeBetweenChecks);
            }
        }

        private static void CheckService()
        {
            //check service status

            switch (TheServiceName.Status)
            {
                case ServiceControllerStatus.Running:
                    Log.Write("Текущий статус службы " + TheServiceName.ServiceName + ": Работает.");
                    CheckTimeOperation();
                    break;
                case ServiceControllerStatus.Paused:
                case ServiceControllerStatus.Stopped:
                    Log.Write("Текущий статус службы " + TheServiceName.ServiceName + " :" + TheServiceName.Status);
                    TheServiceName.Start();
                    TheServiceName.WaitForStatus(ServiceControllerStatus.Running);
                    TheServiceName.Refresh();
                    Log.Write("Попытка перезапуска службы " + TheServiceName.ServiceName + ". Текущий статус:" + TheServiceName.Status);
                    break;
                default:
                    Log.Write(TheServiceName.Status.ToString());
                    break;
            }
        }

        private static void CheckTimeOperation()
        {
            try
            {
                Color.WriteLineColor("Начало проверки времени запуска.", ConsoleColor.Green);

                using (OleDbConnection OleDbconn = new OleDbConnection(ConnectingString))
                {
                    OleDbconn.Open();

                    Color.WriteLineColor("Открытие соединения...", ConsoleColor.Green);

                    //Tn1 Время последнего удачного старта;
                    //Tk1 Время последнего удачного окончания;
                    //Tn2 Время последнего старта;
                    //Tk2 Время последнего окончания;
                    OleDbCommand OleDbcmd = new OleDbCommand(@"SELECT ischedule,Tn1,Tn2,Tk1,Tk2 FROM schedule WHERE Tn2 > Tk2 AND iz = 2");

                    OleDbcmd.Connection = OleDbconn;

                    OleDbcmd.CommandTimeout = 0;

                    using (OleDbDataReader OleDbDr = OleDbcmd.ExecuteReader())
                    {
                        Color.WriteLineColor("Запрос...", ConsoleColor.Green);

                        if (OleDbDr == null || !OleDbDr.HasRows)
                        {
                            Log.Write("На текущий момент активных задач нет.");
                            return;
                        }
                        while (OleDbDr.Read())
                        {
                            Color.WriteLineColor("Получение значений...", ConsoleColor.Green);

                            int id = Convert.ToInt32(OleDbDr.GetValue(0));
                            DateTime NowDate = DateTime.Now;
                            DateTime StartOper = Convert.ToDateTime(OleDbDr.GetValue(2));

                            TimeSpan diff = NowDate - StartOper;
                            Log.Write("Обнаружено активное задание: " + GetStringSname(id) + " .Время выполнения составляет: " + diff.Hours + "." + diff.Minutes + "." + diff.Seconds);

                            int replicationTime = int.Parse(Config.GetParametr("replicationTime"));
                            int replicationNightTime =  int.Parse(Config.GetParametr("replicationNightTime"));
                            int unloadingUserTimes =  int.Parse(Config.GetParametr("unloadingUserTimes"));
                            int loadingSalesTimes =  int.Parse(Config.GetParametr("loadingSalesTimes"));
                            int recalculationTime =  int.Parse(Config.GetParametr("recalculationTime"));
                            int copybdTime =  int.Parse(Config.GetParametr("copybdTime"));

                            switch (id)
                            {
                                case 1:                         //Репликации 7min
                                    if (diff.TotalMinutes > replicationTime)
                                        RestartService(id);
                                    break;
                                case 2:                         //Репликации ночные 20m
                                    if (diff.TotalMinutes > replicationNightTime)
                                        RestartService(id);
                                    break;
                                case 3:                         //Выгрузка терминальных пользователей 5m
                                    if (diff.TotalMinutes > unloadingUserTimes)
                                        RestartService(id);
                                    break;
                                case 4:                         //Прием реализации 7 m
                                    if (diff.TotalMinutes > loadingSalesTimes)
                                        RestartService(id);
                                    break;
                                case 5:                         //Перерасчет агрегаций 2h
                                    if (diff.TotalMinutes > recalculationTime)
                                        RestartService(id);
                                    break;
                                case 6:                         //Копия БД 10m
                                    if (diff.TotalMinutes > copybdTime)
                                        RestartService(id);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Write(ex.Message);
                Log.Write(ConnectingString);
                Log.Write(LsTradeDir);
            }
        }

        private static void RestartService(int id)
        {
            Log.Write("Происходит остановка службы по причине превышения времени выполнения операции :  " + GetStringSname(id));

            TheServiceName.Stop();

            TheServiceName.WaitForStatus(ServiceControllerStatus.Stopped);

            Log.Write("Текущий статус службы " + TheServiceName.ServiceName + " :" + TheServiceName.Status);

            TheServiceName.Start();
            TheServiceName.WaitForStatus(ServiceControllerStatus.Running);
            TheServiceName.Refresh();
            //System.Threading.Thread.Sleep(10000);
            Log.Write("Попытка перезапуска службы " + TheServiceName.ServiceName + ". Текущий статус:" + TheServiceName.Status);
        }

        private static string GetStringSname(int id)
        {
            switch (id)
            {
                case 1:
                    return @"Репликации";
                case 2:
                    return @"Репликации ночные";
                case 3:
                    return @"Выгрузка терминальных пользователей";
                case 4:
                    return @"Прием реализации";
                case 5:
                    return @"Перерасчет агрегаций";
                case 6:
                    return @"Копия БД";
                default:
                    return @"Неизвестная операция";
            }
        }
    }
}
