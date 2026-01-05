# Prompts

Prompts y comandos para agentes IA de desarrollo backend.

---

## Estructura

```
prompts/
└── commands/           # Comandos ejecutables por usuario
    ├── init-backend.md
    ├── plan-backend-feature.md
    ├── implement-backend-feature.md
    ├── review-backend-code.md
    ├── plan-backend-testing.md
    └── implement-backend-testing.md
```

---

## Comandos

Comandos que el usuario puede invocar directamente.

| Comando | Descripcion |
|---------|-------------|
| `/init-backend` | Inicializa proyecto backend con Clean Architecture |
| `/plan-backend-feature` | Planifica implementacion de una feature |
| `/implement-backend-feature` | Implementa feature completa (domain → webapi) |
| `/review-backend-code` | Revisa codigo backend |
| `/plan-backend-testing` | Planifica estrategia de testing |
| `/implement-backend-testing` | Implementa tests segun plan |

---

## Como Funcionan

1. Usuario invoca `/comando`
2. Agente lee el archivo `.md` del comando
3. Agente sigue las instrucciones del comando
4. Usa guias de `architectures/` y templates de `templates/` o `stacks/`
