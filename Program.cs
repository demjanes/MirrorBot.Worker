using Microsoft.Extensions.Options;
using MirrorBot.Worker;
using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data;
using MirrorBot.Worker.Flow;
using MongoDB.Driver;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        //configs
        services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));
        services.Configure<MongoOptions>(context.Configuration.GetSection("Mongo"));

        //Mongo 
        services.AddSingleton<IMongoClient>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return new MongoClient(opt.ConnectionString);
        }); 
        services.AddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(opt.Database);
        });
        
        //registrations
        services.AddHttpClient("telegram").RemoveAllLoggers();

        services.AddSingleton<MirrorBotsRepository>();
        services.AddSingleton<UsersRepository>();
        
        services.AddSingleton<BotFlowService>();
        services.AddSingleton<CommandRouter>();
        
        services.AddHostedService<BotManager>();
        
        
        //логгирование в файл
        services.AddLogging(logging =>
            logging.AddFile("logs/mirrorbot-{Date}.txt",  // ← {Date} вместо -
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} | [{Level:u3}] | {SourceContext} | {Message:lj}:{Exception}{NewLine}",
                fileSizeLimitBytes: 8_388_608,
                retainedFileCountLimit: 31));

    })
    .Build();

await host.RunAsync();
