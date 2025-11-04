using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using LibrarySystem.Data;
using LibrarySystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Настройка базы данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем сервис для работы с сессиями (если нужно)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Автоматическое создание базы данных и таблиц
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Создаем базу данных, если её нет
        context.Database.EnsureCreated();

        // Проверяем, есть ли пользователи в базе, если нет - добавляем тестовых
        if (!context.Users.Any())
        {
            // Добавляем тестовых пользователей
            context.Users.AddRange(
                new User
                {
                    FullName = "Тестовый Читатель",
                    Email = "reader@library.ru",
                    Password = "123456", // В реальном приложении хэшируйте пароли!
                    StudentId = "ST001",
                    Role = "Reader",
                    RegistrationDate = DateTime.Now
                },
                new User
                {
                    FullName = "Тестовый Сотрудник",
                    Email = "librarian@library.ru",
                    Password = "123456",
                    StudentId = "EMP001",
                    Role = "Librarian",
                    RegistrationDate = DateTime.Now
                },
                new User
                {
                    FullName = "Тестовый Администратор",
                    Email = "admin@library.ru",
                    Password = "123456",
                    StudentId = "ADM001",
                    Role = "Admin",
                    RegistrationDate = DateTime.Now
                }
            );

            context.SaveChanges();
            Console.WriteLine("Тестовые пользователи добавлены в базу данных");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при создании базы данных");
    }
}

// Настройка конвейера HTTP запросов
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();