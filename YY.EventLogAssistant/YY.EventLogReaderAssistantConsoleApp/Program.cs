using System;
using System.IO;
using YY.EventLogReaderAssistant;
using YY.EventLogReaderAssistant.Models;

namespace YY.EventLogReaderAssistantConsoleApp
{
    class Program
    {
        private static int _eventNumber = 0;

        static void Main(string[] args)
        {
            if (args.Length == 0)
                return;

            string dataDirectoryPath = args[0];
            Console.WriteLine($"{DateTime.Now}: Инициализация чтения логов \"{dataDirectoryPath}\"...");

            EventLogReader reader = EventLogReader.CreateReader(dataDirectoryPath);
            reader.AfterReadEvent += Reader_AfterReadEvent;
            reader.AfterReadFile += Reader_AfterReadFile;
            reader.BeforeReadEvent += Reader_BeforeReadEvent;
            reader.BeforeReadFile += Reader_BeforeReadFile;
            reader.OnErrorEvent += Reader_OnErrorEvent;

            Console.WriteLine($"{DateTime.Now}: Всего событий к обработке: ({reader.Count()})...");
            Console.WriteLine();
            Console.WriteLine();

            while (reader.Read())           
                _eventNumber += 1;

            Console.WriteLine($"{DateTime.Now}: Для выхода нажмите любую клавишу...");
            Console.ReadKey();
        }

        private static void Reader_BeforeReadFile(EventLogReader sender, BeforeReadFileEventArgs args)
        {
            Console.WriteLine($"{DateTime.Now}: Начало чтения файла \"{args.FileName}\"");
            Console.WriteLine($"{DateTime.Now}: {_eventNumber}");
        }

        private static void Reader_AfterReadFile(EventLogReader sender, AfterReadFileEventArgs args)
        {
            Console.WriteLine($"{DateTime.Now}: Окончание чтения файла \"{args.FileName}\"");
        }

        private static void Reader_BeforeReadEvent(EventLogReader sender, BeforeReadEventArgs args)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine($"{DateTime.Now}: (+){_eventNumber}");
        }

        private static void Reader_AfterReadEvent(EventLogReader sender, AfterReadEventArgs args)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine($"{DateTime.Now}: [+]{_eventNumber}");
        }

        private static void Reader_OnErrorEvent(EventLogReader sender, OnErrorEventArgs args)
        {
            Console.WriteLine($"{DateTime.Now}: Ошибка чтения логов \"{args.Exception}\"");
        }
    }
}
