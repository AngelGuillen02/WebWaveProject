# Contexto del Proyecto: SistemaVisionTech

Este documento describe la arquitectura completa, el stack tecnológico, la estructura de carpetas, el modelo de datos y las convenciones del proyecto **SistemaVisionTech**. Está diseñado para servir como el contexto absoluto de referencia para el entendimiento total de la API.

## 1. Descripción General
SistemaVisionTech es una API RESTful empresarial desarrollada para gestionar el core operativo de una empresa. Actúa como el backend principal (WebWaveProject) e integra módulos completos para el **Control de Inventarios**, **Gestión de Ventas**, **Gestión de Compras** y un **Sistema de Acceso Seguro** (Usuarios, Roles y Permisos).

## 2. Stack Tecnológico
*   **Framework Base:** .NET 10 (ASP.NET Core Web API)
*   **Lenguaje:** C# 12+
*   **ORM (Acceso a Datos):** Entity Framework Core 10.x
*   **Motor de Base de Datos:** SQL Server
*   **Seguridad y Autenticación:** JWT (JSON Web Tokens)
*   **Encriptación de Contraseñas:** BCrypt.Net-Next
*   **Documentación de API:** Swagger (Swashbuckle.AspNetCore)
*   **Librerías Adicionales Referenciadas:** AutoMapper, Microsoft.Teams.AI.Models.OpenAI

---

## 3. Arquitectura y Estructura del Proyecto
El proyecto sigue una **Screaming Architecture** (Arquitectura Orientada a Características o *Feature-based architecture*) combinada fuertemente con el patrón Controlador-Servicio.

### Árbol de Directorios
```text
SistemaVisionTech/
│
├── Common/               # Clases transversales comunes
│   └── Result.cs         # Implementación del Patrón Result para manejo de respuestas
│
├── Controllers/          # Endpoints HTTP expuestos vía Swagger
│   ├── BaseApiController.cs    # Clase base para orquestar los retornos HTTP del Result<T>
│   ├── AccesoController.cs     # Endpoints de Autenticación, Usuarios y Roles
│   ├── ComprasController.cs    # Endpoints para Proveedores y Compras
│   ├── InventarioController.cs # Endpoints para manejo de Stock
│   └── VentasController.cs     # Endpoints para Clientes y Ventas
│
├── Features/             # Lógica de negocio dividida por módulos (Screaming Architecture)
│   ├── Acceso/           # Módulo de Autenticación y Seguridad
│   │   ├── Dtos/         # Request/Response Data Transfer Objects
│   │   ├── Interfaces/   # Contratos (ej. IAuthService, IUsuariosService)
│   │   └── Services/     # Lógica (ej. AuthService, JwtTokenService)
│   ├── Compras/          # Gestión de abastecimiento
│   │   ├── Dtos/, Interfaces/, Services/ (Estructura idéntica)
│   ├── Inventario/       # Movimientos de almacén
│   │   ├── Dtos/, Interfaces/, Services/ (Estructura idéntica)
│   └── Ventas/           # Transacciones con clientes
│       ├── Dtos/, Interfaces/, Services/ (Estructura idéntica)
│
├── Infrastructure/       # Capa de Acceso a Datos (EF Core)
│   ├── Entities/         # Entidades de dominio mapeadas a tablas SQL (Modelos POCO)
│   ├── Maps/             # Mapeos Fluent API (Entity Configurations)
│   └── WebWaveDbContext.cs # Contexto central de Base de Datos
│
├── Kubernetes/           # Configuración de orquestación y despliegue (Docker/K8s)
├── Properties/           # Configuración de entornos de desarrollo (launchSettings.json)
├── appsettings.json      # Variables de entorno (ConnectionStrings, JWT Settings, etc.)
└── Program.cs            # Punto de entrada de la aplicación y Pipeline HTTP
```

---

## 4. Configuración del Pipeline (`Program.cs`)
El archivo de arranque centraliza la configuración:
1. **Configuración de Base de Datos:** Inyecta `WebWaveDbContext` usando la cadena `DefaultConnection` apuntando a SQL Server.
2. **Autenticación JWT:** Configura el middleware `AddJwtBearer` validando el Issuer, Audience, Lifetime y la SigningKey obtenidos del `appsettings.json`.
3. **Serialización JSON:** Configura `JsonStringEnumConverter` para que los enumeradores se manejen como texto en los requests/responses.
4. **Swagger:** Configura el esquema de seguridad `Bearer` para permitir probar la API autenticada desde la interfaz UI.
5. **Inyección de Dependencias (DI):** Registra el ciclo de vida de los servicios como `AddScoped` (ej. `AuthService`, `VentasService`, `InventarioService`).

---

## 5. Dominios, Base de Datos y Entidades (`WebWaveDbContext`)
El sistema está construido alrededor de **Global Query Filters**. En `WebWaveDbContext`, todas las entidades principales tienen un filtro `.HasQueryFilter(e => e.Activo)` para implementar **Soft Delete** (Borrado Lógico) de forma global en todas las consultas.

Las tablas/entidades que conforman el proyecto son:

### A. Dominio de Acceso y Organización
*   `Usuarios`: Empleados/Administradores con contraseñas hasheadas.
*   `Perfiles` y `Permisos`: Sistema de Control de Acceso Basado en Roles (RBAC) gestionado a través de las tablas intermedias `UsuariosPerfiles` y `PerfilesPermisos`.
*   `Empresas` y `Sucursales`: Estructura corporativa (Multi-sucursal).

### B. Dominio Comercial y Productos
*   `Productos`: Catálogo de ítems a comercializar.
*   `Clientes`: Base de datos de consumidores.
*   `Proveedores`: Base de datos de distribuidores de mercancía.
*   `MetodosPago`: Formas de pago aceptadas.

### C. Dominio de Ventas
*   `Ventas`: Cabecera de la transacción comercial.
*   `VentasDetalles`: Líneas de los productos vendidos en una transacción.
*   `PagosVenta`: Registro de abonos o liquidaciones de una venta.
*   `EstadosVenta`: Catálogo de estatus (ej. Pendiente, Pagado, Cancelado).

### D. Dominio de Compras
*   `Compras`: Cabecera de la adquisición a un proveedor.
*   `ComprasDetalles`: Líneas de los productos ingresados en la compra.
*   `PagosCompra`: Registro de pagos emitidos al proveedor.
*   `EstadosCompra`: Catálogo de estatus.

### E. Dominio de Inventario
*   `Inventario`: Cantidades actuales en stock por producto/sucursal.
*   `HistorialMovimientoInventario`: Bitácora de entradas, salidas y ajustes (Kardex).

*(Nota Crítica)*: Actualmente **NO se utiliza el sistema de Migraciones de EF Core (`dotnet ef migrations`)**. Cualquier alteración a estas entidades debe aplicarse directamente creando o alterando las tablas manualmente en SQL Server (Database-First workflow).

---

## 6. Patrones de Diseño y Convenciones

1.  **Patrón Result (`Result<T>`):** Evita el uso ineficiente de Excepciones para control de flujo de negocio. Todo `Service` retorna un `Result.Ok` o `Result.Fail`.
2.  **Patrón DTO (Data Transfer Object):** Aísla las `Entities` de la capa de presentación. Las APIs solo consumen y retornan clases sufijadas con `Dto` (ej. `UsuariosCreacionDto`).
3.  **Patrón Service Layer (Separación de lógicas):** Los Controladores son *tontos*; solo reciben el request y lo pasan al Servicio correspondiente. Todo el negocio vive estrictamente en la carpeta `Features/{Modulo}/Services`.
4.  **Repository y Unit of Work:** Se delegan nativamente a Entity Framework Core mediante la inyección directa de `WebWaveDbContext`.
5.  **Inyección de Dependencias (DI):** Totalmente desacoplado mediante interfaces (`I...Service`).

---

## 7. Catálogo de Funcionalidades por Clase

### Capa de Controladores (Presentación HTTP)
*   **`BaseApiController`**: Contiene la lógica maestra `HandleResult()` que intercepta las respuestas de los servicios y las traduce uniformemente a `200 OK`, `400 Bad Request` (si hay errores de validación) o `404 Not Found`.
*   **`{Modulo}Controller` (Acceso, Ventas, etc.)**: Expuestos en Swagger. Define las rutas (`[HttpGet]`, `[HttpPost]`), protege con `[Authorize]` y redirige a los contratos de interfaz.

### Capa de Servicios (Reglas de Negocio)
*   **`JwtTokenService`**: Implementa el SRP (Responsabilidad Única). Solo recibe los claims de un usuario válido y genera criptográficamente el Token JWT de acceso.
*   **`AuthService`**: Realiza el cruce entre el email provisto, extrae el usuario, e invoca a `BCrypt.Net` para verificar que la contraseña plana coincida con el Hash de la DB.
*   **Servicios CRUD (ej. `PerfilesService`, `VentasService`)**:
    *   Mapean manualmente de DTOs a Entities y viceversa.
    *   Validan restricciones únicas de negocio (ej. *Un perfil no se puede borrar si tiene usuarios vinculados*, *No se pueden asignar permisos inexistentes*).
    *   Se adaptan a las limitantes de LINQ to Entities (ej. usando comparadores `==` en vez de `StringComparison` debido a las traducciones hacia SQL Server).

---

## 8. Flujos de Trabajo Recomendados para Nuevos Desarrollos (Guía para Skills)
Cuando se deba crear un nuevo endpoint o característica en SistemaVisionTech:
1.  Crear los **DTOs** (Request/Response) en `Features/{Modulo}/Dtos`.
2.  Declarar el método en la interfaz correspondiente `Features/{Modulo}/Interfaces`.
3.  Implementar la lógica en `Features/{Modulo}/Services` retornando un objeto `Result<T>`.
4.  Exponer el método en el `Controller` heredando de `BaseApiController` y usando `HandleResult(resultado)`.
5.  Si implica tablas nuevas, modelar en `Infrastructure/Entities/`, añadir el `DbSet` al `WebWaveDbContext`, añadir el QueryFilter de borrado lógico y ejecutar el script `CREATE TABLE` en la base de datos SQL manualmente.
