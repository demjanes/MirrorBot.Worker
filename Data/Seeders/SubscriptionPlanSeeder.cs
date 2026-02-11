using MirrorBot.Worker.Data.Models.Subscription;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Seeders
{
    /// <summary>
    /// Заполнение базы тарифными планами при первом запуске.
    /// </summary>
    /// <summary>
    /// Заполнение базы тарифными планами при первом запуске.
    /// </summary>
    public class SubscriptionPlanSeeder
    {
        private readonly ISubscriptionPlanRepository _planRepo;
        private readonly ILogger<SubscriptionPlanSeeder> _logger;

        public SubscriptionPlanSeeder(
            ISubscriptionPlanRepository planRepo,
            ILogger<SubscriptionPlanSeeder> logger)
        {
            _planRepo = planRepo;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting subscription plans seeding...");

            // Проверяем, есть ли уже планы
            var existingPlans = await _planRepo.GetAllAsync(cancellationToken);
            if (existingPlans.Any())
            {
                _logger.LogInformation("Subscription plans already exist. Skipping seed.");
                return;
            }

            var plans = new[]
            {
                // Free Plan
                new SubscriptionPlan
                {
                    Type = SubscriptionType.Free,
                    Name = "Free",
                    Description = "Базовый бесплатный тариф с ограниченными возможностями",
                    PriceRub = 0,
                    PriceUsd = 0,
                    DurationDays = 0, // Бессрочно
                    DailyTextMessageLimit = 10,
                    DailyVoiceMessageLimit = 0,
                    MaxTokensPerRequest = 1000,
                    VoiceResponseEnabled = false,
                    GrammarCorrectionEnabled = false,
                    VocabularyTrackingEnabled = false,
                    PriorityProcessing = false,
                    IsActive = true
                },

                // Premium Monthly
                new SubscriptionPlan
                {
                    Type = SubscriptionType.PremiumMonthly,
                    Name = "Premium - 1 месяц",
                    Description = "Premium подписка на 1 месяц с полным доступом ко всем функциям",
                    PriceRub = 499,
                    PriceUsd = 5.49m,
                    DurationDays = 30,
                    DailyTextMessageLimit = -1, // Безлимит
                    DailyVoiceMessageLimit = -1, // Безлимит
                    MaxTokensPerRequest = 4000,
                    VoiceResponseEnabled = true,
                    GrammarCorrectionEnabled = true,
                    VocabularyTrackingEnabled = true,
                    PriorityProcessing = true,
                    IsActive = true
                },

                // Premium Quarterly
                new SubscriptionPlan
                {
                    Type = SubscriptionType.PremiumQuarterly,
                    Name = "Premium - 3 месяца",
                    Description = "Premium подписка на 3 месяца со скидкой 10%",
                    PriceRub = 1347, // 499*3 = 1497 - 10% = 1347
                    PriceUsd = 14.82m, // 5.49*3 = 16.47 - 10% = 14.82
                    DurationDays = 90,
                    DailyTextMessageLimit = -1,
                    DailyVoiceMessageLimit = -1,
                    MaxTokensPerRequest = 4000,
                    VoiceResponseEnabled = true,
                    GrammarCorrectionEnabled = true,
                    VocabularyTrackingEnabled = true,
                    PriorityProcessing = true,
                    IsActive = true
                },

                // Premium Half Year
                new SubscriptionPlan
                {
                    Type = SubscriptionType.PremiumHalfYear,
                    Name = "Premium - 6 месяцев",
                    Description = "Premium подписка на 6 месяцев со скидкой 20%",
                    PriceRub = 2394, // 499*6 = 2994 - 20% = 2394
                    PriceUsd = 26.34m, // 5.49*6 = 32.94 - 20% = 26.34
                    DurationDays = 180,
                    DailyTextMessageLimit = -1,
                    DailyVoiceMessageLimit = -1,
                    MaxTokensPerRequest = 4000,
                    VoiceResponseEnabled = true,
                    GrammarCorrectionEnabled = true,
                    VocabularyTrackingEnabled = true,
                    PriorityProcessing = true,
                    IsActive = true
                },

                // Premium Yearly
                new SubscriptionPlan
                {
                    Type = SubscriptionType.PremiumYearly,
                    Name = "Premium - 12 месяцев",
                    Description = "Premium подписка на 12 месяцев со скидкой 30% - лучшее предложение!",
                    PriceRub = 4192, // 499*12 = 5988 - 30% = 4192
                    PriceUsd = 46.11m, // 5.49*12 = 65.88 - 30% = 46.11
                    DurationDays = 365,
                    DailyTextMessageLimit = -1,
                    DailyVoiceMessageLimit = -1,
                    MaxTokensPerRequest = 4000,
                    VoiceResponseEnabled = true,
                    GrammarCorrectionEnabled = true,
                    VocabularyTrackingEnabled = true,
                    PriorityProcessing = true,
                    IsActive = true
                }
            };

            foreach (var plan in plans)
            {
                await _planRepo.CreateAsync(plan, cancellationToken);
                _logger.LogInformation(
                    "Created subscription plan: {Name} - {Price}₽ ({Days} days)",
                    plan.Name,
                    plan.PriceRub,
                    plan.DurationDays);
            }

            _logger.LogInformation("Subscription plans seeding completed. Created {Count} plans.", plans.Length);
        }
    }
}
