# Architectures

Guias de arquitectura para proyectos backend .NET.

---

## Arquitecturas Disponibles

### Clean Architecture

Arquitectura en capas con separacion clara de responsabilidades.

```
architectures/
└── clean-architecture/
    └── init/           # Guias de inicializacion (01-05)
        ├── 01-estructura-base.md
        ├── 02-domain-layer.md
        ├── 03-application-layer.md
        ├── 04-infrastructure-layer.md
        └── 05-webapi-layer.md
```

**Documentacion:** [clean-architecture/README.md](clean-architecture/README.md)

---

## Proximas Arquitecturas

- **Vertical Slice Architecture** - Por feature en lugar de por capa
- **Modular Monolith** - Modulos independientes en un solo deployment

---

## Como Usar

Las guias en `init/` se ejecutan secuencialmente para crear un proyecto desde cero.
