using System;
using System.IO;
using YY.EventLogAssistant;
using YY.EventLogAssistant.Models;

namespace YY.EventLogAssistantConsoleApp
{
    class Program
    {
        private static int _eventNumber = 0;

        static void Main(string[] args)
        {
            string testDataDirectoryPath = $"{ Environment.CurrentDirectory}{Path.DirectorySeparatorChar}TestData\\1Cv8.lgf";

            EventLogReader reader = EventLogReader.CreateReader(testDataDirectoryPath);
            reader.AfterReadEvent += Reader_AfterReadEvent;
            reader.AfterReadFile += Reader_AfterReadFile;
            reader.BeforeReadEvent += Reader_BeforeReadEvent;
            reader.BeforeReadFile += Reader_BeforeReadFile;
            reader.OnErrorEvent += Reader_OnErrorEvent;

            // Пример задания точного положения для чтения в файле. Для формата *.lgf
            //reader.SetCurrentPosition(new EventLogPosition(
            //    5,
            //    reader.LogFilePath,
            //    reader.LogFilePath,
            //    436));

            Console.WriteLine($"Всего событий: {reader.Count()}");

            long totalEvents = 0;
            EventLogRowData rowData;
            while (reader.Read(out rowData))
                totalEvents += 1;

            Console.WriteLine("Для выхода нажмите любую клавишу...");
            Console.ReadKey();
        }

        private static void Reader_BeforeReadFile(EventLogReader sender, BeforeReadFileEventArgs args)
        {
            // Пример получения текущей позиции чтения
            var positionBeforeReadFile = sender.GetCurrentPosition();

            Console.WriteLine("Reader_BeforeReadFile");
        }

        private static void Reader_AfterReadFile(EventLogReader sender, AfterReadFileEventArgs args)
        {
            // Пример получения текущей позиции чтения
            var positionAfterReadFile = sender.GetCurrentPosition();

            Console.WriteLine("Reader_AfterReadFile");
        }

        private static void Reader_BeforeReadEvent(EventLogReader sender, BeforeReadEventArgs args)
        {
            _eventNumber += 1;
            Console.WriteLine($"Reader_BeforeReadEvent: {_eventNumber}");
        }

        private static void Reader_AfterReadEvent(EventLogReader sender, AfterReadEventArgs args)
        {
            Console.WriteLine($"Reader_AfterReadEvent {_eventNumber}");
        }

        private static void Reader_OnErrorEvent(EventLogReader sender, OnErrorEventArgs args)
        {
            Console.WriteLine("Reader_OnErrorEvent");
        }
    }
}
