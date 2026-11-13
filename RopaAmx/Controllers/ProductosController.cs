using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RopaAmx.Models;

namespace RopaAmx.Controllers;

// Cualquier usuario con sesión puede VER (Index, Detalles).
// Solo el Administrador puede CREAR, EDITAR y ELIMINAR.
public class ProductosController : Controller
{
    private readonly RopaAmxContext _context;

    public ProductosController(RopaAmxContext context) => _context = context;

    // ---------- READ: catálogo ----------
    public async Task<IActionResult> Index(string? q)
    {
        var query = _context.Productos.AsNoTracking().Include(p => p.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q) ||
                                     (p.Descripcion != null && p.Descripcion.Contains(q)) ||
                                     (p.Tipo != null && p.Tipo.Contains(q)));

        ViewBag.Q = q;
        return View(await query.OrderBy(p => p.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Detalles(Guid id)
    {
        var producto = await _context.Productos.AsNoTracking()
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null) return NotFound();
        return View(producto);
    }

    // ---------- CREATE ----------
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Crear(bool masVendido = false)
    {
        await CargarListas();
        return View(new Producto { EsMasVendido = masVendido, Existencia = 1 });
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Producto producto)
    {
        ModelState.Remove(nameof(Producto.Categoria));
        Validar(producto);

        if (!ModelState.IsValid)
        {
            await CargarListas();
            return View(producto);
        }

        producto.Id = Guid.NewGuid();
        producto.FechaRegistro = DateTime.Now;
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        TempData["Ok"] = $"Producto “{producto.Nombre}” creado.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- UPDATE ----------
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Editar(Guid id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        await CargarListas();
        return View(producto);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Producto producto)
    {
        ModelState.Remove(nameof(Producto.Categoria));
        Validar(producto);

        if (!ModelState.IsValid)
        {
            await CargarListas();
            return View(producto);
        }

        var actual = await _context.Productos.FindAsync(producto.Id);
        if (actual == null) return NotFound();

        actual.Nombre = producto.Nombre;
        actual.Descripcion = producto.Descripcion;
        actual.Precio = producto.Precio;
        actual.PrecioRebaja = producto.PrecioRebaja;
        actual.Tipo = producto.Tipo;
        actual.Existencia = producto.Existencia;
        actual.EsMasVendido = producto.EsMasVendido;
        actual.ImagenUrl = producto.ImagenUrl;
        actual.IdCategoria = producto.IdCategoria;

        await _context.SaveChangesAsync();

        TempData["Ok"] = $"Producto “{actual.Nombre}” actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- DELETE ----------
    [Authorize(Roles = Roles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, string? volver)
    {
        var producto = await _context.Productos.FindAsync(id);

        if (producto != null)
        {
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            TempData["Ok"] = $"Producto “{producto.Nombre}” eliminado.";
        }

        return volver == "catalogo"
            ? RedirectToAction(nameof(Index))
            : RedirectToAction("Index", "Home");
    }

    // ---------- Auxiliares ----------
    private void Validar(Producto p)
    {
        if (p.Existencia < 0)
            ModelState.AddModelError(nameof(Producto.Existencia), "La existencia no puede ser negativa.");

        if (p.PrecioRebaja != null && (p.PrecioRebaja <= 0 || p.PrecioRebaja >= p.Precio))
            ModelState.AddModelError(nameof(Producto.PrecioRebaja), "La rebaja debe ser mayor a 0 y menor al precio normal.");
    }

    private async Task CargarListas()
    {
        ViewBag.Categorias = new SelectList(
            await _context.Categorias.OrderBy(c => c.Id).ToListAsync(), "Id", "Nombre");
        ViewBag.Tipos = new SelectList(new[] { "Camisas", "Sudaderas", "Pantalones", "Accesorios" });
    }
}
