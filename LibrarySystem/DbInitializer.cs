using LibrarySystem.Data;
using LibrarySystem.Models;

namespace LibrarySystem
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Создаем базу данных, если она не существует
            context.Database.EnsureCreated();

            // Проверяем, есть ли уже данные
            if (context.Users.Any() || context.Books.Any())
            {
                return; // База уже инициализирована
            }

            // Добавляем начальных пользователей
            var users = new User[]
            {
                new User {
                    Email = "reader@library.ru",
                    Password = "123456",
                    FullName = "Иванов Иван",
                    Role = "Reader",
                    StudentId = "ST001",
                    Category = "Студент",
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Email = "librarian@library.ru",
                    Password = "123456",
                    FullName = "Петрова С.М.",
                    Role = "Librarian",
                    Category = "Сотрудник",
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Email = "admin@library.ru",
                    Password = "123456",
                    FullName = "Сидоров А.В.",
                    Role = "Admin",
                    Category = "Администратор",
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Email = "sysadmin@library.ru",
                    Password = "123456",
                    FullName = "Кузнецов Д.С.",
                    Role = "SystemAdmin",
                    Category = "Системный администратор",
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();

            // Добавляем начальные книги
            var books = new Book[]
            {
                new Book {
                    Title = "Война и мир",
                    Author = "Лев Толстой",
                    Genre = "Классика",
                    Year = 1869,
                    Location = "Зал1-Ст2-Пол3",
                    Status = "Available",
                    ISBN = "978-5-699-12014-7",
                    AcquisitionDate = DateTime.Now
                },
                new Book {
                    Title = "Преступление и наказание",
                    Author = "Федор Достоевский",
                    Genre = "Классика",
                    Year = 1866,
                    Location = "Зал1-Ст1-Пол2",
                    Status = "Available",
                    ISBN = "978-5-17-090345-2",
                    AcquisitionDate = DateTime.Now
                },
                new Book {
                    Title = "Мастер и Маргарита",
                    Author = "Михаил Булгаков",
                    Genre = "Классика",
                    Year = 1967,
                    Location = "Зал1-Ст3-Пол1",
                    Status = "Available",
                    ReadingRoomOnly = true,
                    ISBN = "978-5-389-08266-5",
                    AcquisitionDate = DateTime.Now
                },
                new Book {
                    Title = "1984",
                    Author = "Джордж Оруэлл",
                    Genre = "Антиутопия",
                    Year = 1949,
                    Location = "Зал2-Ст1-Пол4",
                    Status = "Available",
                    ISBN = "978-5-17-080115-4",
                    AcquisitionDate = DateTime.Now
                },
                new Book {
                    Title = "Гарри Поттер и философский камень",
                    Author = "Джоан Роулинг",
                    Genre = "Фэнтези",
                    Year = 1997,
                    Location = "Зал2-Ст2-Пол1",
                    Status = "Available",
                    ISBN = "978-5-389-04865-4",
                    AcquisitionDate = DateTime.Now
                },
                new Book {
                    Title = "Игра престолов",
                    Author = "Джордж Мартин",
                    Genre = "Фэнтези",
                    Year = 1996,
                    Location = "Зал2-Ст3-Пол2",
                    Status = "Available",
                    ISBN = "978-5-389-06553-8",
                    AcquisitionDate = DateTime.Now
                },
                new Book {
                    Title = "Три товарища",
                    Author = "Эрих Мария Ремарк",
                    Genre = "Классика",
                    Year = 1936,
                    Location = "Зал1-Ст4-Пол1",
                    Status = "Available",
                    ISBN = "978-5-389-07134-8",
                    AcquisitionDate = DateTime.Now
                },
                new Book {
                    Title = "Атлант расправил плечи",
                    Author = "Айн Рэнд",
                    Genre = "Философия",
                    Year = 1957,
                    Location = "Зал3-Ст1-Пол3",
                    Status = "Available",
                    ReadingRoomOnly = true,
                    ISBN = "978-5-389-04853-1",
                    AcquisitionDate = DateTime.Now
                }
            };

            context.Books.AddRange(books);
            context.SaveChanges();
        }
    }
}