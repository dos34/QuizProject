.PHONY: help env up down logs build test restart clean

help:            ## Показать команды
	@grep -E '^[a-z-]+:.*##' $(MAKEFILE_LIST) | awk -F':.*## ' '{printf "  %-10s %s\n", $$1, $$2}'

env:             ## Создать .env из примера, если его нет
	@test -f .env || (cp .env.example .env && echo ".env created from .env.example")

up: env          ## Собрать и поднять всё в docker compose
	docker compose up -d --build

down:            ## Остановить
	docker compose down

clean:           ## Остановить и удалить данные (volumes)
	docker compose down -v

logs:            ## Логи сервисов
	docker compose logs -f api worker realtime

build:           ## Собрать решение
	dotnet build

test:            ## Прогнать тесты
	dotnet test

infra: env       ## Поднять только Mongo + RabbitMQ (для запуска сервисов из IDE)
	docker compose up -d mongo rabbitmq
