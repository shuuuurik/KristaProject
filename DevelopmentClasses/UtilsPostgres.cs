using System;
using Npgsql;

namespace DevelopmentClasses
{
    /// <summary>
    /// Класс, используемый для работы с Postgres. Отвечает за установку соединения с базой и выполнения sql-команд.
    /// </summary>
    public class UtilsPostgres : IDisposable
    {
        /// <summary>
        /// Соединение с базой.
        /// </summary>
        public NpgsqlConnection Connection { get; private set; }

        /// <summary>
        /// Показывает, был ли объект класса UtilsPostgres уничтожен.
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// Конструктор класса. По входной строке устанавливает соединение с базой или выбрасывает исключение, если соединение не установлено.
        /// </summary>
        /// <param name="connectionString">Строка для подключения к базе Postgres.</param>
        public UtilsPostgres(string connectionString) 
        {
            Connection = new NpgsqlConnection(connectionString);
            try
            {
                Connection.Open();
            }
            catch (Exception e)
            {
                throw new Exception("Error connecting to the database", e);
            }
        }

        /// <summary>
        /// Метод, выполняющий sql-команду CREATE TABLE в базе Postgres.
        /// </summary>
        /// <param name="sql">Строка, содержащая sql-команду.</param>
        public void ExecuteCreateTable(String sql)
        {
            try
            {
                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteScalar();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error executing create table command", e);
            }
            return;
        }

        /// <summary>
        /// Метод, выполняющий sql-команду EXISTS (с подзапросом) в базе Postgres.
        /// </summary>
        /// <param name="sql">Строка, содержащая sql-команду.</param>
        public Boolean ExecuteExists(String sql)
        {
            try
            {
                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = sql;
                    var result = command.ExecuteScalar();
                    if (result is bool isExisting)
                    {
                        return isExisting;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error executing exist command", e);
            }
        }

        /// <summary>
        /// Метод, выполняющий sql-команду INSERT в базе Postgres.
        /// </summary>
        /// <param name="sql">Строка, содержащая sql-команду.</param>
        public void ExecuteInsertOrUpdate(String sql)
        {
            try
            {
                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteScalar();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error executing insert or update command", e);
            }
            return;
        }

        /// <summary>
        /// Dispose method required for the IDispose interface.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~UtilsPostgres()
        {
            Dispose(false);
        }
        protected virtual void Dispose(bool isDisposing)
        {
            if (!IsDisposed)
            {
                if (isDisposing)
                {
                    // Освобождаем ресурсы только если Dispose
                    // был вызван приложением явным образом
                }
                // Всегда освобождаем неуправляемые ресурсы
                if (Connection != null)
                {
                    Connection.Close();
                }
                Connection.Dispose();
            }
            // Указываем, что объект уже уничтожен,
            // а ресурсы освобождены
            IsDisposed = true;
        }
    }
}
