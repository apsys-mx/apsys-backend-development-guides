# Init Backend Project

> **Version:** 3.6.0
> **Ultima actualizacion:** 2025-12-30

Inicializa un proyecto backend .NET con Clean Architecture siguiendo las guias de APSYS.

---

## Configuracion

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Recopilar Informacion

> **IMPORTANTE:** Hacer SIEMPRE como un asistente interactivo.
> Preguntar UNA cosa a la vez y ESPERAR la respuesta del usuario antes de continuar.

### 1. Nombre del proyecto

Preguntar:
```
¿Como se llamara el proyecto?
(minusculas, sin espacios, puede tener puntos. Ej: mi.proyecto, inventario, gestion.usuarios)
```
**Esperar respuesta del usuario.**

### 2. Ubicacion del proyecto

Preguntar con opciones:
```
¿Donde crear el proyecto?
1. [Ruta actual] (default)
2. Otra ubicacion

Selecciona una opcion (1-2):
```
- Si elige 1: usar la ruta donde se esta ejecutando
- Si elige 2: preguntar "Ingresa la ruta absoluta:"

**Esperar respuesta del usuario.**

### 3. Base de datos

Preguntar con opciones:
```
¿Que base de datos usara el proyecto?
1. PostgreSQL (default)
2. SQL Server

Selecciona una opcion (1-2):
```
**Esperar respuesta del usuario.**

### 4. Framework WebAPI

Preguntar con opciones:
```
¿Que framework para la WebAPI?
1. FastEndpoints (default)
2. Solo estructura base (sin framework)

Selecciona una opcion (1-2):
```
**Esperar respuesta del usuario.**

### 5. Migraciones y Escenarios

Preguntar con opciones:
```
¿Incluir proyectos de migraciones y/o escenarios de prueba?
1. Migraciones + Escenarios (recomendado para proyectos completos)
2. Solo Migraciones
3. Solo Escenarios
4. Ninguno

Selecciona una opcion (1-4):
```

> **Nota sobre escenarios:** Los proyectos de escenarios incluyen:
> - `{ProjectName}.ndbunit` - Libreria para cargar/limpiar datos en BD
> - `{ProjectName}.common.tests` - Recursos compartidos (Schema, IDs)
> - `{ProjectName}.scenarios` - Generador de escenarios XML

**Esperar respuesta del usuario.**

### 6. Confirmar configuracion

Mostrar resumen y pedir confirmacion:
```
Configuracion del proyecto:
- Nombre: {nombre}
- Ubicacion: {ruta}
- Base de datos: {database}
- Framework: {framework}
- Migraciones: {si/no}
- Escenarios: {si/no}

¿Confirmar e iniciar? (si/no)
```
**Esperar confirmacion del usuario antes de continuar.**

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
Version: 3.6.0
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

> **IMPORTANTE:** Los reportes deben ser una BITACORA COMPLETA de todo lo que ocurrio.
> Incluir TODOS los pasos ejecutados, exitos, errores, y problemas con las guias.

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

## Bitacora de Ejecucion

| Hora | Accion | Detalle | Resultado |
|------|--------|---------|-----------|
| 10:30:01 | Leer guia | 01-estructura-base.md | OK |
| 10:30:05 | Comando | `mkdir src tests` | OK |
| 10:30:10 | Comando | `dotnet new sln -n proyecto` | OK |
| 10:30:20 | Copiar template | Directory.Packages.props | OK |

## Comandos Ejecutados

| # | Comando | Exit Code | Resultado |
|---|---------|-----------|-----------|
| 1 | `mkdir src tests` | 0 | OK |
| 2 | `dotnet new sln -n proyecto` | 0 | OK |

## Templates Copiados

| # | Origen | Destino | Status |
|---|--------|---------|--------|
| 1 | `{GUIDES_REPO}/templates/Directory.Packages.props` | `Directory.Packages.props` | OK |

## Archivos Creados

- proyecto.sln
- Directory.Packages.props
- src/
- tests/

## Errores

Ninguno

## Problemas con la Guia

Ninguno
```

#### 5.1 Que DEBE incluir el reporte

**OBLIGATORIO registrar:**

1. **Bitacora completa:** Cada accion realizada con timestamp
2. **Comandos ejecutados:** Con exit code y output
3. **Templates copiados:** Origen, destino y si fue exitoso
4. **Archivos creados:** Lista de todos los archivos generados
5. **Errores encontrados:** Cualquier error durante la ejecucion
6. **Problemas con la guia:** Errores en la documentacion que deben corregirse

**Tipos de problemas con guias a reportar:**

- Rutas de templates incorrectas o no encontradas
- Comandos que fallan por sintaxis incorrecta
- Instrucciones ambiguas o incompletas
- Placeholders no documentados
- Archivos referenciados que no existen
- Orden de pasos incorrecto

**Ejemplo de problema con guia en Markdown:**
```markdown
## Problemas con la Guia

| Tipo | Descripcion | Instruccion en Guia | Ruta Intentada | Ruta Correcta | Sugerencia |
|------|-------------|---------------------|----------------|---------------|------------|
| template_not_found | Template no encontrado | Copiar desde templates/... | D:\apsys-mx\...\init\templates\... | D:\apsys-mx\...\templates\... | Usar {GUIDES_REPO}/templates/ |
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

| # | Fase | Duracion | Status | Errores | Problemas Guia |
|---|------|----------|--------|---------|----------------|
| 1 | Estructura Base | 5 min | Success | 0 | 0 |
| 2 | Domain Layer | 3 min | Success | 0 | 0 |
| 3 | Application Layer | 4 min | Success | 0 | 0 |
| ... | ... | ... | ... | ... | ... |

## Resultado

- **Total Fases:** 11
- **Exitosas:** 11
- **Fallidas:** 0
- **Total Errores:** 0
- **Problemas en Guias:** 0
- **Estado General:** Success

## Problemas Encontrados en las Guias

> Esta seccion lista problemas en la documentacion que deben ser corregidos.

Ninguno

(O listar cada problema encontrado con su solucion sugerida)
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
2. **Si es un problema de la guia** (ruta incorrecta, template no encontrado, etc.), registrarlo en la seccion "Problemas con la Guia"
3. **Actualizar status** a "failed" si el error impide continuar
4. **Guardar el reporte** inmediatamente
5. **Reportar** con contexto:
   - Guia en la que fallo
   - Comando que causo el error
   - Mensaje de error
6. **Sugerir solucion**
7. **Preguntar** si continuar o cancelar

### Tipos de Errores a Distinguir

**Errores de ejecucion (errors):**
- Comandos que fallan (dotnet, mkdir, etc.)
- Archivos que no se pueden crear
- Permisos denegados

**Problemas de guia (guideIssues):**
- Templates no encontrados en la ruta indicada
- Rutas relativas incorrectas
- Instrucciones que no funcionan
- Placeholders no documentados
- Dependencias faltantes no mencionadas

Es CRITICO distinguir entre ambos tipos para poder corregir las guias posteriormente.

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
Version: 3.6.0
Ultima actualizacion: 2025-12-30

¿Como se llamara el proyecto?
(minusculas, sin espacios, puede tener puntos. Ej: mi.proyecto, inventario, gestion.usuarios)

Usuario: gestion.inventario

Asistente:
¿Donde crear el proyecto?
1. [C:\projects\inventario] (default)
2. Otra ubicacion

Selecciona una opcion (1-2):

Usuario: 1

Asistente:
¿Que base de datos usara el proyecto?
1. PostgreSQL (default)
2. SQL Server

Selecciona una opcion (1-2):

Usuario: 1

Asistente:
¿Que framework para la WebAPI?
1. FastEndpoints (default)
2. Solo estructura base (sin framework)

Selecciona una opcion (1-2):

Usuario: 1

Asistente:
¿Incluir proyectos de migraciones y/o escenarios de prueba?
1. Migraciones + Escenarios (recomendado para proyectos completos)
2. Solo Migraciones
3. Solo Escenarios
4. Ninguno

Selecciona una opcion (1-4):

Usuario: 1

Asistente:
Configuracion del proyecto:
- Nombre: gestion.inventario
- Ubicacion: C:\projects\inventario
- Base de datos: PostgreSQL
- Framework: FastEndpoints
- Migraciones: Si
- Escenarios: Si

¿Confirmar e iniciar? (si/no)

Usuario: si

Asistente:
[Crea carpeta .claude/init...]
[Crea todo list...]
[Ejecuta guias en orden...]
[Genera reportes por fase con bitacora...]
[Muestra progreso con todo list...]
[Genera summary.md con resumen...]
[Reporte final con tiempos y problemas de guias...]
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
