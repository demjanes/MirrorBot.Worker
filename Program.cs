using Microsoft.Extensions.Options;
using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Flow;
using MirrorBot.Worker.Flow.Handlers;
using MirrorBot.Worker.Services.AdminNotifierService;
using MirrorBot.Worker.Services.TokenEncryption;
using MongoDB.Driver;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        //configs
        services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));
        services.Configure<MongoConfiguration>(context.Configuration.GetSection("Mongo"));
        services.Configure<AdminNotificationsConfiguration>(context.Configuration.GetSection("AdminNotifications"));

        //шифрование токенов пользователей
        services.AddSingleton<ITokenEncryptionService>(sp =>
        {
            var encryptionKey = sp.GetRequiredService<IConfiguration>()
                .GetSection("BotConfiguration")
                .GetValue<string>("EncryptionKey");

            if (string.IsNullOrWhiteSpace(encryptionKey))
                throw new InvalidOperationException("EncryptionKey not configured!");

            return new TokenEncryptionService(encryptionKey);
        });

        //Mongo 
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
        


        //registrations
        services.AddHttpClient("telegram").RemoveAllLoggers();

        services.AddSingleton<MirrorBotsRepository>();
        services.AddSingleton<UsersRepository>();

        services.AddSingleton<BotMessageHandler>();
        services.AddSingleton<BotCallbackHandler>();
        services.AddSingleton<BotFlowService>();       

        services.AddSingleton<BotManager>();
        services.AddSingleton<IBotClientResolver>(sp => sp.GetRequiredService<BotManager>());
        services.AddHostedService(sp => sp.GetRequiredService<BotManager>());


        //////логгирование в тг
        // клиент MAIN бота для админ-уведомлений
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var botToken = sp.GetRequiredService<IOptions<BotConfiguration>>().Value.BotToken;
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("telegram");
            return new TelegramBotClient(new TelegramBotClientOptions(botToken), http);
        });

        services.AddSingleton<IAdminNotifier, TelegramAdminNotifier>();
        services.AddHostedService(sp => (TelegramAdminNotifier)sp.GetRequiredService<IAdminNotifier>());
        //логгирование в файл
        services.AddLogging(logging =>
            logging.AddFile("logs/mirrorbot-{Date}.txt",  // ← {Date} вместо -
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {SourceContext} | {Message:lj}:{Exception}{NewLine}",
                fileSizeLimitBytes: 8_388_608,
                retainedFileCountLimit: 31));      

    })
    .Build();

await host.RunAsync();
