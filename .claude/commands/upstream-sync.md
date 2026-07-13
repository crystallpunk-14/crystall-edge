# upstream-sync: Upstream Sync с Wizden_Upstream/stable

Используй эту команду для синхронизации CrystallEdge с `Wizden_Upstream/stable`.

## Шаг 1 — Fetch + создать ветку

```powershell
git fetch Wizden_Upstream stable
git checkout -b ed-DD-MM-YYYY-upstream-sync master
git merge Wizden_Upstream/stable --no-edit
```

Имя ветки: `ed-{день}-{месяц}-{год}-upstream-sync`

## Шаг 2 — Список конфликтов

```powershell
git diff --name-only --diff-filter=U
```

Читай каждый файл до резолва. Никогда не угадывай — смотри обе стороны.

## Шаг 3 — Правила резолва конфликтов

### [Dependency] поля
**Без `readonly`** — новый стандарт апстрима:
```csharp
[Dependency] private IFoo _foo = default!;
// НЕ: [Dependency] private readonly IFoo _foo = default!;
```

### CE добавил зависимость, апстрим её убрал
Сохранить CE поле с комментарием `// CrystallEdge: <причина>`.

### CE закомментировал зависимость + вызовы
Оставить закомментированным (восстановление поля при закомментированных вызовах = warning "unused field").
Добавить `// CrystallEdge: <причина>` если нет.

### Апстрим добавил метод в интерфейс, CE реализация устарела
Обновить CE класс — добавить метод с новой сигнатурой. Пример брать из других upstream реализаций того же интерфейса.

### Modify/delete конфликт (апстрим удалил файл, CE изменил)
Проверить чем апстрим заменил файл:
```powershell
git log Wizden_Upstream/stable --oneline --diff-filter=D -- <path>
git show <commit> --stat
```
Если заменён лучшей версией → принять удаление (`git rm <file>`).
Если нужен для CE фич → оставить CE версию.
В спорных ситуациях - спросить пользователя.

### Нескриптовые файлы
- `.gitignore`: взять апстрим добавления
- PR template: оставить упрощённую CE версию
- XAML UI: объединить — сохранить CE кнопки + взять StyleClasses из апстрима

## Шаг 4 — Завершить мерж

Через GitHub Desktop "Continue Merge" или:
```powershell
git add <files>
git merge --continue
```

## Шаг 5 — Сборка

```powershell
Start-Process -FilePath "dotnet" `
  -ArgumentList "build","Content.Server/Content.Server.csproj","-v","q" `
  -Wait -NoNewWindow `
  -RedirectStandardOutput "build_out.txt" `
  -RedirectStandardError "build_err.txt"

$errs = Get-Content build_err.txt | Select-String "error CS"
Write-Host "ERRORS=$($errs.Count)"
$errs | ForEach-Object { $_.Line }
Get-Content build_out.txt | Select-Object -Last 3
Remove-Item build_out.txt, build_err.txt -ErrorAction SilentlyContinue
```

`Ошибок: 0` = успех. Исправить все `error CS` до коммита.

## Типичные ошибки после синка

| Ошибка | Причина | Исправление |
|--------|---------|-------------|
| `CS0535: does not implement interface member` | Апстрим добавил метод в интерфейс, CE реализация не обновлена | Добавить метод с новой сигнатурой |
| `CS0103: name does not exist` | Апстрим переименовал/удалил что-то используемое в CE | Найти новое имя или CE альтернативу |
| `CS0246: type not found` | Апстрим перенёс тип в другой namespace | Обновить using |

## CE комментарии — конвенция

```csharp
// CrystallEdge: <причина изменения>
... изменённый код ...
// CrystallEdge end
```

Для однострочных изменений:
```csharp
SomeCall(); // CrystallEdge: <причина>
```

Для закомментированного upstream кода:
```csharp
// CrystallEdge: <фича> отключена, <причина>
//[Dependency] private SomeSystem _system = default!;
```

## Важно

- Всегда создавай **новую ветку** — никогда не мержи напрямую в master
- При спорных конфликтах — спрашивать пользователя
- Для каждого конфликтного файла: читать обе стороны, понять намерение CE, потом резолвить
- Сборка должна завершиться с 0 ошибками
