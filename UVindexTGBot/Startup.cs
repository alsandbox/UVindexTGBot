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

            string? botToken = Configuration["BOT_TOKEN"];
            string? apiKey = Configuration["API_KEY"];
            string? uvUrl = Configuration["UvApiUrl"];

            ValidateConfigurationParameters(botToken, apiKey, uvUrl);

            services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(botToken!));
            services.AddSingleton<BotManager>();
            services.AddSingleton(new ApiManager { BotToken = botToken, ApiKey = apiKey, UvApiUrl = uvUrl });
            services.AddSingleton<LocationService>();
            services.AddSingleton<UvUpdateScheduler>();
            services.AddSingleton<MessageHandler>();
        }

        private void ValidateConfigurationParameters(string? botToken, string? apiKey, string? uvUrl)
        {
            if (botToken is null)
            {
                throw new ArgumentNullException(nameof(botToken), "BOT_TOKEN not provided.");
            }

            if (apiKey is null)
            {
                throw new ArgumentNullException(nameof(apiKey), "API_KEY not provided.");
            }

            if (uvUrl is null)
            {
                throw new ArgumentNullException(nameof(uvUrl), "UvApiUrl not provided.");
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
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
            string startupWebhookUrl = Configuration["WebhookUrl"]
                ?? throw new ArgumentNullException(nameof(startupWebhookUrl), "WebhookUrl not provided.");

            Console.WriteLine($"Setting webhook: {startupWebhookUrl}");

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
