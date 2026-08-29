export interface ServicePackage {
  id: string;
  name: string;
  description: string | null;
  price: number;
  currency: string;
  deliveryDays: number;
  features: string[];
}

export interface ServiceCatalog {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  iconUrl: string | null;
  packages: ServicePackage[];
}

export interface CreateCheckoutPayload {
  packageId: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  companyName?: string;
}

export interface CreateCheckoutResponse {
  checkoutUrl: string;
  preferenceId: string;
}

const DEFAULT_SERVICES: ServiceCatalog[] = [
  {
    id: "10000000-0000-0000-0000-000000000001",
    title: "Desarrollo Web",
    slug: "desarrollo-web",
    description: "Sitios web de alto rendimiento, diseño premium y arquitectura escalable para tu negocio.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000001",
        name: "Landing Page",
        description: "Página de máxima conversión para campañas, lanzamientos o productos.",
        price: 1200,
        currency: "PEN",
        deliveryDays: 10,
        features: [
          "Diseño responsive ultra rápido",
          "Formulario de captación y WhatsApp",
          "SEO técnico y Core Web Vitals 95+",
          "Integración de Analytics y Pixel",
          "Dominio y hosting guiado"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000002",
        name: "Sitio Corporativo",
        description: "Presencia digital completa para consolidar autoridad de marca y captar clientes.",
        price: 2800,
        currency: "PEN",
        deliveryDays: 20,
        features: [
          "Hasta 8 secciones estructuradas",
          "Panel de administración de contenidos",
          "Optimización SEO avanzada",
          "Correos corporativos integrados",
          "Soporte técnico por 30 días"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000003",
        name: "Plataforma Personalizada",
        description: "Aplicación web con flujos de trabajo específicos, autenticación y paneles a medida.",
        price: 6500,
        currency: "PEN",
        deliveryDays: 45,
        features: [
          "Descubrimiento y prototipado UI/UX",
          "Arquitectura escalable en la nube",
          "Base de datos y API seguras",
          "Pasarela de pagos integrada",
          "Capacitación y documentación completa"
        ]
      }
    ]
  },
  {
    id: "10000000-0000-0000-0000-000000000002",
    title: "Chatbots con IA",
    slug: "chatbots-con-ia",
    description: "Asistentes inteligentes entrenados con el conocimiento de tu empresa para atender 24/7.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000004",
        name: "Chatbot Básico",
        description: "Respuestas inmediatas a preguntas frecuentes y captura de leads en tu web.",
        price: 1800,
        currency: "PEN",
        deliveryDays: 14,
        features: [
          "Entrenado con documentos y FAQs",
          "Widget interactivo para la web",
          "Captura automática de contactos",
          "Panel de historial de conversaciones",
          "Métricas esenciales de uso"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000005",
        name: "Chatbot Empresarial",
        description: "Agente autónomo integrado con CRM, WhatsApp y bases de datos internas.",
        price: 4800,
        currency: "PEN",
        deliveryDays: 30,
        features: [
          "Conexión con CRM / WhatsApp API",
          "Escalamiento automático a humanos",
          "Búsqueda semántica y RAG avanzado",
          "Manejo de reservas o cotizaciones",
          "Monitoreo continuo y auditoría"
        ]
      }
    ]
  },
  {
    id: "10000000-0000-0000-0000-000000000003",
    title: "Automatizaciones",
    slug: "automatizaciones",
    description: "Flujos de trabajo conectados que eliminan tareas repetitivas y aceleran tu operación.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000006",
        name: "Automatización Básica",
        description: "Conexión de 2 a 3 herramientas para sincronizar datos sin intervención manual.",
        price: 1400,
        currency: "PEN",
        deliveryDays: 12,
        features: [
          "1 flujo de trabajo principal",
          "Sincronización en tiempo real",
          "Notificaciones automáticas por Slack/Email",
          "Documentación operativa detallada",
          "Garantía de funcionamiento"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000007",
        name: "Automatización Personalizada",
        description: "Sistemas complejos de procesamiento de datos, pipelines y alertas automáticas.",
        price: 3900,
        currency: "PEN",
        deliveryDays: 25,
        features: [
          "Múltiples flujos interconectados",
          "Transformación y limpieza de datos",
          "Alertas proactivas de fallos",
          "Integración con APIs privadas",
          "Mantenimiento incluido por 60 días"
        ]
      }
    ]
  },
  {
    id: "10000000-0000-0000-0000-000000000004",
    title: "Software a Medida",
    slug: "software-a-medida",
    description: "Soluciones de software empresariales diseñadas exactamente para las necesidades de tu empresa.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000008",
        name: "Software Estándar",
        description: "Módulo o herramienta interna con alcance y requerimientos definidos.",
        price: 5500,
        currency: "PEN",
        deliveryDays: 35,
        features: [
          "Levantamiento exhaustivo de requerimientos",
          "Arquitectura modular y escalable",
          "Pruebas automáticas y QA",
          "Despliegue en servidor propio o nube",
          "Soporte post-lanzamiento"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000009",
        name: "Software Empresarial",
        description: "Plataforma integral con múltiples módulos, roles de usuario, integraciones y alta seguridad.",
        price: 12000,
        currency: "PEN",
        deliveryDays: 60,
        features: [
          "Arquitectura empresarial distribuida",
          "Integraciones avanzadas a medida",
          "Seguridad, cifrado y control de acceso RBAC",
          "Monitoreo 24/7 y logs estructurados",
          "Soporte prioritario y SLA garantizado"
        ]
      }
    ]
  }
];

export async function getCatalog(): Promise<ServiceCatalog[]> {
  try {
    const res = await fetch('/api/catalog');
    if (res.ok) {
      return await res.json();
    }
  } catch (error) {
    // Return default pre-seeded catalog during static build or when offline
  }
  return DEFAULT_SERVICES;
}

export function getDefaultCatalog(): ServiceCatalog[] {
  return DEFAULT_SERVICES;
}

export function getServiceBySlug(slug: string): ServiceCatalog | undefined {
  return DEFAULT_SERVICES.find(s => s.slug === slug);
}
