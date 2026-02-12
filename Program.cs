using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Telegram.Bot;

// Bot
using MirrorBot.Worker.Bot;

// Configurations
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Configs.Payments;

// Data Layer
using MirrorBot.Worker.Data.Repositories.Implementations;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Data.Seeders;

// Flow & Handlers
using MirrorBot.Worker.Flow;
using MirrorBot.Worker.Flow.Handlers;

// Services
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

Console.WriteLine("=== Начало инициализации ===");

var builder = WebApplication.CreateBuilder(args);

try
{
    // ================================================================
    // 1. КОНФИГУРАЦИИ
    // ================================================================
    builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));
    builder.Services.Configure<LimitsConfiguration>(builder.Configuration.GetSection("Limits"));
    builder.Services.Configure<MongoConfiguration>(builder.Configuration.GetSection("Mongo"));
    builder.Services.Configure<AdminNotificationsConfiguration>(builder.Configuration.GetSection("AdminNotifications"));
    builder.Services.Configure<ReferralConfiguration>(builder.Configuration.GetSection(ReferralConfiguration.SectionName));
    builder.Services.Configure<AIConfiguration>(builder.Configuration.GetSection(AIConfiguration.SectionName));
    builder.Services.Configure<SpeechConfiguration>(builder.Configuration.GetSection(SpeechConfiguration.SectionName));
    builder.Services.Configure<PaymentConfiguration>(builder.Configuration.GetSection(PaymentConfiguration.SectionName));
    builder.Services.Configure<YooKassaConfiguration>(builder.Configuration.GetSection(YooKassaConfiguration.SectionName));

    // ================================================================
    // 2. ИНФРАСТРУКТУРА
    // ================================================================

    // MongoDB (Singleton - connection pool)
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

    // HttpClients
    builder.Services.AddHttpClient("telegram").RemoveAllLoggers();
    builder.Services.AddHttpClient<YandexGPTProvider>();
    builder.Services.AddHttpClient<YandexSpeechKitProvider>();

    // Logging
    builder.Services.AddLogging(logging =>
        logging.AddFile("logs/mirrorbot-{Date}.txt",
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {SourceContext} | {Message:lj}:{Exception}{NewLine}",
            fileSizeLimitBytes: 8_388_608,
            retainedFileCountLimit: 31));

    // ================================================================
    // 3. БАЗОВЫЕ СЕРВИСЫ
    // ================================================================

    // Token Encryption (Singleton - stateless)
    builder.Services.AddSingleton<ITokenEncryptionService>(sp =>
    {
        var encryptionKey = sp.GetRequiredService<IConfiguration>()
            .GetSection("BotConfiguration")
            .GetValue<string>("EncryptionKey");

        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new InvalidOperationException("EncryptionKey not configured!");

        return new TokenEncryptionService(encryptionKey);
    });

    // Cache (Scoped)
    builder.Services.AddScoped<ICacheService, CacheService>();

    // ================================================================
    // 4. РЕПОЗИТОРИИ (Data Access Layer)
    // ================================================================
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

    // ================================================================
    // 5. AI ПРОВАЙДЕРЫ И ФАБРИКИ
    // ================================================================

    // AI Providers (Singleton - stateless)
    builder.Services.AddSingleton<YandexGPTProvider>();
    builder.Services.AddSingleton<YandexSpeechKitProvider>();

    // Factories (Singleton)
    builder.Services.AddSingleton<AIProviderFactory>();
    builder.Services.AddSingleton<SpeechProviderFactory>();
    builder.Services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<AIProviderFactory>().GetProvider());
    builder.Services.AddSingleton<ISpeechProvider>(sp => sp.GetRequiredService<SpeechProviderFactory>().GetProvider());

    // ================================================================
    // 6. БИЗНЕС-СЕРВИСЫ
    // ================================================================

    // English Tutor Services (Scoped)
    builder.Services.AddScoped<GrammarAnalyzer>();
    builder.Services.AddScoped<VocabularyExtractor>();
    builder.Services.AddScoped<ConversationManager>();
    builder.Services.AddScoped<IEnglishTutorService, EnglishTutorService>();

    // Subscription Services (Scoped)
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

    // Referral Services (Scoped)
    builder.Services.AddScoped<IReferralNotificationService, ReferralNotificationService>();
    builder.Services.AddScoped<IReferralService, ReferralService>();

    // Payment Providers (Scoped)
    builder.Services.AddScoped<YooKassaPaymentProvider>();
    builder.Services.AddScoped<PaymentProviderFactory>();

    // Payment Services (Scoped)
    builder.Services.AddScoped<IPaymentService, PaymentService>();

    // ================================================================
    // 7. HANDLERS И FLOW
    // ================================================================
    builder.Services.AddScoped<BotMessageHandler>();
    builder.Services.AddScoped<BotCallbackHandler>();
    builder.Services.AddScoped<BotFlowService>();

    // ================================================================
    // 8. BOT MANAGEMENT
    // ================================================================

    // BotManager (Singleton - lifecycle management)
    builder.Services.AddSingleton<BotManager>();
    builder.Services.AddSingleton<IBotClientResolver>(sp => sp.GetRequiredService<BotManager>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<BotManager>());

    // Telegram Admin Notifier (Singleton - background service)
    builder.Services.AddSingleton<ITelegramBotClient>(sp =>
    {
        var botToken = sp.GetRequiredService<IOptions<BotConfiguration>>().Value.BotToken;
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("telegram");
        return new TelegramBotClient(new TelegramBotClientOptions(botToken), http);
    });
    builder.Services.AddSingleton<IAdminNotifier, TelegramAdminNotifier>();
    builder.Services.AddHostedService(sp => (TelegramAdminNotifier)sp.GetRequiredService<IAdminNotifier>());

    // ================================================================
    // 9. DATA SEEDERS
    // ================================================================
    builder.Services.AddScoped<SubscriptionPlanSeeder>();

    // ================================================================
    // 10. ASP.NET CORE MIDDLEWARE
    // ================================================================
    builder.Services.AddControllers();

    // ================================================================
    // BUILD APPLICATION
    // ================================================================
    var app = builder.Build();
    Console.WriteLine("=== Host built successfully ===");

    // ================================================================
    // SEED DATA
    // ================================================================
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

    // ================================================================
    // CONFIGURE PIPELINE
    // ================================================================
    app.MapControllers();

    // ================================================================
    // RUN APPLICATION
    // ================================================================
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
