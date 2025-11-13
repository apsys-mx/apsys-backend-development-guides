# Best Practices - .NET Clean Architecture

**Version:** 0.1.0
**Estado:** ⏳ En desarrollo
**Última actualización:** 2025-01-13

## Descripción

Esta sección contiene las mejores prácticas y estándares para el desarrollo de aplicaciones backend .NET con Clean Architecture en APSYS. Cada guía documenta patrones probados, convenciones de código y técnicas utilizadas en proyectos de producción.

## Objetivo

Establecer un conjunto común de prácticas que:
- Mejoren la calidad del código
- Faciliten el mantenimiento y escalabilidad
- Promuevan la consistencia entre proyectos
- Reduzcan errores comunes
- Aceleren el onboarding de nuevos desarrolladores

## Guías Disponibles

### 1. [Clean Architecture Principles](./clean-architecture-principles.md)

Principios fundamentales de Clean Architecture aplicados a .NET.

**Contenido:**
- Dependency Rule (regla de dependencias)
- Separation of Concerns
- Inversión de dependencias
- Testability
- Independence of Frameworks
- Independence of UI
- Independence of Database
- Organización en capas

**Cuándo usar:** Antes de diseñar la arquitectura de un feature o al hacer code reviews arquitecturales.

---

### 2. [Code Organization](./code-organization.md)

Convenciones de organización de código, namespaces, usings y nomenclatura.

**Contenido:**
- Organización de namespaces por capa
- Orden de usings
- Convenciones de nombres (PascalCase, camelCase)
- Estructura de archivos por feature
- Organización de carpetas
- File-scoped namespaces
- Checklist de organización

**Cuándo usar:** Al crear nuevos archivos o refactorizar código existente.

---

### 3. [Async/Await Patterns](./async-await-patterns.md)

Mejores prácticas para programación asíncrona en .NET.

**Contenido:**
- Cuándo usar async/await
- ConfigureAwait(false) vs no usar
- ValueTask vs Task
- Async all the way
- Cancellation tokens
- Evitar async void
- Manejo de excepciones en código async
- Anti-patrones comunes

**Cuándo usar:** Al escribir código asíncrono o al revisar performance de APIs.

---

### 4. [Error Handling](./error-handling.md)

Estrategias de manejo de errores con FluentResults y excepciones.

**Contenido:**
- FluentResults: Result<T> pattern
- Cuándo usar excepciones vs Results
- Custom exceptions en Domain
- Error propagation entre capas
- Error messages y códigos
- Logging de errores
- Traducción a HTTP status codes

**Cuándo usar:** Al implementar use cases o endpoints que requieren manejo de errores robusto.

---

### 5. [Dependency Injection](./dependency-injection.md)

Mejores prácticas de Dependency Injection en .NET.

**Contenido:**
- Service lifetimes (Singleton, Scoped, Transient)
- Constructor injection vs property injection
- Service registration patterns
- Scrutor para registro automático
- Evitar service locator pattern
- Testing con DI
- Anti-patrones comunes

**Cuándo usar:** Al configurar servicios o resolver problemas de inyección de dependencias.

---

### 6. [Testing Conventions](./testing-conventions.md)

Convenciones y mejores prácticas para testing.

**Contenido:**
- Estructura de tests (Arrange-Act-Assert)
- Naming conventions para tests
- NUnit attributes y setup
- Uso de Moq para mocking
- AutoFixture para test data
- FluentAssertions
- Test organization por capa
- Integration tests vs Unit tests

**Cuándo usar:** Al escribir tests o configurar proyectos de testing.

---

## Flujo de Trabajo Recomendado

### Para Nuevos Proyectos

1. **Configurar arquitectura** → [Clean Architecture Principles](./clean-architecture-principles.md)
2. **Establecer estructura** → [Code Organization](./code-organization.md)
3. **Configurar DI** → [Dependency Injection](./dependency-injection.md)
4. **Definir error handling** → [Error Handling](./error-handling.md)
5. **Configurar testing** → [Testing Conventions](./testing-conventions.md)

### Para Code Reviews

Verificar que el código cumple con:
- ✅ Principios de Clean Architecture ([Clean Architecture Principles](./clean-architecture-principles.md))
- ✅ Organización correcta ([Code Organization](./code-organization.md))
- ✅ Uso correcto de async/await ([Async/Await Patterns](./async-await-patterns.md))
- ✅ Manejo apropiado de errores ([Error Handling](./error-handling.md))
- ✅ DI correctamente configurado ([Dependency Injection](./dependency-injection.md))
- ✅ Tests adecuados ([Testing Conventions](./testing-conventions.md))

### Para Refactoring

1. **Identificar problemas** → Revisar todas las guías
2. **Reorganizar código** → [Code Organization](./code-organization.md)
3. **Mejorar async** → [Async/Await Patterns](./async-await-patterns.md)
4. **Mejorar error handling** → [Error Handling](./error-handling.md)
5. **Refactorizar DI** → [Dependency Injection](./dependency-injection.md)
6. **Agregar/mejorar tests** → [Testing Conventions](./testing-conventions.md)

---

## Checklists Rápidas

### Nuevo Feature

- [ ] Sigue Clean Architecture principles
- [ ] Organización de carpetas correcta
- [ ] Namespaces consistentes
- [ ] Usings ordenados
- [ ] Nombres descriptivos (PascalCase para clases, camelCase para parámetros)
- [ ] Métodos async donde corresponde
- [ ] CancellationToken en métodos async públicos
- [ ] Error handling con FluentResults
- [ ] DI configurado correctamente
- [ ] Tests escritos (unit + integration)

### Code Review Checklist

- [ ] No hay violaciones de Dependency Rule
- [ ] Código organizado por feature
- [ ] No hay usings innecesarios
- [ ] Async/await usado correctamente
- [ ] No hay async void (excepto event handlers)
- [ ] Errores manejados apropiadamente
- [ ] No hay service locator anti-pattern
- [ ] Tests cubren casos principales
- [ ] No hay código comentado

---

## Stack Tecnológico

Estas guías asumen el siguiente stack:

- **.NET 9.0** - Framework
- **C# 13** - Lenguaje
- **FluentResults 4.0** - Error handling
- **FluentValidation 12.0** - Validación
- **NUnit 4.2** - Testing
- **Moq 4.20** - Mocking
- **AutoFixture 4.18** - Test data
- **FluentAssertions 8.5** - Test assertions

---

## Recursos Adicionales

### Documentación Oficial

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

### Herramientas

- [SonarLint](https://www.sonarlint.org/) - Análisis estático de código
- [ReSharper](https://www.jetbrains.com/resharper/) - Code analysis y refactoring
- [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) - Code style enforcement

---

## Contribuir

Para agregar o modificar guías:

1. Seguir el formato establecido en guías existentes
2. Incluir ejemplos claros de código
3. Mostrar qué hacer y qué NO hacer
4. Agregar checklist al final de cada guía
5. Actualizar este README con el nuevo contenido
6. Probar ejemplos de código

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
