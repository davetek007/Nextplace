using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nextplace.Functions.Db;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((ctx, config) =>
    {
        var env = ctx.HostingEnvironment.EnvironmentName;
        config.SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
          .AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new ApplicationException("DefaultConnection is not set");
        
        services.AddDbContextFactory<AppDbContext>(
          options => options.UseSqlServer(connectionString, sqlOptions =>
          {
            sqlOptions.EnableRetryOnFailure(5);
          }));

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();