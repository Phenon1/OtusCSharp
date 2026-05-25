# TCP Key-Value Storage

High-performance TCP key-value storage written in C#/.NET.

Проект реализует собственный binary-safe TCP protocol, in-memory storage и pipeline обработки команд без использования внешних брокеров или HTTP.

---

# Архитектура сервиса

Сервис разделён на несколько независимых компонентов:

```
Client (TCP)
      ↓
Сетевой уровень
      ↓
Парсер протокола
      ↓
Конвейер обработки
      ↓
Ядро хранилища
```

## Схема взаимодействия

- Клиент отправляет TCP-сообщение.
- Сетевой уровень выполняет framing и гарантированное чтение пакета.
- Парсер разбирает payload на команду, ключ и значение.
- Pipeline маршрутизирует команду.
- Storage выполняет операцию.
- Ответ сериализуется и отправляется клиенту.

---

# Компоненты системы

## 1. Сетевой уровень

Отвечает за:

- TCP connections;
- чтение/отправку данных;
- packet framing;
- контроль целостности сообщений.

Используется собственный transport layer:

```csharp
ReadExactAsync()
SendExactAsync()
SendPacketWithLenAsync()
ReceivePacketWithLenAsync()
```

---

## 2. Парсер протокола

Парсер преобразует payload в:

```csharp
CommandKeyValue
{
    Command,
    Key,
    Value
}
```

Поддерживаемые команды:

- `SET`
- `GET`
- `DELETE`

Для уменьшения аллокаций используется:

```csharp
ReadOnlySpan<byte>
```

---

## 3. Конвейер обработки

Pipeline отвечает за:

- маршрутизацию команд;
- десериализацию объектов;
- выполнение операций хранилища;
- формирование ответа клиенту.

---

## 4. Ядро хранилища

В качестве хранилища используется:

```csharp
SimpleStore
```

Поддерживаемые операции:

- Get
- Set
- Delete

Значения сериализуются через бинарный сериализатор, автогенерируемый в GenerateBinarySerializer :

```csharp
SerializeToBinary()
DeserializeFromBinary()
```

---
### Сравнение в бэнчмарке реализованного SourceGenerator и библиотек SystemTextJson и NewtonsoftJson показало :
- 15 кратное превосходство по скорости и 9 кратное по памяти NewtonsoftJson 
- 7 кратное превосходство по скорости и 2 кратное отставание по памяти SystemTextJson

```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.108
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v4 [AttachedDebugger]
  DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v4


```
| Method          | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| NewtonsoftJson  | 1,095.08 ns | 21.280 ns | 28.409 ns | 15.00 |    0.39 | 0.5932 | 0.0038 |    4968 B |        9.55 |
| SystemTextJson  |   516.53 ns |  1.001 ns |  0.836 ns |  7.08 |    0.05 | 0.0315 |      - |     264 B |        0.51 |
| SourceGenerator |    72.99 ns |  0.565 ns |  0.472 ns |  1.00 |    0.01 | 0.0621 |      - |     520 B |        1.00 |



---

# Протокол

Для обмена сообщениями реализован собственный binary-safe protocol поверх TCP.

## Формат пакета

Каждое сообщение передаётся в формате:

```text
[length][payload]
```

где:

- `length` — 4 байта (`Int32`)
- `payload` — содержимое сообщения

---

# Формат payload

Payload содержит:

```text
COMMAND KEY [VALUE]
```

---

# Поддерживаемые команды

## SET

Сохраняет объект в storage.

```text
SET user:1 [binary payload]
```

Пример:

```text
SET user:1 <UserProfile binary>
```

---

## GET

Получает объект по ключу.

```text
GET user:1
```

---

## DELETE

Удаляет объект.

```text
DELETE user:1
```

---

# Формат ответов

Ответы также передаются через length-prefixed framing:

```text
[length][payload]
```

Примеры:

## Успех

```text
OK
```

## Значение отсутствует

```text
(nil)
```

## Ошибка

```text
-ERR Unknown command
```

---

# Особенности реализации

## Binary-safe protocol

Протокол поддерживает передачу произвольных бинарных данных без Base64 encoding.

Parser не изменяет бинарный payload.

---

## Zero-allocation parsing

Используется:

```csharp
ReadOnlySpan<byte>
```

Это позволяет:

- минимизировать GC pressure;
- уменьшить копирование памяти;
- повысить throughput.

---

## TCP framing

Использование length-prefixed protocol решает проблемы TCP:

- fragmentation;
- partial send;
- partial receive;
- packet coalescing.

---


# Нагрузочное тестирование

Для нагрузочного тестирования использовался:

- NBomber

Тестирование включало:

- параллельные TCP connections;
- массовые SET/GET операции;
- проверку корректности бинарной сериализации;
- проверку стабильности transport layer.

## Результаты

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `3000`, ok = `3000`, RPS = `100`|
|latency (ms)|min = `0.64`, mean = `3.66`, max = `42.32`, StdDev = `2.9`|
|latency percentile (ms)|p50 = `2.92`, p75 = `4.29`, p95 = `8.79`, p99 = `14.46`|
|||
|name|`GetBytes`|
|request count|all = `3000`, ok = `3000`, RPS = `100`|
|latency (ms)|min = `0.63`, mean = `3.63`, max = `42.26`, StdDev = `2.9`|
|latency percentile (ms)|p50 = `2.91`, p75 = `4.26`, p95 = `8.78`, p99 = `14.44`|


---

# Технологии

- .NET 8
- TCP Socket API
- Span<T>
- ArrayPool<T>
- NBomber
- Activity / Metrics telemetry

---

# Пример взаимодействия

## SET

```text
Client:
[length][SET user:1 binary_payload]

Server:
[length][OK]
```

---

## GET

```text
Client:
[length][GET user:1]

Server:
[length][binary_payload]
```

---

# Запуск

## Server

```bash
dotnet run --project MainServer
```

## Client

```bash
dotnet run --project HW6Client
```

## NBomber tests

```bash
dotnet run --project NBomber
```
