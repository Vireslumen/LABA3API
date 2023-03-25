// Создаем конструктор приложения веб-приложения
using LABA3API;

var webAppBuilder = WebApplication.CreateBuilder(args);

// Добавляем службы контроллеров в контейнер внедрения зависимостей.
webAppBuilder.Services.AddControllers();

// Добавляем службы Swagger в контейнер.
webAppBuilder.Services.AddSwaggerGen();
webAppBuilder.Services.AddEndpointsApiExplorer();

// Создаем приложение веб-приложения
var webApp = webAppBuilder.Build();

// Настраиваем поток HTTP запросов.
if (webApp.Environment.IsDevelopment())
{
    // Включаем Swagger для среды разработки.
    webApp.UseSwagger();
    webApp.UseSwaggerUI();
}

webApp.UseHttpsRedirection();

// Используем промежуточный слой авторизации.
webApp.UseAuthorization();

// Отображаем контроллер маршрутов.
webApp.MapControllers();

//Создать задачу выполняющуюся по времени
JobCreator.CreateJob();
// Запускаем веб-приложение.
webApp.Run();

