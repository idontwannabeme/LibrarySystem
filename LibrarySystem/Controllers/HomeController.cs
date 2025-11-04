using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibrarySystem.Data;
using LibrarySystem.Models;
using System.Security.Cryptography;
using System.Text;

namespace LibrarySystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Вспомогательный метод для проверки роли
        private bool HasAccess(string[] allowedRoles)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return allowedRoles.Contains(userRole);
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
            {
                return View("Login");
            }

            var statistics = new
            {
                TotalBooks = await _context.Books.CountAsync(),
                AvailableBooks = await _context.Books.CountAsync(b => b.Status == "Available"),
                TotalReaders = await _context.Users.CountAsync(u => u.Role == "Reader" && u.IsActive),
                ActiveLoans = await _context.Loans.CountAsync(l => l.Status == "Active")
            };

            ViewBag.Statistics = statistics;
            return View();
        }

        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password && u.IsActive);

            if (user != null)
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.FullName);

                return RedirectToAction("Index");
            }

            ViewBag.Error = "Неверный email или пароль";
            return View();
        }

        public IActionResult Register()
        {
            // Если пользователь уже авторизован, перенаправляем на главную
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, string studentId)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Пользователь с таким email уже существует";
                return View();
            }

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Все поля обязательны для заполнения";
                return View();
            }

            var newUser = new User
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Password = password.Trim(),
                Role = "Reader",
                StudentId = string.IsNullOrWhiteSpace(studentId) ? null : studentId.Trim(),
                Category = "Студент",
                RegistrationDate = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Автоматический вход после регистрации
            HttpContext.Session.SetString("UserId", newUser.Id.ToString());
            HttpContext.Session.SetString("UserEmail", newUser.Email);
            HttpContext.Session.SetString("UserRole", newUser.Role);
            HttpContext.Session.SetString("UserName", newUser.FullName);

            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // 🔍 ФУНКЦИОНАЛ ДЛЯ ЧИТАТЕЛЯ

        public IActionResult Search()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                return RedirectToAction("Login");

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> SearchBooks(string searchTerm = "", string searchBy = "all")
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                return Json(new { success = false, message = "Требуется авторизация" });

            IQueryable<Book> query = _context.Books;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = searchBy switch
                {
                    "title" => query.Where(b => b.Title.ToLower().Contains(searchTerm)),
                    "author" => query.Where(b => b.Author.ToLower().Contains(searchTerm)),
                    "genre" => query.Where(b => b.Genre != null && b.Genre.ToLower().Contains(searchTerm)),
                    _ => query.Where(b =>
                        b.Title.ToLower().Contains(searchTerm) ||
                        b.Author.ToLower().Contains(searchTerm) ||
                        (b.Genre != null && b.Genre.ToLower().Contains(searchTerm)) ||
                        (b.ISBN != null && b.ISBN.Contains(searchTerm)))
                };
            }

            var books = await query.OrderBy(b => b.Title).ToListAsync();
            return Json(books);
        }

        public async Task<IActionResult> Catalog()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                return RedirectToAction("Login");

            var books = await _context.Books.OrderBy(b => b.Title).ToListAsync();
            return View(books);
        }

        [HttpPost]
        public async Task<JsonResult> ReserveBook()
        {
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                    return Json(new { success = false, message = "Требуется авторизация" });

                // Получаем bookId из формы
                var form = await Request.ReadFormAsync();
                if (!int.TryParse(form["bookId"], out int bookId))
                    return Json(new { success = false, message = "Некорректный ID книги" });

                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Json(new { success = false, message = "Пользователь не авторизован" });

                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                    return Json(new { success = false, message = "Книга не найдена" });

                if (book.Status != "Available")
                    return Json(new { success = false, message = "Книга недоступна для бронирования" });

                // Проверяем, нет ли уже активной брони у пользователя
                var existingReservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId && r.Status == "Active");

                if (existingReservation != null)
                    return Json(new { success = false, message = "Вы уже забронировали эту книгу" });

                var reservation = new Reservation
                {
                    BookId = bookId,
                    UserId = userId,
                    ReservationDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(3),
                    Status = "Active"
                };

                book.Status = "Reserved";

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Книга успешно забронирована" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        public async Task<IActionResult> MyBooks()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                return RedirectToAction("Login");

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login");

            var reservations = await _context.Reservations
                .Include(r => r.Book)
                .Where(r => r.UserId == userId && r.Status == "Active")
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            var loans = await _context.Loans
                .Include(l => l.Book)
                .Where(l => l.UserId == userId && l.Status == "Active")
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

            ViewBag.Reservations = reservations;
            ViewBag.Loans = loans;

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> CancelReservation()
        {
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                    return Json(new { success = false, message = "Требуется авторизация" });

                // Получаем reservationId из формы
                var form = await Request.ReadFormAsync();
                if (!int.TryParse(form["reservationId"], out int reservationId))
                    return Json(new { success = false, message = "Некорректный ID бронирования" });

                var reservation = await _context.Reservations
                    .Include(r => r.Book)
                    .FirstOrDefaultAsync(r => r.Id == reservationId);

                if (reservation == null)
                    return Json(new { success = false, message = "Бронь не найдена" });

                // Проверяем, что пользователь отменяет свою бронь
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Json(new { success = false, message = "Пользователь не авторизован" });

                if (reservation.UserId != userId && !HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                    return Json(new { success = false, message = "Вы можете отменять только свои бронирования" });

                // Освобождаем книгу
                reservation.Book.Status = "Available";
                reservation.Status = "Cancelled";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Бронь успешно отменена" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBookDetails(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                book = new
                {
                    title = book.Title,
                    author = book.Author,
                    genre = book.Genre,
                    year = book.Year,
                    isbn = book.ISBN,
                    description = book.Description,
                    location = book.Location,
                    status = book.Status,
                    readingRoomOnly = book.ReadingRoomOnly
                }
            });
        }

        // 👥 ФУНКЦИОНАЛ ДЛЯ СОТРУДНИКОВ И АДМИНИСТРАТОРОВ

        public async Task<IActionResult> Management()
        {
            if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                return RedirectToAction("Index");

            // Статистика для дашборда
            var stats = new
            {
                TotalReaders = await _context.Users.CountAsync(u => u.Role == "Reader" && u.IsActive),
                ActiveReservations = await _context.Reservations.CountAsync(r => r.Status == "Active"),
                ActiveLoans = await _context.Loans.CountAsync(l => l.Status == "Active"),
                OverdueLoans = await _context.Loans.CountAsync(l => l.Status == "Active" && l.DueDate < DateTime.Now)
            };

            ViewBag.Stats = stats;
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> RegisterReader()
        {
            try
            {
                if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                    return Json(new { success = false, message = "Недостаточно прав" });

                // Получаем данные из формы
                var form = await Request.ReadFormAsync();

                string fullName = form["fullName"].ToString();
                string email = form["email"].ToString();
                string studentId = form["studentId"].ToString();
                string category = form["category"].ToString();

                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                    return Json(new { success = false, message = "ФИО и Email обязательны для заполнения" });

                if (await _context.Users.AnyAsync(u => u.Email == email))
                    return Json(new { success = false, message = "Читатель с таким email уже зарегистрирован" });

                var newReader = new User
                {
                    FullName = fullName.Trim(),
                    Email = email.Trim(),
                    Password = "default123",
                    Role = "Reader",
                    StudentId = string.IsNullOrWhiteSpace(studentId) ? null : studentId.Trim(),
                    Category = string.IsNullOrWhiteSpace(category) ? "Студент" : category.Trim(),
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(newReader);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Читатель успешно зарегистрирован" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> AddBook()
        {
            try
            {
                if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                    return Json(new { success = false, message = "Недостаточно прав" });

                // Получаем данные из формы
                var form = await Request.ReadFormAsync();

                string title = form["title"].ToString();
                string author = form["author"].ToString();
                string isbn = form["isbn"].ToString();
                string genre = form["genre"].ToString();
                string yearStr = form["year"].ToString();
                string location = form["location"].ToString();
                string readingRoomOnlyStr = form["readingRoomOnly"].ToString();

                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(location))
                    return Json(new { success = false, message = "Название, автор и местоположение обязательны для заполнения" });

                if (!int.TryParse(yearStr, out int year) || year < 1000 || year > DateTime.Now.Year + 1)
                    return Json(new { success = false, message = "Некорректный год издания" });

                bool readingRoomOnly = readingRoomOnlyStr == "on" || readingRoomOnlyStr == "true";

                var newBook = new Book
                {
                    Title = title.Trim(),
                    Author = author.Trim(),
                    ISBN = string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim(),
                    Genre = string.IsNullOrWhiteSpace(genre) ? null : genre.Trim(),
                    Year = year,
                    Location = location.Trim(),
                    ReadingRoomOnly = readingRoomOnly,
                    Status = "Available",
                    AcquisitionDate = DateTime.Now
                };

                _context.Books.Add(newBook);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Книга успешно добавлена в фонд" });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetReaders()
        {
            if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                return Json(new { success = false, message = "Недостаточно прав" });

            var readers = await _context.Users
                .Where(u => u.Role == "Reader" && u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new {
                    id = u.Id,
                    fullName = u.FullName,
                    email = u.Email,
                    studentId = u.StudentId,
                    category = u.Category,
                    registrationDate = u.RegistrationDate.ToString("dd.MM.yyyy")
                })
                .ToListAsync();

            return Json(new { success = true, readers });
        }

        [HttpGet]
        public async Task<JsonResult> GetActiveReservations()
        {
            if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                return Json(new { success = false, message = "Недостаточно прав" });

            var reservations = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.User)
                .Where(r => r.Status == "Active")
                .OrderByDescending(r => r.ReservationDate)
                .Select(r => new {
                    id = r.Id,
                    bookTitle = r.Book.Title,
                    bookAuthor = r.Book.Author,
                    readerName = r.User.FullName,
                    reservationDate = r.ReservationDate.ToString("dd.MM.yyyy HH:mm"),
                    expiryDate = r.ExpiryDate.Value.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            return Json(new { success = true, reservations });
        }

        [HttpPost]
        public async Task<JsonResult> IssueBook()
        {
            try
            {
                if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                    return Json(new { success = false, message = "Недостаточно прав" });

                // Получаем reservationId из формы
                var form = await Request.ReadFormAsync();
                if (!int.TryParse(form["reservationId"], out int reservationId))
                    return Json(new { success = false, message = "Некорректный ID бронирования" });

                var reservation = await _context.Reservations
                    .Include(r => r.Book)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == reservationId && r.Status == "Active");

                if (reservation == null)
                    return Json(new { success = false, message = "Бронь не найдена" });

                // Создаем запись о выдаче
                var loan = new Loan
                {
                    BookId = reservation.BookId,
                    UserId = reservation.UserId,
                    IssueDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(14), // 14 дней на возврат
                    Status = "Active"
                };

                // Обновляем статус брони и книги
                reservation.Status = "Completed";
                reservation.Book.Status = "Issued";

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Книга '{reservation.Book.Title}' выдана читателю {reservation.User.FullName}"
                });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ReturnBook()
        {
            try
            {
                if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                    return Json(new { success = false, message = "Недостаточно прав" });

                // Получаем loanId из формы
                var form = await Request.ReadFormAsync();
                if (!int.TryParse(form["loanId"], out int loanId))
                    return Json(new { success = false, message = "Некорректный ID выдачи" });

                var loan = await _context.Loans
                    .Include(l => l.Book)
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Id == loanId && l.Status == "Active");

                if (loan == null)
                    return Json(new { success = false, message = "Выдача не найдена" });

                // Возвращаем книгу
                loan.ReturnDate = DateTime.Now;
                loan.Status = "Returned";
                loan.Book.Status = "Available";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Книга '{loan.Book.Title}' возвращена читателем {loan.User.FullName}"
                });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetActiveLoans()
        {
            if (!HasAccess(new[] { "Librarian", "Admin", "SystemAdmin" }))
                return Json(new { success = false, message = "Недостаточно прав" });

            var loans = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .Where(l => l.Status == "Active")
                .OrderBy(l => l.DueDate)
                .Select(l => new {
                    id = l.Id,
                    bookTitle = l.Book.Title,
                    bookAuthor = l.Book.Author,
                    readerName = l.User.FullName,
                    issueDate = l.IssueDate.ToString("dd.MM.yyyy"),
                    dueDate = l.DueDate.ToString("dd.MM.yyyy"),
                    isOverdue = l.DueDate < DateTime.Now
                })
                .ToListAsync();

            return Json(new { success = true, loans });
        }

        // ⚙️ ФУНКЦИОНАЛ ДЛЯ АДМИНИСТРАТОРОВ

        public async Task<IActionResult> Admin()
        {
            if (!HasAccess(new[] { "Admin", "SystemAdmin" }))
                return RedirectToAction("Index");

            // Статистика для админ-панели
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalBooks = await _context.Books.CountAsync(),
                TotalLoans = await _context.Loans.CountAsync(),
                TotalReservations = await _context.Reservations.CountAsync()
            };

            ViewBag.Stats = stats;
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetSystemStats()
        {
            if (!HasAccess(new[] { "Admin", "SystemAdmin" }))
                return Json(new { success = false, message = "Недостаточно прав" });

            var stats = new
            {
                usersByRole = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new { role = g.Key, count = g.Count() })
                    .ToListAsync(),
                booksByStatus = await _context.Books
                    .GroupBy(b => b.Status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToListAsync(),
                monthlyLoans = await _context.Loans
                    .Where(l => l.IssueDate.Year == DateTime.Now.Year && l.IssueDate.Month == DateTime.Now.Month)
                    .CountAsync(),
                popularGenres = await _context.Books
                    .Where(b => b.Genre != null)
                    .GroupBy(b => b.Genre)
                    .Select(g => new { genre = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .Take(5)
                    .ToListAsync()
            };

            return Json(new { success = true, stats });
        }

        [HttpPost]
        public async Task<JsonResult> SetAccessRules()
        {
            try
            {
                if (!HasAccess(new[] { "Admin", "SystemAdmin" }))
                    return Json(new { success = false, message = "Недостаточно прав" });

                // Получаем данные из формы
                var form = await Request.ReadFormAsync();
                if (!int.TryParse(form["bookId"], out int bookId))
                    return Json(new { success = false, message = "Некорректный ID книги" });

                string readingRoomOnlyStr = form["readingRoomOnly"].ToString();
                bool readingRoomOnly = readingRoomOnlyStr == "on" || readingRoomOnlyStr == "true";

                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                    return Json(new { success = false, message = "Книга не найдена" });

                book.ReadingRoomOnly = readingRoomOnly;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Правила доступа для книги '{book.Title}' обновлены"
                });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateUserRole()
        {
            try
            {
                if (!HasAccess(new[] { "Admin", "SystemAdmin" }))
                    return Json(new { success = false, message = "Недостаточно прав" });

                // Получаем данные из формы
                var form = await Request.ReadFormAsync();
                if (!int.TryParse(form["userId"], out int userId))
                    return Json(new { success = false, message = "Некорректный ID пользователя" });

                string newRole = form["newRole"].ToString();

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "Пользователь не найден" });

                user.Role = newRole;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Роль пользователя {user.FullName} изменена на {newRole}"
                });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // 🔧 ФУНКЦИОНАЛ ДЛЯ СИСТЕМНОГО АДМИНИСТРАТОРА
        public IActionResult SystemAdmin()
        {
            if (HttpContext.Session.GetString("UserRole") != "SystemAdmin")
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> DeactivateUser()
        {
            try
            {
                if (HttpContext.Session.GetString("UserRole") != "SystemAdmin")
                    return Json(new { success = false, message = "Недостаточно прав" });

                // Получаем данные из формы
                var form = await Request.ReadFormAsync();
                if (!int.TryParse(form["userId"], out int userId))
                    return Json(new { success = false, message = "Некорректный ID пользователя" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "Пользователь не найден" });

                user.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Пользователь {user.FullName} деактивирован"
                });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Ошибка базы данных: {innerException}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSystemLogs()
        {
            if (HttpContext.Session.GetString("UserRole") != "SystemAdmin")
                return Json(new { success = false, message = "Недостаточно прав" });

            // Логирование действий (в реальной системе было бы в отдельной таблице)
            var logs = new[]
            {
                new { timestamp = DateTime.Now.AddHours(-1).ToString("dd.MM.yyyy HH:mm"), action = "Вход в систему", user = "Петрова С.М.", details = "Успешная аутентификация" },
                new { timestamp = DateTime.Now.AddHours(-2).ToString("dd.MM.yyyy HH:mm"), action = "Выдача книги", user = "Петрова С.М.", details = "Книга 'Война и мир' выдана Иванову И." },
                new { timestamp = DateTime.Now.AddHours(-3).ToString("dd.MM.yyyy HH:mm"), action = "Регистрация читателя", user = "Петрова С.М.", details = "Зарегистрирован новый читатель: Сидоров П.С." },
                new { timestamp = DateTime.Now.AddHours(-4).ToString("dd.MM.yyyy HH:mm"), action = "Добавление книги", user = "Сидоров А.В.", details = "Добавлена новая книга в фонд" }
            };

            return Json(new { success = true, logs });
        }
    }
}