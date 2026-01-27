# Review Backend Code

> **Version Comando:** 1.1.0
> **Ultima actualizacion:** 2025-01-23

---

Realiza peer review de codigo backend, analizando **exclusivamente** los archivos modificados en un branch, verificando que cumplan con los estandares de APSYS y las guias de desarrollo.

## Entrada

**Branch a revisar:** $ARGUMENTS

- Si se proporciona un nombre de branch, usalo directamente
- Si se proporciona `--base={branch}`, usa ese branch como base para comparar (default: `devel`)
- Si `$ARGUMENTS` esta vacio, pregunta al usuario que branch desea revisar

**Ejemplos:**
```bash
/review-backend-code feature/KC-200-reporte-ventas
/review-backend-code feature/KC-200-reporte-ventas --base=main
/review-backend-code --base=master
```

## Configuracion

Las guias se encuentran en `docs/guides/` del proyecto (agregado como git submodule).

---

## Verificacion Inicial (OBLIGATORIO)

**ANTES de cualquier otra accion**, verificar que existe el submodule de guias:

```bash
# Verificar que existe la carpeta docs/guides con contenido
ls docs/guides/README.md
```

**Si la verificacion falla** (la carpeta no existe o esta vacia):

1. **DETENER** la ejecucion inmediatamente
2. **Mostrar** el siguiente mensaje al usuario:

```
ERROR: No se encontro el submodule de guias en docs/guides/

Este comando requiere las guias de desarrollo de APSYS configuradas como submodule.

Para configurarlo, ejecuta:

  git submodule add https://github.com/apsys-mx/apsys-backend-development-guides.git docs/guides

Si ya lo agregaste pero esta vacio:

  git submodule update --init --recursive

Documentacion: https://github.com/apsys-mx/apsys-backend-development-guides#instalacion-en-proyectos
```

3. **NO continuar** con el resto del comando

**Si la verificacion es exitosa**, continuar con el proceso normal.

---

## Guias de Referencia para Review

Consulta estas guias desde `docs/guides` segun la capa del codigo:

### Domain Layer

| Guia | Ruta |
|------|------|
| Entities | `fundamentals/patterns/domain-modeling/entities.md` |
| Validators | `fundamentals/patterns/domain-modeling/validators.md` |
| Repository Interfaces | `fundamentals/patterns/domain-modeling/repository-interfaces.md` |
| Domain Exceptions | `fundamentals/patterns/domain-modeling/domain-exceptions.md` |

### Infrastructure Layer

| Guia | Ruta |
|------|------|
| Repositories | `stacks/orm/nhibernate/guides/repositories.md` |
| Mappers | `stacks/orm/nhibernate/guides/mappers.md` |
| Best Practices | `stacks/orm/nhibernate/guides/best-practices.md` |

### Application Layer

| Guia | Ruta |
|------|------|
| Use Cases | `architectures/clean-architecture/guides/application/use-cases.md` |
| Command Handler Patterns | `architectures/clean-architecture/guides/application/command-handler-patterns.md` |
| Error Handling | `architectures/clean-architecture/guides/application/error-handling.md` |

### WebAPI Layer

| Guia | Ruta |
|------|------|
| FastEndpoints Basics | `stacks/webapi/fastendpoints/guides/fastendpoints-basics.md` |
| Request/Response Models | `stacks/webapi/fastendpoints/guides/request-response-models.md` |
| DTOs | `architectures/clean-architecture/guides/webapi/dtos.md` |
| Error Responses | `architectures/clean-architecture/guides/webapi/error-responses.md` |

### Best Practices

| Guia | Ruta |
|------|------|
| Date Handling | `fundamentals/patterns/best-practices/date-handling.md` |

### Ejemplos de Referencia

| Ejemplo | Ruta |
|---------|------|
| CRUD Feature | `architectures/clean-architecture/examples/crud-feature/` |
| Read-Only Feature | `architectures/clean-architecture/examples/read-only-feature/` |
| Complex Feature | `architectures/clean-architecture/examples/complex-feature/` |

---

## Proceso de Review

### Paso 1: Confirmar Parametros

Antes de iniciar, confirma con el usuario:

```markdown
## Configuracion del Peer Review

**Branch a revisar:** {branch_name}
**Branch base:** {base_branch} (default: devel)

¿Es correcta esta configuracion?
```

### Paso 2: Preparar Entorno

```bash
git fetch origin
git checkout {branch_name}
git pull origin {branch_name}
```

### Paso 3: Identificar Archivos Modificados

```bash
# Lista de archivos modificados
git diff --name-only {base_branch}...{branch_name}

# Estadisticas de cambios
git diff --stat {base_branch}...{branch_name}

# Commits del branch
git log {base_branch}..{branch_name} --oneline
```

### Paso 4: Review por Capa

Revisar **EXCLUSIVAMENTE** los archivos modificados.

#### Domain Layer

- [ ] Entities heredan de `AbstractDomainObject`
- [ ] Propiedades son `virtual`
- [ ] Validators implementados con FluentValidation
- [ ] Repository interfaces definidas correctamente
- [ ] IUnitOfWork actualizado si hay nuevos repositorios
- [ ] XML comments completos

#### Infrastructure Layer

- [ ] Repositories heredan de `NHRepository` o `NHReadOnlyRepository`
- [ ] Mappers configurados correctamente (tabla/columnas en snake_case)
- [ ] `FlushAsync()` despues de operaciones de escritura
- [ ] Mapper registrado en `NHSessionFactory`
- [ ] Lazy property en `NHUnitOfWork`

#### Application Layer

- [ ] Use Cases son thin wrappers (solo orquestacion)
- [ ] NO hay logica de negocio en Use Cases
- [ ] Uso correcto de `Result<T>` (FluentResults)
- [ ] Command/Query como clases internas

#### WebAPI Layer

- [ ] Endpoints heredan de `Endpoint<TRequest, TResponse>`
- [ ] DTOs solo tienen propiedades (sin logica)
- [ ] Strings inicializados con `string.Empty`
- [ ] Colecciones con `Enumerable.Empty<T>()`
- [ ] Codigos HTTP correctos (201, 204, 404, 409)
- [ ] Validators con FluentValidation

#### Date Handling (si hay propiedades DateTime)

- [ ] Request Models usan `DateTimeOffset` para recibir fechas del frontend
- [ ] MappingProfile convierte a UTC con `.UtcDateTime` o `.ToUniversalTime()`
- [ ] Entities almacenan fechas en UTC
- [ ] Comparaciones de fechas usan `DateTime.UtcNow` (NO `DateTime.Now`)
- [ ] Queries de repositorio usan `DateTime.UtcNow` como referencia
- [ ] DTOs devuelven fechas en UTC (se serializan con sufijo `Z`)

### Paso 5: Checklist General

#### Arquitectura

- [ ] Respeta Clean Architecture (dependencias hacia adentro)
- [ ] No hay referencias circulares entre capas
- [ ] Separation of concerns respetada

#### Codigo

- [ ] Naming conventions seguidas (PascalCase)
- [ ] No hay codigo comentado sin razon
- [ ] No hay TODOs sin ticket asociado
- [ ] No hay magic numbers/strings
- [ ] Manejo de errores apropiado

#### Seguridad

- [ ] No hay credenciales hardcodeadas
- [ ] Validacion de inputs
- [ ] Autorizacion implementada correctamente

#### Performance

- [ ] No hay N+1 queries
- [ ] Uso apropiado de async/await
- [ ] Queries con `.ToLower()` para case-insensitive

---

## Severidad de Issues

### Criticos (Bloquean aprobacion)

- Viola principios de Clean Architecture
- Vulnerabilidad de seguridad
- Bug que causa fallo en produccion
- Rompe funcionalidad existente

### Importantes (Deben corregirse)

- No sigue convenciones de las guias
- Falta validacion o manejo de errores
- Codigo duplicado significativo
- Performance issue evidente

### Menores (Sugerencias)

- Mejoras de legibilidad
- Naming podria ser mas claro
- Comentarios faltantes
- Sugerencias de refactoring opcional

---

## Formato de Salida

Crear reporte en `.claude/reviews/{branch_name}-review.md`.

> **Nota:** `{VERSION_COMANDO}` debe sustituirse por la version declarada en el encabezado de este prompt (campo "Version Comando").

```markdown
# Peer Review: {branch_name}

> **Generado con:** review-backend-code v{VERSION_COMANDO}
> **Fecha:** {fecha de generacion}

---

**Revisor:** Claude Code
**Estado:** {Aprobado | Aprobado con observaciones | Requiere cambios}

## Resumen Ejecutivo

{Descripcion breve del resultado en 2-3 lineas}

## Informacion del Branch

- **Branch:** {branch_name}
- **Base:** {base_branch}
- **Commits:** {numero}
- **Archivos modificados:** {numero}
- **Lineas:** +{agregadas} / -{eliminadas}

## Archivos Revisados

| Archivo | Capa | Issues |
|---------|------|--------|
| {ruta/archivo.cs} | {Domain/Infrastructure/etc} | {numero} |

## Issues Encontrados

### Criticos

#### Issue #1: {Titulo}

- **Archivo:** `{ruta/archivo.cs}:{linea}`
- **Tipo:** {Arquitectura | Seguridad | Bug}
- **Descripcion:** {Explicacion}
- **Guia:** `{ruta/a/guia.md}`
- **Sugerencia:** {Como corregirlo}

### Importantes

...

### Menores

...

## Checklist de Cumplimiento

| Capa | Status | Observaciones |
|------|--------|---------------|
| Domain | {OK/Observaciones/Requiere cambios} | {descripcion} |
| Infrastructure | ... | ... |
| Application | ... | ... |
| WebAPI | ... | ... |

## Aspectos Positivos

- {Aspecto positivo 1}
- {Aspecto positivo 2}

## Conclusion

{Parrafo con conclusion y proximos pasos}

```

---

## Restricciones

### NO debes:
- Usar GitHub CLI (`gh`) - trabajar solo con git local
- Modificar codigo durante el review
- Aprobar con issues criticos pendientes
- Hacer suposiciones - verificar en las guias

### DEBES:
- Confirmar parametros antes de iniciar
- Revisar SOLO archivos modificados en el branch
- Referenciar la guia correspondiente para cada issue
- Incluir codigo de ejemplo en issues criticos
- Usar `git diff` y `git log` (no `gh pr`)

---

## Comandos Git Utiles

```bash
# Archivos modificados
git diff --name-only {base_branch}...{branch_name}

# Estadisticas
git diff --stat {base_branch}...{branch_name}

# Historial
git log {base_branch}..{branch_name} --oneline

# Cambios en archivo especifico
git diff {base_branch}...{branch_name} -- path/to/file.cs

# Diff completo
git diff {base_branch}...{branch_name}
```

---

## Referencias

- [Entities](docs/guides/fundamentals/patterns/domain-modeling/entities.md)
- [Validators](docs/guides/fundamentals/patterns/domain-modeling/validators.md)
- [Repositories](docs/guides/stacks/orm/nhibernate/guides/repositories.md)
- [Use Cases](docs/guides/architectures/clean-architecture/guides/application/use-cases.md)
- [FastEndpoints](docs/guides/stacks/webapi/fastendpoints/guides/fastendpoints-basics.md)
- [DTOs](docs/guides/architectures/clean-architecture/guides/webapi/dtos.md)
- [Date Handling](docs/guides/fundamentals/patterns/best-practices/date-handling.md)
