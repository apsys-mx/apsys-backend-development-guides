# Prompts - Backend Development

Comandos para agentes IA que automatizan tareas de desarrollo backend.

## Instalacion Rapida

Requiere `gh auth login` primero.

```powershell
# Windows (PowerShell)
gh repo clone apsys-mx/apsys-backend-development-guides $env:TEMP\apsys-install; & "$env:TEMP\apsys-install\install.ps1"
```

```bash
# Linux / macOS
gh repo clone apsys-mx/apsys-backend-development-guides /tmp/apsys-install && /tmp/apsys-install/install.sh
```

Ver [instrucciones completas](../README.md#instalacion-de-comandos-global) en el README principal.

## Comandos Disponibles

| Comando | Tipo | Version | Descripcion |
|---------|------|---------|-------------|
| [init-backend](commands/init-backend.md) | One-time | 1.0.0 | Inicializa proyecto .NET con Clean Architecture |
| [add-event-store](commands/add-event-store.md) | One-time | 1.0.0 | Agrega Event Store al proyecto |
| [review-backend-code](commands/review-backend-code.md) | Recurring | 1.0.0 | Revisa codigo contra guias |

> **Nota:** Los comandos `plan-backend-feature`, `implement-backend-feature`, `plan-backend-testing` e `implement-backend-testing` fueron reemplazados por el proceso Open Spec.

## Tipos de Comandos

- **One-time**: Se ejecutan una sola vez (ej: inicializacion)
- **Recurring**: Se ejecutan multiples veces durante desarrollo

## Uso

Los comandos se invocan como slash commands en Claude Code:

```
/init-backend
/add-event-store
/review-backend-code [archivos]
```

## Flujo Tipico

```
1. /init-backend        → Crear proyecto base
2. /add-event-store     → (Opcional) Agregar Event Store
3. Open Spec            → Planificar e implementar features
4. /review-backend-code → Revisar antes de merge
```

## Recursos Consultados

Los comandos consultan automaticamente:

- `fundamentals/` - Patrones y mejores practicas
- `architectures/` - Guias de arquitectura
- `stacks/` - Configuracion de tecnologias
- `templates/` - Codigo reutilizable
- `testing/` - Guias de testing
