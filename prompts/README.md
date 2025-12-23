# Prompts

Prompts y comandos para agentes IA de desarrollo backend.

---

## Estructura

```
prompts/
├── commands/           # Comandos ejecutables por usuario
│   ├── init-backend.md
│   ├── plan-backend-feature.md
│   ├── implement-backend-feature.md
│   ├── implement-backend-domain.md
│   ├── implement-backend-infrastructure.md
│   └── implement-backend-webapi.md
│
└── agents/             # Prompts de sistema para agentes
    ├── backend-developer.md
    ├── backend-feature-planner.md
    ├── backend-feature-implementer.md
    ├── backend-task-decomposer.md
    ├── backend-domain-developer.md
    ├── backend-infrastructure-developer.md
    ├── backend-webapi-developer.md
    └── backend-peer-reviewer.md
```

---

## Comandos

Comandos que el usuario puede invocar directamente.

| Comando | Descripcion |
|---------|-------------|
| `/init-backend` | Inicializa proyecto backend con Clean Architecture |
| `/plan-backend-feature` | Planifica implementacion de una feature |
| `/implement-backend-feature` | Implementa feature completa (domain → webapi) |
| `/implement-backend-domain` | Implementa solo capa domain |
| `/implement-backend-infrastructure` | Implementa solo capa infrastructure |
| `/implement-backend-webapi` | Implementa solo capa webapi |

---

## Agentes

Prompts de sistema que definen el comportamiento de agentes especializados.

| Agente | Rol |
|--------|-----|
| `backend-developer` | Desarrollador backend general |
| `backend-feature-planner` | Planificador de features |
| `backend-feature-implementer` | Implementador de features completas |
| `backend-task-decomposer` | Descompone tareas en subtareas |
| `backend-domain-developer` | Especialista en capa domain |
| `backend-infrastructure-developer` | Especialista en capa infrastructure |
| `backend-webapi-developer` | Especialista en capa webapi |
| `backend-peer-reviewer` | Revisor de codigo |

---

## Como Funcionan

### Comandos

1. Usuario invoca `/comando`
2. Agente lee el archivo `.md` del comando
3. Agente sigue las instrucciones del comando
4. Usa guias de `architectures/` y templates de `templates/` o `stacks/`

### Agentes

1. Sistema carga el prompt del agente
2. Define personalidad, habilidades y restricciones
3. Agente opera segun su especializacion
