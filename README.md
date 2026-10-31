# TP Programación III

API en ASP.NET Core y MySQL para jugadores, equipos, entrenadores, estudiantes, cursos y actividades. Incluye login con BCrypt/JWT y un cliente Angular.

## Requisitos

- .NET SDK 9.0.317 (ver `global.json`).
- MySQL en ejecución.
- `dotnet-ef` 9.0.19.
- Node.js compatible con Angular 21 y npm para el cliente.

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
$jwtBytes = New-Object byte[] 32
$jwtRandom = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtRandom.GetBytes($jwtBytes)
$env:Jwt__Key = [Convert]::ToBase64String($jwtBytes)
$jwtRandom.Dispose()
$env:Jwt__Issuer = "Tp3prog"
$env:Jwt__Audience = "Tp3progClient"
$env:Jwt__ExpirationMinutes = "60"
dotnet tool install --global dotnet-ef --version 9.0.19
dotnet restore Tp3prog/Tp3prog.csproj
dotnet ef database update --project Tp3prog.DAO --startup-project Tp3prog
dotnet run --project Tp3prog --launch-profile http
```

La instalación de `dotnet-ef` se hace una sola vez. Las migraciones crean las tablas, incluida `Users`; no se aplican automáticamente al iniciar.

Las variables JWT deben estar en la misma terminal donde ejecutás las migraciones y la API. La clave debe tener al menos 32 bytes UTF-8 y no se guarda en Git. Si la generás de nuevo, los tokens anteriores dejan de servir. `Issuer` y `Audience` son obligatorios; la duración predeterminada es de 60 minutos.

Abrí [Swagger](http://localhost:5175/swagger) para usar la API. También hay ejemplos en `Tp3prog/Tp3prog.http`: actualizá las variables con los IDs de los registros que crees.

La base arranca sin datos. `MockDatabase.cs` tiene ejemplos en memoria que no se cargan en MySQL.

En otra terminal, iniciá Angular:

```powershell
cd tp3prog-client
npm install
npm start
```

Abrí [el login](http://localhost:4200/login). El servidor de desarrollo redirige las solicitudes de la API a `http://localhost:5175` mediante el proxy del cliente.

## Login

Primero creá un usuario desde `POST /User` en Swagger o desde el archivo `.http`, con tu propio email y contraseña. La contraseña debe tener al menos 8 caracteres y ocupar como máximo 72 bytes UTF-8. Sólo se almacena su hash BCrypt.

| Solicitud | Resultado |
| --- | --- |
| `POST /User` con `{email,password}` | `201` con `{id,email}`; email repetido: `409` |
| `POST /User/login` con `{email,password}` | `200` con `{token,expiresAt}`; credenciales incorrectas: `401` |
| `GET /User/me` con `Authorization: Bearer <token>` | `200` con `{id,email}`; token ausente, inválido o vencido: `401` |
| `GET /User/{id}` con Bearer | `200` con `{id,email}` de tu cuenta |
| `PUT /User/{id}` con Bearer y `{email,currentPassword,newPassword?}` | `204`; cambia tu email y, si la enviás, tu contraseña |
| `DELETE /User/{id}` con Bearer y `{currentPassword}` | `204`; elimina tu cuenta |

Consultar, modificar o eliminar por ID requiere el token de esa misma cuenta; un ID ajeno devuelve `403`. Para modificar o eliminar, `currentPassword` debe coincidir con la contraseña actual (`401` si no coincide). Omitir `newPassword` o enviarla como `null` conserva la contraseña; si enviás una nueva, debe cumplir los mismos límites del registro. Un email repetido devuelve `409`. Si otra solicitud cambia la contraseña o elimina la cuenta entre la verificación y la escritura, la operación también devuelve `409`.

Los cuerpos inválidos o con campos desconocidos devuelven `400`. Las respuestas nunca incluyen la contraseña ni su hash. Las rutas de consulta y gestión de cuentas requieren autenticación; los seis CRUD de gestión siguen siendo públicos.

Al enviar el formulario reactivo, Angular arma un `LoginModel` con `email` y `password`. `UserService` lo envía por HTTP a `/User/login`. La API recibe el `LoginDTO`, consulta al usuario con EF y verifica la contraseña con BCrypt; si coincide, firma el JWT y devuelve el token y su vencimiento. Angular lo comprueba consultando `/User/me` con el token como Bearer y accede directamente al panel. El token se usa internamente y no se muestra en pantalla.

El token queda sólo en memoria: recargar la página o cerrar sesión obliga a entrar otra vez. Cerrar sesión limpia el cliente; no revoca el JWT en el servidor. Crear el usuario genera el hash; iniciar sesión verifica ese hash, sin volver a guardar la contraseña.

Cambiar el email o la contraseña no revoca los tokens ya emitidos. Al eliminar la cuenta, `/User/me` y las operaciones sobre esa cuenta dejan de aceptarlos.

La firma de un método identifica su nombre y parámetros. La firma criptográfica del JWT permite detectar cambios en el token; son conceptos distintos. El JWT no contiene la contraseña y su contenido no está cifrado.

## Endpoints

`Players`, `Teams`, `Trainers`, `Students`, `Courses` y `Activities` tienen GET, POST, PUT y DELETE. GET permite listar o consultar por ID.

En POST omití el ID. En PUT podés omitirlo o usar el mismo de la URL. Para actividades, `type` es `0` (Class), `1` (Exam) o `2` (Assignment).

Antes de crear un equipo, registrá sus jugadores; antes de crear un curso, registrá sus alumnos. El alta necesita al menos un integrante existente:

```http
POST /Teams
Content-Type: application/json

{"name":"Los Halcones","category":"Primera","playerIds":[1]}

###
POST /Courses
Content-Type: application/json

{"name":"Programación III","studentIds":[3]}
```

Usá los IDs reales de tus registros. Las listas ausentes, nulas, vacías o con IDs no positivos devuelven `400`; si algún integrante no existe, devuelve `404` y no se crea el equipo o curso. Los IDs repetidos se toman una sola vez. El formulario de Angular permite seleccionarlos. PUT sigue actualizando sólo los campos del registro, sin cambiar sus integrantes.

Las relaciones se agregan con PUT y se quitan con DELETE, sin cuerpo:

- `/Courses/{id}/students/{studentId}`
- `/Courses/{id}/activities/{activityId}`
- `/Teams/{id}/players/{playerId}`
- `/Teams/{id}/trainers/{trainerId}`

Una actividad sólo puede estar en un curso. Para cambiarla, quitá primero la asociación anterior. Quitar una relación no elimina los registros.

Cada equipo debe conservar al menos un jugador y cada curso al menos un alumno. Quitar al último devuelve `409`. También se rechaza eliminar un jugador o alumno si dejaría vacío cualquiera de sus equipos o cursos; primero agregá un reemplazo o eliminá ese equipo o curso. Eliminar un equipo o curso conserva a sus integrantes. Estas operaciones usan transacciones y bloqueos para comprobar el mínimo antes de guardar.

Los registros vacíos que ya existieran en la base deben completarse con las rutas de asociación; este cambio no agrega integrantes ni modifica datos anteriores automáticamente.

POST devuelve 201; PUT y DELETE, 204. Datos inválidos o campos desconocidos devuelven 400; registros inexistentes, 404; conflictos de asociación o restricciones de la base, 409.

## Pruebas

```powershell
dotnet test Tp3prog.Tests/Tp3prog.Tests.csproj
```
