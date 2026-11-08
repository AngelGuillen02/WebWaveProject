using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Productos.Dtos;
using SistemaVisionTech.Features.Productos.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Productos.Services
{
    public class ProductosService : IProductosService
    {
        private readonly WebWaveDbContext _context;

        public ProductosService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ProductoResponseDto>>> ListarAsync()
        {
            var inventarios = await _context.Inventario
                .AsNoTracking()
                .ToDictionaryAsync(i => i.ProductoId, i => i.Cantidad);

            List<ProductoResponseDto> productos = await _context.Productos
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .Select(p => MapToDto(p))
                .ToListAsync();

            foreach (var p in productos)
            {
                p.Stock = inventarios.TryGetValue(p.ProductoId, out int cant) ? cant : 0;
            }

            return Result<List<ProductoResponseDto>>.Ok(productos);
        }

        public async Task<Result<ProductoResponseDto>> ObtenerPorIdAsync(int id)
        {
            Producto? producto = await _context.Productos.FindAsync(id);

            if (producto is null)
                return Result<ProductoResponseDto>.Fail("El producto no existe.");

            var dto = MapToDto(producto);
            var inv = await _context.Inventario.FirstOrDefaultAsync(i => i.ProductoId == id);
            dto.Stock = inv?.Cantidad ?? 0;

            return Result<ProductoResponseDto>.Ok(dto);
        }

        public async Task<Result<ProductoResponseDto>> CrearAsync(ProductoCreacionDto dto, string? usuario = null)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<ProductoResponseDto>.Fail("El nombre del producto es obligatorio.", isValidation: true);

            string nombreNorm = NormalizarNombre(dto.Nombre);
            var productosExistentes = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo)
                .Select(p => new { p.ProductoId, p.Nombre })
                .ToListAsync();

            if (productosExistentes.Any(p => NormalizarNombre(p.Nombre) == nombreNorm))
                return Result<ProductoResponseDto>.Fail($"Ya existe un producto registrado con el nombre '{dto.Nombre.Trim()}'.", isValidation: true);

            if (dto.Precio <= 0)
                return Result<ProductoResponseDto>.Fail("El precio de venta debe ser mayor a cero.", isValidation: true);

            if (!string.IsNullOrEmpty(dto.CodigoBarras))
            {
                bool existeCodigoBarras = await _context.Productos
                    .AnyAsync(p => p.CodigoBarras == dto.CodigoBarras);

                if (existeCodigoBarras)
                    return Result<ProductoResponseDto>.Fail("Ya existe un producto con ese código de barras.", isValidation: true);
            }

            Producto producto = new()
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
                Precio = dto.Precio,
                TieneNumeroSerie = dto.TieneNumeroSerie,
                TieneLote = dto.TieneLote,
                CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras.Trim(),
                TipoISV = string.IsNullOrWhiteSpace(dto.TipoISV) ? "ISV15" : dto.TipoISV,
                Activo = true
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            int stockInicial = dto.StockInicial ?? 0;
            if (stockInicial >= 0)
            {
                Inventarios inventario = new()
                {
                    ProductoId = producto.ProductoId,
                    Cantidad = stockInicial,
                    FechaIngreso = DateTime.UtcNow
                };

                _context.Inventario.Add(inventario);
                await _context.SaveChangesAsync();
            }

            _context.ProductosHistorial.Add(new SistemaVisionTech.Infrastructure.Entities.ProductosHistorial
            {
                ProductoId = producto.ProductoId,
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "Administrador" : usuario,
                FechaModificacion = DateTime.UtcNow,
                Accion = "CREACIÓN",
                DetalleCambios = $"Producto registrado en catálogo con stock inicial: {stockInicial}, ISV: {producto.TipoISV}.",
                PrecioAnterior = null,
                PrecioNuevo = producto.Precio,
                Activo = true
            });
            await _context.SaveChangesAsync();

            var response = MapToDto(producto);
            response.Stock = stockInicial;
            return Result<ProductoResponseDto>.Ok(response);
        }

        public async Task<Result<ProductoResponseDto>> EditarAsync(int id, ProductoCreacionDto dto, string? usuario = null)
        {
            Producto? producto = await _context.Productos.FindAsync(id);

            if (producto is null)
                return Result<ProductoResponseDto>.Fail("El producto no existe.");

            if (!string.IsNullOrWhiteSpace(dto.Nombre))
            {
                string nuevoNombreNorm = NormalizarNombre(dto.Nombre);
                if (NormalizarNombre(producto.Nombre) != nuevoNombreNorm)
                {
                    var productosExistentes = await _context.Productos
                        .AsNoTracking()
                        .Where(p => p.Activo && p.ProductoId != id)
                        .Select(p => new { p.ProductoId, p.Nombre })
                        .ToListAsync();

                    if (productosExistentes.Any(p => NormalizarNombre(p.Nombre) == nuevoNombreNorm))
                        return Result<ProductoResponseDto>.Fail($"Ya existe otro producto registrado con el nombre '{dto.Nombre.Trim()}'.", isValidation: true);
                }
            }

            if (!string.IsNullOrEmpty(dto.CodigoBarras) && producto.CodigoBarras != dto.CodigoBarras)
            {
                bool existeCodigoBarras = await _context.Productos
                    .AnyAsync(p => p.CodigoBarras == dto.CodigoBarras);

                if (existeCodigoBarras)
                    return Result<ProductoResponseDto>.Fail("Ya existe otro producto con ese código de barras.");
            }

            var cambios = new List<string>();
            if (producto.Nombre != dto.Nombre)
                cambios.Add($"Nombre: '{producto.Nombre}' -> '{dto.Nombre}'");
            if (producto.Precio != dto.Precio)
                cambios.Add($"Precio: L. {producto.Precio:N2} -> L. {dto.Precio:N2}");
            if (producto.Descripcion != dto.Descripcion)
                cambios.Add("Descripción modificada");
            if (producto.CodigoBarras != dto.CodigoBarras)
                cambios.Add($"Código/SKU: '{producto.CodigoBarras}' -> '{dto.CodigoBarras}'");
            if (producto.TipoISV != dto.TipoISV)
                cambios.Add($"Impuesto: '{producto.TipoISV}' -> '{dto.TipoISV}'");

            decimal precioAnterior = producto.Precio;

            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.Precio = dto.Precio;
            producto.TieneNumeroSerie = dto.TieneNumeroSerie;
            producto.TieneLote = dto.TieneLote;
            producto.CodigoBarras = dto.CodigoBarras;
            producto.TipoISV = dto.TipoISV;

            if (cambios.Count > 0)
            {
                var auditLog = new SistemaVisionTech.Infrastructure.Entities.ProductosHistorial
                {
                    ProductoId = id,
                    Usuario = string.IsNullOrWhiteSpace(usuario) ? "Administrador" : usuario,
                    FechaModificacion = DateTime.UtcNow,
                    Accion = "MODIFICACION",
                    DetalleCambios = string.Join(" | ", cambios),
                    PrecioAnterior = precioAnterior,
                    PrecioNuevo = dto.Precio,
                    Activo = true
                };
                _context.ProductosHistorial.Add(auditLog);
            }

            await _context.SaveChangesAsync();

            var inv = await _context.Inventario.FirstOrDefaultAsync(i => i.ProductoId == id);
            var response = MapToDto(producto);
            response.Stock = inv?.Cantidad ?? 0;
            return Result<ProductoResponseDto>.Ok(response);
        }

        public async Task<Result<ImportacionLoteResultadoDto>> ImportarLoteAsync(List<ProductoCreacionDto> dtos, string? usuario = null)
        {
            var resultado = new ImportacionLoteResultadoDto
            {
                TotalRecibidos = dtos.Count
            };

            if (dtos == null || dtos.Count == 0)
                return Result<ImportacionLoteResultadoDto>.Fail("No se proporcionaron registros para importar.", isValidation: true);

            string usuarioAudit = string.IsNullOrWhiteSpace(usuario) ? "Administrador" : usuario;

            var codigosExistentes = await _context.Productos
                .Where(p => !string.IsNullOrEmpty(p.CodigoBarras))
                .Select(p => p.CodigoBarras!.Trim().ToUpper())
                .ToHashSetAsync();

            var nombresExistentes = (await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo)
                .Select(p => p.Nombre)
                .ToListAsync())
                .Select(n => NormalizarNombre(n))
                .ToHashSet();

            var codigosEnLote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nombresEnLote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                {
                    resultado.Omitidos++;
                    resultado.Mensajes.Add("Omitido: Producto con nombre vacío.");
                    continue;
                }

                string nombreNorm = NormalizarNombre(dto.Nombre);
                if (nombresExistentes.Contains(nombreNorm) || nombresEnLote.Contains(nombreNorm))
                {
                    resultado.Omitidos++;
                    resultado.Mensajes.Add($"Omitido: Ya existe un producto con el nombre '{dto.Nombre.Trim()}'.");
                    continue;
                }
                nombresEnLote.Add(nombreNorm);

                string codigo = dto.CodigoBarras?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(codigo))
                {
                    string codigoUpper = codigo.ToUpperInvariant();
                    if (codigosExistentes.Contains(codigoUpper) || codigosEnLote.Contains(codigoUpper))
                    {
                        resultado.Omitidos++;
                        resultado.Mensajes.Add($"Omitido: Código de barras '{codigo}' ya existe en el sistema o en el archivo.");
                        continue;
                    }
                    codigosEnLote.Add(codigoUpper);
                }

                decimal precio = dto.Precio > 0 ? dto.Precio : 1.00m;
                int stockInicial = dto.StockInicial.HasValue && dto.StockInicial.Value >= 0 ? dto.StockInicial.Value : 0;

                var producto = new Producto
                {
                    Nombre = dto.Nombre.Trim(),
                    Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
                    Precio = precio,
                    TieneNumeroSerie = dto.TieneNumeroSerie,
                    TieneLote = dto.TieneLote,
                    CodigoBarras = string.IsNullOrWhiteSpace(codigo) ? null : codigo,
                    TipoISV = string.IsNullOrWhiteSpace(dto.TipoISV) ? "ISV15" : dto.TipoISV.Trim(),
                    Activo = true
                };

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                var inventario = new Inventarios
                {
                    ProductoId = producto.ProductoId,
                    Cantidad = stockInicial,
                    FechaIngreso = DateTime.UtcNow
                };
                _context.Inventario.Add(inventario);

                _context.ProductosHistorial.Add(new ProductosHistorial
                {
                    ProductoId = producto.ProductoId,
                    Usuario = usuarioAudit,
                    FechaModificacion = DateTime.UtcNow,
                    Accion = "IMPORTACION_MASIVA",
                    DetalleCambios = $"Producto importado en lote con stock inicial: {stockInicial}, ISV: {producto.TipoISV}.",
                    PrecioAnterior = null,
                    PrecioNuevo = producto.Precio,
                    Activo = true
                });

                resultado.Insertados++;
            }

            await _context.SaveChangesAsync();
            return Result<ImportacionLoteResultadoDto>.Ok(resultado);
        }

        public async Task<Result<List<SistemaVisionTech.Infrastructure.Entities.ProductosHistorial>>> ObtenerHistorialAsync(int productoId)
        {
            var logs = await _context.ProductosHistorial
                .Where(h => h.ProductoId == productoId)
                .OrderByDescending(h => h.FechaModificacion)
                .ToListAsync();

            return Result<List<SistemaVisionTech.Infrastructure.Entities.ProductosHistorial>>.Ok(logs);
        }

        public async Task<Result<bool>> EliminarAsync(int id, string? usuario = null)
        {
            Producto? producto = await _context.Productos.FindAsync(id);

            if (producto is null)
                return Result<bool>.Fail("El producto no existe.");

            producto.Activo = false;

            _context.ProductosHistorial.Add(new ProductosHistorial
            {
                ProductoId = id,
                Usuario = usuario ?? "Usuario",
                FechaModificacion = DateTime.UtcNow,
                Accion = "ELIMINACION_LOGICA",
                DetalleCambios = $"Producto '{producto.Nombre}' marcado como inactivo (Borrado Lógico).",
                PrecioAnterior = producto.Precio,
                PrecioNuevo = 0,
                Activo = true
            });

            await _context.SaveChangesAsync();

            return Result<bool>.Ok(true);
        }

        private static ProductoResponseDto MapToDto(Producto p) => new()
        {
            ProductoId = p.ProductoId,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            TieneNumeroSerie = p.TieneNumeroSerie,
            TieneLote = p.TieneLote,
            CodigoBarras = p.CodigoBarras,
            TipoISV = p.TipoISV,
            Activo = p.Activo
        };

        private static string NormalizarNombre(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return string.Empty;
            return Regex.Replace(nombre.Trim(), @"\s+", " ").ToLowerInvariant();
        }
    }
}
