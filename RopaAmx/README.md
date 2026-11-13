# AMX K – Catálogo de ropa (ASP.NET Core MVC + SQL Server)

## Requisitos que cumple
1. ASP.NET Core MVC.
2. Interfaz según el tipo de producto (Camisas, Sudaderas, Pantalones, Accesorios) y la categoría (Hombre, Mujer, Niños).
3. Base de datos SQL Server (`SQL/`).
4. Navegación entre vistas (menú, buscador, mosaicos, catálogo, detalle).
5. CRUD completo de productos; **sin sesión no se ve nada del catálogo**.
6. Pantalla de Login, Registro y administración de usuarios.

## Roles
| Rol | Permisos |
|---|---|
| Lector | Ver catálogo, buscar y ver detalle. Es el rol de todo usuario que se registra. |
| Administrador | Lo anterior + crear, editar y eliminar productos + administrar usuarios. |

## Puesta en marcha
1. Base nueva: ejecuta `SQL/ROPA_AMX.sql`. Base existente: ejecuta `SQL/ROPA_AMX_actualizacion.sql`.
2. Ajusta `ConnectionStrings:RopaAmxConnection` en `appsettings.json`.
3. `dotnet run`. Al iniciar se crean los usuarios `admin@amxk.mx` / `Admin123*` y `lector@amxk.mx` / `Lector123*`.
4. Cambia esas contraseñas antes de publicar de verdad.
