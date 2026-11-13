using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RopaAmx.Models;

namespace RopaAmx.Controllers;

public class CuentaController : Controller
{
    private readonly RopaAmxContext _context;
    private readonly IPasswordHasher<Usuario> _hasher;

    public CuentaController(RopaAmxContext context, IPasswordHasher<Usuario> hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    // ---------- LOGIN ----------
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(modelo);

        var correo = modelo.Correo.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoElectronico == correo);

        var valido = usuario != null
            && usuario.Activo != false
            && !string.IsNullOrEmpty(usuario.Password)
            && _hasher.VerifyHashedPassword(usuario, usuario.Password, modelo.Password) != PasswordVerificationResult.Failed;

        if (!valido)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(modelo);
        }

        await IniciarSesion(usuario!, modelo.Recordarme);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ---------- REGISTRO (siempre crea usuarios con rol Lector) ----------
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegistroViewModel());
    }

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var correo = modelo.Correo.Trim().ToLowerInvariant();

        if (await _context.Usuarios.AnyAsync(u => u.CorreoElectronico == correo))
        {
            ModelState.AddModelError(nameof(modelo.Correo), "Ya existe una cuenta con ese correo.");
            return View(modelo);
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreCompleto = modelo.NombreCompleto.Trim(),
            CorreoElectronico = correo,
            Rol = Roles.Lector,
            Activo = true,
            FechaRegistro = DateTime.Now
        };
        usuario.Password = _hasher.HashPassword(usuario, modelo.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        TempData["Ok"] = "Cuenta creada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- LOGOUT ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    // ---------- ADMINISTRACIÓN DE USUARIOS (solo Administrador) ----------
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Usuarios()
    {
        var lista = await _context.Usuarios.AsNoTracking()
            .OrderBy(u => u.Rol).ThenBy(u => u.NombreCompleto).ToListAsync();
        return View(lista);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(Guid id)
    {
        if (EsUsuarioActual(id))
        {
            TempData["Error"] = "No puedes cambiar tu propio rol.";
            return RedirectToAction(nameof(Usuarios));
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        usuario.Rol = usuario.Rol == Roles.Administrador ? Roles.Lector : Roles.Administrador;
        await _context.SaveChangesAsync();

        TempData["Ok"] = $"{usuario.NombreCompleto} ahora es {usuario.Rol}.";
        return RedirectToAction(nameof(Usuarios));
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(Guid id)
    {
        if (EsUsuarioActual(id))
        {
            TempData["Error"] = "No puedes desactivar tu propia cuenta.";
            return RedirectToAction(nameof(Usuarios));
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        usuario.Activo = usuario.Activo == false;
        await _context.SaveChangesAsync();

        TempData["Ok"] = $"{usuario.NombreCompleto}: cuenta {(usuario.Activo == true ? "activada" : "desactivada")}.";
        return RedirectToAction(nameof(Usuarios));
    }

    // ---------- Auxiliares ----------
    private bool EsUsuarioActual(Guid id) =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString();

    private async Task IniciarSesion(Usuario usuario, bool recordarme)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto ?? usuario.CorreoElectronico ?? "Usuario"),
            new(ClaimTypes.Email, usuario.CorreoElectronico ?? ""),
            new(ClaimTypes.Role, usuario.Rol)
        };

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = recordarme });
    }
}
