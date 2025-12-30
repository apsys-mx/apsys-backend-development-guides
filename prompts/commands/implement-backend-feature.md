# Implement Backend Feature

> **Version Comando:** 2.0.0
> **Ultima actualizacion:** 2025-12-30

---

Eres un orquestador que ejecuta planes de implementacion de features backend. Lee un archivo de plan y coordina la implementacion por capas ejecutando los comandos especializados.

## Entrada

**Plan a implementar:** $ARGUMENTS

Si `$ARGUMENTS` esta vacio, pregunta al usuario que plan desea implementar (ej: `gestion-proveedores-implementation-plan.md`).

## Configuracion

**Ubicacion de planes:** `.claude/planning/`

**Repositorio de Guias:**

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Flujo de Orquestacion

### Paso 1: Cargar y Validar Plan

1. Lee el archivo `.claude/planning/{$ARGUMENTS}` (o `.claude/planning/{$ARGUMENTS}-implementation-plan.md`)
2. Extrae el resumen del feature
3. Muestra al usuario:

```markdown
# Implement Feature - Orquestador

**Plan:** {nombre del plan}
**Feature:** {nombre del feature}
**Entidad Principal:** {entidad}

## Fases a Ejecutar

| Fase | Capa | Comando | Status |
|------|------|---------|--------|
| 1 | Domain | /implement-backend-domain | Pendiente |
| 2 | Infrastructure | /implement-backend-infrastructure | Pendiente |
| 3 | Application + WebAPI | /implement-backend-webapi | Pendiente |

Escribe "continuar" para iniciar, o "cancelar" para abortar.
```

### Paso 2: Ejecutar Fases Secuencialmente

**IMPORTANTE:** Ejecuta cada fase usando el SlashCommand tool correspondiente.

#### Fase 1: Domain Layer
```
/implement-backend-domain {contexto extraido de "Fase 1: Domain Layer" del plan}
```

Espera a que complete. Si hay errores, reporta y pregunta si continuar.

#### Fase 2: Infrastructure Layer
```
/implement-backend-infrastructure {contexto extraido de "Fase 2: Infrastructure Layer" del plan}
```

Espera a que complete. Si hay errores, reporta y pregunta si continuar.

#### Fase 3: Application + WebAPI Layer
```
/implement-backend-webapi {contexto extraido de "Fase 3 y 4" del plan}
```

### Paso 3: Reporte Final

Al completar todas las fases:

```markdown
# Feature Implementation Complete

**Plan:** {nombre}
**Feature:** {feature}

## Resumen por Capa

### Domain Layer
- Entity: {Entity}.cs
- Validator: {Entity}Validator.cs
- Repository Interface: I{Entity}Repository.cs
- Tests: {n} pasando

### Infrastructure Layer
- Mapper: {Entity}Mapper.cs
- Repository: NH{Entity}Repository.cs
- Scenarios: {n} XML
- Tests: {n} pasando

### Application + WebAPI Layer
- Use Cases: {n}
- Endpoints: {n}
- DTOs: {n}
- Tests: {n} pasando

## Archivos Creados
[Lista completa]

## Archivos Modificados
- IUnitOfWork.cs
- NHUnitOfWork.cs

**Status:** SUCCESS
```

---

## Manejo de Errores

Si alguna fase falla:

```markdown
# Feature Implementation PAUSED

**Fase que fallo:** {Domain | Infrastructure | WebAPI}
**Error:** {descripcion}

## Estado de las Fases

| Fase | Status |
|------|--------|
| Domain | {completado/fallido/pendiente} |
| Infrastructure | {completado/fallido/pendiente} |
| WebAPI | {completado/fallido/pendiente} |

## Opciones
1. Escribe "reintentar" para volver a intentar la fase fallida
2. Escribe "continuar" para saltar y seguir con la siguiente fase
3. Escribe "cancelar" para abortar
```

---

## Restricciones

### NO debes:
- Implementar codigo directamente - delega a los comandos especializados
- Saltarte fases sin confirmacion del usuario
- Continuar automaticamente si hay errores

### DEBES:
- Cargar y validar el plan antes de empezar
- Mostrar progreso claro al usuario
- Esperar confirmacion entre fases si hay problemas
- Generar reporte final consolidado
