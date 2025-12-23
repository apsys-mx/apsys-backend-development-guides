# Init Backend Project

> **Versión:** 3.0.0
> **Última actualización:** 2025-12-23

Inicializa un proyecto backend .NET con Clean Architecture siguiendo las guías de APSYS.

---

## Información Requerida

Antes de comenzar, solicita al usuario:

### 1. Nombre del proyecto
- Formato: PascalCase, sin espacios
- Ejemplo: `MiProyecto`, `GestionUsuarios`, `InventarioAPI`
- Se usará para reemplazar `{ProjectName}` en templates

### 2. Ubicación del proyecto
- Ruta absoluta donde crear el proyecto
- Ejemplo: `C:\projects\mi-proyecto`, `D:\workspace\backend`
- Si no existe, se creará

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

**Guías de inicialización:**
```
architectures/clean-architecture/init/
├── 01-estructura-base.md
├── 02-domain-layer.md
├── 03-application-layer.md
├── 04-infrastructure-layer.md
└── 05-webapi-layer.md
```

**Guías de stacks:**
```
stacks/
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
templates/
├── domain/
├── webapi/
├── tests/
└── Directory.Packages.props

stacks/{stack}/templates/
```

---

## Proceso de Ejecución

### Fase 1: Validación

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
   - Sugerir corrección si no cumple

### Fase 2: Crear Todo List

Crear lista de tareas según opciones seleccionadas:

```
- [ ] Crear estructura base de solución
- [ ] Implementar capa de dominio
- [ ] Implementar capa de aplicación
- [ ] Implementar capa de infraestructura
- [ ] Implementar capa WebAPI
- [ ] Configurar base de datos ({database})
- [ ] Configurar NHibernate
- [ ] Configurar FastEndpoints (si aplica)
- [ ] Configurar migraciones (si aplica)
- [ ] Verificación final
```

### Fase 3: Ejecutar Guías

Para cada guía, en orden:

1. **Leer la guía completa** con el tool Read
2. **Ejecutar los comandos** reemplazando `{ProjectName}`
3. **Copiar templates** cuando se indique, reemplazando placeholders
4. **Marcar como completado** en el todo list

#### Orden de ejecución:

| Paso | Guía | Descripción |
|------|------|-------------|
| 1 | `architectures/clean-architecture/init/01-estructura-base.md` | Solución .NET |
| 2 | `architectures/clean-architecture/init/02-domain-layer.md` | Capa de dominio |
| 3 | `architectures/clean-architecture/init/03-application-layer.md` | Capa de aplicación |
| 4 | `architectures/clean-architecture/init/04-infrastructure-layer.md` | Capa de infraestructura |
| 5 | `architectures/clean-architecture/init/05-webapi-layer.md` | Capa WebAPI base |
| 6 | `stacks/database/{database}/guides/setup.md` | Driver y ConnectionString |
| 7 | `stacks/orm/nhibernate/guides/setup.md` | Repositorios NHibernate |
| 8 | `stacks/webapi/fastendpoints/guides/setup.md` | FastEndpoints (si aplica) |
| 9 | `stacks/database/migrations/fluent-migrator/guides/setup.md` | Migraciones (si aplica) |

### Fase 4: Verificación Final

1. **Compilar solución**:
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

1. **Milestones completados** con ✅
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
3. **Comandos útiles**:
   ```bash
   dotnet build                                    # Compilar
   dotnet run --project src/{ProjectName}.webapi  # Ejecutar API
   dotnet run --project src/{ProjectName}.migrations cnn="..."  # Migraciones
   ```
4. **Próximos pasos**:
   - Crear entidades de dominio
   - Crear migraciones de base de datos
   - Implementar endpoints

---

## Reemplazo de Placeholders

En todos los archivos y rutas:
- `{ProjectName}` → Nombre del proyecto (PascalCase)
- `{projectname}` → Nombre del proyecto (lowercase, para DB)

---

## Manejo de Errores

Si ocurre un error:

1. **Detener ejecución**
2. **Reportar** con contexto:
   - Guía en la que falló
   - Comando que causó el error
   - Mensaje de error
3. **Sugerir solución**
4. **Preguntar** si continuar o cancelar

---

## Ejemplo de Flujo

```
Usuario: /init-backend

Asistente: ¿Cómo se llamará el proyecto? (PascalCase)
Usuario: GestionInventario

Asistente: ¿Dónde crear el proyecto?
Usuario: C:\projects\inventario

Asistente: ¿Qué base de datos?
1. PostgreSQL (recomendado)
2. SQL Server
Usuario: 1

Asistente: ¿Framework WebAPI?
1. FastEndpoints (recomendado)
2. Solo estructura base
Usuario: 1

Asistente: ¿Incluir proyecto de migraciones?
Usuario: Sí

Asistente:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🏗️  Inicializando: GestionInventario
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Ubicación: C:\projects\inventario
Base de datos: PostgreSQL
Framework: FastEndpoints
Migraciones: Sí
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Ejecuta guías en orden...]
[Muestra progreso con todo list...]
[Reporte final...]
```

---

## Notas Importantes

- **Leer guías completas** antes de ejecutar comandos
- **Respetar el orden** de ejecución (hay dependencias)
- **Reemplazar TODOS los placeholders** en archivos y rutas
- **Validar cada paso** antes de continuar
- **Usar TodoWrite** para mantener al usuario informado
