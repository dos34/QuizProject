# RabbitMQ с плагином отложенных сообщений — на нём работают таймеры вопросов (MassTransit delayed scheduler).
FROM rabbitmq:4.2-management
ARG DELAYED_PLUGIN_VERSION=4.2.0
ADD --chmod=644 https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_PLUGIN_VERSION}/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez /opt/rabbitmq/plugins/
RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
