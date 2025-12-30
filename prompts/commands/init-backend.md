# Init Backend Project

> **Version:** 3.5.0
> **Ultima actualizacion:** 2025-12-30

Inicializa un proyecto backend .NET con Clean Architecture siguiendo las guias de APSYS.

---

## Configuracion

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Informacion Requerida

Antes de comenzar, solicita al usuario:

### 1. Nombre del proyecto
- Formato: minusculas, sin espacios ni caracteres especiales
- Ejemplo: `miproyecto`, `gestionusuarios`, `inventario.api`
- Se usara para reemplazar `{ProjectName}` en templates

### 2. Ubicacion del proyecto
- Ruta absoluta donde crear el proyecto
- Ejemplo: `C:\projects\mi-proyecto`, `D:\workspace\backend`
- Si no existe, se creara

### 3. Base de datos
- `postgresql` - PostgreSQL (recomendado)
- `sqlserver` - SQL Server

### 4. Framework WebAPI
- `fastendpoints` - FastEndpoints + JWT + AutoMapper (recomendado)
- `none` - Solo estructura base con Swagger

### 5. Incluir migraciones
- `yes` - Incluir proyecto de migraciones con FluentMigrator
- `no` - Sin proyecto de migraciones

### 6. Incluir generador de escenarios
- `yes` - Incluir proyectos para testing de integracion:
  - `{ProjectName}.ndbunit` - Libreria para cargar/limpiar datos en BD
  - `{ProjectName}.common.tests` - Recursos compartidos (Schema, IDs)
  - `{ProjectName}.scenarios` - Generador de escenarios XML
- `no` - Sin proyectos de escenarios

> **Nota:** Estos proyectos son necesarios para pruebas de integracion con datos pre-cargados.

---

## Rutas de Recursos

Todas las rutas son relativas a `{GUIDES_REPO}`.

**Guias de inicializacion:**
```
{GUIDES_REPO}/architectures/clean-architecture/init/
├── 01-estructura-base.md
├── 02-domain-layer.md
├── 03-application-layer.md
├── 04-infrastructure-layer.md
└── 05-webapi-layer.md
```

**Guias de stacks:**
```
{GUIDES_REPO}/stacks/
├── database/
│   ├── postgresql/guides/setup.md
│   ├── sqlserver/guides/setup.md
│   └── migrations/fluent-migrator/guides/setup.md
├── orm/
│   └── nhibernate/guides/setup.md
└── webapi/
    └── fastendpoints/guides/setup.md
```

**Guias de testing:**
```
{GUIDES_REPO}/testing/integration/
├── tools/ndbunit/guides/setup.md
└── scenarios/guides/
    ├── setup.md
    └── scenarios-creation-guide.md
```

**Templates:**
```
{GUIDES_REPO}/templates/
├── domain/
├── webapi/
├── tests/
└── Directory.Packages.props

{GUIDES_REPO}/stacks/{stack}/templates/

{GUIDES_REPO}/testing/integration/
├── tools/ndbunit/templates/project/
│   ├── INDbUnit.cs
│   ├── NDbUnit.cs
│   ├── PostgreSQLNDbUnit.cs
│   └── SqlServerNDbUnit.cs
└── scenarios/templates/
    ├── project/                    # Proyecto scenarios
    │   ├── IScenario.cs
    │   ├── ScenarioBuilder.cs
    │   ├── Program.cs
    │   ├── CommandLineArgs.cs
    │   ├── ExitCode.cs
    │   └── Sc010CreateSandBox.cs
    └── common.tests/               # Proyecto common.tests
        ├── AppSchemaExtender.cs
        └── ScenarioIds.cs
```

---

## Proceso de Ejecucion

### Fase 0: Mostrar Informacion del Comando

Al iniciar, mostrar:

```
Init Backend Project
Version: 3.4.0
Ultima actualizacion: 2025-12-30
```

### Fase 1: Validacion

1. **Verificar .NET SDK**:
   ```bash
   dotnet --version  # >= 9.0.0
   ```

2. **Verificar directorio destino**:
   - Si existe y contiene `.sln` o `src/`: DETENER y avisar
   - Si no existe: crear

3. **Validar nombre del proyecto**:
   - Usar el nombre exacto que proporcione el usuario
   - Sin espacios ni caracteres especiales (excepto punto)
   - NO convertir a PascalCase

### Fase 2: Crear Carpeta de Reportes

> **OBLIGATORIO:** Crear carpeta para reportes de ejecucion.

Inmediatamente despues de recopilar la informacion del usuario:

```bash
mkdir -p {ProjectPath}/.claude/init
```

Esto permite analizar tiempos y errores posteriormente para optimizar el proceso.

### Fase 3: Crear Todo List

> **OBLIGATORIO:** Usar la herramienta `TodoWrite` para crear la lista de tareas.
> Esto permite al usuario ver el progreso en tiempo real.

Inmediatamente despues de crear la carpeta de reportes, invocar `TodoWrite` con las siguientes tareas (omitir las que no apliquen segun las opciones seleccionadas):

```
- [ ] Crear estructura base de solucion
- [ ] Implementar capa de dominio
- [ ] Implementar capa de aplicacion
- [ ] Implementar capa de infraestructura
- [ ] Implementar capa WebAPI
- [ ] Configurar base de datos ({database})
- [ ] Configurar NHibernate
- [ ] Configurar FastEndpoints (si aplica)
- [ ] Configurar migraciones (si aplica)
- [ ] Configurar NDbUnit (si aplica)
- [ ] Configurar common.tests (si aplica)
- [ ] Configurar generador de escenarios (si aplica)
- [ ] Verificacion final
```

### Fase 4: Ejecutar Guias

Para cada guia, en orden:

1. **Marcar tarea como `in_progress`** en TodoWrite
2. **Registrar hora de inicio** en reporte de fase
3. **Leer la guia completa** con el tool Read desde `{GUIDES_REPO}`
4. **Ejecutar los comandos** reemplazando `{ProjectName}`
5. **Copiar templates** desde `{GUIDES_REPO}`, reemplazando placeholders
6. **Registrar hora de fin y errores** en reporte de fase
7. **Guardar reporte de fase** en `.claude/init/` (Markdown)
8. **Marcar tarea como `completed`** en TodoWrite inmediatamente al terminar

> **IMPORTANTE:** Actualizar TodoWrite despues de CADA paso completado, no al final de todos.

#### Orden de ejecucion:

| Paso | Guia | Descripcion |
|------|------|-------------|
| 1 | `{GUIDES_REPO}/architectures/clean-architecture/init/01-estructura-base.md` | Solucion .NET |
| 2 | `{GUIDES_REPO}/architectures/clean-architecture/init/02-domain-layer.md` | Capa de dominio |
| 3 | `{GUIDES_REPO}/architectures/clean-architecture/init/03-application-layer.md` | Capa de aplicacion |
| 4 | `{GUIDES_REPO}/architectures/clean-architecture/init/04-infrastructure-layer.md` | Capa de infraestructura |
| 5 | `{GUIDES_REPO}/architectures/clean-architecture/init/05-webapi-layer.md` | Capa WebAPI base |
| 6 | `{GUIDES_REPO}/stacks/database/{database}/guides/setup.md` | Driver y ConnectionString |
| 7 | `{GUIDES_REPO}/stacks/orm/nhibernate/guides/setup.md` | Repositorios NHibernate |
| 8 | `{GUIDES_REPO}/stacks/webapi/fastendpoints/guides/setup.md` | FastEndpoints (si aplica) |
| 9 | `{GUIDES_REPO}/stacks/database/migrations/fluent-migrator/guides/setup.md` | Migraciones (si aplica) |
| 10 | `{GUIDES_REPO}/testing/integration/tools/ndbunit/guides/setup.md` | NDbUnit (si aplica) |
| 11 | `{GUIDES_REPO}/testing/integration/scenarios/guides/setup.md` | Escenarios (si aplica) |

### Fase 5: Generacion de Reportes por Fase

Para CADA fase, crear un archivo Markdown en `.claude/init/`:

**Nombre:** `phase-{numero}-{nombre-corto}.md`

```markdown
# Phase 01: Estructura Base

| Campo | Valor |
|-------|-------|
| **Guia** | `architectures/clean-architecture/init/01-estructura-base.md` |
| **Inicio** | 10:30:00 |
| **Fin** | 10:35:00 |
| **Duracion** | 5 min |
| **Status** | Success |

## Comandos Ejecutados

| # | Comando | Resultado |
|---|---------|-----------|
| 1 | `dotnet new sln -n proyecto` | OK |
| 2 | `dotnet new classlib -n proyecto.domain` | OK |

## Archivos Creados

- proyecto.sln
- Directory.Build.props

## Errores

Ninguno
```

**Usar el tool Write** para crear el reporte al finalizar cada fase.

### Fase 6: Verificacion Final

1. **Compilar solucion**:
   ```bash
   dotnet build
   ```

2. **Verificar estructura**:
   ```bash
   dotnet sln list
   ```

3. **Ejecutar WebAPI** (si paso 5+ completado):
   ```bash
   dotnet run --project src/{ProjectName}.webapi
   ```

### Fase 7: Reporte Final

Al finalizar TODAS las fases, crear el archivo de resumen `summary.md`:

```markdown
# Init Backend Report

## Configuracion

| Campo | Valor |
|-------|-------|
| **Proyecto** | nombre.proyecto |
| **Ubicacion** | D:\projects\nombre-proyecto |
| **Base de datos** | PostgreSQL |
| **WebAPI** | FastEndpoints |
| **Migraciones** | Si |
| **Escenarios** | Si |

## Tiempo de Ejecucion

| Campo | Valor |
|-------|-------|
| **Inicio** | 2025-01-15 10:30:00 |
| **Fin** | 2025-01-15 11:10:00 |
| **Duracion Total** | 40 min |

## Resumen de Fases

| # | Fase | Duracion | Status |
|---|------|----------|--------|
| 1 | Estructura Base | 5 min | Success |
| 2 | Domain Layer | 3 min | Success |
| 3 | Application Layer | 4 min | Success |
| ... | ... | ... | ... |

## Resultado

- **Total Fases:** 11
- **Exitosas:** 11
- **Fallidas:** 0
- **Estado General:** Success
```

### Fase 8: Reporte Final al Usuario

Mostrar al usuario:

1. **Milestones completados** con checkmark
2. **Estructura creada**:
   ```
   {ProjectName}/
   ├── {ProjectName}.sln
   ├── Directory.Packages.props
   ├── Directory.Build.props
   ├── buildscenarios.bat  (si aplica)
   ├── .claude/
   │   └── init/
   │       ├── summary.md
   │       └── phase-*.md
   ├── src/
   │   ├── {ProjectName}.domain/
   │   ├── {ProjectName}.application/
   │   ├── {ProjectName}.infrastructure/
   │   ├── {ProjectName}.webapi/
   │   └── {ProjectName}.migrations/  (si aplica)
   └── tests/
       ├── {ProjectName}.ndbunit/         (si aplica)
       ├── {ProjectName}.common.tests/    (si aplica)
       └── {ProjectName}.scenarios/       (si aplica)
   ```
3. **Resumen de tiempos** por fase (desde summary.md)
4. **Comandos utiles**:
   ```bash
   dotnet build                                    # Compilar
   dotnet run --project src/{ProjectName}.webapi  # Ejecutar API
   dotnet run --project src/{ProjectName}.migrations cnn="..."  # Migraciones
   buildscenarios.bat                             # Generar escenarios (si aplica)
   ```
5. **Proximos pasos**:
   - Crear entidades de dominio
   - Crear migraciones de base de datos
   - Implementar endpoints

---

## Reemplazo de Placeholders

En todos los archivos y rutas:
- `{GUIDES_REPO}` → Ruta al repositorio de guias (ver seccion Configuracion)
- `{ProjectName}` → Nombre del proyecto (como lo proporciono el usuario)
- `{ProjectPath}` → Ruta absoluta del proyecto
- `{database}` → Base de datos seleccionada (postgresql | sqlserver)

---

## Manejo de Errores

Si ocurre un error:

1. **Registrar el error** en el reporte de la fase actual
2. **Actualizar status** a "failed" en el reporte
3. **Guardar el reporte** inmediatamente
4. **Reportar** con contexto:
   - Guia en la que fallo
   - Comando que causo el error
   - Mensaje de error
5. **Sugerir solucion**
6. **Preguntar** si continuar o cancelar

---

## Estructura de Reportes Generada

```
{ProjectPath}/
└── .claude/
    └── init/
        ├── summary.md                            # Resumen general
        ├── phase-01-estructura-base.md
        ├── phase-02-domain-layer.md
        ├── phase-03-application-layer.md
        ├── phase-04-infrastructure-layer.md
        ├── phase-05-webapi-layer.md
        ├── phase-06-database-setup.md
        ├── phase-07-nhibernate-setup.md
        ├── phase-08-fastendpoints-setup.md       (si aplica)
        ├── phase-09-migrations-setup.md          (si aplica)
        ├── phase-10-ndbunit-setup.md             (si aplica)
        └── phase-11-scenarios-setup.md           (si aplica)
```

---

## Ejemplo de Flujo

```
Usuario: /init-backend

Asistente:
Init Backend Project
Version: 3.5.0
Ultima actualizacion: 2025-12-30

¿Como se llamara el proyecto?
Usuario: gestion.inventario

Asistente: ¿Donde crear el proyecto?
Usuario: C:\projects\inventario

Asistente: ¿Que base de datos?
1. PostgreSQL (recomendado)
2. SQL Server
Usuario: 1

Asistente: ¿Framework WebAPI?
1. FastEndpoints (recomendado)
2. Solo estructura base
Usuario: 1

Asistente: ¿Incluir proyecto de migraciones?
Usuario: Si

Asistente: ¿Incluir generador de escenarios para testing?
Usuario: Si

Asistente:
Inicializando: gestion.inventario
Ubicacion: C:\projects\inventario
Base de datos: PostgreSQL
Framework: FastEndpoints
Migraciones: Si
Escenarios: Si

[Crea carpeta .claude/init...]
[Crea todo list...]
[Ejecuta guias en orden...]
[Genera reportes por fase...]
[Muestra progreso con todo list...]
[Genera reportes finales...]
[Reporte final con tiempos...]
```

---

## Notas Importantes

- **Configurar GUIDES_REPO** antes de usar el comando
- **Leer guias completas** antes de ejecutar comandos
- **Respetar el orden** de ejecucion (hay dependencias)
- **Reemplazar TODOS los placeholders** en archivos y rutas
- **Validar cada paso** antes de continuar
- **Generar reportes** para cada fase (Markdown)

### Uso Obligatorio de TodoWrite

**SIEMPRE** usar la herramienta `TodoWrite` para:

1. **Al inicio (Fase 3):** Crear la lista completa de tareas
2. **Durante ejecucion (Fase 4):**
   - Marcar tarea actual como `in_progress` ANTES de comenzar
   - Marcar tarea como `completed` INMEDIATAMENTE al terminar
3. **No acumular:** Actualizar despues de CADA paso, no al final

Esto es critico para que el usuario vea el progreso en tiempo real.

### Uso Obligatorio de Reportes

**SIEMPRE** generar reportes para:

1. **Cada fase:** Markdown con tiempos, comandos y errores
2. **Al finalizar:** summary.md con resumen total
3. **En errores:** Registrar inmediatamente antes de preguntar al usuario

Esto permite analizar y optimizar el proceso posteriormente.
