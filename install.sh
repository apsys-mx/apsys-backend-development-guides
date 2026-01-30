#!/bin/bash
# install.sh
# Instalador remoto de APSYS Backend Prompts (para repos privados)
# Requiere: gh auth login
# Version: 1.0.0

set -e

# Configuracion
VERSION="1.0.0"
REPO="apsys-mx/apsys-backend-development-guides"
INSTALL_DIR="$HOME/.apsys-backend-prompts"

# Archivos a instalar
PROMPTS_TO_INSTALL=(
    "init-backend.md"
    "add-event-store.md"
    "review-backend-code.md"
)

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m'

# Banner
show_banner() {
    echo ""
    echo -e "${CYAN}================================================${NC}"
    echo -e "${CYAN}   APSYS Backend Prompts Installer v${VERSION}${NC}"
    echo -e "${CYAN}================================================${NC}"
    echo ""
}

# Verificar GitHub CLI
check_gh() {
    if ! command -v gh &> /dev/null; then
        echo -e "${RED}Error: GitHub CLI (gh) no esta instalado${NC}"
        echo "Instala gh desde: https://cli.github.com"
        exit 1
    fi

    # Verificar autenticacion
    if ! gh auth status &> /dev/null; then
        echo -e "${RED}Error: No estas autenticado en GitHub CLI${NC}"
        echo -e "${YELLOW}Ejecuta: gh auth login${NC}"
        exit 1
    fi
}

# Clonar o actualizar repositorio
sync_repository() {
    if [ -d "$INSTALL_DIR" ]; then
        echo -e "${CYAN}Actualizando repositorio...${NC}"
        cd "$INSTALL_DIR"
        git pull origin master --quiet 2>/dev/null || echo -e "${YELLOW}No se pudo actualizar, usando version local${NC}"
        cd - > /dev/null
        echo -e "${GREEN}Repositorio actualizado${NC}"
    else
        echo -e "${CYAN}Descargando repositorio...${NC}"
        gh repo clone "$REPO" "$INSTALL_DIR" -- --quiet 2>/dev/null
        echo -e "${GREEN}Repositorio descargado en $INSTALL_DIR${NC}"
    fi
}

# Obtener ruta destino
get_destination() {
    local ai=$1
    case $ai in
        claude)
            echo "$HOME/.claude/commands"
            ;;
        chatgpt)
            echo "$HOME/.chatgpt/prompts"
            ;;
    esac
}

# Obtener nombre de IA
get_ai_name() {
    local ai=$1
    case $ai in
        claude)
            echo "Claude Code"
            ;;
        chatgpt)
            echo "ChatGPT"
            ;;
    esac
}

# Seleccion de IA
select_ai() {
    echo -e "${YELLOW}Selecciona la IA donde instalar los prompts:${NC}"
    echo ""
    echo "  [1] Claude Code  - Slash commands (~/.claude/commands/)"
    echo "  [2] ChatGPT      - Prompts (~/.chatgpt/prompts/)"
    echo "  [0] Cancelar"
    echo ""
    read -p "Opcion: " choice

    case $choice in
        1) echo "claude" ;;
        2) echo "chatgpt" ;;
        0)
            echo -e "${CYAN}Instalacion cancelada.${NC}"
            exit 0
            ;;
        *)
            echo -e "${RED}Opcion no valida${NC}"
            select_ai
            ;;
    esac
}

# Instalar prompts
install_prompts() {
    local selected_ai=$1
    local dest_path=$(get_destination $selected_ai)
    local ai_name=$(get_ai_name $selected_ai)

    echo ""
    echo -e "${CYAN}Instalando prompts para ${ai_name}...${NC}"
    echo -e "${GRAY}Destino: $dest_path${NC}"
    echo ""

    # Crear directorio destino
    mkdir -p "$dest_path"

    # Copiar archivos
    local source_path="$INSTALL_DIR/prompts/commands"
    local installed=0

    for file in "${PROMPTS_TO_INSTALL[@]}"; do
        local source_file="$source_path/$file"
        local dest_file="$dest_path/$file"

        if [ -f "$source_file" ]; then
            cp "$source_file" "$dest_file"
            echo -e "  ${GREEN}[OK]${NC} $file"
            ((installed++))
        else
            echo -e "  ${YELLOW}[SKIP]${NC} $file - No encontrado"
        fi
    done

    echo ""
    echo -e "${CYAN}========================================${NC}"
    echo -e "${GREEN}Instalacion completada! ($installed archivos)${NC}"
    echo -e "${CYAN}========================================${NC}"

    # Instrucciones de uso
    if [ "$selected_ai" = "claude" ]; then
        echo ""
        echo -e "${YELLOW}Uso en Claude Code:${NC}"
        echo "  /init-backend"
        echo "  /add-event-store"
        echo "  /review-backend-code"
    fi

    echo ""
    echo -e "${GRAY}Para actualizar en el futuro, ejecuta el mismo comando.${NC}"
}

# Main
show_banner
check_gh
sync_repository

selected_ai=$(select_ai)
install_prompts "$selected_ai"
