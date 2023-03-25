using HtmlAgilityPack;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.NetworkInformation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LABA3API
{
    public static class ExchangeRate
    {
        /// <summary>
        /// Метод добавления курса валют из API в базу данных
        /// </summary>
        /// <param name="startDate">Начальная дата</param>
        /// <param name="endDate">Конечная дата</param>
        public static void InsertExchangeRates(DateTime startDate, DateTime endDate)
        {
            string connectionString = @"Data Source=.\SQLEXPRESS;Database=LABA3;Integrated Security=SSPI";
            string insertQuery = "";

            using (var client = new HttpClient())
            {
                int currentYear = startDate.Year;
                while (currentYear <= endDate.Year)
                {
                    // получаем исторические данные за указанный период
                    var response = client.GetAsync($"https://www.cnb.cz/en/financial_markets/foreign_exchange_market/exchange_rate_fixing/year.txt?year={currentYear}")
                                         .Result;
                    var htmlContent = response.Content.ReadAsStringAsync().Result;

                    // парсим HTML-страницу
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlContent);

                    // извлекаем строки таблицы
                    string[] rows = htmlDoc.DocumentNode.InnerText.Split('\n');

                    List<string> CurrentsABR = new List<string>();
                    List<int> CurrentsVALUE = new List<int>();
                    // проходим в цикле по строкам таблицы
                    foreach (var rowI in rows)
                    {
                        string[] row = rowI.Split("|");
                        if (row[0] == "Date")
                        {
                            CurrentsVALUE.Clear();
                            CurrentsABR.Clear();

                            // Этот код используется для генерации оператора SQL MERGE для обновления курсов обмена в базе данных.

                            string mergeStatementIntro = "MERGE INTO exchange_rates AS target USING (VALUES (@DATE";
                            string valuedateSection = "(VALUEDATE";
                            string updateSection = "";
                            string columnsToUpdate = "";

                            // Циклический проход всех строк/столбцов входных данных для извлечения соответствующих столбцов и создания компонентов оператора MERGE
                            for (int columnIndex = 1; columnIndex < row.Length; columnIndex++)
                            {
                                // Разбить данные столбца на значение и аббревиатуру
                                string[] tempColumnData = row[columnIndex].Split(' ');

                                // Извлечь и сохранить аббревиатуру и значение в отдельные списки
                                CurrentsABR.Add(tempColumnData[1]);
                                CurrentsVALUE.Add(Convert.ToInt32(tempColumnData[0]));

                                // Добавить аббревиатуру в оператор MERGE введение, а значение - в раздел VALUEDATE
                                mergeStatementIntro += ",@" + tempColumnData[1];
                                valuedateSection += ",VALUE" + tempColumnData[1];

                                // Создать и добавить строку обновления для этого столбца в раздел обновления оператора MERGE
                                updateSection += $"{(columnIndex == 1 ? "" : ",")}target.{tempColumnData[1]}=VALUE{tempColumnData[1]}";

                                // Добавить аббревиатуру столбца в список через запятую для обновления
                                columnsToUpdate += $"{(columnIndex == 1 ? "" : ",")}{tempColumnData[1]}";
                            }

                            // Завершить несколько разделов оператора MERGE
                            valuedateSection += ")";
                            updateSection = $" {updateSection} WHEN NOT MATCHED THEN INSERT (date,{columnsToUpdate}) VALUES {valuedateSection}";
                            mergeStatementIntro += $")) AS source {valuedateSection} ON target.date = VALUEDATE WHEN MATCHED THEN UPDATE SET {updateSection};";

                            // Сохранить полный оператор MERGE в переменной insertQuery
                            insertQuery = mergeStatementIntro;

                        }
                        else
                        {
                            DateTime date = new DateTime();
                            try
                            {
                                // получаем значение ячеки с датой (в первом столбце)
                                date = DateTime.ParseExact(row[0], "dd.MM.yyyy", null);
                            }
                            catch
                            {
                                break;
                            }
                            // проверяем, находится ли дата в указанном периоде
                            if (date >= startDate && date <= endDate)
                            {
                                // сохраняем данные в БД
                                using (var connection = new SqlConnection(connectionString))
                                {
                                    connection.Open();
                                    using (var command = new SqlCommand(insertQuery, connection))
                                    {
                                        command.Parameters.AddWithValue("@Date", date);
                                        for (int i = 1; i < row.Length; i++)
                                        {
                                            command.Parameters.AddWithValue($"@{CurrentsABR[i - 1]}", Convert.ToDouble(row[i].Replace(".", ",")) / CurrentsVALUE[i - 1]);
                                        }
                                        command.ExecuteNonQuery();
                                    }
                                }
                            }



                        }

                    }
                    currentYear++;
                }
            }
        }
    }
    
}
