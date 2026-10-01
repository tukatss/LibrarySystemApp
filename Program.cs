using System;
using System.Collections;
using System.Collections.Generic;
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

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
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

        public void PrintDetails()
        {
            Console.WriteLine($"Автор: {FullName} (род. {BirthYear} г.)");
        }

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

        public FictionGenre(string name, string description, bool isAgeRestricted = false) 
            : base(name, description)
        {
            IsAgeRestricted = isAgeRestricted;
        }

        public override string GetAccessRestrictions()
        {
            return IsAgeRestricted ? "18+ (Требуется паспорт)" : "Без ограничений (Выдается на дом)";
        }
    }

    public class NonFictionGenre : Genre
    {
        public string AcademicField { get; set; }

        public NonFictionGenre(string name, string description, string academicField) 
            : base(name, description)
        {
            AcademicField = academicField;
        }

        public override string GetAccessRestrictions()
        {
            return $"Научный фонд ({AcademicField}): Выдача только в читальный зал.";
        }
    }

    public class PoetryGenre : Genre
    {
        public string RhymeStyle { get; set; }

        public PoetryGenre(string name, string description, string rhymeStyle) 
            : base(name, description)
        {
            RhymeStyle = rhymeStyle;
        }

        public override string GetAccessRestrictions()
        {
            return "Редкий поэтический фонд: Доступен для чтения и копирования.";
        }
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
        public IReadOnlyStorage<IPrintable> GetReadOnlyStorage()
        {
            return Storage; // КОВАРИАНТНОСТЬ 
        }

        public void ProcessStorage(IStorageProcessor<Book> processor)
        {
            foreach (var book in Storage)
            {
                processor.Process(book); // КОНТРАВАРИАНТНОСТЬ 
                Console.WriteLine();
            }
        }
    }

    public class Library
    {
        public string Name { get; set; }
        public List<LibraryHall> Halls { get; private set; } = new List<LibraryHall>();

        public Library(string name)
        {
            Name = name;
        }

        public int GetTotalBooksCount() => Halls.Sum(h => h.Storage.Count);
    }

    public class BookSearchService
    {
        private readonly Library _library;

        public BookSearchService(Library library)
        {
            _library = library;
        }

        public List<Book> FindByAuthor(string authorName)
        {
            return _library.Halls
                .SelectMany(h => h.Storage)
                .Where(b => b.BookAuthor.FullName.Contains(authorName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<Book> FindByGenre(string genreName)
        {
            return _library.Halls
                .SelectMany(h => h.Storage)
                .Where(b => b.BookGenre.Name.Equals(genreName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public static class BookFactory
    {
        public static void SeedLibraryData(Library library)
        {
            var hall1 = new LibraryHall("Зал Художественной Литературы", 1);
            var hall2 = new LibraryHall("Зал Научной Литературы и Поэзии", 2);

            var classicFiction = new FictionGenre("Классическая проза", "Мировая классика");
            var darkFiction = new FictionGenre("Ужасы/Мистика", "Острая проза", isAgeRestricted: true);
            var scienceNonFiction = new NonFictionGenre("Популярная наука", "Научпоп", "Естествознание");
            var historyNonFiction = new NonFictionGenre("История", "Исторические хроники", "Гуманитарные науки");
            var classicPoetry = new PoetryGenre("Поэзия", "Стихотворные произведения", "Силлабо-тоника");

            var pushkin = new Author("Александр Пушкин", 1799);
            var dostoevsky = new Author("Федор Достоевский", 1821);
            var tolstoy = new Author("Лев Толстой", 1828);
            var gogol = new Author("Николай Гоголь", 1809);
            var chekhov = new Author("Антон Чехов", 1860);
            var hawking = new Author("Стивен Хокинг", 1942);
            var feynman = new Author("Ричард Фейнман", 1918);
            var esenin = new Author("Сергей Есенин", 1895);
            var lermontov = new Author("Михаил Лермонтов", 1814);
            var king = new Author("Стивен Кинг", 1947);

            var books = new List<Book>
            {
                new Book("Евгений Онегин", pushkin, classicFiction, 1833, "978-5-00001-001"),
                new Book("Капитанская дочка", pushkin, classicFiction, 1836, "978-5-00001-002"),
                new Book("Пиковая дама", pushkin, classicFiction, 1834, "978-5-00001-003"),
                new Book("Руслан и Людмила", pushkin, classicPoetry, 1820, "978-5-00001-004"),
                new Book("Сборник лирики", pushkin, classicPoetry, 1825, "978-5-00001-005"),

                new Book("Преступление и наказание", dostoevsky, classicFiction, 1866, "978-5-00002-001"),
                new Book("Идиот", dostoevsky, classicFiction, 1869, "978-5-00002-002"),
                new Book("Братья Карамазовы", dostoevsky, classicFiction, 1880, "978-5-00002-003"),
                new Book("Бесы", dostoevsky, classicFiction, 1872, "978-5-00002-004"),
                new Book("Записки из подземья", dostoevsky, classicFiction, 1864, "978-5-00002-005"),

                new Book("Война и мир. Том 1", tolstoy, classicFiction, 1869, "978-5-00003-001"),
                new Book("Война и мир. Том 2", tolstoy, classicFiction, 1869, "978-5-00003-002"),
                new Book("Анна Каренина", tolstoy, classicFiction, 1877, "978-5-00003-003"),
                new Book("Воскресение", tolstoy, classicFiction, 1899, "978-5-00003-004"),
                new Book("Смерть Ивана Ильича", tolstoy, classicFiction, 1886, "978-5-00003-005"),

                new Book("Мертвые души", gogol, classicFiction, 1842, "978-5-00004-001"),
                new Book("Ревизор", gogol, classicFiction, 1836, "978-5-00004-002"),
                new Book("Вечера на хуторе близ Диканьки", gogol, darkFiction, 1832, "978-5-00004-003"),
                new Book("Вий", gogol, darkFiction, 1835, "978-5-00004-004"),

                new Book("Вишневый сад", chekhov, classicFiction, 1904, "978-5-00005-001"),
                new Book("Палата №6", chekhov, classicFiction, 1892, "978-5-00005-002"),
                new Book("Дама с собачкой", chekhov, classicFiction, 1899, "978-5-00005-003"),
                new Book("Чайка", chekhov, classicFiction, 1896, "978-5-00005-004"),

                new Book("Краткая история времени", hawking, scienceNonFiction, 1988, "978-5-00006-001"),
                new Book("Мир в ореховой скорлупке", hawking, scienceNonFiction, 2001, "978-5-00006-002"),
                new Book("Высший замысел", hawking, scienceNonFiction, 2010, "978-5-00006-003"),

                new Book("Вы, конечно, шутите, мистер Фейнман!", feynman, scienceNonFiction, 1985, "978-5-00007-001"),
                new Book("Дюжина лекций", feynman, scienceNonFiction, 1963, "978-5-00007-002"),
                new Book("Характер физических законов", feynman, scienceNonFiction, 1965, "978-5-00007-003"),

                new Book("Исповедь хулигана", esenin, classicPoetry, 1921, "978-5-00008-001"),
                new Book("Черный человек", esenin, classicPoetry, 1926, "978-5-00008-002"),
                new Book("Радуница", esenin, classicPoetry, 1916, "978-5-00008-003"),
                new Book("Анна Снегина", esenin, classicPoetry, 1925, "978-5-00008-004"),

                new Book("Герой нашего времени", lermontov, classicFiction, 1840, "978-5-00009-001"),
                new Book("Демон", lermontov, classicPoetry, 1839, "978-5-00009-002"),
                new Book("Мцыри", lermontov, classicPoetry, 1840, "978-5-00009-003"),
                new Book("Бородино", lermontov, classicPoetry, 1837, "978-5-00009-004"),

                new Book("Оно", king, darkFiction, 1986, "978-5-00010-001"),
                new Book("Сияние", king, darkFiction, 1977, "978-5-00010-002"),
                new Book("Зеленая миля", king, classicFiction, 1996, "978-5-00010-003")
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
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var library = new Library("Городская Центральная Библиотека");
            BookFactory.SeedLibraryData(library);
            var searchService = new BookSearchService(library);

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine($"=== СИСТЕМА УПРАВЛЕНИЯ ФОНДОМ: {library.Name.ToUpper()} ===");
                Console.WriteLine($"Всего книг в базе: {library.GetTotalBooksCount()}");
                Console.WriteLine("-------------------------------------------------------");
                Console.WriteLine("1. Посмотреть каталог (обработка хранилища через контравариантный процессор)");
                Console.WriteLine("2. Найти книги по автору");
                Console.WriteLine("3. Найти книги по жанру");
                Console.WriteLine("0. Выход");
                Console.Write("\nВыберите пункт меню: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        ShowAllCatalog(library);
                        break;
                    case "2":
                        SearchByAuthorMenu(searchService);
                        break;
                    case "3":
                        SearchByGenreMenu(searchService);
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Неверный ввод! Нажмите ENTER для продолжения...");
                        Console.ReadLine();
                        break;
                }
            }
        }

        private static void ShowAllCatalog(Library library)
        {
            Console.Clear();
            Console.WriteLine("=== ПОЛНЫЙ КАТАЛОГ ===\n");

            IStorageProcessor<IPrintable> generalProcessor = new BasePrintProcessor();

            foreach (var hall in library.Halls)
            {
                Console.WriteLine($"\n>>> {hall.Name} (Этаж {hall.Floor}) - Книг: {hall.Storage.Count}");
                Console.WriteLine(new string('-', 60));
                hall.ProcessStorage(generalProcessor);
            }

            Console.WriteLine("\nНажмите ENTER для возврата в меню...");
            Console.ReadLine();
        }

        private static void SearchByAuthorMenu(BookSearchService searchService)
        {
            Console.Clear();
            Console.Write("Введите имя или фамилию автора (например, Пушкин): ");
            string query = Console.ReadLine() ?? string.Empty;

            var results = searchService.FindByAuthor(query);
            Console.WriteLine($"\nНайдено книг: {results.Count}\n");
            foreach (var book in results)
            {
                book.PrintDetails();
                Console.WriteLine();
            }
            Console.WriteLine("Нажмите ENTER для возврата в меню...");
            Console.ReadLine();
        }

        private static void SearchByGenreMenu(BookSearchService searchService)
        {
            Console.Clear();
            Console.Write("Введите название жанра (Поэзия, История, Популярная наука, Ужасы/Мистика): ");
            string query = Console.ReadLine() ?? string.Empty;

            var results = searchService.FindByGenre(query);
            Console.WriteLine($"\nНайдено книг: {results.Count}\n");
            foreach (var book in results)
            {
                book.PrintDetails();
                Console.WriteLine();
            }
            Console.WriteLine("Нажмите ENTER для возврата в меню...");
            Console.ReadLine();
        }
    }
}