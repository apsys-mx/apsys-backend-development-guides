# Architectures

Guias de arquitectura para proyectos backend .NET.

---

## Arquitecturas Disponibles

### Clean Architecture

Arquitectura en capas con separacion clara de responsabilidades.

```
architectures/
└── clean-architecture/
    ├── init/              # Guias de inicializacion (01-05)
    │   ├── 01-estructura-base.md
    │   ├── 02-domain-layer.md
    │   ├── 03-application-layer.md
    │   ├── 04-infrastructure-layer.md
    │   └── 05-webapi-layer.md
    │
    ├── guides/            # Guias por capa
    │   ├── domain/        # Referencias a fundamentals
    │   ├── application/   # Use cases, command handlers
    │   ├── infrastructure/# Referencias a stacks
    │   ├── webapi/        # Auth, DTOs
    │   └── feature-structure/
    │
    └── examples/          # Ejemplos completos
        ├── crud-feature/
        ├── read-only-feature/
        └── complex-feature/
```

**Documentacion:** [clean-architecture/README.md](clean-architecture/README.md)

---

## Proximas Arquitecturas

- **Vertical Slice Architecture** - Por feature en lugar de por capa
- **Modular Monolith** - Modulos independientes en un solo deployment

---

## Como Usar

1. Las guias en `init/` se ejecutan secuencialmente para crear un proyecto desde cero
2. Las guias en `guides/` proporcionan documentacion detallada por capa
3. Los `examples/` muestran features completas implementadas
