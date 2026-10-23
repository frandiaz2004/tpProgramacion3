# TP Programación III

API ASP.NET Core con persistencia en MySQL mediante Entity Framework Core. Gestiona jugadores, equipos, entrenadores, estudiantes, cursos y actividades.

## Requisitos

- .NET SDK **9.0.317**. `global.json` permite versiones de parche posteriores de la misma banda 9.0.3xx.
- Un servidor MySQL accesible y una base de datos `programaciontp3`.
- Un usuario de MySQL con permisos para crear/modificar tablas y leer/escribir en esa base.
- Herramienta `dotnet-ef` 9.0.19 para aplicar las migraciones.

Los ejemplos siguientes se ejecutan en PowerShell desde la raíz del repositorio, donde están las carpetas `Tp3prog`, `Tp3prog.DAO` y `Tp3prog.Tests`. No requieren un archivo de solución.

## Configurar la base de datos

Creá una base vacía en tu servidor MySQL y otorgale permisos a un usuario específico del proyecto. No uses la cuenta administrativa para ejecutar la aplicación.

Por ejemplo, desde un cliente MySQL con una cuenta administradora, para un servidor local:

```sql
CREATE DATABASE IF NOT EXISTS programaciontp3 CHARACTER SET utf8mb4;
CREATE USER 'tp3_app'@'localhost' IDENTIFIED BY 'REEMPLAZAR_POR_TU_CLAVE';
GRANT ALL PRIVILEGES ON programaciontp3.* TO 'tp3_app'@'localhost';
```

Si el usuario ya existe, reutilizalo y ajustá sus permisos; no vuelvas a ejecutar `CREATE USER`. Si accedés desde otra máquina o contenedor, el host del usuario debe corresponder a ese acceso.

Definí la conexión en la misma terminal donde vas a ejecutar los comandos:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Port=3306;Database=programaciontp3;User=tp3_app;Password=REEMPLAZAR_POR_TU_CLAVE;"
```

Reemplazá los datos por los de tu servidor. La variable vive durante esa sesión de terminal. No guardes contraseñas reales en archivos versionados ni compartas capturas que las muestren.

Como alternativa local, podés crear `Tp3prog/appsettings.json`, que está excluido por `.gitignore`, con este formato:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=programaciontp3;User=tp3_app;Password=REEMPLAZAR_POR_TU_CLAVE;"
  }
}
```

Las variables de entorno tienen prioridad sobre ese archivo. La aplicación requiere `DefaultConnection` y el servidor debe estar accesible para detectar su versión.

## Preparar y ejecutar

Si todavía no tenés la herramienta de migraciones:

```powershell
dotnet tool install --global dotnet-ef --version 9.0.19
```

Restaurá las dependencias y aplicá la migración existente:

```powershell
dotnet restore Tp3prog/Tp3prog.csproj
dotnet ef database update --project Tp3prog.DAO --startup-project Tp3prog
```

El proyecto `Tp3prog.DAO` contiene las migraciones y `Tp3prog` provee la configuración de arranque. No hace falta generar una nueva migración para la instalación inicial. La actualización de la base se hace explícitamente con el comando anterior: el inicio de la API no la aplica automáticamente.

Iniciá la API:

```powershell
dotnet run --project Tp3prog --launch-profile http
```

Swagger está disponible en [http://localhost:5175/swagger](http://localhost:5175/swagger) mientras el entorno sea `Development`. El perfil `https` usa también `https://localhost:7167`; requiere un certificado de desarrollo confiable.

Los comandos de migración, ejecución y pruebas pueden compilar los proyectos. Se documentan para ejecutar localmente; su presencia en esta guía no significa que se hayan ejecutado durante una revisión del código.

## Datos iniciales y verificación

La base comienza vacía. `MockDatabase.cs` contiene ejemplos en memoria, pero no está conectado a los DAOs ni carga datos en MySQL. Creá los registros desde Swagger o con las solicitudes de `Tp3prog/Tp3prog.http`.

Para comprobar la instalación, creá un registro, consultalo, actualizalo, reiniciá la API y volvé a consultarlo; finalmente eliminá el registro. Verificá además respuestas de validación y búsquedas de IDs inexistentes.

## Contrato de la API

Cada recurso ofrece `GET /{recurso}`, `GET /{recurso}/{id}`, `POST /{recurso}`, `PUT /{recurso}/{id}` y `DELETE /{recurso}/{id}`.

| Recurso | Campos de creación y actualización |
| --- | --- |
| `Players` | `name`, `age`, `dni` opcional, `number` |
| `Trainers` | `name`, `age`, `dni` opcional |
| `Students` | `name`, `age`, `dni` opcional, `file` |
| `Teams` | `name`, `category` |
| `Courses` | `name` |
| `Activities` | `title`, `description`, `date` opcional, `type` |

En `POST`, omití `id` o enviá cero. En `PUT`, podés omitirlo, enviar cero o enviar el mismo ID de la ruta; un ID distinto produce `400`. Los campos de relación se administran por las rutas específicas siguientes. Los campos desconocidos y las colecciones de relaciones embebidas en el CRUD se rechazan con `400`.

`type` usa valores numéricos: `0` = Class, `1` = Exam y `2` = Assignment. Otros valores son inválidos.

| Relación | Ruta para asociar o quitar |
| --- | --- |
| Estudiante de un curso | `/Courses/{id}/students/{studentId}` |
| Actividad de un curso | `/Courses/{id}/activities/{activityId}` |
| Jugador de un equipo | `/Teams/{id}/players/{playerId}` |
| Entrenador de un equipo | `/Teams/{id}/trainers/{trainerId}` |

Usá `PUT` sin cuerpo para asociar y `DELETE` sin cuerpo para quitar. Son operaciones idempotentes: repetirlas devuelve `204` si ambos registros existen. Un registro inexistente produce `404`. Una actividad sólo puede pertenecer a un curso: asociarla a otro devuelve `409`; primero quitá la asociación anterior.

Las consultas devuelven resúmenes de las relaciones para evitar ciclos. Las creaciones devuelven `201`, las actualizaciones `204` sin cuerpo y las eliminaciones exitosas `204`. Los datos inválidos producen `400`. Los conflictos de concurrencia y las restricciones conocidas de MySQL (duplicados y claves foráneas) se informan con `409`; otros errores de persistencia no se convierten en conflictos.

## Pruebas automatizadas

El proyecto `Tp3prog.Tests` contiene pruebas de regresión. Para ejecutarlas localmente:

```powershell
dotnet test Tp3prog.Tests/Tp3prog.Tests.csproj
```

El comando restaura también las dependencias del proyecto de pruebas. Las pruebas no reemplazan la comprobación de conexión, migraciones y persistencia contra tu servidor MySQL. Consultá el resultado real de la ejecución antes de considerar aprobada la entrega.

## Problemas habituales

- **SDK no encontrado:** revisá `dotnet --list-sdks` y la versión fijada en `global.json`.
- **Falta `DefaultConnection`:** definí la variable en la terminal actual o creá el archivo local indicado arriba.
- **No se puede conectar a MySQL:** verificá servicio, host, puerto, usuario y permisos sobre la base.
- **Tablas inexistentes:** aplicá la migración usando ambos proyectos indicados.
- **Swagger no aparece:** confirmá el entorno `Development` y el puerto del perfil elegido.
