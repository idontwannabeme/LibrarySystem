using Microsoft.EntityFrameworkCore;
using LibrarySystem.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LibrarySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Loan> Loans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Начальные данные
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Email = "reader@library.ru", Password = "123456", FullName = "Иванов Иван", Role = "Reader", StudentId = "ST001", Category = "Студент" },
                new User { Id = 2, Email = "librarian@library.ru", Password = "123456", FullName = "Петрова С.М.", Role = "Librarian", Category = "Сотрудник" },
                new User { Id = 3, Email = "admin@library.ru", Password = "123456", FullName = "Сидоров А.В.", Role = "Admin", Category = "Администратор" },
                new User { Id = 4, Email = "sysadmin@library.ru", Password = "123456", FullName = "Кузнецов Д.С.", Role = "SystemAdmin", Category = "Системный администратор" }
            );

            modelBuilder.Entity<Book>().HasData(
                new Book { Id = 1, Title = "Война и мир", Author = "Лев Толстой", Genre = "Классика", Year = 1869, Location = "Зал1-Ст2-Пол3", Status = "Available", ISBN = "978-5-699-12014-7" },
                new Book { Id = 2, Title = "Преступление и наказание", Author = "Федор Достоевский", Genre = "Классика", Year = 1866, Location = "Зал1-Ст1-Пол2", Status = "Available", ISBN = "978-5-17-090345-2" },
                new Book { Id = 3, Title = "Мастер и Маргарита", Author = "Михаил Булгаков", Genre = "Классика", Year = 1967, Location = "Зал1-Ст3-Пол1", Status = "Reserved", ReadingRoomOnly = true, ISBN = "978-5-389-08266-5" },
                new Book { Id = 4, Title = "1984", Author = "Джордж Оруэлл", Genre = "Антиутопия", Year = 1949, Location = "Зал2-Ст1-Пол4", Status = "Available", ISBN = "978-5-17-080115-4" },
                new Book { Id = 5, Title = "Гарри Поттер и философский камень", Author = "Джоан Роулинг", Genre = "Фэнтези", Year = 1997, Location = "Зал2-Ст2-Пол1", Status = "Issued", ISBN = "978-5-389-04865-4" },
                new Book { Id = 6, Title = "Игра престолов", Author = "Джордж Мартин", Genre = "Фэнтези", Year = 1996, Location = "Зал2-Ст3-Пол2", Status = "Available", ISBN = "978-5-389-06553-8" },
                new Book { Id = 7, Title = "Три товарища", Author = "Эрих Мария Ремарк", Genre = "Классика", Year = 1936, Location = "Зал1-Ст4-Пол1", Status = "Available", ISBN = "978-5-389-07134-8" },
                new Book { Id = 8, Title = "Атлант расправил плечи", Author = "Айн Рэнд", Genre = "Философия", Year = 1957, Location = "Зал3-Ст1-Пол3", Status = "Available", ReadingRoomOnly = true, ISBN = "978-5-389-04853-1" }
            );
        }
    }
}