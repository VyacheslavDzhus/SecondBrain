# SecondBrain Telegram Bot 🧠

Персональный ассистент-редактор на базе Telegram и Google Gemini.
Бот умеет принимать текстовые, голосовые и фотосообщения, превращать сумбурный поток мыслей в структурированные списки и автоматически пересылать их в нужные топики вашей группы-копилки.

## 🚀 Настройка и запуск

Проект использует многослойную конфигурацию .NET, что позволяет безопасно хранить приватные ключи и токены вне исходного кода.

### 1. Настройка маршрутов (`routing.json`)
Ваши личные ID чатов не хранятся в репозитории (файл `routing.json` добавлен в `.gitignore`).
1. Скопируйте файл `routing.example.json` и назовите его `routing.json`.
2. Замените фейковые ID на ваши реальные `ChatId` и `ThreadId`.

### 2. Хранение токенов

Бот требует два секрета:
- `Telegram:BotToken` (токен от BotFather)
- `Gemini:ApiKey` (токен от Google AI Studio)

#### Вариант А: Локальная разработка (User Secrets)
Самый безопасный способ для локального запуска — встроенный механизм `dotnet user-secrets`. Токены сохранятся в защищенной папке Windows/Linux.

Откройте консоль в папке `SecondBrain.Host` и выполните:
```bash
dotnet user-secrets set "Telegram:BotToken" "ВАШ_ТОКЕН_ТЕЛЕГРАМ"
dotnet user-secrets set "Gemini:ApiKey" "ВАШ_ТОКЕН_GEMINI"
```
После этого просто запустите `dotnet run`.

#### Вариант Б: Запуск на сервере (Environment Variables)
На боевом сервере или в Docker контейнере используйте переменные окружения. .NET автоматически прочитает их и подставит в конфигурацию.

Linux/Mac:
```bash
export Telegram__BotToken="ВАШ_ТОКЕН_ТЕЛЕГРАМ"
export Gemini__ApiKey="ВАШ_ТОКЕН_GEMINI"
dotnet run
```

Windows (PowerShell):
```powershell
$env:Telegram__BotToken="ВАШ_ТОКЕН_ТЕЛЕГРАМ"
$env:Gemini__ApiKey="ВАШ_ТОКЕН_GEMINI"
dotnet run
```

В Docker Compose это выглядит так:
```yaml
environment:
  - Telegram__BotToken=ВАШ_ТОКЕН_ТЕЛЕГРАМ
  - Gemini__ApiKey=ВАШ_ТОКЕН_GEMINI
```

## 🛠 Архитектура
- **Domain:** Модели данных
- **Core:** Бизнес-логика и интерфейсы
- **Telegram:** Работа с Telegram Bot API
- **Integrations:** Подключение к Gemini (Google.GenAI)
- **Host:** Точка входа (IHostedService), DI и конфигурация
