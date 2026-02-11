using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Configs.Payments;
using MirrorBot.Worker.Data.Repositories.Implementations;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Data.Seeders;
using MirrorBot.Worker.Flow;
using MirrorBot.Worker.Flow.Handlers;
using MirrorBot.Worker.Services.AdminNotifierService;
using MirrorBot.Worker.Services.AI;
using MirrorBot.Worker.Services.AI.Implementations;
using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.AI.Providers.YandexGPT;
using MirrorBot.Worker.Services.English;
using MirrorBot.Worker.Services.Payments;
using MirrorBot.Worker.Services.Payments.Providers;
using MirrorBot.Worker.Services.Payments.Providers.YooKassa;
using MirrorBot.Worker.Services.Referral;
using MirrorBot.Worker.Services.Subscr;
using MirrorBot.Worker.Services.TokenEncryption;
using MongoDB.Driver;
using Telegram.Bot;

Console.WriteLine("=== Начало инициализации ===");

// ✅ ИЗМЕНЕНО: Используем WebApplicationBuilder вместо HostBuilder
var builder = WebApplication.CreateBuilder(args);

try
{
    // ============ Конфигурации ============
    builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));
    builder.Services.Configure<LimitsConfiguration>(builder.Configuration.GetSection("Limits"));
    builder.Services.Configure<MongoConfiguration>(builder.Configuration.GetSection("Mongo"));
    builder.Services.Configure<AdminNotificationsConfiguration>(builder.Configuration.GetSection("AdminNotifications"));
    builder.Services.Configure<ReferralConfiguration>(builder.Configuration.GetSection(ReferralConfiguration.SectionName));
    builder.Services.Configure<AIConfiguration>(builder.Configuration.GetSection(AIConfiguration.SectionName));
    builder.Services.Configure<SpeechConfiguration>(builder.Configuration.GetSection(SpeechConfiguration.SectionName));

    builder.Services.Configure<PaymentConfiguration>(builder.Configuration.GetSection(PaymentConfiguration.SectionName));
    builder.Services.Configure<YooKassaConfiguration>(builder.Configuration.GetSection(YooKassaConfiguration.SectionName));

    // ============ HttpClient для AI провайдеров ============
    builder.Services.AddHttpClient<YandexGPTProvider>();
    builder.Services.AddHttpClient<YandexSpeechKitProvider>();

    // ============ Регистрация AI провайдеров (Singleton - stateless) ============
    builder.Services.AddSingleton<YandexGPTProvider>();
    builder.Services.AddSingleton<YandexSpeechKitProvider>();

    // ============ Фабрики (Singleton) ============
    builder.Services.AddSingleton<AIProviderFactory>();
    builder.Services.AddSingleton<SpeechProviderFactory>();
    builder.Services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<AIProviderFactory>().GetProvider());
    builder.Services.AddSingleton<ISpeechProvider>(sp => sp.GetRequiredService<SpeechProviderFactory>().GetProvider());

    // ============ English Tutor Services (Scoped - зависят от репозиториев) ============
    builder.Services.AddScoped<GrammarAnalyzer>();
    builder.Services.AddScoped<VocabularyExtractor>();
    builder.Services.AddScoped<ConversationManager>();
    builder.Services.AddScoped<IEnglishTutorService, EnglishTutorService>();

    // ============ Cache service (Scoped) ============
    builder.Services.AddScoped<ICacheService, CacheService>();

    // ============ Шифрование токенов (Singleton - stateless) ============
    builder.Services.AddSingleton<ITokenEncryptionService>(sp =>
    {
        var encryptionKey = sp.GetRequiredService<IConfiguration>()
            .GetSection("BotConfiguration")
            .GetValue<string>("EncryptionKey");

        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new InvalidOperationException("EncryptionKey not configured!");

        return new TokenEncryptionService(encryptionKey);
    });

    // ============ MongoDB (Singleton - connection) ============
    builder.Services.AddSingleton<IMongoClient>(sp =>
    {
        var opt = sp.GetRequiredService<IOptions<MongoConfiguration>>().Value;
        return new MongoClient(opt.ConnectionString);
    });
    builder.Services.AddSingleton(sp =>
    {
        var opt = sp.GetRequiredService<IOptions<MongoConfiguration>>().Value;
        var client = sp.GetRequiredService<IMongoClient>();
        return client.GetDatabase(opt.Database);
    });

    // ============ HttpClient для Telegram ============
    builder.Services.AddHttpClient("telegram").RemoveAllLoggers();

    // ============ Репозитории (Scoped - работают с БД) ============
    builder.Services.AddScoped<IMirrorBotsRepository, MirrorBotsRepository>();
    builder.Services.AddScoped<IUsersRepository, UsersRepository>();
    builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
    builder.Services.AddScoped<IVocabularyRepository, VocabularyRepository>();
    builder.Services.AddScoped<IUserProgressRepository, UserProgressRepository>();
    builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
    builder.Services.AddScoped<IUsageStatsRepository, UsageStatsRepository>();
    builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
    builder.Services.AddScoped<ICacheRepository, CacheRepository>();
    builder.Services.AddScoped<IReferralStatsRepository, ReferralStatsRepository>();
    builder.Services.AddScoped<IReferralTransactionRepository, ReferralTransactionRepository>();
    builder.Services.AddScoped<IMirrorBotOwnerSettingsRepository, MirrorBotOwnerSettingsRepository>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

    // ============ Сервисы подписок (Scoped - зависят от репозиториев) ============
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

    // ============ Referral services (Scoped - зависят от репозиториев) ============
    builder.Services.AddScoped<IReferralNotificationService, ReferralNotificationService>();
    builder.Services.AddScoped<IReferralService, ReferralService>();

    // ✅ ДОБАВЬ ЭТУ СЕКЦИЮ
    // ============ Payment providers (Scoped - используют HttpClient и конфиги) ============
    builder.Services.AddScoped<YooKassaPaymentProvider>();
    builder.Services.AddScoped<PaymentProviderFactory>();

    // ============ Payment services (Scoped - зависят от репозиториев) ============
    builder.Services.AddScoped<IPaymentService, PaymentService>();

    // ============ Handlers (Scoped - зависят от сервисов и репозиториев) ============
    builder.Services.AddScoped<BotMessageHandler>();
    builder.Services.AddScoped<BotCallbackHandler>();
    builder.Services.AddScoped<BotFlowService>();

    // ============ BotManager (Singleton - управляет lifecycle ботов) ============
    builder.Services.AddSingleton<BotManager>();
    builder.Services.AddSingleton<IBotClientResolver>(sp => sp.GetRequiredService<BotManager>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<BotManager>());

    // ============ Логгирование в Telegram ============
    builder.Services.AddSingleton<ITelegramBotClient>(sp =>
    {
        var botToken = sp.GetRequiredService<IOptions<BotConfiguration>>().Value.BotToken;
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("telegram");
        return new TelegramBotClient(new TelegramBotClientOptions(botToken), http);
    });

    builder.Services.AddSingleton<IAdminNotifier, TelegramAdminNotifier>();
    builder.Services.AddHostedService(sp => (TelegramAdminNotifier)sp.GetRequiredService<IAdminNotifier>());

    // ============ Логгирование в файл ============
    builder.Services.AddLogging(logging =>
        logging.AddFile("logs/mirrorbot-{Date}.txt",
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {SourceContext} | {Message:lj}:{Exception}{NewLine}",
            fileSizeLimitBytes: 8_388_608,
            retainedFileCountLimit: 31));

    // ============ Data Seeders ============
    builder.Services.AddScoped<SubscriptionPlanSeeder>();

    // ✅ ДОБАВЛЕНО: Minimal API для webhook
    builder.Services.AddControllers();

    var app = builder.Build();

    Console.WriteLine("=== Host built successfully ===");

    // ============ Запуск seeder'ов ============
    Console.WriteLine("=== Starting seeders ===");
    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<SubscriptionPlanSeeder>();
        await seeder.SeedAsync();
        Console.WriteLine("=== Seeding completed ===");
    }
    catch (Exception ex)
    {
        Console.WriteLine("=== ERROR DURING SEEDING ===");
        Console.WriteLine(ex.ToString());
    }

    // ✅ ДОБАВЛЕНО: Настройка Minimal API routes
    app.MapControllers();

    Console.WriteLine("=== Starting host ===");
    await app.RunAsync();
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
