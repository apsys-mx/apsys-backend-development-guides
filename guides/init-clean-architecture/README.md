# Guía: Inicialización de Clean Architecture

## Descripción General

Esta guía documenta el proceso completo para crear un proyecto backend con **Clean Architecture** para APSYS. El proyecto generado es independiente de cualquier base de datos específica, permitiendo máxima flexibilidad en la elección de tecnología de persistencia.

## Propósito

Esta guía cubre la creación de:
- Solución .NET con gestión centralizada de paquetes
- Capa de dominio con entidades, validaciones y repositorios de interfaces
- Capa de aplicación con casos de uso, DTOs y validadores
- Capa de infraestructura con repositorios NHibernate y sistema de filtering
- API REST con FastEndpoints, Swagger, JWT y AutoMapper
- Sistema de migraciones de base de datos (pendiente)
- Proyectos de testing completos (pendiente)

## Arquitectura del Proyecto

La guía genera un proyecto siguiendo los principios de **Clean Architecture**:

```
┌─────────────────────────────────────────┐
│           WebApi Layer                  │
│      (FastEndpoints + Swagger)          │
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
│   (Repositories + NHibernate)           │
└─────────────────────────────────────────┘
```

### Características Clave

✅ **Independencia de BD:** Sin código específico de PostgreSQL o SQL Server
✅ **Testing First:** Proyectos de test para cada capa
✅ **Gestión Centralizada:** Paquetes NuGet versionados en un solo lugar
✅ **Filtering Avanzado:** Sistema de filtrado con LINQ dinámico
✅ **Validaciones:** FluentValidation integrado
✅ **API Moderna:** FastEndpoints + Swagger + JWT Bearer

## Estructura Final Generada

```
{ProjectName}/
├── {ProjectName}.sln
├── Directory.Packages.props
├── src/
│   ├── {ProjectName}.domain/
│   ├── {ProjectName}.application/
│   ├── {ProjectName}.infrastructure/
│   └── {ProjectName}.webapi/
└── tests/
    ├── {ProjectName}.domain.tests/
    ├── {ProjectName}.application.tests/
    ├── {ProjectName}.infrastructure.tests/
    ├── {ProjectName}.webapi.tests/
    ├── {ProjectName}.ndbunit/           (auxiliar)
    └── {ProjectName}.common.tests/      (auxiliar)
```

## 📋 Mapa de Guías - Orden de Ejecución

La guía está organizada en **4 milestones** para facilitar desarrollo y testing incremental. Cada archivo debe ejecutarse en orden secuencial.

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

### 🔧 Milestone 3: Infraestructura

**Estado:** ✅ Completado

4. **[04-infrastructure-layer.md](./04-infrastructure-layer.md)** (v1.3.5)
   - Crear proyectos auxiliares (ndbunit, common.tests)
   - Crear proyecto infrastructure + tests
   - Copiar templates de repositorios NHibernate (NHRepository, NHReadOnlyRepository, NHUnitOfWork)
   - Copiar sistema de filtering completo (8 archivos: QueryStringParser, FilterExpressionParser, operators, sorting)
   - Instalar NHibernate y System.Linq.Dynamic.Core
   - **Duración estimada:** 20-25 minutos
   - **Depende de:** 02-domain-layer.md
   - **Recomendado:** 03-application-layer.md (para entender qué necesita Application)

**Total Milestone 3:** ~25 minutos

---

### 🚀 Milestone 4: WebApi

**Estado:** ✅ Completado

5. **[05-webapi-configuration.md](./05-webapi-configuration.md)** (v1.4.5)
   - Crear proyecto webapi + tests
   - Copiar templates de infrastructure (ServiceCollectionExtender, authorization)
   - Copiar templates de features (BaseEndpoint, HelloEndpoint)
   - Copiar templates de DTOs y mapping profiles
   - Copiar Program.cs configurado
   - Configurar FastEndpoints, Swagger, JWT Bearer, CORS
   - Configurar AutoMapper con mapeo genérico
   - Configurar DotNetEnv para variables de entorno
   - **Duración estimada:** 25-30 minutos
   - **Depende de:** 02-domain-layer.md, 03-application-layer.md, 04-infrastructure-layer.md

**Total Milestone 4:** ~30 minutos

---

### ⏳ Milestone 5: Migraciones y Testing (PENDIENTE)

**Estado:** ⏳ Pendiente

6. **06-migrations-base.md** (pendiente)
   - Crear proyecto migrations con FluentMigrator
   - CLI interactivo con Spectre.Console
   - Program.cs genérico (sin provider específico)
   - **Duración estimada:** 20-25 minutos
   - **Depende de:** 04-infrastructure-layer.md

7. **07-testing-support.md** (pendiente)
   - Configurar proyectos ndbunit y common.tests
   - Schemas XSD para datos de prueba
   - Generadores de datos
   - **Duración estimada:** 15-20 minutos
   - **Depende de:** Todos los anteriores

**Total Milestone 5:** ~40 minutos

---

## ⏱️ Tiempo Total Estimado

| Milestone | Estado | Duración |
|-----------|--------|----------|
| Milestone 1: Base + Domain | ✅ Completado | ~20 min |
| Milestone 2: Application | ✅ Completado | ~15 min |
| Milestone 3: Infrastructure | ✅ Completado | ~25 min |
| Milestone 4: WebApi | ✅ Completado | ~30 min |
| Milestone 5: Migrations + Testing | ⏳ Pendiente | ~40 min |
| **TOTAL (hasta M4)** | | **~90 min** |
| **TOTAL (completo)** | | **~130 min** |

## 🎯 Cómo Usar Esta Guía

### Opción 1: Ejecución Automatizada (con agente IA)

Un agente de IA puede leer estos archivos secuencialmente y ejecutar los comandos automáticamente:

```
1. Leer 01-estructura-base.md → Ejecutar comandos bash → Copiar templates
2. Leer 02-domain-layer.md → Ejecutar comandos bash → Copiar templates
3. Leer 03-application-layer.md → Ejecutar comandos bash → Copiar templates
4. Leer 04-infrastructure-layer.md → Ejecutar comandos bash → Copiar templates
5. Leer 05-webapi-configuration.md → Ejecutar comandos bash → Copiar templates
```

**Reemplazo de placeholders:**
- Todos los templates usan `{ProjectName}` que debe reemplazarse con el nombre real del proyecto
- Los comandos bash también usan `{ProjectName}` que debe reemplazarse antes de ejecutar

### Opción 2: Ejecución Manual (paso a paso)

Un desarrollador puede seguir la guía manualmente:

1. Abrir el primer archivo del milestone deseado
2. Leer las instrucciones y ejecutar los comandos en la terminal
3. Copiar los templates desde la carpeta `templates/` y reemplazar `{ProjectName}` manualmente
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

# Milestone 3: Infrastructure
./execute 04-infrastructure-layer.md
dotnet build  # Verificar que compile

# Milestone 4: WebApi
./execute 05-webapi-configuration.md
dotnet build  # Verificar que compile
dotnet run --project src/{ProjectName}.webapi  # Probar la API
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
📄 COPIAR TEMPLATE: `templates/domain/IRepository.cs` → `src/{ProjectName}.domain/IRepository.cs`
```

#### Copiar directorio completo
```markdown
📁 COPIAR DIRECTORIO COMPLETO: `templates/domain/` → `src/{ProjectName}.domain/`
```

Después de cada instrucción hay un bloque que explica qué se debe hacer:
```markdown
> El agente/usuario debe:
> 1. Descargar todos los archivos desde `templates/...` en GitHub
> 2. Copiarlos a `src/{ProjectName}...` respetando estructura
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
```

## 🔄 Siguiente Paso

Una vez completada esta guía (todos los milestones), el proyecto está listo para configurar una base de datos específica con:

**[../configure-database/README.md](../configure-database/README.md)** - Configuración de PostgreSQL o SQL Server

## 🛠️ Stack Tecnológico

### Frameworks y Bibliotecas
- **.NET 9.0** - Framework base
- **C# 13** - Lenguaje
- **FastEndpoints 7.0** - API REST framework
- **NHibernate 5.5** - ORM
- **FluentValidation 12.0** - Validaciones declarativas
- **AutoMapper 15.0** - Mapeo de objetos
- **System.Linq.Dynamic.Core 1.6** - LINQ dinámico para filtering

### Testing
- **NUnit 4.2** - Framework de testing
- **Moq 4.20** - Mocking framework
- **AutoFixture 4.18** - Generación automática de datos de prueba
- **FluentAssertions 8.5** - Aserciones expresivas

### Utilidades
- **Spectre.Console 0.50** - CLI interactiva
- **DotNetEnv 3.1** - Variables de entorno

## 📚 Referencias

- **Manual completo:** [MANUAL_CONSTRUCCION_PROYECTO.md](../../MANUAL_CONSTRUCCION_PROYECTO.md)
- **Repositorio:** [README.md](../../README.md)
- **Templates:** [templates/README.md](../../templates/README.md)

## 🤝 Contribuir

Para agregar o modificar componentes:

1. Revisar el manual completo para extraer información
2. Seguir el formato de los documentos existentes
3. Actualizar este README con los cambios
4. Probar manualmente los comandos antes de commitear

## 📅 Changelog

- **2025-01-30:** Milestone 4 completado (WebApi Layer) - v1.4.7
- **2025-01-30:** Milestone 3 completado (Infrastructure Layer) - v1.3.0
- **2025-01-30:** Milestone 2 completado (Application Layer) - v1.2.0
- **2025-01-29:** Milestone 1 completado (Base + Domain Layer) - v1.0.0
