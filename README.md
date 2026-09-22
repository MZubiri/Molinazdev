# Digital Services

Base productiva en ASP.NET Core MVC para publicar servicios digitales, vender paquetes y cobrar mediante Mercado Pago Checkout Pro. La aplicación conserva el precio de cada compra en una orden interna y confirma los pagos únicamente después de validar el webhook y consultar el pago directamente en Mercado Pago.

## Arquitectura

La solución usa una separación pragmática en cuatro proyectos:

```text
src/
├── DigitalServices.Domain          Entidades, enums y reglas de dominio
├── DigitalServices.Application     Casos de uso, DTOs y abstracciones
├── DigitalServices.Infrastructure  EF Core, MySQL y Mercado Pago
└── DigitalServices.Web             MVC, Razor, composición y endpoints HTTP

tests/
└── DigitalServices.Tests           Pruebas de la lógica crítica
```

Las dependencias apuntan hacia el dominio: `Domain` no conoce ASP.NET Core, Entity Framework ni Mercado Pago; `Application` depende de `Domain`; `Infrastructure` implementa las abstracciones de `Application`; y `Web` compone la aplicación. Las versiones NuGet se administran centralmente en `Directory.Packages.props`.

## Requisitos para desarrollo

- .NET SDK 8.0.
- MySQL 8.0 o una versión compatible con Pomelo 8.
- Credenciales de prueba de una aplicación de Mercado Pago.
- Docker 24 o posterior, opcional.
- `dotnet-ef` 8.0.2 para administrar migraciones.

Instala la herramienta de EF Core si aún no está disponible:

```bash
dotnet tool install --global dotnet-ef --version 8.0.2
```

En entornos donde el lanzador Snap de .NET esté bloqueado, sustituye `dotnet` en los comandos por `/snap/dotnet-sdk/current/dotnet`.

## MySQL local

Crea una base y un usuario con permisos limitados a esa base. Cambia la contraseña del ejemplo antes de ejecutar los comandos:

```sql
CREATE DATABASE digital_services
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE USER 'digitalservices'@'localhost'
  IDENTIFIED BY 'use-una-contrasena-local-segura';

GRANT ALL PRIVILEGES ON digital_services.*
  TO 'digitalservices'@'localhost';

FLUSH PRIVILEGES;
```

La aplicación no contiene credenciales. Para una sesión local, configura las variables de entorno con tus propios valores:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection='Server=127.0.0.1;Port=3306;Database=digital_services;User=digitalservices;Password=use-su-clave-local;TreatTinyAsBoolean=true;'
export MercadoPago__AccessToken='use-su-access-token-de-prueba'
export MercadoPago__WebhookSecret='use-el-secreto-del-webhook'
export MercadoPago__SuccessUrl='https://su-tunel-publico.example/Checkout/Success'
export MercadoPago__FailureUrl='https://su-tunel-publico.example/Checkout/Failure'
export MercadoPago__PendingUrl='https://su-tunel-publico.example/Checkout/Pending'
export MercadoPago__NotificationUrl='https://su-tunel-publico.example/api/webhooks/mercadopago'
```

Las URLs de retorno y notificación deben ser absolutas. Para probar webhooks desde una máquina local se necesita un túnel HTTPS público; Mercado Pago no puede llamar a `localhost`.

## Migraciones y datos iniciales

Restaura y compila primero la solución:

```bash
dotnet restore DigitalServices.sln
dotnet build DigitalServices.sln --configuration Release --no-restore
```

Aplica la migración incluida:

```bash
dotnet ef database update \
  --project src/DigitalServices.Infrastructure \
  --startup-project src/DigitalServices.Web
```

Para crear una migración posterior:

```bash
dotnet ef migrations add NombreDescriptivo \
  --project src/DigitalServices.Infrastructure \
  --startup-project src/DigitalServices.Web \
  --output-dir Persistence/Migrations
```

Los servicios y paquetes iniciales se cargan desde la estrategia de inicialización incluida en Infrastructure. Sus precios son datos persistidos y pueden cambiarse en MySQL sin recompilar; una orden conserva siempre el importe y la moneda vigentes al crearla.

Las migraciones no se aplican automáticamente durante cada arranque en producción. Antes de desplegar una versión que cambie el esquema, realiza un respaldo y ejecuta `dotnet ef database update` desde una estación administrativa o un trabajo de despliegue con acceso restringido a MySQL.

La migración `RepairUtf8CatalogText` corrige registros históricos del catálogo que hayan sido guardados con UTF-8 interpretado como Latin-1. Debe ejecutarse una vez en producción después de respaldar la base.

## Ejecutar localmente

Con la configuración cargada y la base actualizada:

```bash
dotnet run --project src/DigitalServices.Web
```

Para ejecutar todas las verificaciones:

```bash
dotnet restore DigitalServices.sln
dotnet build DigitalServices.sln --configuration Release --no-restore
dotnet test DigitalServices.sln --configuration Release --no-build
```

El catálogo permite seleccionar un paquete y enviar únicamente sus datos de contacto y `packageId`. El servidor recupera el precio base y la moneda desde MySQL, agrega el IGV del 18 %, crea o actualiza el cliente, genera una orden `Pending`, crea la Preference y redirige a Checkout Pro. Las páginas `Success`, `Failure` y `Pending` son informativas: visitar `Success` nunca confirma una orden.

## Configuración de Mercado Pago

1. Crea una aplicación en el panel de desarrolladores de Mercado Pago.
2. Durante desarrollo utiliza credenciales, comprador y vendedor de prueba que pertenezcan a cuentas distintas.
3. Configura `MercadoPago__AccessToken` con el token privado del ambiente elegido.
4. Registra el endpoint público `https://tu-dominio/api/webhooks/mercadopago` y habilita el evento de pagos.
5. Copia la clave secreta entregada para la firma a `MercadoPago__WebhookSecret`.
6. Configura las tres Back URLs HTTPS del mismo dominio y la Notification URL pública.
7. En producción sustituye las credenciales de prueba por las productivas y realiza un pago real controlado de extremo a extremo.

El Access Token y el secreto del webhook son credenciales de servidor: no deben aparecer en JavaScript, vistas, imágenes Docker, repositorios, logs ni URLs. Después de modificar el endpoint o regenerar el secreto, actualiza la configuración de Coolify y verifica una notificación firmada.

El webhook realiza este flujo:

```text
firma HMAC válida
  -> obtiene data.id
  -> consulta el pago en Mercado Pago
  -> resuelve ExternalReference
  -> valida importe y moneda
  -> actualiza Payment y Order en una transacción
```

Las restricciones únicas de base de datos sobre los identificadores externos, junto con el procesamiento transaccional, hacen seguro reintentar una misma notificación. Los estados recibidos se conservan en `Payment`; las reglas de transición impiden que una notificación antigua degrade una orden ya avanzada.

## Docker

La imagen usa etapas independientes de restore, build, publish y runtime. La etapa final contiene únicamente ASP.NET Core Runtime, escucha en el puerto `8080` y ejecuta la aplicación con el usuario no privilegiado `app`.

Construye la imagen desde la raíz:

```bash
docker build --tag digital-services:local .
```

Crea un archivo local `.env` —está excluido de Git y del contexto Docker— con las variables descritas anteriormente y ejecútala:

```bash
docker run --rm \
  --name digital-services \
  --publish 8080:8080 \
  --add-host host.docker.internal:host-gateway \
  --env-file .env \
  digital-services:local
```

Si MySQL corre en el host Linux, usa `Server=host.docker.internal` en la cadena del contenedor. Si ambos servicios comparten una red Docker, usa como `Server` el nombre DNS del servicio MySQL.

Comprueba que la aplicación esté lista con:

```bash
curl --fail http://localhost:8080/health
```

El endpoint devuelve únicamente el estado de salud y no expone cadenas de conexión, credenciales ni detalles internos.

## Despliegue en Coolify

Configura el recurso como una aplicación Docker basada en el `Dockerfile` de la raíz:

- Puerto interno: `8080`.
- Health check HTTP: `/health`.
- HTTPS: habilitado en el dominio público gestionado por Coolify.
- Migraciones: ejecútalas como paso administrativo antes de sustituir la versión activa.
- MySQL: usa el nombre interno y puerto de la base si está en la misma red de Coolify; limita su exposición pública.

Variables requeridas:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ConnectionStrings__DefaultConnection
MercadoPago__AccessToken
MercadoPago__WebhookSecret
MercadoPago__SuccessUrl
MercadoPago__FailureUrl
MercadoPago__PendingUrl
MercadoPago__NotificationUrl
```

Ejemplo de rutas de producción:

```text
MercadoPago__SuccessUrl=https://servicios.example/Checkout/Success
MercadoPago__FailureUrl=https://servicios.example/Checkout/Failure
MercadoPago__PendingUrl=https://servicios.example/Checkout/Pending
MercadoPago__NotificationUrl=https://servicios.example/api/webhooks/mercadopago
```

No añadas comillas al valor desde la interfaz de Coolify salvo que formen parte de él. Tras el despliegue, confirma que el proxy envía `X-Forwarded-For` y `X-Forwarded-Proto`, que `/health` responde desde Internet y que las URLs generadas conservan `https`. La aplicación procesa forwarded headers antes de redirecciones y HTTPS.

## Operación segura

- Mantén MySQL y las credenciales de Mercado Pago fuera del repositorio.
- Respalda la base antes de aplicar una migración productiva.
- Conserva logs estructurados, pero no registres tokens, secretos, firmas completas ni payloads que incluyan datos personales innecesarios.
- Rota inmediatamente cualquier credencial expuesta y vuelve a desplegar.
- Restringe el acceso a la base y usa TLS cuando MySQL se encuentre fuera de la red privada.
- Verifica periódicamente los reintentos y errores del webhook en Mercado Pago.
- No uses las páginas de retorno del navegador como prueba de pago; la fuente definitiva es la consulta autenticada al API.

## Límites actuales

Esta base cubre catálogo, clientes, órdenes, Checkout Pro, pagos y webhook. Cuentas de usuario, paneles, facturación, cupones, cotizaciones, adjuntos, tickets, notificaciones, suscripciones y pagos recurrentes quedan fuera del alcance inicial, pero la separación por capas y la abstracción `IPaymentGateway` permiten incorporarlos sin acoplar el dominio a Mercado Pago.
