# Stacks

Configuracion de tecnologias especificas para proyectos backend.

---

## Estructura

```
stacks/
├── database/               # Bases de datos
│   ├── postgresql/         # PostgreSQL
│   │   ├── guides/
│   │   └── templates/
│   ├── sqlserver/          # SQL Server
│   │   ├── guides/
│   │   └── templates/
│   └── migrations/         # Herramientas de migracion
│       └── fluent-migrator/
│
├── orm/                    # Object-Relational Mappers
│   └── nhibernate/
│       ├── guides/
│       └── templates/
│
└── webapi/                 # Frameworks WebAPI
    └── fastendpoints/
        ├── guides/
        └── templates/
```

---

## Bases de Datos

### PostgreSQL

Base de datos relacional open source. **Recomendado** para nuevos proyectos.

| Recurso | Descripcion |
|---------|-------------|
| [guides/setup.md](database/postgresql/guides/setup.md) | Configuracion inicial |
| [templates/ConnectionStringBuilder.cs](database/postgresql/templates/ConnectionStringBuilder.cs) | Constructor de connection string |

### SQL Server

Base de datos Microsoft SQL Server.

| Recurso | Descripcion |
|---------|-------------|
| [guides/setup.md](database/sqlserver/guides/setup.md) | Configuracion inicial |
| [templates/ConnectionStringBuilder.cs](database/sqlserver/templates/ConnectionStringBuilder.cs) | Constructor de connection string |

### Migraciones

#### FluentMigrator

Sistema de migraciones basado en codigo C#.

| Recurso | Descripcion |
|---------|-------------|
| [guides/setup.md](database/migrations/fluent-migrator/guides/setup.md) | Crear proyecto de migraciones |
| [guides/best-practices.md](database/migrations/fluent-migrator/guides/best-practices.md) | Mejores practicas |
| [guides/patterns.md](database/migrations/fluent-migrator/guides/patterns.md) | Patrones comunes |

---

## ORMs

### NHibernate

ORM maduro con soporte completo para mapeo objeto-relacional.

| Recurso | Descripcion |
|---------|-------------|
| [guides/setup.md](orm/nhibernate/guides/setup.md) | Configuracion de NHibernate |
| [templates/NHSessionFactory.cs](orm/nhibernate/templates/NHSessionFactory.cs) | Factory de sesiones |

---

## WebAPI Frameworks

### FastEndpoints

Framework de endpoints con minimo boilerplate.

| Recurso | Descripcion |
|---------|-------------|
| [guides/setup.md](webapi/fastendpoints/guides/setup.md) | Configuracion de FastEndpoints |

---

## Como Usar

1. Ejecutar las guias de `architectures/clean-architecture/init/` primero
2. Seleccionar base de datos y ejecutar su guia de setup
3. Configurar ORM con NHibernate
4. (Opcional) Configurar WebAPI con FastEndpoints
5. (Opcional) Configurar proyecto de migraciones

Cada carpeta de stack contiene:
- `guides/` - Documentacion paso a paso
- `templates/` - Archivos de codigo reutilizables
