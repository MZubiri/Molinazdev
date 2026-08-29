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
    description: "Sitios web rápidos y modernos para captar clientes y vender por internet.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000001",
        name: "Landing Page",
        description: "Página enfocada en conversión directa para campañas, anuncios o servicios específicos.",
        price: 799,
        currency: "PEN",
        deliveryDays: 10,
        features: [
          "Diseño 100% responsive para móviles",
          "Formulario de contacto y botón WhatsApp directo",
          "Dominio .com o .lat + 1er año de hosting incluido",
          "Soporte técnico por 60 días tras la entrega",
          "Optimización de velocidad y SEO en Google"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000002",
        name: "Web Autoadministrable",
        description: "Sitio web completo con panel intuitivo para que edites textos, fotos y secciones sin programar.",
        price: 1800,
        currency: "PEN",
        deliveryDays: 20,
        features: [
          "Panel de control fácil e intuitivo",
          "Hasta 8 secciones o páginas de contenido",
          "Dominio + 1er año de hosting incluido",
          "Soporte técnico por 60 días tras la entrega",
          "Correos corporativos y formularios integrados"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000010",
        name: "Ecommerce",
        description: "Tienda online completa con catálogo, carrito y pasarela de pagos integrada para vender 24/7.",
        price: 2400,
        currency: "PEN",
        deliveryDays: 25,
        features: [
          "Catálogo de productos, categorías y stock",
          "Pasarela integrada (Tarjetas, Yape, Plin, Transferencia)",
          "Dominio + 1er año de hosting incluido",
          "Soporte técnico por 60 días tras la entrega",
          "Panel de administración de pedidos y clientes"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000003",
        name: "Plataforma Personalizada",
        description: "Desarrollo a medida con arquitectura escalable y flujos según la operación de tu empresa.",
        price: 4000,
        currency: "PEN",
        deliveryDays: 45,
        features: [
          "Desde S/. 4,000 según alcance técnico acordado",
          "Relevamiento técnico y arquitectura en la nube",
          "Panel de usuarios con roles y base de datos",
          "Dominio + 1er año de hosting incluido",
          "Soporte técnico por 60 días tras la entrega"
        ]
      }
    ]
  },
  {
    id: "10000000-0000-0000-0000-000000000002",
    title: "Chatbots con IA",
    slug: "chatbots-con-ia",
    description: "Asistentes inteligentes entrenados con los datos de tu empresa para responder y vender 24/7.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000004",
        name: "Chatbot Básico",
        description: "Respuestas inmediatas a preguntas frecuentes y captura automática de leads en tu web.",
        price: 1800,
        currency: "PEN",
        deliveryDays: 14,
        features: [
          "Entrenado con documentos y preguntas de tu negocio",
          "Widget interactivo para tu página web",
          "Captura automática de datos de contacto",
          "Soporte técnico por 60 días tras la entrega",
          "Panel con historial de conversaciones"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000005",
        name: "Chatbot Empresarial",
        description: "Agente autónomo conectado con WhatsApp API, CRM y bases de datos internas.",
        price: 4800,
        currency: "PEN",
        deliveryDays: 30,
        features: [
          "Integración con WhatsApp API y CRM",
          "Búsqueda semántica inteligente en tus datos",
          "Derivación automática a asesores humanos",
          "Soporte técnico por 60 días tras la entrega",
          "Monitoreo de conversaciones y reportes"
        ]
      }
    ]
  },
  {
    id: "10000000-0000-0000-0000-000000000003",
    title: "Automatizaciones",
    slug: "automatizaciones",
    description: "Flujos automatizados que conectan tus herramientas para ahorrar horas de trabajo manual.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000006",
        name: "Automatización Básica",
        description: "Conexión de 2 a 3 aplicaciones para sincronizar datos y enviar alertas automáticas.",
        price: 1400,
        currency: "PEN",
        deliveryDays: 12,
        features: [
          "1 flujo de trabajo principal sincronizado",
          "Alertas en tiempo real por WhatsApp o Email",
          "Documentación clara de funcionamiento",
          "Soporte técnico por 60 días tras la entrega",
          "Garantía de ejecución sin fallos"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000007",
        name: "Automatización Personalizada",
        description: "Automatización avanzada de procesos con múltiples pasos, validaciones y lógica condicional.",
        price: 3900,
        currency: "PEN",
        deliveryDays: 25,
        features: [
          "Múltiples flujos de trabajo interconectados",
          "Procesamiento y limpieza automática de datos",
          "Integración con APIs privadas de la empresa",
          "Soporte técnico por 60 días tras la entrega",
          "Alertas de contingencia y registros detallados"
        ]
      }
    ]
  },
  {
    id: "10000000-0000-0000-0000-000000000004",
    title: "Software a Medida",
    slug: "software-a-medida",
    description: "Sistemas digitales y paneles administrativos desarrollados para la operación exacta de tu empresa.",
    iconUrl: null,
    packages: [
      {
        id: "20000000-0000-0000-0000-000000000008",
        name: "Software Estándar",
        description: "Módulo o herramienta interna con requerimientos y alcance previamente definidos.",
        price: 5500,
        currency: "PEN",
        deliveryDays: 35,
        features: [
          "Levantamiento técnico y diseño modular",
          "Base de datos optimizada y API REST segura",
          "Panel administrativo con roles de usuario",
          "Dominio + 1er año de hosting incluido",
          "Soporte técnico por 60 días tras la entrega"
        ]
      },
      {
        id: "20000000-0000-0000-0000-000000000009",
        name: "Software Empresarial",
        description: "Plataforma empresarial integral con múltiples módulos, alta seguridad y alta concurrencia.",
        price: 12000,
        currency: "PEN",
        deliveryDays: 60,
        features: [
          "Arquitectura empresarial distribuida y robusta",
          "Integraciones a medida con sistemas existentes",
          "Seguridad, cifrado y auditoría de accesos",
          "Dominio + 1er año de hosting incluido",
          "Soporte técnico prioritario por 60 días"
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
    // Return default pre-seeded catalog
  }
  return DEFAULT_SERVICES;
}

export function getDefaultCatalog(): ServiceCatalog[] {
  return DEFAULT_SERVICES;
}

export function getServiceBySlug(slug: string): ServiceCatalog | undefined {
  return DEFAULT_SERVICES.find(s => s.slug === slug);
}
