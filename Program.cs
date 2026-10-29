using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibrarySystem
{
    public interface IPrintable
    {
        void PrintDetails();
    }

    public interface IReadOnlyStorage<out T>
    {
        T GetAt(int index);
        int Count { get; }
    }

    public interface IStorageProcessor<in T>
    {
        void Process(T item);
    }

    public class BasePrintProcessor : IStorageProcessor<IPrintable>
    {
        public void Process(IPrintable item)
        {
            item.PrintDetails();
        }
    }


    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public LogEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    // Единый интерфейс для целевых устройств вывода логов (консоль, файл)
    public interface ILogTarget
    {
        void WriteLog(object sender, LogEventArgs e);
    }

    public class ConsoleLogTarget : ILogTarget
    {
        public void WriteLog(object sender, LogEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[LOG КОНСОЛЬ] [{e.Timestamp:HH:mm:ss}] {e.Message}");
            Console.ResetColor();
        }
    }

    public class FileLogTarget : ILogTarget
    {
        private readonly string _filePath;

        public FileLogTarget(string filePath = "library_log.txt")
        {
            _filePath = filePath;
        }

        public void WriteLog(object sender, LogEventArgs e)
        {
            try
            {
                string logLine = $"[LOG ФАЙЛ] [{e.Timestamp:yyyy-MM-dd HH:mm:ss}] {e.Message}";
                File.AppendAllText(_filePath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в лог-файл: {ex.Message}");
            }
        }
    }

    // В классе только событие и обобщенный метод его вызова
    public class Logger
    {
        public event EventHandler<LogEventArgs> LogEvent;

        public void Log<T>(T data)
        {
            string message = data?.ToString() ?? "null";
            LogEvent?.Invoke(this, new LogEventArgs(message));
        }
    }


    public class LibraryStorage<T> : IEnumerable<T>, IReadOnlyStorage<T> where T : IPrintable
    {
        private readonly List<T> _items = new List<T>();

        public int Count => _items.Count;

        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Элемент не может быть null!");
            _items.Add(item);
        }

        public T GetAt(int index)
        {
            if (index < 0 || index >= _items.Count)
                throw new IndexOutOfRangeException("Индекс вне диапазона хранилища!");
            return _items[index];
        }

        public void Sort(Comparison<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            _items.Sort(comparer);
        }

        public void SortBy<TKey>(Func<T, TKey> keySelector, bool descending = false) where TKey : IComparable<TKey>
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            if (descending)
                _items.Sort((x, y) => keySelector(y).CompareTo(keySelector(x)));
            else
                _items.Sort((x, y) => keySelector(x).CompareTo(keySelector(y)));
        }

        public void ForEach(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            foreach (var item in _items) action(item);
        }

        public List<T> FindAll(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var result = new List<T>();
            foreach (var item in _items)
            {
                if (predicate(item)) result.Add(item);
            }
            return result;
        }

        // Сравнение элементов: условие сравнения задается снаружи
        public T FindExtreme(Comparison<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            if (_items.Count == 0) throw new InvalidOperationException("Хранилище пусто!");

            T best = _items[0];
            for (int i = 1; i < _items.Count; i++)
            {
                if (comparer(_items[i], best) > 0) best = _items[i];
            }
            return best;
        }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


    public class Author : IPrintable
    {
        public string FullName { get; set; }
        public int BirthYear { get; set; }

        public Author(string fullName, int birthYear)
        {
            FullName = fullName;
            BirthYear = birthYear;
        }

        public void PrintDetails() => Console.WriteLine($"Автор: {FullName} (род. {BirthYear} г.)");
        public override string ToString() => FullName;
    }

    public abstract class Genre
    {
        public string Name { get; set; }
        public string Description { get; set; }

        protected Genre(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public abstract string GetAccessRestrictions();
        public override string ToString() => Name;
    }

    public class FictionGenre : Genre
    {
        public bool IsAgeRestricted { get; set; }
        public FictionGenre(string name, string description, bool isAgeRestricted = false) : base(name, description)
        {
            IsAgeRestricted = isAgeRestricted;
        }
        public override string GetAccessRestrictions() => IsAgeRestricted ? "18+ (Требуется паспорт)" : "Без ограничений";
    }

    public class NonFictionGenre : Genre
    {
        public string AcademicField { get; set; }
        public NonFictionGenre(string name, string description, string academicField) : base(name, description)
        {
            AcademicField = academicField;
        }
        public override string GetAccessRestrictions() => $"Научный фонд ({AcademicField}): Читальный зал.";
    }

    public class PoetryGenre : Genre
    {
        public string RhymeStyle { get; set; }
        public PoetryGenre(string name, string description, string rhymeStyle) : base(name, description)
        {
            RhymeStyle = rhymeStyle;
        }
        public override string GetAccessRestrictions() => "Редкий поэтический фонд: Для чтения и копирования.";
    }

    public class Book : IPrintable
    {
        private int _year;
        public string Title { get; set; }
        public Author BookAuthor { get; set; }
        public Genre BookGenre { get; set; }
        public string ISBN { get; set; }

        public int Year
        {
            get => _year;
            set
            {
                if (value < 800 || value > DateTime.Now.Year)
                    throw new ArgumentException("Некорректный год издания!");
                _year = value;
            }
        }

        public Book(string title, Author author, Genre genre, int year, string isbn)
        {
            Title = title;
            BookAuthor = author;
            BookGenre = genre;
            Year = year;
            ISBN = isbn;
        }

        public void PrintDetails()
        {
            Console.WriteLine($"[{ISBN}] \"{Title}\" — {BookAuthor.FullName} ({Year}) | Жанр: {BookGenre.Name}");
            Console.WriteLine($"   Правила доступа: {BookGenre.GetAccessRestrictions()}");
        }
    }

    public class LibraryHall
    {
        public string Name { get; set; }
        public int Floor { get; set; }
        public LibraryStorage<Book> Storage { get; private set; } = new LibraryStorage<Book>();

        public LibraryHall(string name, int floor)
        {
            Name = name;
            Floor = floor;
        }

        public void AddBook(Book book) => Storage.Add(book);
        public IReadOnlyStorage<IPrintable> GetReadOnlyStorage() => Storage;

        public void ProcessStorage(IStorageProcessor<Book> processor)
        {
            foreach (var book in Storage)
            {
                processor.Process(book);
                Console.WriteLine();
            }
        }
    }

    public class Library
    {
        public string Name { get; set; }
        public List<LibraryHall> Halls { get; private set; } = new List<LibraryHall>();

        public Library(string name) { Name = name; }
        public int GetTotalBooksCount() => Halls.Sum(h => h.Storage.Count);
    }

    public class BookSearchService
    {
        private readonly Library _library;
        private readonly Logger _logger;

        public BookSearchService(Library library, Logger logger)
        {
            _library = library;
            _logger = logger;
        }

        public List<Book> FindByAuthor(string authorName)
        {
            _logger.Log($"Выполнен поиск книг по автору: '{authorName}'");
            return _library.Halls
                .SelectMany(h => h.Storage)
                .Where(b => b.BookAuthor.FullName.Contains(authorName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<Book> FindByGenre(string genreName)
        {
            _logger.Log($"Выполнен поиск книг по жанру: '{genreName}'");
            return _library.Halls
                .SelectMany(h => h.Storage)
                .Where(b => b.BookGenre.Name.Equals(genreName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public static class BookFactory
    {
        public static void SeedLibraryData(Library library, Logger logger)
        {
            logger.Log("Начало инициализации данных библиотеки...");

            var hall1 = new LibraryHall("Зал Художественной Литературы", 1);
            var hall2 = new LibraryHall("Зал Научной Литературы и Поэзии", 2);

            var classicFiction = new FictionGenre("Классическая проза", "Мировая классика");
            var darkFiction = new FictionGenre("Ужасы/Мистика", "Острая проза", isAgeRestricted: true);
            var scienceNonFiction = new NonFictionGenre("Популярная наука", "Научпоп", "Естествознание");
            var classicPoetry = new PoetryGenre("Поэзия", "Стихотворные произведения", "Силлабо-тоника");

            var pushkin = new Author("Александр Пушкин", 1799);
            var dostoevsky = new Author("Федор Достоевский", 1821);
            var tolstoy = new Author("Лев Толстой", 1828);
            var gogol = new Author("Николай Гоголь", 1809);
            var hawking = new Author("Стивен Хокинг", 1942);
            var esenin = new Author("Сергей Есенин", 1895);

            var books = new List<Book>
            {
                new Book("Евгений Онегин", pushkin, classicFiction, 1833, "978-5-00001-001"),
                new Book("Капитанская дочка", pushkin, classicFiction, 1836, "978-5-00001-002"),
                new Book("Преступление и наказание", dostoevsky, classicFiction, 1866, "978-5-00002-001"),
                new Book("Идиот", dostoevsky, classicFiction, 1869, "978-5-00002-002"),
                new Book("Война и мир. Том 1", tolstoy, classicFiction, 1869, "978-5-00003-001"),
                new Book("Мертвые души", gogol, classicFiction, 1842, "978-5-00004-001"),
                new Book("Краткая история времени", hawking, scienceNonFiction, 1988, "978-5-00006-001"),
                new Book("Исповедь хулигана", esenin, classicPoetry, 1921, "978-5-00008-001")
            };

            foreach (var book in books)
            {
                if (book.BookGenre is FictionGenre)
                    hall1.AddBook(book);
                else
                    hall2.AddBook(book);
            }

            library.Halls.Add(hall1);
            library.Halls.Add(hall2);

            logger.Log($"Инициализация завершена. Загружено книг: {library.GetTotalBooksCount()}");
        }
    }

    internal class Program
    {
        private static void SafeClear()
        {
            try { Console.Clear(); } catch (System.IO.IOException) { }
        }

        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 1. Создаем логгер
            var logger = new Logger();

            // 2. Создаем внешние источники вывода (подписчиков на событие)
            ILogTarget consoleTarget = new ConsoleLogTarget();
            ILogTarget fileTarget = new FileLogTarget("library_activity.log");

            // 3. Подписываем внешние источники на событие логгера
            logger.LogEvent += consoleTarget.WriteLog;
            logger.LogEvent += fileTarget.WriteLog;

            logger.Log("Приложение 'Библиотека' успешно запущено.");

            var library = new Library("Городская Центральная Библиотека");
            BookFactory.SeedLibraryData(library, logger);
            var searchService = new BookSearchService(library, logger);

            bool exit = false;
            while (!exit)
            {
                SafeClear();
                Console.WriteLine($"=== СИСТЕМА УПРАВЛЕНИЯ ФОНДОМ: {library.Name.ToUpper()} ===");
                Console.WriteLine($"Всего книг в базе: {library.GetTotalBooksCount()}");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine("1. Посмотреть каталог книг");
                Console.WriteLine("2. Сортировка книг (Демо делегатов)");
                Console.WriteLine("3. Фильтрация и поиск (Action/Func)");
                Console.WriteLine("4. Найти книги по автору");
                Console.WriteLine("5. Найти книги по жанру");
                Console.WriteLine("0. Выход");
                Console.Write("\nВыберите пункт меню: ");

                string choice = Console.ReadLine() ?? "";
                switch (choice)
                {
                    case "1":
                        logger.Log("Пользователь открыл полный каталог.");
                        ShowAllCatalog(library);
                        break;
                    case "2":
                        logger.Log("Пользователь перешел в меню сортировки.");
                        SortCatalogMenu(library, logger);
                        break;
                    case "3":
                        logger.Log("Пользователь запустил демонстрацию Action/Func.");
                        ActionAndFuncDemoMenu(library, logger);
                        break;
                    case "4":
                        SearchByAuthorMenu(searchService);
                        break;
                    case "5":
                        SearchByGenreMenu(searchService);
                        break;
                    case "0":
                        logger.Log("Завершение работы приложения.");
                        exit = true;
                        break;
                    default:
                        logger.Log($"Неверный пункт меню: '{choice}'.");
                        Console.WriteLine("Неверный ввод! Нажмите ENTER для продолжения...");
                        Console.ReadLine();
                        break;
                }
            }

            // Отписываем источники от события перед выходом
            logger.LogEvent -= consoleTarget.WriteLog;
            logger.LogEvent -= fileTarget.WriteLog;
        }

        private static void ShowAllCatalog(Library library)
        {
            SafeClear();
            Console.WriteLine("=== ПОЛНЫЙ КАТАЛОГ ===\n");
            IStorageProcessor<IPrintable> generalProcessor = new BasePrintProcessor();

            foreach (var hall in library.Halls)
            {
                Console.WriteLine($"\n>>> {hall.Name} (Этаж {hall.Floor})");
                hall.ProcessStorage(generalProcessor);
            }

            Console.WriteLine("\nНажмите ENTER для возврата в меню...");
            Console.ReadLine();
        }

        private static void SortCatalogMenu(Library library, Logger logger)
        {
            SafeClear();
            Console.WriteLine("=== СОРТИРОВКА КОЛЛЕКЦИИ ===");
            Console.WriteLine("1. По названию (Func)");
            Console.WriteLine("2. По году издания (Comparison)");
            Console.WriteLine("3. По году, по убыванию (Func)");
            Console.Write("\nВыберите режим: ");
            string choice = Console.ReadLine() ?? "";

            // Условия сортировки задаются снаружи коллекции
            Comparison<Book> byYear = (b1, b2) => b1.Year.CompareTo(b2.Year);
            Func<Book, string> titleKey = b => b.Title;
            Func<Book, int> yearKey = b => b.Year;

            foreach (var hall in library.Halls)
            {
                if (choice == "1") hall.Storage.SortBy(titleKey);
                else if (choice == "2") hall.Storage.Sort(byYear);
                else if (choice == "3") hall.Storage.SortBy(yearKey, descending: true);
            }

            if (choice == "1" || choice == "2" || choice == "3")
            {
                logger.Log($"Выполнена сортировка каталога, режим {choice}.");
                ShowAllCatalog(library);
            }
            else
            {
                logger.Log("Сортировка отменена: неверный режим.");
                Console.WriteLine("Неверный режим! Нажмите ENTER...");
                Console.ReadLine();
            }
        }

        private static void ActionAndFuncDemoMenu(Library library, Logger logger)
        {
            SafeClear();

            // Условия приходят снаружи коллекции
            Func<Book, bool> isOld = b => b.Year < 1850;
            Action<Book> printShort = b => Console.WriteLine($" - {b.Title} ({b.Year})");
            Comparison<Book> byYear = (b1, b2) => b1.Year.CompareTo(b2.Year);

            foreach (var hall in library.Halls)
            {
                Console.WriteLine($">>> {hall.Name}");

                Console.WriteLine("Книги до 1850 года (Func + Action):");
                foreach (var b in hall.Storage.FindAll(isOld)) printShort(b);

                Console.WriteLine("Все книги зала (Action):");
                hall.Storage.ForEach(printShort);

                var newest = hall.Storage.FindExtreme(byYear);
                Console.WriteLine($"Самая новая книга (Comparison): {newest.Title} ({newest.Year})\n");
            }

            logger.Log("Выполнена демонстрация Action/Func/Comparison.");
            Console.WriteLine("Нажмите ENTER...");
            Console.ReadLine();
        }

        private static void SearchByAuthorMenu(BookSearchService searchService)
        {
            SafeClear();
            Console.Write("Введите автора: ");
            string query = Console.ReadLine() ?? "";
            var results = searchService.FindByAuthor(query);

            Console.WriteLine($"\nНайдено: {results.Count}\n");
            foreach (var b in results) { b.PrintDetails(); Console.WriteLine(); }
            Console.ReadLine();
        }

        private static void SearchByGenreMenu(BookSearchService searchService)
        {
            SafeClear();
            Console.Write("Введите жанр: ");
            string query = Console.ReadLine() ?? "";
            var results = searchService.FindByGenre(query);

            Console.WriteLine($"\nНайдено: {results.Count}\n");
            foreach (var b in results) { b.PrintDetails(); Console.WriteLine(); }
            Console.ReadLine();
        }
    }
}