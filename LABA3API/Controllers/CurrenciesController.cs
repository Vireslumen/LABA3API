using System.Data.SqlClient;
using System.Text.Json;
using LABA3API.Models;
using Microsoft.AspNetCore.Mvc;
using LABA3API;

[ApiController]
[Route("[controller]")]
public class CurrencyExchangeController : ControllerBase
{

    [HttpGet]
    public string Get(string currenciesName, DateTime startDate, DateTime endDate)// Получение запроса GET
    {
        try
        {
            // Вставка курсов обмена из API в базу данных
            ExchangeRate.InsertExchangeRates(startDate, endDate);

            // Инициализация нового списка валютных объектов
            List<Currency> currencies = new List<Currency>();

            int i = 0;
            // Определение строки подключения SQL
            string connectionString = @"Data Source=.\SQLEXPRESS;Database=LABA3;Integrated Security=SSPI";

            // Открытие SQL-соединения с использованием строки подключения
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Определение команды SQL с параметрами
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT {currenciesName} ,date FROM exchange_rates WHERE date >= '{startDate.ToShortDateString()}' AND date <= '{endDate.ToShortDateString()}'";

                    // Выполнение SQL-запроса и получение результатов
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Проход по каждой строке в результатах
                        while (reader.Read())
                        {
                            // Проход по каждому столбцу в строке
                            for (int i = 0; i < reader.FieldCount - 1; i++)
                            {
                                // Если курс обмена равен null, пропустить этот столбец
                                if (reader.IsDBNull(i))
                                {
                                    continue;
                                }
                                // Получение имени валюты и курса обмена из строки
                                string currencyName = reader.GetName(i);
                                double exchangeRate = reader.GetDouble(i);

                                // Проверка, существует ли валюта уже в списке, и обновление ее свойств, если она есть
                                var existingCurrency = currencies.SingleOrDefault(c => c.CurrencyName == currencyName);

                                if (existingCurrency == null)
                                {
                                    currencies.Add(new Currency { CurrencyName = currencyName, MAX = exchangeRate, MIN = exchangeRate, AVG = exchangeRate });
                                }
                                else
                                {
                                    if (exchangeRate > existingCurrency.MAX)
                                    {
                                        existingCurrency.MAX = exchangeRate;
                                    }

                                    if (exchangeRate < existingCurrency.MIN)
                                    {
                                        existingCurrency.MIN = exchangeRate;
                                    }

                                    double sum = existingCurrency.AVG * (currencies.Count - 1) + exchangeRate;
                                    existingCurrency.AVG = sum / currencies.Count;
                                }
                            }
                        }
                    }
                }
            }

            // Сериализация списка валют в JSON и возвращение его в виде строки
            var json = JsonSerializer.Serialize(currencies);
            return json.ToString();
        }
        catch (Exception ex)
        {
            // Возврат любых сообщений об исключениях
            return ex.Message;
        }
    }
}