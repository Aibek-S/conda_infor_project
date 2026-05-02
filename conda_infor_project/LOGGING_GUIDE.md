# Logging System Guide

## Где пишутся логи?

Все логи приложения пишутся в следующую папку:
```
C:\Users\{YourUsername}\AppData\Roaming\conda_infor_project\logs\
```

**Полный путь логов:**
```
%APPDATA%\conda_infor_project\logs\log_YYYY-MM-DD.txt
```

Каждый день создается новый файл логов.

## Как просмотреть логи?

### Способ 1: Открыть папку напрямую
1. Нажмите `Win + R`
2. Введите: `%APPDATA%\conda_infor_project\logs`
3. Нажмите Enter

### Способ 2: Через проводник
1. Откройте File Explorer
2. Перейдите: `C:\Users\{YourUsername}\AppData\Roaming\conda_infor_project\logs\`

## Формат логов

```
[2024-01-15 14:30:45.123] [INFO] Starting login for email: user@example.com
[2024-01-15 14:30:46.456] [INFO] Authentication successful for email: user@example.com
[2024-01-15 14:30:47.789] [ERROR] Login failed for email: user@example.com
  Exception: HttpRequestException
  Message: Connection timeout
  StackTrace: at System.Net.Http.HttpClient.SendAsync...
```

### Уровни логирования:

| Уровень | Описание |
|---------|---------|
| **INFO** | Информационные сообщения о нормальном ходе работы |
| **WARNING** | Предупреждения (например, профиль не найден) |
| **ERROR** | Ошибки с полной информацией об исключении |

## Логируемые события

### Регистрация (RegisterAsync)
- ✅ Начало регистрации: `Starting registration for email: ...`
- ✅ Создание auth аккаунта: `Auth account created with ID: ...`
- ✅ Создание профиля: `User profile created successfully for: ...`
- ❌ Ошибка: `Registration failed for email: ...` с деталями исключения

### Вход (LoginAsync)
- ✅ Начало входа: `Starting login for email: ...`
- ✅ Аутентификация успешна: `Authentication successful for email: ...`
- ✅ Профиль загружен: `User profile loaded for: ...`
- ❌ Ошибка: `Login failed for email: ...` с деталями исключения

### API запросы (AuthRepository)
- ✅ SignUp успешен: `SignUp successful, userId: ...`
- ✅ SignIn успешен: `SignIn successful for email: ...`
- ✅ Профиль создан: `User profile created for userId: ...`
- ✅ Профиль найден: `User profile found for email: ...`
- ⚠️ Профиль не найден: `No user profile found for email: ...`
- ❌ API ошибки: `SignUp failed: ...` / `SignIn failed: ...`

## Устранение неполадок

### Логи не создаются
1. **Проверьте права доступа** на папку `%APPDATA%\`
2. **Переиндексируйте** приложение (очистите Debug папку)
3. **Проверьте консоль** - логи также пишутся в Visual Studio Output

### Логи пусты
1. Запустите операцию (логин или регистрацию)
2. Проверьте файл логов снова

### "Файл не найден" ошибка
- Логирование продолжает работать даже при ошибке записи
- Проверьте Output window в VS

## Интеграция логирования в коде

### Использование Logger

```csharp
using conda_infor_project.services;

// Информационное логирование
Logger.LogInfo("User profile created successfully");

// Логирование ошибок
try 
{
    // код
}
catch (Exception ex)
{
    Logger.LogError("Operation failed", ex);
}

// Предупреждения
Logger.LogWarning("User profile not found");

// Получить путь к логам
string logPath = Logger.GetLogPath();
```

## Полезные команды PowerShell

### Открыть папку логов
```powershell
explorer "$env:APPDATA\conda_infor_project\logs"
```

### Просмотреть последние 20 строк логов
```powershell
Get-Content "$env:APPDATA\conda_infor_project\logs\log_$(Get-Date -Format yyyy-MM-dd).txt" -Tail 20
```

### Удалить старые логи (старше 7 дней)
```powershell
Get-ChildItem "$env:APPDATA\conda_infor_project\logs" -Filter "*.txt" | Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-7)} | Remove-Item
```

## Примеры логов

### Успешная регистрация
```
[2024-01-15 14:25:10.123] [INFO] Starting registration for email: john@example.com
[2024-01-15 14:25:11.456] [INFO] Auth account created with ID: 550e8400-e29b-41d4-a716-446655440000
[2024-01-15 14:25:12.789] [INFO] User profile created successfully for: john@example.com
```

### Ошибка при логине
```
[2024-01-15 14:30:45.123] [INFO] Starting login for email: wrong@example.com
[2024-01-15 14:30:46.456] [ERROR] Login failed for email: wrong@example.com
  Exception: Exception
  Message: Authentication failed: Invalid login credentials
  StackTrace: at conda_infor_project.repository.AuthRepository.SignInAsync(String email, String password)...
```

### Операция с предупреждением
```
[2024-01-15 14:35:20.123] [INFO] Starting login for email: test@example.com
[2024-01-15 14:35:21.456] [INFO] Authentication successful for email: test@example.com
[2024-01-15 14:35:22.789] [WARNING] No user profile found for email: test@example.com
```
