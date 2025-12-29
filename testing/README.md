# Testing

Guias, templates y ejemplos para testing de proyectos backend .NET.

---

## Estructura

```
testing/
├── fundamentals/           # Conceptos basicos de testing
│   └── guides/
│       └── conventions.md  # Convenciones de testing
│
├── unit/                   # Tests unitarios
│   ├── guides/
│   │   ├── domain-testing.md
│   │   ├── infrastructure-testing.md
│   │   └── webapi-testing.md
│   └── templates/
│       ├── DomainTestBase.cs
│       ├── ApplicationTestBase.cs
│       └── EndpointTestBase.cs
│
├── integration/            # Tests de integracion
│   ├── guides/
│   │   └── database-testing.md
│   ├── examples/
│   │   ├── README.md
│   │   └── step-by-step.md
│   ├── scenarios/
│   │   └── guides/
│   │       └── scenarios-creation-guide.md
│   └── tools/
│       └── ndbunit/
│           └── templates/
│               ├── NHRepositoryTestBase.cs
│               └── NHRepositoryTestInfrastructureBase.cs
│
└── by-layer/               # Referencias por capa
    ├── domain/
    ├── infrastructure/
    └── webapi/
```

---

## Guias

### Fundamentals

Conceptos basicos y convenciones de testing.

| Guia | Descripcion |
|------|-------------|
| [conventions.md](fundamentals/guides/conventions.md) | Convenciones de nombres, estructura, mejores practicas |

### Unit Testing

Tests unitarios por capa.

| Guia | Descripcion |
|------|-------------|
| [domain-testing.md](unit/guides/domain-testing.md) | Testing de entidades y validadores |
| [infrastructure-testing.md](unit/guides/infrastructure-testing.md) | Testing de repositorios (mocks) |
| [webapi-testing.md](unit/guides/webapi-testing.md) | Testing de endpoints |

### Integration Testing

Tests de integracion con base de datos.

| Guia | Descripcion |
|------|-------------|
| [database-testing.md](integration/guides/database-testing.md) | Tests con base de datos real |
| [scenarios-creation-guide.md](integration/scenarios/guides/scenarios-creation-guide.md) | Crear escenarios de datos |

---

## Templates

### Unit Tests

| Template | Descripcion |
|----------|-------------|
| [DomainTestBase.cs](unit/templates/DomainTestBase.cs) | Clase base para tests de dominio |
| [ApplicationTestBase.cs](unit/templates/ApplicationTestBase.cs) | Clase base para tests de aplicacion |
| [EndpointTestBase.cs](unit/templates/EndpointTestBase.cs) | Clase base para tests de endpoints |

### Integration Tests (NDBUnit)

| Template | Descripcion |
|----------|-------------|
| [NHRepositoryTestBase.cs](integration/tools/ndbunit/templates/NHRepositoryTestBase.cs) | Clase base para tests de repositorio |
| [NHRepositoryTestInfrastructureBase.cs](integration/tools/ndbunit/templates/NHRepositoryTestInfrastructureBase.cs) | Infraestructura de tests |

---

## Ejemplos

| Ejemplo | Descripcion |
|---------|-------------|
| [integration/examples/](integration/examples/) | Ejemplos de tests de integracion |

---

## Stack de Testing

| Herramienta | Uso |
|-------------|-----|
| NUnit | Framework de tests |
| Moq | Mocking de dependencias |
| AutoFixture | Generacion de datos de prueba |
| FluentAssertions | Assertions legibles |
| NDBUnit | Datos de prueba para DB |

---

## Como Usar

### Tests Unitarios

1. Crear proyecto de tests: `{ProjectName}.domain.tests`
2. Copiar template base correspondiente
3. Heredar de la clase base en tus tests
4. Usar Moq para dependencias

### Tests de Integracion

1. Crear proyecto: `{ProjectName}.infrastructure.tests`
2. Configurar connection string de test
3. Copiar templates de NDBUnit
4. Crear escenarios de datos XML
