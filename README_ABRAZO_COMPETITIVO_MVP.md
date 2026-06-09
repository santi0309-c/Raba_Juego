# Abrazo Competitivo — MVP v2 (viernes)

Scripts corregidos y expandidos. Leé esto antes de abrir Unity.

---

## Scripts incluidos

| Script | Rol |
|---|---|
| AC_PlayerController.cs | Movimiento, dash, aguantar, caída, respawn |
| AC_HugDetector.cs | Volumen de abrazo, Raycast línea de visión, Gizmos |
| AC_GameManager.cs | Rondas, leyes, puntaje, viento, arena |
| AC_EnvironmentEvents.cs | Viento lateral / arena que se achica |
| AC_PracticeBotNavMesh.cs | Bot opcional con NavMeshAgent |
| AC_ArenaCamera.cs | **NUEVO** — Cámara cenital con zoom dinámico |
| AC_DashCooldownUI.cs | **NUEVO** — Feedback visual del cooldown del dash |

---

## Bugs corregidos en v2

1. **AC_HugDetector** — doble verificación por referencia Y por `playerId` para no detectarse a uno mismo
2. **AC_GameManager** — `resolvingPair` ahora se resetea al inicio de cada ronda (antes podía bloquear abrazos en la ronda siguiente)
3. **AC_GameManager** — el viento se limpia al terminar cada ronda (antes podía quedar activo)

---

## Cómo armar la escena

### Arena
- Crear un Cilindro grande (escala X=15, Y=0.3, Z=15)
- Llamarlo `Arena_Cilindro`
- Elevarlo un poco (Y=0.15)
- Layer: `Default`

### Jugadores
Crear dos Cápsulas: `Player1` (azul) y `Player2` (rojo).
A cada una agregarle:
- `CharacterController` (radio 0.4, height 2)
- `AC_PlayerController` (configurar playerId = 1 o 2)
- `AC_HugDetector` (asignar playerMask = Player)
- Layer: `Player` (crear esta layer)

Dentro de cada jugador crear un cubo hijo `HugVolumeVisual`:
- Posición local: (0, 1, 0.85)
- Escala: (1.3, 1.3, 1.1)
- Asignarlo en `volumeVisual` del AC_HugDetector
- Puede ser semitransparente o de color chillón

### Spawns
Dos Empty Objects: `Spawn1` y `Spawn2` (separados ~4 unidades del centro)

### GameManager
Empty Object `GameManager` con:
- `AC_GameManager` — asignar player1, player2, spawn1, spawn2, arenaCenter
- `AC_EnvironmentEvents` — asignar `arenaVisual` = Arena_Cilindro

### Cámara (NUEVO)
Seleccionar `Main Camera` y agregar `AC_ArenaCamera`.
- No requiere referencias si el GameManager ya está en escena (las busca solo)
- `dynamicZoom = true` → hace zoom out cuando los jugadores se alejan

### UI
Canvas con:
- `statusText` — texto de estado/ley/countdown (centro)
- `scoreText` — puntaje (arriba)
- `dashTextP1` — cooldown P1 (abajo izquierda)
- `dashTextP2` — cooldown P2 (abajo derecha)

Crear un Empty `DashUI` con `AC_DashCooldownUI`, asignar los textos y los jugadores.

---

## Controles

| Acción | P1 | P2 |
|---|---|---|
| Moverse | WASD | Flechas |
| Abrazar | Space | Enter |
| Dash | Left Shift | Right Shift |
| Aguantar | Left Ctrl | Right Ctrl |

---

## Leyes de ronda (aleatorias)

- **Abrazo por la espalda** — solo cuenta si venís desde atrás
- **Abrazo mutuo** — si los dos abrazan al mismo tiempo, +2 para ambos
- **Abrazo cargado** — cuanto más tiempo mantenés cerca al rival, más puntos
- **Toque maldito** — el que toca pierde el punto (se lo lleva el otro)
- **Solo uno vale** — el primer abrazo de la ronda vale 3 puntos

---

## Qué decirle al profe

"Prototipo funcional sin estética. Prioricé mecánicas: movimiento con CharacterController, abrazo por volumen OverlapBox, Raycast para línea de visión (referencia al PDF de Raycast), dash con cooldown, aguantar, leyes de ronda, evento de entorno, caída y respawn. El bot usa NavMeshAgent con persecución y patrulla (referencia al PDF de NavMesh). Animaciones y modelos van en la entrega final."
