# Templates

Templates de codigo reutilizables para proyectos backend.

---

## Estructura

```
templates/
├── Directory.Packages.props     # Gestion centralizada de paquetes NuGet
│
├── domain/                      # Capa Domain
│   ├── entities/
│   │   └── AbstractDomainObject.cs
│   ├── events/                  # Event-Driven Pattern
│   │   ├── DomainEvent.cs
│   │   └── PublishableEventAttribute.cs
│   ├── exceptions/
│   │   ├── InvalidDomainException.cs
│   │   ├── InvalidFilterArgumentException.cs
│   │   ├── ResourceNotFoundException.cs
│   │   └── DuplicatedDomainException.cs
│   └── interfaces/
│       ├── IEventStore.cs
│       └── repositories/
│           ├── IRepository.cs
│           ├── IReadOnlyRepository.cs
│           ├── IUnitOfWork.cs
│           ├── IDomainEventRepository.cs
│           └── ...
│
├── application/                 # Capa Application
│   └── ...
│
├── infrastructure/              # Capa Infrastructure
│   └── event-driven/            # Event-Driven Pattern
│       ├── EventStore.cs
│       └── nhibernate/
│           ├── NHDomainEventRepository.cs
│           └── DomainEventMapper.cs
│
├── webapi/                      # Capa WebAPI
│   ├── Program.cs
│   ├── .env.example
│   └── ...
│
└── tests/                       # Tests compartidos
    ├── DomainTestBase.cs
    ├── ApplicationTestBase.cs
    └── ...
```

---

## Placeholders

Los templates usan placeholders que se reemplazan al copiar:

| Placeholder | Descripcion | Ejemplo |
|-------------|-------------|---------|
| `{ProjectName}` | Nombre del proyecto (como lo proporciona el usuario) | `backend.api` |
| `{SchemaName}` | Nombre del schema de base de datos | `my_schema` |
| `{MigrationNumber}` | Numero de la migracion (3 digitos) | `013` |

**Ejemplo:**

```csharp
// Template
namespace {ProjectName}.domain.entities;

// Resultado (proyecto: backend.api)
namespace backend.api.domain.entities;
```

---

## Templates Especificos de Stack

Los templates de tecnologias especificas estan en `stacks/`:

| Stack | Ubicacion |
|-------|-----------|
| NHibernate | `stacks/orm/nhibernate/templates/` |
| PostgreSQL | `stacks/database/postgresql/templates/` |
| SQL Server | `stacks/database/sqlserver/templates/` |
| FluentMigrator | `stacks/database/migrations/fluent-migrator/templates/` |
| FastEndpoints | `stacks/webapi/fastendpoints/templates/` |

---

## Como Usar

### Con Agente IA

1. Leer template desde el repositorio
2. Reemplazar placeholders con valores reales
3. Escribir archivo procesado en destino

### Manual

1. Copiar template al proyecto
2. Buscar/reemplazar `{ProjectName}` con nombre real
3. Compilar para verificar
