# Guía de Puesta en Producción, Seguridad y Runbook Operativo
**Proyecto:** WebWave · SistemaVisionTech
**Stack:** Angular 19 (Vercel) + .NET 9 ASP.NET Core Web API en contenedor Docker (Render) + PostgreSQL (Supabase Pooler).

---

## 1. Variables de Entorno Requeridas

### A. Backend (Render / Docker)
Configurar en el panel de **Render -> Environment**:

| Variable | Tipo / Ejemplo | Propósito |
|---|---|---|
| `DATABASE_URL` | `Host=aws-1-ca-central-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[REF];Password=***;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;` | Conexión directa al Transaction Pooler de Supabase (puerto 6543). |
| `JWT_KEY` | Cadena aleatoria de 32+ caracteres (ej. generada con `openssl rand -base64 32`) | Firma y validación de tokens JWT de autenticación. |
| `ENCRYPTION_KEY` | Cadena aleatoria de 32+ caracteres | Clave simétrica para cifrado autenticado AES-256-GCM en reposo de credenciales. |
| `JWT_ISSUER` | `SistemaVisionTech.API` | Emisor oficial del token. |
| `JWT_AUDIENCE` | `SistemaVisionTech.Client` | Audiencia permitida. |
| `JWT_EXPIRES_IN_MINUTES` | `120` | Expiración de sesión (2 horas). |
| `CORS_ALLOWED_ORIGINS` | `https://webwave-sigma.vercel.app` | Orígenes web autorizados (sin comodines en producción). |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Activa modo producción (oculta Swagger, activa HSTS, headers de seguridad y enmascara stack traces). |
| `ASPNETCORE_HTTP_PORTS` | `8080` | Puerto interno del contenedor Kestrel. |

### B. Frontend (Vercel)
Configurar en **Vercel -> Settings -> Environment Variables**:
- `API_URL`: `https://sistemavisiontech-api.onrender.com/api`

---

## 2. Endpoints de Verificación y Health Checks

El backend expone endpoints nativos para monitoreo continuo de infraestructura:

1. **Liveness Probe (Render Health Check):**
   - **URL:** `GET /health`
   - **Respuesta esperada (200 OK):**
     ```json
     { "status": "Healthy", "servicio": "SistemaVisionTech API", "version": "1.0.0" }
     ```
   - **Configuración en Render:** Especificar `/health` en el campo **Health Check Path** del servicio web.

2. **Readiness Probe (Verificación de Base de Datos):**
   - **URL:** `GET /ready`
   - **Respuesta esperada (200 OK):**
     ```json
     { "status": "Ready", "database": "Connected" }
     ```
   - **Si falla (503 Service Unavailable):** Indica que la API no puede contactar al pooler de Supabase.

3. **Trazabilidad (Request-ID):**
   - Cada petición entrante y saliente incluye la cabecera `X-Request-ID`.
   - Si ocurre un error 500, el cliente recibe el código de error y el `RequestId` correlacionado en los logs.

---

## 3. Rotación de Credenciales (Acción Manual Recomendada)

Dado que en commits antiguos del historial se identificaron contraseñas de prueba:
1. **Supabase:**
   - Entrar al dashboard de Supabase -> **Project Settings -> Database**.
   - Presionar **Reset Database Password**.
   - Copiar la nueva contraseña y actualizar `DATABASE_URL` en el dashboard de Render.
2. **Clave JWT y Cifrado:**
   - Generar un nuevo token aleatorio criptográfico:
     ```bash
     openssl rand -base64 48
     ```
   - Actualizar `JWT_KEY` y `ENCRYPTION_KEY` en Render.
   - Las sesiones anteriores expirarán de forma segura y los usuarios reingresarán con sus credenciales.

---

## 4. Runbook de Incidentes

### A. Caída de Conexión a Base de Datos (Error 503 en `/ready` o Timeouts)
1. Verificar si el pooler de Supabase está activo:
   - Supabase suspende proyectos gratuitos inactivos tras 7 días. Si ocurrió, entrar a Supabase y presionar **Restore Project**.
2. Verificar el puerto del pooler:
   - Usar el puerto `6543` (Transaction Pooler) para alta concurrencia (500 usuarios concurrentes), no el puerto directo `5432`.
3. Revisar en los logs de Render si aparecen errores de autenticación `28P01` (revisar que el usuario tenga el prefijo `postgres.[REF]`).

### B. Error de CORS en el Navegador
1. Comprobar que el dominio del frontend coincida exactamente (incluyendo protocolo `https://` y sin barra final `/`) con `CORS_ALLOWED_ORIGINS`.
2. Verificar en la pestaña Network de DevTools que las peticiones `OPTIONS` (Preflight) devuelvan status `204 No Content` con cabeceras `Access-Control-Allow-Origin`.

### C. Contenedor de Render no arranca
1. Render compilará usando el `Dockerfile` optimizado con usuario no-root (`appuser`).
2. Comprobar que la variable `ASPNETCORE_HTTP_PORTS=8080` esté activa y que Render mapee el puerto `8080`.

---

## 5. Estrategia de Rollback

### Rollback en Render (Backend):
1. Ir al panel de Render -> Servicio `sistemavisiontech-api` -> Pestaña **Deploys**.
2. Seleccionar el commit o despliegue previo exitoso y hacer clic en **Rollback to this deploy**.

### Rollback en Vercel (Frontend):
1. Ir al panel de Vercel -> Proyecto `webwave-sigma` -> **Deployments**.
2. En el despliegue anterior estable, hacer clic en los tres puntos `(...)` -> **Promote to Production**.
