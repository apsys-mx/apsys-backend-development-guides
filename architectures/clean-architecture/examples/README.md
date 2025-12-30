# Clean Architecture Examples

Ejemplos paso a paso para implementar diferentes tipos de features en proyectos con Clean Architecture.

## Ejemplos Disponibles

| Ejemplo | Descripcion | Complejidad |
|---------|-------------|-------------|
| [CRUD Feature](crud-feature/README.md) | Feature completo con Create, Read, Update, Delete | Media |
| [Read-Only Feature](read-only-feature/README.md) | Feature de solo lectura usando patron DAO | Baja |
| [Complex Feature](complex-feature/README.md) | Feature con relaciones, validaciones complejas y logica de negocio | Alta |

## Estructura de Cada Ejemplo

Cada ejemplo incluye:

- **README.md** - Descripcion del caso de uso y requerimientos
- **step-by-step.md** - Guia detallada de implementacion paso a paso

## Cuando Usar Cada Patron

### CRUD Feature
- Entidades que requieren todas las operaciones basicas
- Gestion de catalogos, usuarios, productos, etc.
- Validaciones de dominio estandar

### Read-Only Feature (DAO)
- Reportes y dashboards
- Vistas consolidadas de datos
- Consultas que cruzan multiples entidades
- No requiere operaciones de escritura

### Complex Feature
- Entidades con multiples relaciones
- Logica de negocio compleja
- Validaciones que dependen de otros agregados
- Flujos de trabajo con estados

## Orden de Aprendizaje Sugerido

1. **CRUD Feature** - Entender el flujo completo Entity → Endpoint
2. **Read-Only Feature** - Aprender el patron DAO para consultas
3. **Complex Feature** - Manejar casos avanzados

## Referencias

- [Feature Structure](../guides/feature-structure/README.md)
- [Domain Modeling](../../../fundamentals/patterns/domain-modeling/README.md)
- [Application Layer](../guides/application/README.md)
