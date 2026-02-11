using Microsoft.Extensions.Options;
using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Models.Subscription;
using MirrorBot.Worker.Data.Repositories.Implementations;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Flow;
using MirrorBot.Worker.Flow.Handlers;
using MirrorBot.Worker.Services.AdminNotifierService;
using MirrorBot.Worker.Services.AI;
using MirrorBot.Worker.Services.AI.Implementations;
using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.AI.Providers.OpenAI;
using MirrorBot.Worker.Services.AI.Providers.YandexGPT;
using MirrorBot.Worker.Services.English;
using MirrorBot.Worker.Services.Referral;
using MirrorBot.Worker.Services.Subscr;
using MirrorBot.Worker.Services.TokenEncryption;
using MongoDB.Driver;
using Telegram.Bot;

Console.WriteLine("=== Начало инициализации ===");

IHost host;
try
{
    host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // ============ Конфигурации ============
        services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));
        services.Configure<LimitsConfiguration>(context.Configuration.GetSection("Limits"));
        services.Configure<MongoConfiguration>(context.Configuration.GetSection("Mongo"));
        services.Configure<AdminNotificationsConfiguration>(context.Configuration.GetSection("AdminNotifications"));
        services.Configure<ReferralConfiguration>(context.Configuration.GetSection(ReferralConfiguration.SectionName));
        services.Configure<AIConfiguration>(context.Configuration.GetSection(AIConfiguration.SectionName));
        services.Configure<SpeechConfiguration>(context.Configuration.GetSection(SpeechConfiguration.SectionName));

        // ============ HttpClient для AI провайдеров ============
        services.AddHttpClient<YandexGPTProvider>();
        services.AddHttpClient<YandexSpeechKitProvider>();
        //services.AddHttpClient<OpenAIProvider>();
        //services.AddHttpClient<WhisperProvider>();

        // ============ Регистрация AI провайдеров (Singleton - stateless) ============
        services.AddSingleton<YandexGPTProvider>();
        services.AddSingleton<YandexSpeechKitProvider>();
        //services.AddSingleton<OpenAIProvider>();
        //services.AddSingleton<WhisperProvider>();

        // ============ Фабрики (Singleton) ============
        services.AddSingleton<AIProviderFactory>();
        services.AddSingleton<SpeechProviderFactory>();
        services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<AIProviderFactory>().GetProvider());
        services.AddSingleton<ISpeechProvider>(sp => sp.GetRequiredService<SpeechProviderFactory>().GetProvider());

        // ============ English Tutor Services (Scoped - зависят от репозиториев) ============
        services.AddScoped<GrammarAnalyzer>();
        services.AddScoped<VocabularyExtractor>();
        services.AddScoped<ConversationManager>();
        services.AddScoped<IEnglishTutorService, EnglishTutorService>();

        // ============ Cache service (Singleton - stateless) ============
        services.AddScoped<ICacheService, CacheService>();

        // ============ Шифрование токенов (Singleton - stateless) ============
        services.AddSingleton<ITokenEncryptionService>(sp =>
        {
            var encryptionKey = sp.GetRequiredService<IConfiguration>()
                .GetSection("BotConfiguration")
                .GetValue<string>("EncryptionKey");

            if (string.IsNullOrWhiteSpace(encryptionKey))
                throw new InvalidOperationException("EncryptionKey not configured!");

            return new TokenEncryptionService(encryptionKey);
        });

        // ============ MongoDB (Singleton - connection) ============
        services.AddSingleton<IMongoClient>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<MongoConfiguration>>().Value;
            return new MongoClient(opt.ConnectionString);
        });
        services.AddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<MongoConfiguration>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(opt.Database);
        });

        // ============ HttpClient для Telegram ============
        services.AddHttpClient("telegram").RemoveAllLoggers();

        // ============ Репозитории (Scoped - работают с БД) ============
        services.AddScoped<IMirrorBotsRepository, MirrorBotsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IVocabularyRepository, VocabularyRepository>();
        services.AddScoped<IUserProgressRepository, UserProgressRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IUsageStatsRepository, UsageStatsRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<ICacheRepository, CacheRepository>();
        services.AddScoped<IReferralStatsRepository, ReferralStatsRepository>();
        services.AddScoped<IReferralTransactionRepository, ReferralTransactionRepository>();
        services.AddScoped<IMirrorBotOwnerSettingsRepository, MirrorBotOwnerSettingsRepository>();

        // ============ Сервисы подписок (Scoped - зависят от репозиториев) ============
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // ============ Referral services (Scoped - зависят от репозиториев) ============
        services.AddScoped<IReferralNotificationService, ReferralNotificationService>();
        services.AddScoped<IReferralService, ReferralService>();

        // ============ Handlers (Scoped - зависят от сервисов и репозиториев) ============
        services.AddScoped<BotMessageHandler>();
        services.AddScoped<BotCallbackHandler>();
        services.AddScoped<BotFlowService>();

        // ============ BotManager (Singleton - управляет lifecycle ботов) ============
        services.AddSingleton<BotManager>();
        services.AddSingleton<IBotClientResolver>(sp => sp.GetRequiredService<BotManager>());
        services.AddHostedService(sp => sp.GetRequiredService<BotManager>());

        // ============ Логгирование в Telegram ============
        // Клиент MAIN бота для админ-уведомлений
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var botToken = sp.GetRequiredService<IOptions<BotConfiguration>>().Value.BotToken;
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("telegram");
            return new TelegramBotClient(new TelegramBotClientOptions(botToken), http);
        });

        services.AddSingleton<IAdminNotifier, TelegramAdminNotifier>();
        services.AddHostedService(sp => (TelegramAdminNotifier)sp.GetRequiredService<IAdminNotifier>());

        // ============ Логгирование в файл ============
        services.AddLogging(logging =>
            logging.AddFile("logs/mirrorbot-{Date}.txt",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {SourceContext} | {Message:lj}:{Exception}{NewLine}",
                fileSizeLimitBytes: 8_388_608,
                retainedFileCountLimit: 31));



        // ============ Data Seeders ============
        services.AddScoped<SubscriptionPlanSeeder>();
    })
    .Build();

    Console.WriteLine("=== Host built successfully ===");
}
catch (Exception ex)
{
    Console.WriteLine("=== ERROR DURING BUILD ===");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    throw;
}


// ============ Запуск seeder'ов ============
try
{
    using var scope = host.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<SubscriptionPlanSeeder>();
    await seeder.SeedAsync();
    Console.WriteLine("=== Seeding completed ===");
}
catch (Exception ex)
{
    Console.WriteLine("=== ERROR DURING SEEDING ===");
    Console.WriteLine(ex.ToString());
    // Продолжаем работу даже если seeding не удался
}

try
{
    await host.RunAsync();
    Console.WriteLine("=== host.RunAsync() завершен ===");
}
catch (Exception ex)
{
    Console.WriteLine("=== FATAL ERROR ===");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    throw;
}



try
{
    await host.RunAsync();
    Console.WriteLine("=== host.RunAsync() завершен ===");
}
catch (Exception ex)
{
    Console.WriteLine("=== FATAL ERROR ===");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    throw;
}


