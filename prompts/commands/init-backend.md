# Init Backend Project

> **Version:** 3.1.0
> **Ultima actualizacion:** 2025-12-23

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

**Templates:**
```
{GUIDES_REPO}/templates/
├── domain/
├── webapi/
├── tests/
└── Directory.Packages.props

{GUIDES_REPO}/stacks/{stack}/templates/
```

---

## Proceso de Ejecucion

### Fase 0: Mostrar Informacion del Comando

Al iniciar, mostrar:

```
Init Backend Project
Version: 3.1.0
Ultima actualizacion: 2025-12-23
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
   - Debe ser PascalCase
   - Sin espacios ni caracteres especiales
   - Sugerir correccion si no cumple

### Fase 2: Crear Todo List

Crear lista de tareas segun opciones seleccionadas:

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
- [ ] Verificacion final
```

### Fase 3: Ejecutar Guias

Para cada guia, en orden:

1. **Leer la guia completa** con el tool Read desde `{GUIDES_REPO}`
2. **Ejecutar los comandos** reemplazando `{ProjectName}`
3. **Copiar templates** desde `{GUIDES_REPO}`, reemplazando placeholders
4. **Marcar como completado** en el todo list

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

### Fase 4: Verificacion Final

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

### Fase 5: Reporte Final

Mostrar al usuario:

1. **Milestones completados** con checkmark
2. **Estructura creada**:
   ```
   {ProjectName}/
   ├── {ProjectName}.sln
   ├── Directory.Packages.props
   ├── Directory.Build.props
   ├── src/
   │   ├── {ProjectName}.domain/
   │   ├── {ProjectName}.application/
   │   ├── {ProjectName}.infrastructure/
   │   ├── {ProjectName}.webapi/
   │   └── {ProjectName}.migrations/  (si aplica)
   └── tests/
   ```
3. **Comandos utiles**:
   ```bash
   dotnet build                                    # Compilar
   dotnet run --project src/{ProjectName}.webapi  # Ejecutar API
   dotnet run --project src/{ProjectName}.migrations cnn="..."  # Migraciones
   ```
4. **Proximos pasos**:
   - Crear entidades de dominio
   - Crear migraciones de base de datos
   - Implementar endpoints

---

## Reemplazo de Placeholders

En todos los archivos y rutas:
- `{GUIDES_REPO}` → Ruta al repositorio de guias (ver seccion Configuracion)
- `{ProjectName}` → Nombre del proyecto (como lo proporciono el usuario)
- `{database}` → Base de datos seleccionada (postgresql | sqlserver)

---

## Manejo de Errores

Si ocurre un error:

1. **Detener ejecucion**
2. **Reportar** con contexto:
   - Guia en la que fallo
   - Comando que causo el error
   - Mensaje de error
3. **Sugerir solucion**
4. **Preguntar** si continuar o cancelar

---

## Ejemplo de Flujo

```
Usuario: /init-backend

Asistente:
Init Backend Project
Version: 3.1.0
Ultima actualizacion: 2025-12-23

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

Asistente:
Inicializando: gestion.inventario
Ubicacion: C:\projects\inventario
Base de datos: PostgreSQL
Framework: FastEndpoints
Migraciones: Si

[Ejecuta guias en orden...]
[Muestra progreso con todo list...]
[Reporte final...]
```

---

## Notas Importantes

- **Configurar GUIDES_REPO** antes de usar el comando
- **Leer guias completas** antes de ejecutar comandos
- **Respetar el orden** de ejecucion (hay dependencias)
- **Reemplazar TODOS los placeholders** en archivos y rutas
- **Validar cada paso** antes de continuar
- **Usar TodoWrite** para mantener al usuario informado
