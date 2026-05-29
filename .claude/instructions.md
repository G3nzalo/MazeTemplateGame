# Unity Audio Agent Scope

## RESTRICCIÓN PRINCIPAL
El agente SOLO debe analizar y modificar código dentro de:

Assets/Scripts/SounApp

Ignorar completamente:
- otros scripts fuera de esta carpeta
- template code del proyecto Unity
- Packages/
- ProjectSettings/

## CONTEXTO PERMITIDO
Solo se permite leer archivos dentro de:
Assets/Scripts/SounApp

Cualquier otro archivo es fuera de scope.

## OBJETIVO DEL PROYECTO
Este módulo contiene un sistema de audio educativo para detección de notas cuando el estudiante canta.

## ARQUITECTURA
- Audio input
- Pitch detection
- Note mapping
- Game logic

## REGLA CRÍTICA
Si el código está fuera de SounApp:
- NO analizarlo
- NO proponer cambios
- NO incluirlo en soluciones