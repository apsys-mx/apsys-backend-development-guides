# Guía: Inicialización de Clean Architecture

## Descripción General

Esta guía documenta el proceso completo para crear un proyecto backend con **Clean Architecture** para APSYS. El proyecto generado es **agnóstico de frameworks específicos**, permitiendo máxima flexibilidad en la elección de tecnologías de persistencia y presentación.

## Propósito

Esta guía cubre la creación de:
- Solución .NET con gestión centralizada de paquetes
- Capa de dominio con entidades, validaciones y repositorios de interfaces
- Capa de aplicación con casos de uso, DTOs y validadores
- Capa de infraestructura base (estructura agnóstica)
- Capa de WebApi base (estructura mínima)
- Implementaciones opcionales:
  - **FastEndpoints** para WebApi (disponible)
  - **NHibernate** para persistencia (disponible vía configure-database)
  - **Minimal APIs** (próximamente)
  - **Entity Framework** (próximamente)

## Arquitectura del Proyecto

La guía genera un proyecto siguiendo los principios de **Clean Architecture**:

```
┌─────────────────────────────────────────┐
│           WebApi Layer                  │
│    (Base + Implementación opcional)     │
│     ├─ FastEndpoints (disponible)      │
│     ├─ Minimal APIs (futuro)           │
│     └─ MVC (futuro)                    │
└──────────────┬──────────────────────────┘
               │ depende de
┌──────────────▼──────────────────────────┐
│        Application Layer                │
│       (Use Cases + DTOs)                │
└──────────────┬──────────────────────────┘
               │ depende de
┌──────────────▼──────────────────────────┐
│          Domain Layer                   │
│  (Entities + Interfaces + Rules)        │
│         ★ NÚCLEO ★                      │
└──────────────▲──────────────────────────┘
               │ implementado por
┌──────────────┴──────────────────────────┐
│      Infrastructure Layer               │
│   (Base + Implementación opcional)      │
│     ├─ NHibernate (disponible)         │
│     ├─ Entity Framework (futuro)       │
│     └─ Dapper (futuro)                 │
└─────────────────────────────────────────┘
```

### Características Clave

✅ **Agnóstico de tecnología:** Domain y Application sin dependencias específicas
✅ **Infraestructura modular:** Elige tu ORM (NHibernate, EF, Dapper)
✅ **WebApi flexible:** Elige tu framework (FastEndpoints, Minimal APIs, MVC)
✅ **Testing First:** Proyectos de test para cada capa
✅ **Gestión Centralizada:** Paquetes NuGet versionados en un solo lugar
✅ **Extensible:** Fácil agregar nuevas implementaciones

## Estructura Final Generada

```
{ProjectName}/
├── {ProjectName}.sln
├── Directory.Packages.props
├── src/
│   ├── {ProjectName}.domain/
│   ├── {ProjectName}.application/
│   ├── {ProjectName}.infrastructure/      # Solo estructura base
│   └── {ProjectName}.webapi/              # Solo estructura base
└── tests/
    ├── {ProjectName}.domain.tests/
    ├── {ProjectName}.application.tests/
    ├── {ProjectName}.infrastructure.tests/
    └── {ProjectName}.webapi.tests/
```

> **Nota:** Proyectos auxiliares (`ndbunit`, `common.tests`) se crean al configurar la base de datos con una implementación específica.

## 📋 Mapa de Guías - Orden de Ejecución

La guía está organizada en **4 milestones principales** para desarrollo incremental. Cada archivo debe ejecutarse en orden secuencial.

### 📦 Milestone 1: Estructura Base y Dominio

**Estado:** ✅ Completado

1. **[01-estructura-base.md](./01-estructura-base.md)** (v1.0.1)
   - Crear solución .sln y carpetas src/ y tests/
   - Configurar Directory.Packages.props con gestión centralizada de paquetes
   - **Duración estimada:** 5-10 minutos

2. **[02-domain-layer.md](./02-domain-layer.md)** (v1.1.2)
   - Crear proyecto domain + tests
   - Copiar templates de interfaces de repositorios (IRepository, IReadOnlyRepository, IUnitOfWork)
   - Instalar FluentValidation
   - **Duración estimada:** 10-15 minutos
   - **Depende de:** 01-estructura-base.md

**Total Milestone 1:** ~20 minutos

---

### 🎯 Milestone 2: Capa de Aplicación

**Estado:** ✅ Completado

3. **[03-application-layer.md](./03-application-layer.md)** (v1.2.1)
   - Crear proyecto application + tests
   - Copiar templates de testing con AutoFixture
   - Configurar MediaTR y AutoMapper
   - Estructura para casos de uso (Commands/Queries)
   - **Duración estimada:** 15-20 minutos
   - **Depende de:** 02-domain-layer.md

**Total Milestone 2:** ~15 minutos

---

### 🔧 Milestone 3: Capa de Infraestructura (Base Agnóstica)

**Estado:** ✅ Completado

4. **[04-infrastructure-layer.md](./04-infrastructure-layer.md)** (v2.0.0)
   - Crear proyecto infrastructure + tests
   - Copiar READMEs explicativos para estructura de carpetas:
     - `repositories/` - Guía para implementar repositorios
     - `persistence/` - Guía para configurar ORM
     - `services/` - Guía para servicios externos
     - `configuration/` - Guía para Dependency Injection
   - **Sin código específico de tecnología** (agnóstico)
   - **Duración estimada:** 10-15 minutos
   - **Depende de:** 02-domain-layer.md

**Total Milestone 3:** ~15 minutos

---

### 🚀 Milestone 4: Capa de WebApi (Base + Implementación)

**Estado:** ✅ Completado

#### 4a. Estructura Base (Agnóstica)

5. **[05-webapi-layer.md](./05-webapi-layer.md)** (v2.0.0)
   - Crear proyecto webapi + tests
   - Copiar estructura base mínima:
     - Program.cs con endpoint /health
     - READMEs explicativos (endpoints/, dtos/, configuration/)
     - Configuración de variables de entorno (.env)
   - Instalar solo DotNetEnv
   - **Sin framework específico** (agnóstico)
   - **Duración estimada:** 10-15 minutos
   - **Depende de:** 02-domain-layer.md, 03-application-layer.md, 04-infrastructure-layer.md

#### 4b. Implementación de Framework (Opcional)

**Seleccionar UNA implementación según parámetro `--webapi-framework`:**

**Opción A: FastEndpoints (disponible)**

6a. **[webapi-implementations/fastendpoints/setup-fastendpoints.md](./webapi-implementations/fastendpoints/setup-fastendpoints.md)** (v1.0.0)
   - Instalar FastEndpoints, JWT Bearer, AutoMapper, FluentResults
   - Copiar templates específicos:
     - Program.cs completo con configuración
     - BaseEndpoint con manejo de errores
     - ServiceCollectionExtender para DI
     - Autorización personalizada (MustBeApplicationUser)
     - DTOs y mapping profiles
   - Configurar CORS, Swagger, JWT
   - **Duración estimada:** 20-25 minutos
   - **Depende de:** 05-webapi-layer.md

**Opción B: Minimal APIs (próximamente)**

6b. **[webapi-implementations/minimal-apis/setup-minimal-apis.md](./webapi-implementations/minimal-apis/)** (pendiente)
   - Configuración de Minimal APIs
   - Endpoints con métodos de extensión
   - **Estado:** 🔜 Próximamente

**Opción C: MVC (próximamente)**

6c. **[webapi-implementations/mvc/setup-mvc.md](./webapi-implementations/mvc/)** (pendiente)
   - Configuración de MVC Controllers
   - Controladores tradicionales
   - **Estado:** 🔜 Próximamente

**Total Milestone 4:** ~30-40 minutos (base + implementación)

---

### ⏳ Milestone 5: Configuración de Base de Datos (Opcional)

**Estado:** ⏳ Pendiente

Después de completar la estructura base, configurar persistencia específica con:

**📁 ../configure-database/** - Guías para configurar base de datos:
- **NHibernate + PostgreSQL** (disponible)
- **Entity Framework + SQL Server** (futuro)
- **Dapper + MySQL** (futuro)

---

## ⏱️ Tiempo Total Estimado

| Milestone | Estado | Duración |
|-----------|--------|----------|
| Milestone 1: Base + Domain | ✅ Completado | ~20 min |
| Milestone 2: Application | ✅ Completado | ~15 min |
| Milestone 3: Infrastructure (base) | ✅ Completado | ~15 min |
| Milestone 4a: WebApi (base) | ✅ Completado | ~15 min |
| Milestone 4b: FastEndpoints | ✅ Disponible | ~25 min |
| Milestone 5: Database | ⏳ Pendiente | ~30 min |
| **TOTAL (base agnóstica)** | | **~65 min** |
| **TOTAL (con FastEndpoints)** | | **~90 min** |
| **TOTAL (completo con DB)** | | **~120 min** |

## 🎯 Cómo Usar Esta Guía

### Opción 1: Ejecución Automatizada (con comando MCP/IA)

Un comando automatizado puede ejecutar las guías secuencialmente:

```bash
# Base agnóstica (sin tecnologías específicas)
/init-clean-architecture --project-name=MyProject

# Con FastEndpoints (default)
/init-clean-architecture --project-name=MyProject --webapi-framework=fastendpoints

# Con Minimal APIs (futuro)
/init-clean-architecture --project-name=MyProject --webapi-framework=minimal-apis
```

**Reemplazo de placeholders:**
- Todos los templates usan `{ProjectName}` que debe reemplazarse con el nombre real del proyecto
- Los comandos bash también usan `{ProjectName}` que debe reemplazarse antes de ejecutar

### Opción 2: Ejecución Manual (paso a paso)

Un desarrollador puede seguir la guía manualmente:

1. Abrir el primer archivo del milestone deseado
2. Leer las instrucciones y ejecutar los comandos en la terminal
3. Copiar los templates desde la carpeta `templates/init-clean-architecture/` y reemplazar `{ProjectName}` manualmente
4. Verificar que cada paso funcione antes de continuar al siguiente
5. Pasar al siguiente archivo cuando el actual esté completo

**Útil para:**
- Aprendizaje: Entender cómo funciona cada componente
- Debugging: Identificar problemas en pasos específicos
- Customización: Modificar componentes según necesidades específicas

### Opción 3: Ejecución por Milestones (incremental)

Puedes ejecutar milestone por milestone para validar el progreso:

```bash
# Milestone 1: Base + Domain
./execute 01-estructura-base.md
./execute 02-domain-layer.md
dotnet build  # Verificar que compile

# Milestone 2: Application
./execute 03-application-layer.md
dotnet build  # Verificar que compile

# Milestone 3: Infrastructure (base)
./execute 04-infrastructure-layer.md
dotnet build  # Verificar que compile

# Milestone 4a: WebApi (base)
./execute 05-webapi-layer.md
dotnet build  # Verificar que compile
dotnet run --project src/{ProjectName}.webapi  # Probar /health

# Milestone 4b: FastEndpoints (opcional)
./execute webapi-implementations/fastendpoints/setup-fastendpoints.md
dotnet build  # Verificar que compile
dotnet run --project src/{ProjectName}.webapi  # Probar API completa
```

## 📝 Formato de los Documentos

Cada documento de guía tiene la siguiente estructura estándar:

1. **Descripción:** Qué construye este paso
2. **Dependencias:** Qué pasos previos se requieren
3. **Validaciones Previas:** Qué verificar antes de empezar
4. **Pasos de Construcción:** Comandos bash secuenciales
5. **Referencia de Templates:** Tabla con descripción de cada template
6. **Verificación:** Cómo validar que todo funcionó
7. **Próximos Pasos:** Qué hacer después
8. **Historial de Versiones:** Cambios del documento

### Instrucciones de Templates

Las guías usan dos formatos para indicar operaciones con templates:

#### Copiar archivo individual
```markdown
📄 COPIAR TEMPLATE: `templates/init-clean-architecture/domain/IRepository.cs` → `src/{ProjectName}.domain/IRepository.cs`
```

#### Copiar directorio completo
```markdown
📁 COPIAR DIRECTORIO COMPLETO: `templates/init-clean-architecture/domain/` → `src/{ProjectName}.domain/`
```

Después de cada instrucción hay un bloque que explica qué se debe hacer:
```markdown
> El agente/usuario debe:
> 1. Descargar todos los archivos desde `templates/init-clean-architecture/...` en GitHub
> 2. Copiarlos a `src/{ProjectName}...` respetando estructura
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
```

## 📁 Estructura de Templates

Los templates están organizados para reflejar la estructura modular:

```
templates/init-clean-architecture/
├── domain/                          # Templates de Domain
├── domain.tests/                    # Templates de tests de Domain
├── application.tests/               # Templates de tests de Application
├── infrastructure/                  # Templates base (READMEs)
│   ├── README.md
│   ├── repositories/README.md
│   ├── persistence/README.md
│   ├── services/README.md
│   └── configuration/README.md
├── webapi/                          # Templates base (mínimo)
│   ├── Program.cs
│   ├── README.md
│   ├── .env.example
│   ├── endpoints/README.md
│   ├── dtos/README.md
│   ├── configuration/README.md
│   └── Properties/InternalsVisibleTo.cs
└── webapi-implementations/          # Implementaciones específicas
    └── fastendpoints/
        ├── Program.cs (completo)
        ├── features/BaseEndpoint.cs
        ├── infrastructure/ServiceCollectionExtender.cs
        ├── dtos/GetManyAndCountResultDto.cs
        └── ...
```

## 🔄 Siguiente Paso

Una vez completada la estructura base, elige tu camino:

### Opción A: Configurar Base de Datos
**[../configure-database/](../configure-database/)** - Configuración de persistencia:
- NHibernate + PostgreSQL (disponible)
- Entity Framework + SQL Server (futuro)
- Dapper + MySQL (futuro)

### Opción B: Implementar Endpoints
Si elegiste **solo la base** en Milestone 4a, ahora implementa el framework:
- **[webapi-implementations/fastendpoints/](./webapi-implementations/fastendpoints/)** (disponible)
- Minimal APIs (futuro)
- MVC (futuro)

## 🛠️ Stack Tecnológico

### Base (Incluido en estructura agnóstica)
- **.NET 9.0** - Framework base
- **C# 13** - Lenguaje
- **FluentValidation 12.0** - Validaciones declarativas

### Testing (Incluido)
- **NUnit 4.2** - Framework de testing
- **Moq 4.20** - Mocking framework
- **AutoFixture 4.18** - Generación automática de datos de prueba
- **FluentAssertions 8.5** - Aserciones expresivas

### Implementaciones Opcionales

#### WebApi (elegir una)
- **FastEndpoints 7.0** ✅ Disponible
- **Minimal APIs** (nativo .NET) 🔜 Próximamente
- **ASP.NET Core MVC** (nativo .NET) 🔜 Próximamente

#### Persistencia (elegir una)
- **NHibernate 5.5** ✅ Disponible (vía configure-database)
- **Entity Framework Core** 🔜 Próximamente
- **Dapper** 🔜 Próximamente

#### Complementos opcionales
- **AutoMapper 15.0** - Mapeo de objetos (con FastEndpoints)
- **System.Linq.Dynamic.Core 1.6** - LINQ dinámico (con NHibernate)
- **DotNetEnv 3.1** - Variables de entorno (base WebApi)
- **Spectre.Console 0.50** - CLI interactiva (migrations)

## 📚 Referencias

- **Manual completo:** [MANUAL_CONSTRUCCION_PROYECTO.md](../../MANUAL_CONSTRUCCION_PROYECTO.md)
- **Repositorio:** [README.md](../../README.md)
- **Templates:** [templates/README.md](../../templates/README.md)

## 🤝 Contribuir

Para agregar o modificar componentes:

1. Revisar el manual completo para extraer información
2. Seguir el formato de los documentos existentes
3. Mantener el principio de modularidad (base + implementaciones)
4. Actualizar este README con los cambios
5. Probar manualmente los comandos antes de commitear

## 📅 Changelog

- **2025-01-30:** Reestructuración modular - v2.0.0
  - Infrastructure y WebApi ahora son capas base agnósticas
  - Implementaciones específicas separadas en subcarpetas
  - FastEndpoints disponible como implementación opcional
  - Templates reorganizados en `templates/init-clean-architecture/`
- **2025-01-30:** Milestone 4 completado (WebApi Layer) - v1.4.7
- **2025-01-30:** Milestone 3 completado (Infrastructure Layer) - v1.3.0
- **2025-01-30:** Milestone 2 completado (Application Layer) - v1.2.0
- **2025-01-29:** Milestone 1 completado (Base + Domain Layer) - v1.0.0
