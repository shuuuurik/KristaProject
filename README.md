# KRISTA PROJECT
Данное консольное приложение считывает введенные пользователем параметры __lastUpdateFrom__ и __lastUpdateTo__, после чего постранично скачивает данные в формате json из источника (например, по ссылке https://bus.gov.ru/public-rest/api/epbs/fap?lastUpdateFrom=04.12.2019&lastUpdateTo=10.12.2019&page=0&size=100), добавляет в таблицу my_table запись с данными и перемещает скачанный .json файл в zip архив (./AppData/data.zip)

## Технологии
* C# (.Net Fraemwork 4.7.2, Microsoft Visual Studio 2019)
* PostgreSQL
* Docker

## Docker
Перед началом работы приложения требуется запустить базу Postgres в Docker-контейнере:
```
docker-compose build
docker-compose up
```
_Выполнять команду нужно в основной папке проекта, где лежит файл docker-compose.yaml. Требует установленного на устройстве Docker и инструмента Docker Compose._

## Запуск приложения:
```
./KristaProject/bin/Debug/KristaProject.exe
```
Можно запукскать через среду разработки MS Visual Studio:
```
./KristaProject.sln
```

## Структура таблицы my_table
| Name | Data type | Not NULL | Primary Key |
| :------ | :------: | :------: | :------: |
| last_update_from | date | yes | yes |
| last_update_to | date | yes | yes |
| json_data | json | yes | no |

## Комментари по файловой структуре проекта:
* __AppData__ - папка для скачанных данных (хранятся в zip архиве)
* __db__ - папка для инициализации базы докером (не работает, по крайней мере на Windows)
    * __sqripts__ - папка с sql-скриптом создания таблицы.
* __DevelopmentClasses__ - C# проект, тип: Class Library. Данная библиотека содержит разработанные пользовательские классы.
* __KristaProject__ - основной C# проект, тип: консольное приложение.
* __packages__ - папка со скачанными пакетами, созданная средой разработки.
* __docker-compose.yaml__ - docker compose файл. Разворачивает базу Postgres в контейнере.
* __KristaProject.sln__ - MS Visual Studio 2019 solution - решение с разработанным приложением.
* __Описание проекта.pdf__ текстовое и более детальное описание некоторых аспектов приложения.