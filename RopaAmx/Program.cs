using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RopaAmx.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<RopaAmxContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RopaAmxConnection")));

// Hash seguro de contraseñas (PBKDF2) incluido en ASP.NET Core
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// Inicio de sesión con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Login";
        o.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.Name = "AmxK.Auth";

        // Si el admin cambia el rol o desactiva a un usuario, su sesión se cierra y debe volver a entrar
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var idTexto = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var rolCookie = ctx.Principal?.FindFirstValue(ClaimTypes.Role);

            if (!Guid.TryParse(idTexto, out var id))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync();
                return;
            }

            var db = ctx.HttpContext.RequestServices.GetRequiredService<RopaAmxContext>();
            var u = await db.Usuarios.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Rol, x.Activo })
                .FirstOrDefaultAsync();

            if (u == null || u.Activo == false || u.Rol != rolCookie)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync();
            }
        };
    });

// Todo exige sesión, salvo lo marcado con [AllowAnonymous] (Login, Registro, Contacto)
builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// Crea usuarios iniciales si no existen (cambia las contraseñas en producción)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RopaAmxContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var iniciales = new[]
        {
            (Nombre: "AdministradorK", Correo: "a24310241@gmail.com", Clave: "Admin123*", Rol: Roles.Administrador),
            (Nombre: "Usuario Lector", Correo: "karol.galdamez22@unach.mx", Clave: "Lector123*", Rol: Roles.Lector)
        };

        foreach (var s in iniciales)
        {
            if (db.Usuarios.Any(u => u.CorreoElectronico == s.Correo)) continue;

            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                NombreCompleto = s.Nombre,
                CorreoElectronico = s.Correo,
                Rol = s.Rol,
                Activo = true,
                FechaRegistro = DateTime.Now
            };
            usuario.Password = hasher.HashPassword(usuario, s.Clave);
            db.Usuarios.Add(usuario);
        }
        db.SaveChanges();
    }
    catch (Exception ex)
    {
        log.LogError(ex, "No se pudieron crear los usuarios iniciales. ¿Ejecutaste SQL/ROPA_AMX_actualizacion.sql?");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// /Categoria/hombre, /Categoria/mujer, /Categoria/ninos
app.MapControllerRoute(
    name: "categoria",
    pattern: "Categoria/{slug}",
    defaults: new { controller = "Categoria", action = "Ver" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
