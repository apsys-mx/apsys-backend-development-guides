# Review Backend Code

> **Version Comando:** 1.0.0
> **Ultima actualizacion:** 2025-12-30

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

**Repositorio de Guias:**

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Guias de Referencia para Review

Consulta estas guias desde `{GUIDES_REPO}` segun la capa del codigo:

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

Crear reporte en `.claude/reviews/{branch_name}-review.md`:

```markdown
# Peer Review: {branch_name}

**Fecha:** {fecha}
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

---

_Generado por Claude Code_
_Fecha: {fecha y hora}_
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

- [Entities]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/entities.md)
- [Validators]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/validators.md)
- [Repositories]({GUIDES_REPO}/stacks/orm/nhibernate/guides/repositories.md)
- [Use Cases]({GUIDES_REPO}/architectures/clean-architecture/guides/application/use-cases.md)
- [FastEndpoints]({GUIDES_REPO}/stacks/webapi/fastendpoints/guides/fastendpoints-basics.md)
- [DTOs]({GUIDES_REPO}/architectures/clean-architecture/guides/webapi/dtos.md)
