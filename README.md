# MolinazDev &middot; Plataforma de Servicios Digitales y Checkout

Plataforma productiva de alta fidelidad que combina un **frontend moderno en Astro (diseño Neo-brutalista)** con un **backend robusto en ASP.NET Core (.NET 8) bajo arquitectura limpia**. Permite publicar servicios digitales, calcular de forma transparente el desglose de precios (Base + 18% IGV), recibir cotizaciones personalizadas vía WhatsApp/correo y cobrar mediante **Mercado Pago Checkout Pro** con validación criptográfica HMAC, conciliación periódica y notificaciones multicanal.

---

## 🚀 Novedades y Mejoras Implementadas

1. **Frontend Neo-Brutalista Integrado (Astro + TailwindCSS):**
   - Estética contemporánea de alto impacto con sombras duras (`4px 4px 0px #000`), microinteracciones y tipografías expresivas.
   - **Desglose de precios en vivo en el Checkout Modal:** Subtotal neto, 18% IGV peruano y Total final a abonar en Mercado Pago.
   - **Manejo de errores por campo:** Feedback visual inmediato ante datos incompletos o inválidos devueltos por el backend.
   - **Bento Features Grid:** Sección de propuesta de valor y beneficios integrada en la página principal (`#beneficios`).
   - **Página de Cotización Personalizada (`/contacto`):** Formulario interactivo que genera mensajes formateados para WhatsApp Business y enlace de correo empresarial.
   - **Botón flotante de WhatsApp:** Acceso rápido para atención y consultas directas en cualquier página.

2. **Backend Robusto (.NET 8 & Clean Architecture):**
   - **Documentación interactiva Swagger / OpenAPI:** Disponible en `/swagger` en entornos de desarrollo.
   - **Protección contra abusos y Rate Limiting:** Política `CheckoutPolicy` (límite de 5 solicitudes/minuto por IP con queue de 2).
   - **CORS configurable:** Habilitado para orígenes de desarrollo y producción configurables vía `appsettings.json`.
   - **Notificaciones automáticas post-pago:**
     - Correo HTML responsive para el cliente con el comprobante de su orden y los pasos inmediatos.
     - Alertas en tiempo real para el equipo de ventas vía **Discord Webhook** y **Telegram Bot**.
   - **Worker en segundo plano para Conciliación de Pagos (`PaymentReconciliationWorker`):**
     - Revisa periódicamente órdenes pendientes con más de 10 minutos de antigüedad y consulta su estado real en Mercado Pago, resolviendo pagos desfasados o webhooks perdidos.

3. **DevOps y Calidad:**
   - **`docker-compose.dev.yml`:** Entorno reproducible con MySQL 8 (`utf8mb4_unicode_ci`) y healthcheck.
   - **GitHub Actions CI (`.github/workflows/ci.yml`):** Validación automatizada de compilación y 55 pruebas unitarias/integración en .NET 8, junto con compilación estática de Astro en Node 20.

---

## 🏛️ Arquitectura del Sistema

```text
PROYECTO/
├── frontend/                     # Aplicación Astro con TailwindCSS y Neo-brutalismo
│   ├── src/pages/                # Páginas (index, contacto, servicios, checkout states)
│   ├── src/components/           # Componentes UI (Navbar, BentoFeatures, CheckoutModal, etc.)
│   └── public/js/                # Scripts del cliente (checkout.js, catalog.js)
│
├── src/
│   ├── DigitalServices.Domain          # Entidades, enums y reglas de dominio puras
│   ├── DigitalServices.Application     # Casos de uso, servicios de checkout y notificaciones
│   ├── DigitalServices.Infrastructure  # EF Core MySQL, Mercado Pago, SMTP y Webhooks
│   └── DigitalServices.Web             # ASP.NET Core Web API, Swagger, Rate Limiting y Middleware
│
├── tests/
│   └── DigitalServices.Tests           # 55 pruebas unitarias e integración de la lógica crítica
│
├── docker-compose.dev.yml        # Orquestación de desarrollo (MySQL 8 + Web API)
├── Dockerfile                    # Compilación multi-stage (Astro dist -> ASP.NET Core wwwroot)
└── .github/workflows/ci.yml      # Pipeline CI automatizado
```

---

## 🛠️ Requisitos Previos

- [.NET SDK 8.0+](https://dotnet.microsoft.com/)
- [Node.js 20+ y npm](https://nodejs.org/)
- [Docker y Docker Compose](https://www.docker.com/) (opcional pero recomendado)
- Credenciales de prueba de Mercado Pago

---

## 💻 Desarrollo Local

### 1. Iniciar Base de Datos MySQL con Docker

Ejecuta el servicio de base de datos listo para desarrollo:

```bash
docker compose -f docker-compose.dev.yml up -d mysql
```

Esto levanta MySQL 8.0 en el puerto `3306` con la base de datos `digitalservices_dev`.

### 2. Ejecutar Migraciones de Base de Datos

```bash
dotnet ef database update \
  --project src/DigitalServices.Infrastructure \
  --startup-project src/DigitalServices.Web
```

### 3. Ejecutar el Backend ASP.NET Core

```bash
dotnet run --project src/DigitalServices.Web
```

El backend se iniciará escuchando endpoints API y Swagger en:
- **Swagger UI:** `http://localhost:5000/swagger` (o `https://localhost:5001/swagger`)
- **Health Check:** `http://localhost:5000/health`

### 4. Ejecutar el Frontend Astro (Modo Desarrollo)

En otra terminal:

```bash
cd frontend
npm install
npm run dev
```

El frontend se iniciará en `http://localhost:4321` con recarga rápida (HMR) y proxy a la API.

---

## 🧪 Pruebas Automatizadas

Para compilar y ejecutar las **55 pruebas unitarias e integración** del backend:

```bash
dotnet test
```

Para verificar la compilación estática del frontend Astro:

```bash
cd frontend
npm run build
```

---

## ⚙️ Variables de Configuración

En `src/DigitalServices.Web/appsettings.json` o mediante variables de entorno en producción:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=digitalservices_dev;User=molinazdev;Password=molinazpass;TreatTinyAsBoolean=true;"
  },
  "MercadoPago": {
    "AccessToken": "APP_USR-...",
    "WebhookSecret": "...",
    "SuccessUrl": "https://molinazdev.lat/Checkout/Success",
    "FailureUrl": "https://molinazdev.lat/Checkout/Failure",
    "PendingUrl": "https://molinazdev.lat/Checkout/Pending",
    "NotificationUrl": "https://molinazdev.lat/api/payments/webhook"
  },
  "Notifications": {
    "Email": {
      "Enabled": true,
      "SmtpHost": "smtp.resend.com",
      "SmtpPort": 587,
      "SmtpUsername": "resend",
      "SmtpPassword": "...",
      "FromAddress": "notificaciones@molinazdev.lat",
      "FromName": "MolinazDev Ventas"
    },
    "AdminAlert": {
      "Enabled": true,
      "DiscordWebhookUrl": "https://discord.com/api/webhooks/...",
      "TelegramBotToken": "123456:ABC-DEF...",
      "TelegramChatId": "987654321"
    }
  },
  "PaymentReconciliation": {
    "Enabled": true,
    "IntervalSeconds": 120,
    "PendingThresholdMinutes": 10
  }
}
```

---

## 🐳 Despliegue con Docker y Coolify

La solución incluye un `Dockerfile` multi-stage optimizado que en la primera etapa compila el frontend Astro con Node.js 20, y luego copia los artefactos estáticos (`dist/`) directamente a la carpeta `wwwroot/` de ASP.NET Core, creando un único contenedor autónomo, seguro y ultra-rápido.

### Construir y Ejecutar Localmente

```bash
# Compilar imagen de producción
docker build -t molinazdev-app:latest .

# Ejecutar el contenedor
docker run -p 8080:8080 --env-file .env molinazdev-app:latest
```

### Configuración en Coolify / VPS

- **Tipo:** Dockerfile
- **Puerto expuesto:** `8080`
- **Health Check Path:** `/health`
- **Recomendación:** Configurar el webhook de Mercado Pago con la URL pública HTTPS proporcionada por Coolify.

---

## 🛡️ Seguridad y Buenas Prácticas

- **Cero secretos en el código:** Todos los tokens, contraseñas y webhooks secretos se configuran vía variables de entorno.
- **Validación Criptográfica HMAC:** Cada notificación entrante de Mercado Pago se verifica contra la clave secreta antes de procesar el pago.
- **Idempotencia Transaccional:** Se previene el doble procesamiento de webhooks repetidos mediante restricciones únicas a nivel de base de datos.
- **Aislamiento de Fallos en Notificaciones:** Si el servidor SMTP o Discord/Telegram están temporalmente caídos, la confirmación del pago en la base de datos se mantiene intacta.
- **Rate Limiting:** Control de peticiones concurrentes para evitar ataques de denegación de servicio o saturación del checkout.

---

&copy; MolinazDev &middot; Software y Servicios Digitales de Alto Rendimiento.
