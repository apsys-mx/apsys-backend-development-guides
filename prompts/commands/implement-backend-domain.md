# Implement Backend Domain Layer

> **Version Comando:** 2.0.0
> **Ultima actualizacion:** 2025-12-30

---

Implementa la capa de Domain Layer siguiendo el plan de implementacion y las guias de desarrollo de APSYS.

## Entrada

**Contexto:** $ARGUMENTS

- Si se proporciona un archivo de plan (`.claude/planning/*-implementation-plan.md`), extrae la seccion "Fase 1: Domain Layer"
- Si se proporciona una descripcion directa, usala como contexto
- Si `$ARGUMENTS` esta vacio, pregunta al usuario que desea implementar

## Configuracion

**Repositorio de Guias:**

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Guias a Consultar

Antes de implementar, lee las guias relevantes desde `{GUIDES_REPO}`:

| Guia | Ruta | Cuando Usar |
|------|------|-------------|
| Entities | `fundamentals/patterns/domain-modeling/entities.md` | Siempre |
| Validators | `fundamentals/patterns/domain-modeling/validators.md` | Siempre |
| Repository Interfaces | `fundamentals/patterns/domain-modeling/repository-interfaces.md` | Siempre |
| DAOs | `fundamentals/patterns/domain-modeling/daos.md` | Solo para read-only |
| Domain Exceptions | `fundamentals/patterns/domain-modeling/domain-exceptions.md` | Si hay errores custom |

---

## Proceso de Implementacion

### Paso 1: Analizar el Plan

Extrae del plan o contexto:

- Nombre de la entidad
- Propiedades con tipos y restricciones
- Validaciones requeridas
- Metodos del repositorio
- Nombre de la propiedad en IUnitOfWork

### Paso 2: Explorar Proyecto Actual

Busca implementaciones existentes como referencia:

```bash
# Entities existentes
Glob: **/entities/*.cs

# Validators existentes
Glob: **/entities/validators/*Validator.cs

# Repository interfaces
Glob: **/interfaces/repositories/I*Repository.cs

# IUnitOfWork
Glob: **/interfaces/repositories/IUnitOfWork.cs
```

### Paso 3: Implementar Componentes

Implementa en este orden:

#### 3.1 Entity

**Archivo:** `{proyecto}.domain/entities/{Entity}.cs`

Siguiendo la guia `entities.md`:
- Heredar de `AbstractDomainObject`
- Propiedades `virtual` para NHibernate
- Constructor vacio + constructor parametrizado
- Override de `GetValidator()`

#### 3.2 Validator

**Archivo:** `{proyecto}.domain/entities/validators/{Entity}Validator.cs`

Siguiendo la guia `validators.md`:
- Heredar de `AbstractValidator<{Entity}>`
- Reglas con FluentValidation
- Mensajes de error descriptivos
- Error codes para cada regla

#### 3.3 Repository Interface

**Archivo:** `{proyecto}.domain/interfaces/repositories/I{Entity}Repository.cs`

Siguiendo la guia `repository-interfaces.md`:
- Heredar de `IRepository<{Entity}, Guid>`
- Metodos custom segun el plan (CreateAsync, GetByXxxAsync, UpdateAsync)
- XML documentation completa

#### 3.4 Actualizar IUnitOfWork

**Archivo a modificar:** `{proyecto}.domain/interfaces/repositories/IUnitOfWork.cs`

- Agregar propiedad: `I{Entity}Repository {Entities} { get; }`

### Paso 4: Verificar

- [ ] Codigo compila sin errores
- [ ] Entity hereda de AbstractDomainObject
- [ ] Propiedades son virtual
- [ ] Validator implementa todas las reglas del plan
- [ ] Repository interface tiene todos los metodos del plan
- [ ] IUnitOfWork actualizado

---

## Formato de Salida

Al finalizar, muestra:

```markdown
## Domain Layer Implementado

### Archivos Creados

| Archivo | Descripcion |
|---------|-------------|
| `{proyecto}.domain/entities/{Entity}.cs` | Entidad con {n} propiedades |
| `{proyecto}.domain/entities/validators/{Entity}Validator.cs` | Validador con {n} reglas |
| `{proyecto}.domain/interfaces/repositories/I{Entity}Repository.cs` | Interface con {n} metodos |

### Archivos Modificados

| Archivo | Cambio |
|---------|--------|
| `IUnitOfWork.cs` | Agregada propiedad `{Entities}` |

### Validaciones Implementadas

- {Campo}: {Regla}
- {Campo}: {Regla}

### Metodos de Repositorio

- `CreateAsync({params})` - Crear entidad
- `GetBy{Campo}Async({tipo})` - Buscar por campo unico
- `UpdateAsync(Guid, {params})` - Actualizar entidad

### Proximos Pasos

Continuar con Infrastructure Layer: `/implement-backend-infrastructure`

**Status:** SUCCESS
```

---

## Restricciones

### NO debes:
- Implementar la capa de Infrastructure (eso es otro comando)
- Crear archivos fuera de `{proyecto}.domain/`
- Inventar propiedades o validaciones no especificadas en el plan

### DEBES:
- Seguir estrictamente las guias de desarrollo
- Usar los patrones de implementaciones existentes como referencia
- Implementar TODOS los componentes listados en el plan
- Actualizar IUnitOfWork

---

## Referencias

- [Entities]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/entities.md)
- [Validators]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/validators.md)
- [Repository Interfaces]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/repository-interfaces.md)
- [DAOs]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/daos.md)
