# TP Programación III

API en ASP.NET Core y MySQL para jugadores, equipos, entrenadores, estudiantes, cursos y actividades.

## Requisitos

- .NET SDK 9.0.317 (ver `global.json`).
- MySQL en ejecución.
- `dotnet-ef` 9.0.19.

## Base de datos

Creá la base y un usuario con permisos sobre ella desde un cliente MySQL:

```sql
CREATE DATABASE programaciontp3 CHARACTER SET utf8mb4;
CREATE USER 'tp3_app'@'localhost' IDENTIFIED BY 'TU_CLAVE';
GRANT ALL PRIVILEGES ON programaciontp3.* TO 'tp3_app'@'localhost';
```

Si ya existen, usá los datos que tengas configurados. Creá `Tp3prog/appsettings.json` con tu conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=programaciontp3;User=tp3_app;Password=TU_CLAVE;"
  }
}
```

Reemplazá `TU_CLAVE`. Este archivo está excluido de Git.

## Ejecutar

Desde la raíz del repositorio, en PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet tool install --global dotnet-ef --version 9.0.19
dotnet restore Tp3prog/Tp3prog.csproj
dotnet ef database update --project Tp3prog.DAO --startup-project Tp3prog
dotnet run --project Tp3prog --launch-profile http
```

La instalación de `dotnet-ef` se hace una sola vez. La migración crea las tablas; no se aplica automáticamente al iniciar.

Abrí [Swagger](http://localhost:5175/swagger) para usar la API. También hay ejemplos en `Tp3prog/Tp3prog.http`: actualizá las variables con los IDs de los registros que crees.

La base arranca sin datos. `MockDatabase.cs` tiene ejemplos en memoria que no se cargan en MySQL.

## Endpoints

`Players`, `Teams`, `Trainers`, `Students`, `Courses` y `Activities` tienen GET, POST, PUT y DELETE. GET permite listar o consultar por ID.

En POST omití el ID. En PUT podés omitirlo o usar el mismo de la URL. Para actividades, `type` es `0` (Class), `1` (Exam) o `2` (Assignment).

Las relaciones se agregan con PUT y se quitan con DELETE, sin cuerpo:

- `/Courses/{id}/students/{studentId}`
- `/Courses/{id}/activities/{activityId}`
- `/Teams/{id}/players/{playerId}`
- `/Teams/{id}/trainers/{trainerId}`

Una actividad sólo puede estar en un curso. Para cambiarla, quitá primero la asociación anterior. Quitar una relación no elimina los registros.

POST devuelve 201; PUT y DELETE, 204. Datos inválidos o campos desconocidos devuelven 400; registros inexistentes, 404; conflictos de asociación o restricciones de la base, 409.

## Pruebas

```powershell
dotnet test Tp3prog.Tests/Tp3prog.Tests.csproj
```
