using Microsoft.Extensions.DependencyInjection;

using Telegram.Bot;

namespace UVindexTGBot
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddSwaggerGen();

            string? botToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new ArgumentNullException(nameof(services), "BOT_TOKEN not provided.");
            string? apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? throw new ArgumentNullException(nameof(services), "API_KEY not provided.");
            string? uvUrl = Environment.GetEnvironmentVariable("UvApiUrl") ?? throw new ArgumentNullException(nameof(services), "UvApiUrl not provided.");

            services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(botToken!));
            services.AddSingleton<BotManager>();
            services.AddSingleton(new ApiManager { BotToken = botToken, ApiKey = apiKey, UvApiUrl = uvUrl });
            services.AddSingleton<LocationService>();
            services.AddSingleton<UvUpdateScheduler>();
            services.AddSingleton<MessageHandler>();
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var botClientStartup = app.ApplicationServices.GetRequiredService<ITelegramBotClient>();
            string startupWebhookUrl = Environment.GetEnvironmentVariable("WebhookUrl")
                ?? throw new ArgumentNullException(nameof(app), "WebhookUrl not provided.");

            try
            {
                // Ensure the webhook is set after app startup
                botClientStartup.SetWebhookAsync(startupWebhookUrl).GetAwaiter().GetResult();
                Console.WriteLine("Webhook set successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting webhook: {ex.Message}");
            }
        }
    }
}
