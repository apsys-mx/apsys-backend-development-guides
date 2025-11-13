# APSYS Backend Development Guides

> **Versión:** 2.1.0 | **Release:** 2025-01-30 | **Estado:** Milestone 4 Completado (Arquitectura Modular + Agnóstico de Persistencia)

## Descripción

Este repositorio contiene las **guías de desarrollo** y **templates** de APSYS para crear proyectos backend con **Clean Architecture**.

Las guías pueden seguirse manualmente o ejecutarse automáticamente mediante agentes de IA, permitiendo generar estructuras completas de proyectos .NET siguiendo las mejores prácticas y estándares de APSYS, eliminando el trabajo manual repetitivo y asegurando consistencia entre proyectos.

## Versionado

Este repositorio usa **versionado semántico** (MAJOR.MINOR.PATCH):

- **Versión actual:** 2.1.0
- **Compatibilidad:** .NET 9.0
- **Documentación completa:** [VERSIONING.md](VERSIONING.md)
- **Metadata de versión:** [guides-version.json](guides-version.json)

**Para agentes automatizados:**
```typescript
// Leer versión
const version = await fetch(
  'https://raw.githubusercontent.com/.../guides-version.json'
).then(r => r.json())

console.log(`Using APSYS Guides v${version.version}`)
```

## Propósito del Repositorio

Este repositorio sirve como la **fuente de verdad** para:

1. **Guías paso a paso** de cómo construir proyectos backend
2. **Templates** de código reutilizables
3. **Documentación técnica** de la arquitectura y componentes
4. **Estándares** de desarrollo del equipo APSYS

## Estructura del Repositorio

```
apsys-backend-development-guides/
│
├── README.md                                 # Este archivo
├── MANUAL_CONSTRUCCION_PROYECTO.md          # Manual técnico completo
├── conversacion-mcp-servers.txt             # Conversación de diseño original
│
├── guides/                                   # Guías de desarrollo por tool
│   ├── README.md                             # Índice de guías
│   │
│   ├── init-clean-architecture/              # Tool #1: Inicialización
│   │   ├── README.md
│   │   ├── 01-estructura-base.md
│   │   ├── 02-domain-layer.md
│   │   ├── 03-application-layer.md
│   │   ├── 04-infrastructure-layer.md
│   │   ├── 05-webapi-layer.md
│   │   ├── webapi-implementations/           # Implementaciones específicas
│   │   │   └── fastendpoints/
│   │   │       └── setup-fastendpoints.md
│   │   ├── 06-migrations-base.md                (pendiente)
│   │   └── 07-testing-support.md                (pendiente)
│   │
│   └── configure-database/                   # Tool #2: Configuración BD
│       ├── README.md                            (pendiente)
│       ├── 01-setup-postgresql.md               (pendiente)
│       └── 02-setup-sqlserver.md                (pendiente)
│
└── templates/                                # Templates de código (futuro)
    └── (por definir)
```

## Guías Disponibles

### 1. Inicialización de Clean Architecture

**Estado:** 🟡 En desarrollo (Milestone 4 completado)

Crea la estructura completa de un proyecto backend con Clean Architecture, independiente de cualquier base de datos específica.

**Proceso:**
Ejecutar las guías secuenciales desde `guides/init-clean-architecture/` siguiendo el orden de los milestones (ver README de la guía para el mapa detallado).

**Documentación:** [guides/init-clean-architecture/README.md](guides/init-clean-architecture/README.md)

**Genera:**
- Solución .NET con gestión centralizada de paquetes
- Capa de dominio con entidades, validaciones y repositorios de interfaces
- Capa de aplicación con use cases, DTOs y validadores
- Capa de infraestructura con estructura base agnóstica (sin ORM específico)
- API REST con estructura base mínima + Swagger
- Implementación opcional de FastEndpoints (JWT Bearer, CORS, AutoMapper)
- Sistema de migraciones con FluentMigrator (pendiente)
- Proyectos de testing con AutoFixture y Moq (pendiente)

---

### 2. Configuración de Base de Datos

**Estado:** ⏳ Pendiente

Configura un proyecto existente para trabajar con una base de datos específica (PostgreSQL o SQL Server).

**Proceso:**
Ejecutar las guías desde `guides/configure-database/` después de completar la inicialización de Clean Architecture.

**Documentación:** [guides/configure-database/README.md](guides/configure-database/README.md) *(pendiente)*

**Configura:**
- Paquetes NuGet específicos de BD (Npgsql o Microsoft.Data.SqlClient)
- Driver y dialect de NHibernate
- ConnectionStringBuilder
- Proyecto NDbUnit para datos de prueba
- Sistema de migraciones
- Variables de entorno

---

## Arquitectura de Proyectos Generados

Los proyectos generados siguen los principios de **Clean Architecture**:

```
┌─────────────────────────────────────────┐
│            WebApi Layer                 │
│      (FastEndpoints + Swagger)          │
└──────────────┬──────────────────────────┘
               │ depende de
┌──────────────▼──────────────────────────┐
│         Application Layer               │
│       (Use Cases + DTOs)                │
└──────────────┬──────────────────────────┘
               │ depende de
┌──────────────▼──────────────────────────┐
│           Domain Layer                  │
│  (Entities + Interfaces + Rules)        │
│         ★ NÚCLEO ★                      │
└──────────────▲──────────────────────────┘
               │ implementado por
┌──────────────┴──────────────────────────┐
│       Infrastructure Layer              │
│   (Repositories + NHibernate + BD)      │
└─────────────────────────────────────────┘
```

### Características Clave

✅ **Independencia de frameworks** - Lógica de negocio sin dependencias externas
✅ **Independencia de UI** - Domain no conoce la API
✅ **Independencia de BD** - Domain no conoce la persistencia
✅ **Testeable** - Cada capa tiene sus propios tests
✅ **Separación de responsabilidades** - Cada capa tiene un propósito claro

## Stack Tecnológico

### Backend Core (Base)
- **.NET 9.0** - Framework base
- **C# 13** - Lenguaje
- **Swagger/OpenAPI** - Documentación de API
- **DotNetEnv 3.1** - Gestión de variables de entorno

### Implementaciones Opcionales

#### API & Web
- **FastEndpoints 7.0** - Framework de API REST (default)
- **JWT Bearer** - Autenticación (con FastEndpoints)
- **AutoMapper 15.0** - Mapeo de objetos (con FastEndpoints)
- **FluentValidation 12.0** - Validaciones (con FastEndpoints)
- *Próximamente: Minimal APIs, MVC*

#### Persistencia
- **NHibernate 5.5** - ORM (disponible en configure-database)
- **FluentMigrator 7.1** - Migraciones de BD
- **PostgreSQL** o **SQL Server** - Base de datos
- *Próximamente: Entity Framework, Dapper*

### Testing (Pendiente)
- **NUnit 4.2** - Framework de testing
- **Moq 4.20** - Mocking framework
- **AutoFixture 4.18** - Generación automática de datos de prueba
- **FluentAssertions 8.5** - Aserciones expresivas

### Utilidades
- **Spectre.Console 0.50** - CLI interactiva elegante
- **System.Linq.Dynamic.Core 1.6** - LINQ dinámico (con NHibernate)

## Flujo de Trabajo

### Para crear un proyecto nuevo desde cero:

```mermaid
graph LR
    A[Inicio] --> B[init-clean-architecture]
    B --> C[configure-database]
    C --> D[Proyecto listo para desarrollo]
```

**Paso 1:** Inicializar arquitectura base
```bash
init-clean-architecture --name=MiProyecto --version=net9.0 --path=C:\projects\miproyecto
```

**Paso 2:** Configurar base de datos
```bash
configure-database --project-path=C:\projects\miproyecto --db=PostgreSQL
```

**Resultado:** Proyecto completamente configurado y listo para:
- Agregar entidades de dominio
- Crear endpoints de API
- Escribir migraciones de BD
- Ejecutar tests

## Documentos Clave

### Para Usuarios del Servidor MCP

- **[guides/README.md](guides/README.md)** - Índice completo de guías
- **[guides/init-clean-architecture/README.md](guides/init-clean-architecture/README.md)** - Tool de inicialización

### Para Desarrollo y Referencia

- **[MANUAL_CONSTRUCCION_PROYECTO.md](MANUAL_CONSTRUCCION_PROYECTO.md)** - Manual técnico completo y detallado
- **[conversacion-mcp-servers.txt](conversacion-mcp-servers.txt)** - Conversación original de diseño del sistema

### Para Contribuidores

- **[guides/README.md#contribuir](guides/README.md#contribuir)** - Cómo agregar o modificar guías

## Uso de las Guías

### Opción 1: Automatizado (con agente IA)

Un agente de IA puede ejecutar automáticamente todos los pasos leyendo las guías secuencialmente:

```
# El agente lee y ejecuta:
1. guides/init-clean-architecture/01-estructura-base.md
2. guides/init-clean-architecture/02-domain-layer.md
3. ... y así sucesivamente
```

### Opción 2: Manual (paso a paso)

Las guías también pueden seguirse manualmente:

1. Abre el documento relevante (ej: [01-estructura-base.md](guides/init-clean-architecture/01-estructura-base.md))
2. Ejecuta los comandos secuencialmente
3. Verifica cada paso con las secciones de validación

**Útil para:**
- Entender cómo funciona cada componente
- Depurar problemas específicos
- Extender proyectos manualmente
- Capacitación del equipo

## Estado del Proyecto

### ✅ Completado

- [x] Estructura de carpetas para guías
- [x] Tool: init-clean-architecture (Milestone 4 - Arquitectura Modular v2.0.0)
  - [x] 01-estructura-base.md (v1.0.1)
  - [x] 02-domain-layer.md (v2.0.0 - sin interfaces de repositorios)
  - [x] 03-application-layer.md (v1.2.1)
  - [x] 04-infrastructure-layer.md (v2.0.0 - base agnóstica)
  - [x] 05-webapi-layer.md (v2.0.0 - base mínima)
  - [x] webapi-implementations/fastendpoints/setup-fastendpoints.md (v1.1.0)
- [x] Templates de código (dominio, aplicación, infraestructura, webapi base y webapi implementations)
- [x] Comando init-clean-architecture.md (v2.1.0)
- [x] Documentación de arquitectura
- [x] Manual técnico completo
- [x] Sistema de versionado semántico

### 🟡 En Progreso

- [ ] Guía: init-clean-architecture (Milestone 5)
  - [ ] 06-migrations-base.md
  - [ ] 07-testing-support.md

### ⏳ Pendiente

- [ ] Guía: configure-database
  - [ ] Documentación completa
  - [ ] Guía de PostgreSQL
  - [ ] Guía de SQL Server
- [ ] Templates adicionales de código

## Ventajas de este Enfoque

### 1. Separación de Responsabilidades

Al separar `init-clean-architecture` y `configure-database`:

✅ Puedes desarrollar lógica de negocio sin elegir la BD primero
✅ Cambiar de BD es solo re-ejecutar `configure-database`
✅ Testing sin dependencias de infraestructura
✅ Proyectos más portables entre equipos

### 2. Consistencia

Todos los proyectos APSYS siguen:

✅ La misma estructura de carpetas
✅ Las mismas convenciones de nombres
✅ Los mismos patrones arquitecturales
✅ El mismo stack tecnológico

### 3. Velocidad

De horas de setup manual a minutos con automatización:

- ⏰ **Manual:** 2-3 horas configurando proyecto
- ⚡ **Automatizado:** 2-3 minutos ejecutando las guías

### 4. Reducción de Errores

El código generado:

✅ Está probado y validado
✅ Sigue las mejores prácticas
✅ Tiene toda la configuración correcta
✅ Incluye tests desde el inicio

## Casos de Uso del Equipo

### Para Developers

```bash
# Crear nuevo microservicio
init-clean-architecture --name=UsuariosService --version=net9.0 --path=./services/usuarios
configure-database --project-path=./services/usuarios --db=PostgreSQL

# Empezar a codear entidades inmediatamente
```

### Para Tech Leads

- Revisar guías como documentación de estándares
- Usar como material de onboarding
- Validar que proyectos existentes sigan las convenciones

### Para QA

- Entender la estructura de proyectos
- Saber dónde encontrar tests
- Validar que nuevos proyectos tengan testing

## Contribuir

### Para Agregar/Modificar Guías

1. Extraer información del [MANUAL_CONSTRUCCION_PROYECTO.md](MANUAL_CONSTRUCCION_PROYECTO.md)
2. Seguir el formato estándar de documentos en [guides/README.md](guides/README.md)
3. Probar todos los comandos manualmente
4. Actualizar los READMEs correspondientes
5. Documentar dependencias entre pasos

### Para Reportar Problemas

- Abre un issue describiendo el problema
- Incluye: qué guía, qué paso, qué error
- Adjunta logs si es posible

### Para Sugerir Mejoras

- Describe el caso de uso
- Explica qué se podría automatizar
- Propón la estructura de la nueva guía

## Recursos Adicionales

### Documentación Externa

- **Clean Architecture:** [Blog de Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- **FastEndpoints:** [Documentación oficial](https://fast-endpoints.com/)
- **NHibernate:** [Documentación oficial](https://nhibernate.info/)
- **FluentMigrator:** [Documentación oficial](https://fluentmigrator.github.io/)

### Dentro del Repositorio

- **Manual completo:** [MANUAL_CONSTRUCCION_PROYECTO.md](MANUAL_CONSTRUCCION_PROYECTO.md)
- **Guías detalladas:** [guides/README.md](guides/README.md)
- **Conversación de diseño:** [conversacion-mcp-servers.txt](conversacion-mcp-servers.txt)

## Preguntas Frecuentes

### ¿Por qué Clean Architecture?

Proporciona:
- Separación clara de responsabilidades
- Código testeable sin dependencias
- Flexibilidad para cambiar frameworks
- Escalabilidad a largo plazo

### ¿Por qué FastEndpoints en lugar de Controllers?

FastEndpoints ofrece:
- Menor boilerplate que MVC Controllers
- Mejor performance
- Validación integrada
- Endpoints como clases independientes (REPR pattern)

### ¿Por qué NHibernate en lugar de Entity Framework?

Decisión del equipo APSYS basada en:
- Mayor control sobre queries
- Mejor soporte para escenarios complejos
- Experiencia previa del equipo

### ¿Puedo usar estas guías manualmente?

¡Sí! Las guías están diseñadas para ser:
- Ejecutables manualmente paso a paso
- Autocontenidas
- Educativas
- Referencia de mejores prácticas

### ¿Qué pasa con proyectos existentes?

Para proyectos existentes:
- Usa las guías como referencia
- Adapta componentes específicos que necesites
- Revisa el manual completo para casos especiales

## Licencia

Este proyecto es de uso interno de APSYS.

## Contacto

Para preguntas, sugerencias o problemas:
- Abre un issue en este repositorio
- Contacta al equipo de arquitectura

---

**Última actualización:** 2025-01-30
**Versión:** 2.1.0-milestone4
**Mantenedores:** Equipo de Desarrollo APSYS
