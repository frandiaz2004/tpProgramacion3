# Cliente TP3

Requiere Node.js compatible con Angular 21 y la API en `http://localhost:5175`.

Desde esta carpeta:

```powershell
npm install
npm start
```

Abrí [http://localhost:4200](http://localhost:4200). El proxy redirige las solicitudes de la API al backend.

Ingresá con un usuario creado mediante `POST /User` en Swagger. El login usa `POST /User/login`, comprueba el token con `GET /User/me` y accede directamente al panel. El token se usa internamente y no se muestra en pantalla.

El token queda en memoria: cerrar sesión o recargar la página lo elimina. La restricción del panel es de navegación; los endpoints CRUD siguen siendo públicos en el servidor.

```powershell
npm test -- --watch=false
npm run build
```
