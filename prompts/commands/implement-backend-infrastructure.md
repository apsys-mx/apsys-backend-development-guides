# Implement Backend Infrastructure Layer

> **Version Comando:** 2.0.0
> **Ultima actualizacion:** 2025-12-30

---

Implementa la capa de Infrastructure Layer siguiendo el plan de implementacion y las guias de desarrollo de APSYS.

## Entrada

**Contexto:** $ARGUMENTS

- Si se proporciona un archivo de plan (`.claude/planning/*-implementation-plan.md`), extrae la seccion "Fase 2: Infrastructure Layer"
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
| Repositories | `stacks/orm/nhibernate/guides/repositories.md` | Siempre |
| Mappers | `stacks/orm/nhibernate/guides/mappers.md` | Siempre |
| Scenarios XML | `testing/integration/scenarios/guides/scenarios-creation-guide.md` | Para tests de integracion |
| Best Practices | `stacks/orm/nhibernate/guides/best-practices.md` | Recomendado |

---

## Proceso de Implementacion

### Paso 1: Analizar el Plan

Extrae del plan o contexto:

- Entidad y tabla a mapear
- Metodos CRUD y custom del repositorio
- Relaciones con otras entidades (FK)
- Nombre de la propiedad en NHUnitOfWork

### Paso 2: Explorar Proyecto Actual

Busca implementaciones existentes como referencia:

```bash
# Mappers existentes
Glob: **/nhibernate/mappers/*Mapper.cs

# Repositories existentes
Glob: **/nhibernate/NH*Repository.cs

# NHUnitOfWork
Glob: **/nhibernate/NHUnitOfWork.cs

# NHSessionFactory
Glob: **/nhibernate/NHSessionFactory.cs

# Escenarios XML existentes
Glob: **/scenarios/*.xml
```

### Paso 3: Implementar Componentes

Implementa en este orden:

#### 3.1 Mapper

**Archivo:** `{proyecto}.infrastructure/nhibernate/mappers/{Entity}Mapper.cs`

Siguiendo la guia `mappers.md`:
- Heredar de `ClassMapping<{Entity}>`
- Configurar tabla con nombre en snake_case
- Mapear Id con Generators.Assigned
- Propiedades con columnas en snake_case
- Relaciones ManyToOne con LazyRelation.Proxy

#### 3.2 Repository

**Archivo:** `{proyecto}.infrastructure/nhibernate/NH{Entity}Repository.cs`

Siguiendo la guia `repositories.md`:
- Heredar de `NHRepository<{Entity}, Guid>`
- Implementar `I{Entity}Repository`
- Constructor con `ISession` y `IServiceProvider`
- Implementar metodos segun el plan
- FlushAsync() despues de operaciones de escritura
- Validar entidad antes de persistir

#### 3.3 Registrar Mapper

**Archivo a modificar:** `{proyecto}.infrastructure/nhibernate/NHSessionFactory.cs`

- Agregar: `mapper.AddMapping<{Entity}Mapper>();`

#### 3.4 Actualizar NHUnitOfWork

**Archivo a modificar:** `{proyecto}.infrastructure/nhibernate/NHUnitOfWork.cs`

- Agregar lazy property para el repositorio:

```csharp
private I{Entity}Repository? _{entities};
public I{Entity}Repository {Entities} => _{entities} ??= new NH{Entity}Repository(_session, _serviceProvider);
```

### Paso 4: Verificar

- [ ] Codigo compila sin errores
- [ ] Mapper tiene tabla y columnas en snake_case
- [ ] Repository hereda de NHRepository
- [ ] Repository implementa todos los metodos del plan
- [ ] NHSessionFactory tiene el mapper registrado
- [ ] NHUnitOfWork tiene la lazy property

---

## Formato de Salida

Al finalizar, muestra:

```markdown
## Infrastructure Layer Implementado

### Archivos Creados

| Archivo | Descripcion |
|---------|-------------|
| `{proyecto}.infrastructure/nhibernate/mappers/{Entity}Mapper.cs` | Mapper con {n} propiedades |
| `{proyecto}.infrastructure/nhibernate/NH{Entity}Repository.cs` | Repository con {n} metodos |

### Archivos Modificados

| Archivo | Cambio |
|---------|--------|
| `NHSessionFactory.cs` | Registrado `{Entity}Mapper` |
| `NHUnitOfWork.cs` | Agregada lazy property `{Entities}` |

### Metodos Implementados

- `CreateAsync({params})` - Crear entidad
- `GetAsync(Guid)` - Obtener por ID
- `GetBy{Campo}Async({tipo})` - Buscar por campo unico
- `UpdateAsync(Guid, {params})` - Actualizar entidad

### Proximos Pasos

Continuar con Application + WebAPI Layer: `/implement-backend-webapi`

**Status:** SUCCESS
```

---

## Restricciones

### NO debes:
- Implementar la capa de Application o WebAPI (eso es otro comando)
- Crear archivos fuera de `{proyecto}.infrastructure/`
- Inventar mapeos o metodos no especificados en el plan

### DEBES:
- Seguir estrictamente las guias de desarrollo
- Usar los patrones de implementaciones existentes como referencia
- Implementar TODOS los componentes listados en el plan
- Registrar el mapper en NHSessionFactory
- Actualizar NHUnitOfWork

---

## Referencias

- [Repositories]({GUIDES_REPO}/stacks/orm/nhibernate/guides/repositories.md)
- [Mappers]({GUIDES_REPO}/stacks/orm/nhibernate/guides/mappers.md)
- [Scenarios XML]({GUIDES_REPO}/testing/integration/scenarios/guides/scenarios-creation-guide.md)
- [Best Practices]({GUIDES_REPO}/stacks/orm/nhibernate/guides/best-practices.md)
