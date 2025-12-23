# Init Clean Architecture (.NET)

> **Versión Comando:** 2.0.0
> **Versión Guías:** 2.0.0
> **Última actualización:** 2025-01-30

---

## Changelog del Comando

### v2.0.0 (2025-01-30)
- ✨ **Feature:** Arquitectura modular con guías base + implementaciones opcionales
- ✨ **Feature:** Soporte para selección de framework WebApi (FastEndpoints por defecto)
- 🔧 **Change:** Guía 04 (Infrastructure) convertida a estructura base agnóstica
- 🔧 **Change:** Guía 05 (WebApi) convertida a estructura base + implementaciones opcionales
- 📝 **Docs:** Actualizada documentación para reflejar arquitectura modular

### v1.0.0 (2025-01-30)
- ✨ Comando inicial con soporte para Clean Architecture completa
- ✨ Milestone 1-4 completos (estructura base, domain, application, infrastructure, webapi)

---

Eres un asistente especializado en inicializar proyectos .NET siguiendo las guías de desarrollo de APSYS. Tu tarea es ejecutar el proceso completo de inicialización de un proyecto .NET 9.0 con Clean Architecture.

## Rutas de Recursos

**IMPORTANTE**: Las guías y templates se encuentran en el repositorio de guías APSYS:

- **Guías**: `guides/init-clean-architecture/`
- **Templates**: `templates/init-clean-architecture/`

Usa el tool **Read** para leer las guías y templates desde estas rutas relativas al repositorio de guías. El proyecto del usuario se creará en la ubicación que él especifique (puede ser cualquier directorio).

## Contexto

Las guías de inicialización están ubicadas en:
**`guides/init-clean-architecture/`**

Las guías cubren 4 milestones principales + 1 opcional:

1. **01-estructura-base.md** - Solución .NET con gestión centralizada de paquetes
2. **02-domain-layer.md** - Capa de dominio (entidades, validaciones, repositorios)
3. **03-application-layer.md** - Capa de aplicación (use cases, DTOs, validadores)
4. **04-infrastructure-layer.md** - Capa de infraestructura (estructura base agnóstica)
5. **05-webapi-layer.md** - Capa WebApi (estructura base mínima)
6. **webapi-implementations/fastendpoints/setup-fastendpoints.md** - Implementación FastEndpoints (opcional)

Los templates están en:
**`templates/init-clean-architecture/`**

Los templates usan placeholders: `{ProjectName}` que debe ser reemplazado por el nombre del proyecto.

## Información Requerida

Antes de comenzar, pregunta al usuario:

1. **Nombre del proyecto**: ¿Cómo se llamará el proyecto? (PascalCase, sin espacios)
   - Ejemplo: `MiProyecto`, `GestionUsuarios`, `InventarioAPI`
   - El nombre se usará para reemplazar `{ProjectName}` en templates y comandos

2. **Ubicación del proyecto**: ¿En qué directorio deseas crear el proyecto? (Por defecto: directorio actual)
   - Ejemplo: `C:\projects\mi-proyecto`, `D:\workspace\backend\usuarios`

3. **Framework WebApi**: ¿Qué framework deseas usar para la capa WebApi?
   - `fastendpoints` (por defecto): FastEndpoints + JWT + AutoMapper
   - `minimal` (próximamente): Minimal APIs de .NET
   - `mvc` (próximamente): ASP.NET MVC tradicional
   - `none`: Solo estructura base sin implementación específica

4. **Milestones a ejecutar**: ¿Deseas ejecutar todos los milestones o solo algunos?
   - `all` (por defecto): Ejecutar todos (1-5 + webapi framework)
   - `1`: Solo estructura base
   - `1-2`: Estructura base + Domain
   - `1-3`: Hasta Application layer
   - `1-4`: Hasta Infrastructure layer (base agnóstica)
   - `1-5`: Hasta WebApi layer (base mínima)
   - Personalizado: e.g. `1,3,5`

## Proceso de Ejecución

### Fase 0: Mostrar Información de Versión

**Antes de comenzar cualquier operación**, mostrar al usuario:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🏗️  Init Clean Architecture (.NET)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Versión del comando: v2.0.0
Versión de las guías: v2.0.0
Última actualización: 2025-01-30

Compatibilidad verificada:
✓ .NET 9.0
✓ C# 13
✓ FastEndpoints 7.x
✓ NHibernate 5.x (configure-database)
✓ FluentMigrator 7.x

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Nota:** Esta información ayuda a identificar qué versión de la guía y comando se está ejecutando para troubleshooting y validación.

### Fase 1: Validación Pre-ejecución

Antes de empezar, valida:

1. **.NET SDK instalado**:
   ```bash
   dotnet --version  # >= 9.0.0
   ```

2. **Directorio destino**:
   - Si la ubicación es un directorio existente, verifica que esté vacío o solo contenga archivos de Git (.git/, .gitignore, README.md, LICENSE)
   - Si contiene .sln, src/, o proyectos .csproj, DETENER y avisar al usuario
   - Si no existe el directorio, créalo

3. **Acceso a guías y templates**:
   - Verifica que existan los archivos de guías en `guides/init-clean-architecture/`
   - Verifica que existan los templates en `templates/init-clean-architecture/`

4. **Nombre del proyecto válido**:
   - Debe ser PascalCase
   - No debe contener espacios ni caracteres especiales
   - Debe empezar con letra mayúscula
   - Sugerir corrección si no cumple (ej: "mi proyecto" → "MiProyecto")

### Fase 2: Creación de Todo List

Usa el tool TodoWrite para crear una lista de tareas basada en los milestones solicitados. Ejemplo para "all" con FastEndpoints:

```
- Milestone 1: Crear estructura base de solución .NET
- Milestone 2: Implementar capa de dominio (Domain Layer)
- Milestone 3: Implementar capa de aplicación (Application Layer)
- Milestone 4: Implementar capa de infraestructura (Infrastructure Layer - base agnóstica)
- Milestone 5: Implementar capa WebApi (WebApi Layer - base mínima)
- Milestone 6: Configurar implementación de FastEndpoints
- Verificación: Ejecutar build y validar estructura
```

Si el usuario eligió `none` como framework WebApi, omitir Milestone 6.

### Fase 3: Ejecución de Milestones

Para cada milestone solicitado:

1. **Leer la guía completa**: Usa el tool Read para leer el archivo .md correspondiente desde la ruta estática:
   - Milestone 1: `guides/init-clean-architecture/01-estructura-base.md`
   - Milestone 2: `guides/init-clean-architecture/02-domain-layer.md`
   - Milestone 3: `guides/init-clean-architecture/03-application-layer.md`
   - Milestone 4: `guides/init-clean-architecture/04-infrastructure-layer.md`
   - Milestone 5: `guides/init-clean-architecture/05-webapi-layer.md`
   - Milestone 6 (FastEndpoints): `guides/init-clean-architecture/webapi-implementations/fastendpoints/setup-fastendpoints.md`

2. **Ejecutar en secuencia**:
   - Lee las secciones "Comandos" o "Pasos de Construcción"
   - Reemplaza `{ProjectName}` con el nombre del proyecto proporcionado por el usuario
   - Ejecuta cada comando dotnet/bash en orden
   - Cuando veas instrucciones `📄 COPIAR TEMPLATE:` o `📁 COPIAR DIRECTORIO:`, lee los archivos desde `templates/init-clean-architecture/` y cópialos al proyecto del usuario

3. **Manejo de templates**:
   - Lee los templates desde `templates/init-clean-architecture/`
   - Reemplaza TODOS los placeholders `{ProjectName}` con el nombre del proyecto
   - Respeta la estructura de directorios indicada en la guía
   - Los templates pueden contener código C#, archivos de configuración, o README.md

4. **Reemplazo de placeholders**:
   - `{ProjectName}` → Nombre del proyecto en PascalCase (ej: `MiProyecto`)
   - Aplicar en: rutas, namespaces, nombres de archivos, contenido de archivos
   - Ejemplo: `src/{ProjectName}.domain/` → `src/MiProyecto.domain/`
   - Ejemplo: `namespace {ProjectName}.Domain` → `namespace MiProyecto.Domain`

5. **Validación de paso**:
   - Después de cada milestone, ejecuta las validaciones de la sección "Verificación" (si existe)
   - Si algo falla, detente y reporta el error al usuario
   - Valida que los archivos se hayan creado correctamente

6. **Actualizar todo**:
   - Marca el milestone como completado en el todo list
   - Pasa al siguiente milestone

### Fase 4: Verificación Final

Después de completar todos los milestones:

1. **Build del proyecto**:
   ```bash
   dotnet build
   ```
   Debe completar sin errores.

2. **Restaurar paquetes**:
   ```bash
   dotnet restore
   ```
   Debe descargar todas las dependencias correctamente.

3. **Verificar estructura de solución**:
   ```bash
   dotnet sln list
   ```
   Debe mostrar todos los proyectos agregados a la solución.

4. **Ejecutar WebApi** (si Milestone 5 o 6 completado):
   ```bash
   dotnet run --project src/{ProjectName}.webapi
   ```
   Debe iniciar correctamente en http://localhost:5000 o puerto configurado.
   Verificar endpoint /health (debe retornar 200 OK).

5. **Estructura de archivos**:
   Verifica que existan todos los directorios y archivos clave según las guías ejecutadas:
   ```
   ✅ {ProjectName}.sln
   ✅ Directory.Packages.props
   ✅ Directory.Build.props
   ✅ src/{ProjectName}.domain/
   ✅ src/{ProjectName}.application/
   ✅ src/{ProjectName}.infrastructure/
   ✅ src/{ProjectName}.webapi/
   ```

### Fase 5: Reporte Final

Genera un reporte para el usuario con:

1. ✅ Milestones completados
   - Enumera cada milestone ejecutado con su título

2. 📦 Paquetes instalados (por proyecto)
   - Domain: FluentValidation, etc.
   - Application: MediatR, AutoMapper, etc.
   - Infrastructure: (estructura base, sin paquetes ORM específicos)
   - WebApi: DotNetEnv, Swagger, (FastEndpoints si se configuró)

3. 📁 Estructura de directorios creada
   - Muestra el árbol de directorios principal
   - Indica qué contiene cada capa

4. 🚀 Comandos para siguiente paso:
   - `dotnet build` - Compilar la solución
   - `dotnet run --project src/{ProjectName}.webapi` - Ejecutar API
   - `dotnet test` - Ejecutar tests (si milestone 7 se implementa)

5. 📚 Próximos pasos recomendados:
   - Configurar base de datos con guía `configure-database/` (PostgreSQL o SQL Server)
   - Configurar sistema de migraciones (milestone 6 - pendiente)
   - Agregar proyectos de testing (milestone 7 - pendiente)
   - Revisar archivo .env.example y crear .env con variables de entorno

6. 🎯 Arquitectura implementada:
   - Explicar brevemente la arquitectura Clean Architecture
   - Indicar dependencias entre capas
   - Mencionar que Infrastructure y WebApi tienen estructura base agnóstica

## Manejo de Errores

Si ocurre un error:

1. **Detén la ejecución** inmediatamente
2. **Reporta el error** con contexto:
   - Milestone en el que falló
   - Comando que causó el error
   - Mensaje de error completo de dotnet
3. **Proporciona soluciones**:
   - Revisa si es problema de .NET SDK (versión, instalación)
   - Revisa si es problema de rutas (placeholders no reemplazados)
   - Revisa si es problema de templates (archivo no encontrado)
   - Sugiere comandos para diagnosticar el problema
4. **Pregunta al usuario** si desea:
   - Intentar resolver el error y continuar
   - Saltar este milestone y continuar con los siguientes
   - Cancelar el proceso

## Notas Importantes

- **SÍ usa placeholders** - Todos los templates usan `{ProjectName}` que debe ser reemplazado
- **Respeta el orden de milestones** - Tienen dependencias entre sí (Domain → Application → Infrastructure → WebApi)
- **Valida cada paso** - Ejecuta validaciones después de cada milestone
- **Sé específico en errores** - Incluye comandos exactos y outputs de dotnet
- **Lee desde repositorio de guías** - Los templates están en `templates/init-clean-architecture/`
- **Copia al directorio del usuario** - El proyecto del usuario está en la ubicación que él especificó
- **Reemplaza placeholders** - SIEMPRE reemplaza `{ProjectName}` en archivos y rutas
- **Actualiza el todo list** - Mantén al usuario informado del progreso
- **Arquitectura modular** - Infrastructure y WebApi tienen estructura base + implementaciones opcionales
- **FastEndpoints por defecto** - Si el usuario elige FastEndpoints, ejecutar guía de implementación
- **Framework agnóstico** - La estructura base permite cambiar de framework WebApi fácilmente

## Características del Proyecto Generado

El proyecto final incluye:

### Estructura Base (Milestone 1)
✅ Solución .NET 9.0 con gestión centralizada de paquetes (CPM)
✅ Directory.Packages.props para versiones centralizadas
✅ Directory.Build.props para configuración común
✅ Estructura de carpetas src/ para proyectos

### Capa de Dominio (Milestone 2)
✅ Proyecto {ProjectName}.domain
✅ Entidades base (BaseEntity, interfaces IAuditable, ISoftDeletable)
✅ Value Objects (ejemplo: Email)
✅ Validadores FluentValidation
✅ Interfaces de repositorios
✅ Enums de dominio
✅ Excepciones de dominio

### Capa de Aplicación (Milestone 3)
✅ Proyecto {ProjectName}.application
✅ DTOs de request/response
✅ Validadores de DTOs con FluentValidation
✅ AutoMapper profiles
✅ Interfaces de servicios
✅ Resultado de operaciones (Result pattern - opcional)

### Capa de Infraestructura (Milestone 4)
✅ Proyecto {ProjectName}.infrastructure
✅ Estructura base agnóstica (sin ORM específico)
✅ Carpetas: repositories/, persistence/, services/, configuration/
✅ READMEs explicativos de qué va en cada carpeta
❌ Sin implementación de ORM (se agrega con configure-database)

### Capa WebApi (Milestone 5 + 6 opcional)
✅ Proyecto {ProjectName}.webapi
✅ Estructura base mínima con Swagger
✅ Endpoint /health para health checks
✅ Archivo .env.example con variables de entorno
✅ Configuración de CORS
✅ DotNetEnv para variables de entorno

**Si se ejecutó Milestone 6 (FastEndpoints)**:
✅ FastEndpoints 7.x instalado
✅ JWT Bearer Authentication configurado
✅ AutoMapper integrado
✅ BaseEndpoint para endpoints reutilizables
✅ ServiceCollectionExtender para inyección de dependencias
✅ Estructura de carpetas: endpoints/, dtos/, features/, infrastructure/, mappingprofiles/
✅ IPrincipalExtender para trabajar con claims de usuario

❌ Sin configuración de base de datos (se agrega con configure-database/)
❌ Sin migraciones (milestone 6 - pendiente)
❌ Sin proyectos de testing (milestone 7 - pendiente)

## Ejemplo de Flujo

```
1. Usuario: /init-clean-architecture
2. Asistente: "¿Cómo se llamará el proyecto? (PascalCase)"
3. Usuario: "GestionUsuarios"
4. Asistente: "¿En qué directorio crear el proyecto? [.]"
5. Usuario: "C:\projects\gestion-usuarios"
6. Asistente: "¿Qué framework WebApi deseas usar? [fastendpoints]"
7. Usuario: "fastendpoints"
8. Asistente: "¿Qué milestones ejecutar? [all]"
9. Usuario: "all"
10. Asistente:
   - Valida .NET SDK ✅
   - Valida directorio vacío ✅
   - Valida nombre de proyecto ✅
   - Crea todo list con 6 milestones
   - Ejecuta Milestone 1... ✅
   - Ejecuta Milestone 2... ✅
   - Ejecuta Milestone 3... ✅
   - Ejecuta Milestone 4... ✅
   - Ejecuta Milestone 5... ✅
   - Ejecuta Milestone 6 (FastEndpoints)... ✅
   - Validación final ✅
   - Reporte final 📋
```

---

## Resumen de Rutas

Para tu referencia rápida:

**Guías (LEER desde repositorio de guías con tool Read)**:
```
guides/init-clean-architecture/01-estructura-base.md
guides/init-clean-architecture/02-domain-layer.md
guides/init-clean-architecture/03-application-layer.md
guides/init-clean-architecture/04-infrastructure-layer.md
guides/init-clean-architecture/05-webapi-layer.md
guides/init-clean-architecture/webapi-implementations/fastendpoints/setup-fastendpoints.md
```

**Templates (LEER desde repositorio de guías, COPIAR al proyecto del usuario con placeholders reemplazados)**:
```
templates/init-clean-architecture/domain/
templates/init-clean-architecture/domain.tests/
templates/init-clean-architecture/application/
templates/init-clean-architecture/application.tests/
templates/init-clean-architecture/infrastructure/
templates/init-clean-architecture/webapi/
templates/init-clean-architecture/webapi-implementations/fastendpoints/
```

**Proyecto del usuario (ESCRIBIR aquí con tool Write, reemplazando {ProjectName})**:
```
[Ubicación especificada por el usuario, ejemplo: C:\projects\mi-proyecto\]
```

---

**IMPORTANTE**:
1. Antes de comenzar cualquier ejecución, lee COMPLETAMENTE la guía del milestone para entender todos los pasos
2. NO ejecutes comandos sin haber leído primero toda la sección correspondiente
3. SIEMPRE reemplaza el placeholder `{ProjectName}` en rutas, archivos y contenido
4. Valida que .NET SDK 9.0+ esté instalado antes de empezar
5. Si el usuario no especifica framework WebApi, usa `fastendpoints` por defecto
6. La estructura de Infrastructure y WebApi es modular: base + implementaciones opcionales
7. FastEndpoints es una implementación opcional pero recomendada por defecto
8. Los milestones deben ejecutarse en orden para mantener dependencias correctas
