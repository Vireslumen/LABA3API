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
    public string Get(string currenciesName, DateTime startDate, DateTime endDate)// ��������� ������� GET
    {
        try
        {
            // ������� ������ ������ �� API � ���� ������
            ExchangeRate.InsertExchangeRates(startDate, endDate);

            // ������������� ������ ������ �������� ��������
            List<Currency> currencies = new List<Currency>();

            int i = 0;
            // ����������� ������ ����������� SQL
            string connectionString = @"Data Source=.\SQLEXPRESS;Database=LABA3;Integrated Security=SSPI";

            // �������� SQL-���������� � �������������� ������ �����������
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // ����������� ������� SQL � �����������
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT {currenciesName} ,date FROM exchange_rates WHERE date >= '{startDate.ToShortDateString()}' AND date <= '{endDate.ToShortDateString()}'";

                    // ���������� SQL-������� � ��������� �����������
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // ������ �� ������ ������ � �����������
                        while (reader.Read())
                        {
                            // ������ �� ������� ������� � ������
                            for (int i = 0; i < reader.FieldCount - 1; i++)
                            {
                                // ���� ���� ������ ����� null, ���������� ���� �������
                                if (reader.IsDBNull(i))
                                {
                                    continue;
                                }
                                // ��������� ����� ������ � ����� ������ �� ������
                                string currencyName = reader.GetName(i);
                                double exchangeRate = reader.GetDouble(i);

                                // ��������, ���������� �� ������ ��� � ������, � ���������� �� �������, ���� ��� ����
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

            // ������������ ������ ����� � JSON � ����������� ��� � ���� ������
            var json = JsonSerializer.Serialize(currencies);
            return json.ToString();
        }
        catch (Exception ex)
        {
            // ������� ����� ��������� �� �����������
            return ex.Message;
        }
    }
}