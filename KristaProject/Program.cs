using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.IO.Compression;
using Newtonsoft.Json;
using DevelopmentClasses;

namespace KristaProject
{
    /// <summary>
    /// Главный класс приложения. Задействует все вспомогательные классы и выводит результаты своей работы в консоль.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Свойство возвращает или изменяет относительный путь к .json файлу, в который скачиваются данные с сайта.
        /// </summary>
        public static FileInfo SavedFileInfo { get; private set; }
        /// <summary>
        /// Начальная дата (левая граница временного отрезка).
        /// </summary>
        public static string LastUpdateFrom { get; private set; }
        /// <summary>
        /// Конечная дата (правая граница временного отрезка).
        /// </summary>
        public static string LastUpdateTo { get; private set; }
        /// <summary>
        /// Главный метод класса Program. Начинает выполняться при старте приложения.
        /// </summary>
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Введите начальную дату в формате дд.мм.гггг (день.месяц.год)");
                DateTime date1;
                while (!DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy", null, DateTimeStyles.None, out date1))
                {
                    Console.WriteLine("You have entered an incorrect value.");
                    Console.WriteLine("Введите начальную дату в формате дд.ММ.гггг (день.месяц.год)");
                }
                LastUpdateFrom = date1.ToString("dd.MM.yyyy");

                DateTime date2;
                Console.WriteLine("Введите конечную дату в формате дд.мм.гггг (день.месяц.год)");
                while (!DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy", null, DateTimeStyles.None, out date2) || DateTime.Compare(date1, date2) > 0)
                {
                    Console.WriteLine("You have entered an incorrect value.");
                    if (DateTime.Compare(date1, date2) > 0)
                    {
                        Console.WriteLine("Конечная дата не может быть раньше начальной");
                    }
                    Console.WriteLine("Введите конечную дату в формате дд.ММ.гггг (день.месяц.год)");
                }
                LastUpdateTo = date2.ToString("dd.MM.yyyy");

                string fileName = @".\..\..\..\AppData\" + LastUpdateFrom + "-" + LastUpdateTo + ".json";
                SavedFileInfo = new FileInfo(fileName);

                UtilsHttp utilsHttp = new UtilsHttp(LastUpdateFrom, LastUpdateTo);
                var task1 = utilsHttp.DownloadData(SavedFileInfo.FullName);
                Console.WriteLine("Ожидание получения данных с сайта...");
                await task1;
                Console.WriteLine("Данные скачаны");

                //var myObject = JsonConvert.DeserializeObject<MyObject>(File.ReadAllText(fileName)); // через JsonSerializer десериализация вроде работает быстрее
                MyObject myObject;
                using (FileStream openStream = File.OpenRead(SavedFileInfo.FullName))
                {
                    using (StreamReader reader = new StreamReader(SavedFileInfo.FullName))
                    {
                        JsonSerializer serialiser = new JsonSerializer();
                        JsonReader jsonRreader = new JsonTextReader(reader);
                        myObject = serialiser.Deserialize<MyObject>(jsonRreader);
                    }
                }
                Console.WriteLine("Количество скачанных объектов: " + myObject.content.Length);

                if (myObject.content.Length == 0)
                {
                    throw new NotSupportedException("Данные для указанного временного интервала на сайте отсутствуют");
                }

                string connectionString = "Server=localhost; Port=5435; Database=KristaDB; UserId=admin; Password=12345678; commandTimeout=120;";
                UtilsPostgres connection = new UtilsPostgres(connectionString);

                // Команда для инициализации БД в Docker-контейнере. Перемещена в код, так как sql-скрипт почему-то не выполняется в docker-compose (по крайней мере в Windows)
                string sqlCreateTableCommand = $@"CREATE TABLE IF NOT EXISTS my_table 
                    (
                        last_update_from date NOT NULL,
                        last_update_to date NOT NULL,
                        json_data json NOT NULL,
                        CONSTRAINT pk_is PRIMARY KEY (last_update_from,last_update_to)
                    )";
                connection.ExecuteCreateTable(sqlCreateTableCommand);

                string sqlExitstsCommand = $@"SELECT EXISTS(
                    SELECT last_update_from FROM my_table
                    WHERE last_update_from = to_date('" + LastUpdateFrom + "', 'dd.mm.yyyy') AND last_update_to = to_date('" + LastUpdateTo + "', 'dd.mm.yyyy')) LIMIT 1";
                Boolean isExists = connection.ExecuteExists(sqlExitstsCommand);

                if (isExists == true)
                {
                    Console.WriteLine("Такие данные уже есть в таблице");
                    string jsonData = File.ReadAllText(SavedFileInfo.FullName).Replace("\r\n", string.Empty).Replace("'", "''");
                    string sqlUpdateCommand = $@"UPDATE my_table 
                        SET json_data='" + @jsonData + "' " +
                        "WHERE last_update_from = to_date('" + LastUpdateFrom + "', 'dd.mm.yyyy') AND last_update_to = to_date('" + LastUpdateTo + "', 'dd.mm.yyyy')";
                    connection.ExecuteInsertOrUpdate(sqlUpdateCommand);
                    Console.WriteLine("Данные перезаписаны");
                }
                else
                if (isExists == false)
                {
                    string jsonData = File.ReadAllText(SavedFileInfo.FullName).Replace("\r\n", string.Empty).Replace("'", "''"); 
                    string sqlInsertCommand = $@"INSERT INTO my_table(last_update_from, last_update_to, json_data)
                    VALUES(to_date('" + LastUpdateFrom + "', 'dd.mm.yyyy'), to_date('" + LastUpdateTo + "', 'dd.mm.yyyy'), '" + @jsonData + "')";
                    connection.ExecuteInsertOrUpdate(sqlInsertCommand);
                    Console.WriteLine("Данные добавлены в таблицу");
                }

                string archivePath = @".\..\..\..\AppData\data.zip";
                MoveToArchive(archivePath);

                connection.Dispose();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                File.Delete(SavedFileInfo.FullName);
                Console.WriteLine("Работа программы завершена. Нажмите любую клавишу, чтобы закрыть это окно.");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Метод, который создает zip архив при его отсутствии по указанному пути и перемещает json-файл с данными в этот архив.
        /// Если в архиве уже есть файл с таким именем, то файл перезаписывается.
        /// </summary>
        /// <param name="archivePath">Относительный путь к zip архиву в файловой системе.</param>
        private static void MoveToArchive(string archivePath)
        {
            if (!File.Exists(archivePath))
            {
                File.Create(archivePath).Close();
            }
            using (FileStream zipToOpen = new FileStream(archivePath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    var entry = archive.GetEntry(SavedFileInfo.Name);
                    if (entry != null)
                    {
                        entry.Delete();
                    }
                    ZipArchiveEntry readmeEntry = archive.CreateEntryFromFile(SavedFileInfo.FullName, SavedFileInfo.Name);
                }
            }
        }
    }
}
