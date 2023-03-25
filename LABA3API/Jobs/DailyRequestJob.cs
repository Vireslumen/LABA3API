using HtmlAgilityPack;
using Quartz;
using System.Data.SqlClient;

namespace LABA3API.Jobs
{
    /// <summary>
    /// Ежедневный запрос данных о курсе валют
    /// </summary>
    public class DailyRequestJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            string connectionString = @"Data Source=.\SQLEXPRESS;Database=LABA3;Integrated Security=SSPI";
            string insertQuery = "";
            using (var client = new HttpClient())
            {
                // получаем данные за текущий день 
                var response = client.GetAsync($"https://www.cnb.cz/en/financial_markets/foreign_exchange_market/exchange_rate_fixing/daily.txt")
                                         .Result;
                var htmlContent = response.Content.ReadAsStringAsync().Result;

                // парсим HTML-страницу
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                // извлекаем строки таблицы
                string[] rows = htmlDoc.DocumentNode.InnerText.Split('\n');

                List<string> CurrentsABR = new List<string>();
                List<int> CurrentsVALUE = new List<int>();
                string mergeStatementIntro = "MERGE INTO exchange_rates AS target USING (VALUES (@DATE";
                string valuedateSection = "(VALUEDATE";
                string updateSection = "";
                string columnsToUpdate = "";
                for (int i = 2; i < rows.Length - 1; i++)
                {
                    // Разбить данные строки на ячейки
                    string[] cells = rows[i].Split("|");
                    // Извлечь и сохранить аббревиатуру и значение в отдельные списки
                    CurrentsABR.Add(cells[3]);
                    CurrentsVALUE.Add(Convert.ToInt32(cells[2]));
                    // Добавить аббревиатуру в оператор MERGE введение, а значение - в раздел VALUEDATE
                    mergeStatementIntro += ",@" + cells[3];
                    valuedateSection += ",VALUE" + cells[3];

                    // Создать и добавить строку обновления для этого столбца в раздел обновления оператора MERGE
                    updateSection += $"{(i == 2 ? "" : ",")}target.{cells[3]}=VALUE{cells[3]}";

                    // Добавить аббревиатуру столбца в список через запятую для обновления
                    columnsToUpdate += $"{(i == 2 ? "" : ",")}{cells[3]}";
                }
                // Завершить несколько разделов оператора MERGE
                valuedateSection += ")";
                updateSection = $" {updateSection} WHEN NOT MATCHED THEN INSERT (date,{columnsToUpdate}) VALUES {valuedateSection}";
                mergeStatementIntro += $")) AS source {valuedateSection} ON target.date = VALUEDATE WHEN MATCHED THEN UPDATE SET {updateSection};";

                // Сохранить полный оператор MERGE в переменной insertQuery
                insertQuery = mergeStatementIntro;
                // сохраняем данные в БД
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Date", DateTime.Now.AddHours(-2)); // Переводим время на чешское
                        for (int i = 2; i < rows.Length - 1; i++)
                        {
                            command.Parameters.AddWithValue($"@{CurrentsABR[i - 2]}", Convert.ToDouble(rows[i].Split("|")[4].Replace(".", ",")) / CurrentsVALUE[i - 2]);
                        }
                        command.ExecuteNonQuery();
                    }
                }

            }
            return Task.CompletedTask;
        }
    }
}
