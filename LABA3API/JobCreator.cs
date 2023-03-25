using LABA3API.Jobs;
using Quartz.Impl;
using Quartz;

namespace LABA3API
{
    public static class JobCreator
    {
        public static async void CreateJob()
        {
            // Создаем фабрику задач
            var schedulerFactory = new StdSchedulerFactory();

            // Получаем планировщик
            var scheduler = await schedulerFactory.GetScheduler();

            // Создаем задачу
            var job = JobBuilder.Create<DailyRequestJob>().Build();

            // Получаем расписание из конфигурации
            Console.Write("Введите время ежедневного считывания курса через ':': ");
            string cronExpression = Console.ReadLine();
            cronExpression = "0  " + cronExpression.Split(":")[1] + " " + cronExpression.Split(":")[0] + " * * ?";
            // Создаем расписание выполнения задачи
            var trigger = TriggerBuilder.Create()
                .WithCronSchedule(cronExpression)
                .Build();

            // Регистрируем задачу в планировщике
            await scheduler.ScheduleJob(job, trigger);

            // Запускаем планировщик
            await scheduler.Start();
        }
    }
}
