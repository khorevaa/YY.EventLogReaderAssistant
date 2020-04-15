# Помощник чтения данных журнала регистрации 1С:Предприятие 8.x

Библиотека для чтения файлов журнала регистрации платформы 1С:Предприятие 8.x. Поддерживается как старый текстовый формат (*.lgf, *.lgp), так и новый формат SQLite-базы (*.lgd).

# Состав репозитория

* **YY.EventLogReaderAssistant** - исходный код библиотеки
* **YY.EventLogReaderAssistant.Tests** - unit-тесты для проверки работоспособности библиотеки.
* **YY.EventLogReaderAssistantConsoleApp** - консольное приложение с примерами использования библиотеки.

# Примеры использования

Для примера создадим консольное приложение с таким содержимым в методе "Main()":

```csharp
private static int _eventNumber = 0;

static void Main(string[] args)
{
    if (args.Length == 0)
        return;

    // Каталог хранения файлов журнала регистрации.
    // Может быть указан конкретный файл журнала (*.lgd / *.lgf)
    string dataDirectoryPath = args[0];
    Console.WriteLine($"{DateTime.Now}: Инициализация чтения логов \"{dataDirectoryPath}\"...");

    // Инициализация объекта чтения логов
    using (EventLogReader reader = EventLogReader.CreateReader(dataDirectoryPath))
    {
        // Устанавливаем обработчики событий
        reader.AfterReadEvent += Reader_AfterReadEvent;
        reader.AfterReadFile += Reader_AfterReadFile;
        reader.BeforeReadEvent += Reader_BeforeReadEvent;
        reader.BeforeReadFile += Reader_BeforeReadFile;
        reader.OnErrorEvent += Reader_OnErrorEvent;

        // Выводим общее количество собыитй
        Console.WriteLine($"{DateTime.Now}: Всего событий к обработке: ({reader.Count()})...");
        Console.WriteLine();
        Console.WriteLine();
        
        // Последовательно читаем все события журнала
        while (reader.Read())
        {
            // reader.CurrentRow - данные текущего события
            _eventNumber += 1;
        }
    }

    Console.WriteLine($"{DateTime.Now}: Для выхода нажмите любую клавишу...");
    Console.ReadKey();
}
```
Для удобной обработки результатов чтения и других связанных событий можно использовать события (инициализировали подписки на события выше), но не обязательно. Для подписки доступны события:

* **BeforeReadFile** - перед чтением файла.
* **AfterReadFile** - после чтения файла.
* **BeforeReadEvent** - перед чтением события.
* **AfterReadEvent** - после чтения события.
* **OnErrorEvent** - событие при возникновении ошибки.

Пример обработчиков событий.

```csharp
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
```

В объекта "EventLogReader" также есть возможность обращаться к ссылочным данным журнала (приложения, пользователи, уровни событий, статус транзакции и другое).

# Лицензия

MIT - делайте все, что посчитаете нужным. Никакой гарантии и никаких ограничений по использованию.
