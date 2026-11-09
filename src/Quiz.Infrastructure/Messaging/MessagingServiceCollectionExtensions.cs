using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Quiz.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <param name="serviceName">Префикс имён очередей: у каждого сервиса свои очереди → каждый получает свою копию события.</param>
    /// <param name="configureConsumers">Регистрация consumer'ов конкретного сервиса.</param>
    public static IServiceCollection AddQuizMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.Section));

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, false));

            // Отложенные сообщения (таймеры вопросов) через плагин rabbitmq_delayed_message_exchange.
            x.AddDelayedMessageScheduler();

            configureConsumers?.Invoke(x);

            x.AddConfigureEndpointsCallback((_, cfg) =>
            {
                // Экспоненциальный retry: 200ms → ... → 10s, 5 попыток. Затем сообщение уходит в <queue>_error (DLQ).
                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(500));
                    r.Ignore<PoisonMessageException>();
                });
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                cfg.Host(options.Host, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.UseDelayedMessageScheduler();
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
