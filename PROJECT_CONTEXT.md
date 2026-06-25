# Juego Raba — Documentación de Contexto y Arquitectura (v3)

## 1. Visión General

**Género / Propósito:** Juego de arena competitivo local para 2 jugadores (pantalla dividida). Los jugadores deben abrazarse mutuamente para sumar puntos mientras evitan caerse del mapa. Incluye leyes especiales por ronda (Abrazo por la Espalda, Abrazo Mutuo, Abrazo Cargado, Toque Maldito, Solo Uno Vale) y eventos de entorno aleatorios (viento, niebla, arena menguante, piso inclinado).

**Estado actual:** MVP refactorizado — loop de partida completo, persistencia de highscores (top 10), feedback visual (shake de cámara + flash de color), hooks de audio (null-safe), ScriptableObjects para config de jugador y leyes, UI con soporte dual prefab/procedural. **Overhaul visual aplicado:** tema Dojo Japonés con pabellón de madera, faroles, exterior con montañas y árboles, iluminación cálida, skybox procedural.

**Engine:** Unity 6000.0.76f1 (Unity 6 LTS)

---

## 2. Arquitectura y Patrones de Diseño

**Patrón principal:** **Coordinador con subsistemas.** `AC_GameManager` es el coordinador central (~550 líneas, bajó de 802). La lógica específica está delegada a subsistemas en el mismo GameObject.

**Flujo de control:** El GameManager orquesta el flujo de partida. Cada subsistema maneja su dominio:
- `AC_HugResolver` → lógica de abrazos y leyes
- `AC_MatchHUD` → textos, marcador, menú
- `AC_ArenaManager` → radio, viento, validación
- `AC_Control` → puntaje, vidas, tiempo
- `AC_HugFeedback` → shake de cámara, flash, partículas
- `AC_ScorePersistence` → guardado/carga de highscores
- `AC_AudioManager` → hooks de audio null-safe

**Diagrama de dependencias (actualizado):**

```
AC_GameManager (Singleton, coordinador ~550 líneas)
 ├── AC_Control (puntaje/vidas/tiempo)
 ├── AC_HugResolver (resolución de abrazos y leyes)
 │    └── llama a gm.SumarPuntaje(), gm.MostrarMensaje(), gm.FeedbackAbrazo()
 ├── AC_MatchHUD (textos de estado, marcador, menú, carteles)
 │    └── usa AC_MainMenuUI (procedural o prefab)
 ├── AC_ArenaManager (radio, viento, validación de referencias)
 ├── AC_HugFeedback (shake cámara, flash color, partículas)
 ├── AC_ScorePersistence (PlayerPrefs + JSON, top 10)
 ├── AC_AudioManager (hooks null-safe, sin clips aún)
 ├── AC_SceneDecorator (carteles 3D, TextMesh)
 ├── AC_EnvironmentEvents (viento, niebla, arena menguante, piso inclinado)
 ├── AC_PlayerController player1 ▶ CharacterController, AC_HugDetector, AC_Spawner, AC_PlayerConfig (opcional)
 └── AC_PlayerController player2 ▶ (misma estructura)
```

**Sistemas independientes:**
- `AC_ArenaCamera` — lee `AC_GameManager.Instance` para referencias.
- `AC_PracticeBotNavMesh` — IA simple con NavMeshAgent.
- `AC_DashCooldownUI` — muestra cooldown de dash.
- `AC_UILayoutManager` — posiciona textos (modo procedural o prefab).
- `AC_PlayerFaceMarkers` — ojos decorativos (`ExecuteAlways`).
- `ThirdPersonCameraFollow` — cámara alternativa (sin uso en flujo actual).
- `AC_ScreenshotTool` — F12 para captura de pantalla.

---

## 3. Mapa de Sistemas

| Sistema | Archivo | Responsabilidad |
|---|---|---|
| **Game Manager** | `Core/AC_GameManager.cs` | Loop de partida, rondas, orquestación |
| **Control** | `Core/AC_Control.cs` | Puntaje, vidas, tiempo de ronda |
| **Hug Resolver** | `Core/AC_HugResolver.cs` | Resolución de abrazos, leyes, ventana mutua |
| **Match HUD** | `UI/AC_MatchHUD.cs` | Textos, marcador, carteles arena, menú |
| **Arena Manager** | `Environment/AC_ArenaManager.cs` | Radio, viento, validación referencias |
| **Hug Feedback** | `Core/AC_HugFeedback.cs` | Shake cámara, flash color, partículas |
| **Score Persistence** | `Core/AC_ScorePersistence.cs` | Top 10 en PlayerPrefs + JSON |
| **Audio Manager** | `Audio/AC_AudioManager.cs` | Hooks null-safe para música y efectos |
| **Player Config** | `Player/AC_PlayerConfig.cs` | ScriptableObject de configuración de jugador |
| **Law Definition** | `Core/AC_LawDefinition.cs` | ScriptableObject de definición de ley |
| **Screenshot Tool** | `Core/AC_ScreenshotTool.cs` | F12 → PNG en Screenshots/ |
| **Jugador** | `Player/AC_PlayerController.cs` | Movimiento, dash, salto, aguante |
| **Hug Detector** | `Player/AC_HugDetector.cs` | OverlapBox + Raycast para detectar abrazo |
| **Spawner** | `Player/AC_Spawner.cs` | Respawn por caída |
| **Face Markers** | `Player/AC_PlayerFaceMarkers.cs` | Ojos procedurales |
| **Main Menu UI** | `UI/AC_MainMenuUI.cs` | Menú principal (prefab o procedural) |
| **Dash Cooldown UI** | `UI/AC_DashCooldownUI.cs` | Indicador de cooldown de dash |
| **UI Layout Manager** | `UI/AC_UILayoutManager.cs` | Posicionamiento de textos (flag usarPrefab) |
| **Environment Events** | `Environment/AC_EnvironmentEvents.cs` | Viento, niebla, arena menguante, piso inclinado |
| **Scene Decorator** | `Decoration/AC_SceneDecorator.cs` | Carteles 3D con controles |
| **Arena Camera** | `Camera/AC_ArenaCamera.cs` | Cámara con zoom dinámico |
| **Practice Bot** | `Bot/AC_PracticeBotNavMesh.cs` | IA simple con NavMeshAgent |

---

## 4. Estructura de Carpetas

```
Assets/
├── Scripts/
│   ├── Core/           AC_GameManager, AC_Control, AC_HugResolver,
│   │                   AC_ScorePersistence, AC_HugFeedback,
│   │                   AC_LawDefinition, AC_ScreenshotTool
│   ├── Player/         AC_PlayerController, AC_HugDetector,
│   │                   AC_Spawner, AC_PlayerFaceMarkers, AC_PlayerConfig
│   ├── UI/             AC_MainMenuUI, AC_MatchHUD, AC_DashCooldownUI,
│   │                   AC_UILayoutManager
│   ├── Environment/    AC_EnvironmentEvents, AC_ArenaManager
│   ├── Camera/         AC_ArenaCamera, ThirdPersonCameraFollow
│   ├── Bot/            AC_PracticeBotNavMesh
│   ├── Decoration/     AC_SceneDecorator
│   └── Audio/          AC_AudioManager
├── Materials/          Dojo_Tatami, Dojo_DarkWood, Dojo_LightWood,
│                       Dojo_Roof, Dojo_Lantern, Dojo_Shoji,
│                       Dojo_Gravel, Dojo_Mountain, Dojo_TreeBark,
│                       Dojo_TreeLeaf, ArenaSkybox, ArenaFloor, ArenaEdge
├── Screenshots/        Capturas de pantalla (F12 o MCP)
├── ArenaScene.unity
└── Resources/
```

---

## 5. Tema Visual — Dojo Japonés (v3)

La escena tiene un overhaul visual completo con temática de dojo japonés tradicional (道場) dentro de un pabellón de madera al aire libre. Todos los elementos decorativos son primitivas de Unity (cubos, cilindros, esferas) con materiales mate sin colliders. La física y gameplay no se ven afectados.

### 5.1 Jerarquía de la escena

```
ArenaScene.unity
├── DojoStructure (raíz, 0,0,0) — pabellón abierto
│   ├── ArenaCylinder → piso tatami (material Dojo_Tatami)
│   ├── WoodSkirt → zócalo perimetral madera oscura (15.2×0.15×15.2)
│   ├── Pillar_NE/NW/SE/SW → columnas (r=0.35, h=5) en (±7, 2.5, ±7)
│   ├── Beam_N/S/E/W → vigas de unión entre pilares en y=4.9
│   ├── Roof → techo base (16.5×0.12×16.5) en y=5
│   ├── RoofOverhang → voladizo (17×0.08×17) en y=5.06
│   ├── Lantern_NE/NW/SE/SW → faroles (r=0.25, h=0.7) con PointLight naranja
│
├── ExteriorGround → suelo grava 60×60 en y=-2.5
├── Mountain_N/S/E/W → montañas de fondo (cubos verde oscuro a 35u)
├── Tree_1..4 → árboles (tronco cilindro + copa esfera) en (±18-20, -0.5, ±16-22)
│
├── Directional Light → rot(50, -30, 0), intensity=1.0, color cálido #FFD9A6, Soft Shadows
├── ArenaPointLight → Point central (0,5,0), intensity=0.4, naranja suave, range=20
└── Canvas → UI con textos blancos bold + outline negro
```

### 5.2 Materiales creados (Assets/Materials/)

| Material | Shader | Color | Smoothness | Uso |
|----------|--------|-------|------------|-----|
| `Dojo_Tatami.mat` | Standard | #C5B358 pajizo | 0.02 | Piso arena |
| `Dojo_DarkWood.mat` | Standard | #3E2723 marrón oscuro | 0.05 | Pilares, vigas, zócalo |
| `Dojo_LightWood.mat` | Standard | #C4956A beige madera | 0.08 | (reservado) |
| `Dojo_Roof.mat` | Standard | #2C3E50 gris azulado | 0.10 | Techos |
| `Dojo_Lantern.mat` | Standard | #CC4422 rojo anaranjado | 0.01 | Faroles |
| `Dojo_Shoji.mat` | Standard | #F5F0E8 crema | 0.05 | (reservado) |
| `Dojo_Gravel.mat` | Standard | #736B61 gris tierra | 0.00 | Suelo exterior |
| `Dojo_Mountain.mat` | Standard | #384733 verde oscuro | 0.00 | Montañas |
| `Dojo_TreeBark.mat` | Standard | #472E1A marrón | 0.02 | Troncos |
| `Dojo_TreeLeaf.mat` | Standard | #26591F verde hoja | 0.03 | Copas |
| `ArenaSkybox.mat` | Skybox/Procedural | Celeste→beige | — | Cielo |
| `ArenaFloor.mat` | Standard | #D4C5B9 beige | 0.10 | (legado, reemplazado por Tatami) |
| `ArenaEdge.mat` | Standard | #5C4A3A marrón | 0.05 | (legado) |

### 5.3 Iluminación

| Luz | Tipo | Intensidad | Color | Shadows | Notas |
|-----|------|-----------|-------|---------|-------|
| Directional Light | Directional | 1.0 | #FFD9A6 dorado | Soft | Rot (50, -30, 0) |
| ArenaPointLight | Point | 0.4 | #FFBF73 naranja | None | Centro arena, range 20 |
| Lantern_NE/NW/SE/SW | Point ×4 | 0.6 | #FFB359 naranja | None | Range 8 cada uno |

### 5.4 Entorno

- **Skybox:** `ArenaSkybox.mat` con shader `Skybox/Procedural`, tint atardecer (SkyTint #BF9973, Ground #59664D, Exposure 1.1, Atmosphere 1.2).
- **Fog:** Linear, start 15, end 40, color celeste claro (#D1E3F2).
- **Render Pipeline:** Built-in (no URP/HDRP). Post Processing Stack no instalado.
- **Espacio de color:** Linear.

### 5.5 Regla de colliders

**Todos los objetos decorativos (DojoStructure, exterior, faroles) tienen sus colliders removidos.** Solo mantienen colliders funcionales:
- `ArenaCylinder` → CapsuleCollider + MeshCollider (piso físico)
- `Player1` / `Player 2` → CapsuleCollider + CharacterController

---

## 6. Gestión de Datos y Persistencia

**Almacenamiento:** `AC_ScorePersistence` guarda el top 10 de puntajes en `PlayerPrefs` serializado como JSON. Los puntajes se persisten al terminar cada partida. Si ambos jugadores tienen puntaje ≥ 0, se guardan ambos.

**Estructuras clave:**
- `AC_HighScoreEntry` — `nombreJugador`, `puntaje`, `fecha`
- `AC_Control` — puntaje y vidas en memoria (se pierden al cerrar)

**ScriptableObjects:**
- `AC_PlayerConfig` — perfil de jugador (teclas, velocidades, dash). Se asigna opcionalmente en el Inspector. Si no se asigna, usa los defaults hardcodeados.
- `AC_LawDefinition` — definición de ley de abrazo (nombre, puntaje, reglas especiales). Creado como asset para documentar leyes; el resolver aún usa lógica procedural.

---

## 7. Entradas y Paquetes

**Input:** Legacy Input Manager. `Input.GetKey()`, `Input.GetKeyDown()`.

**Librerías en manifiesto:**
- `com.demigiant.dotween` — DOTween (OpenUPM). **No usado en código actualmente** — las transiciones usan corrutinas vanilla. Se puede adoptar DOTween más adelante para mejorar game feel.
- `com.unity.ai.navigation` — NavMesh (usado por `AC_PracticeBotNavMesh`)
- `com.coplaydev.unity-mcp` — herramienta de desarrollo

---

## 8. Flujo de Escenas

**Punto de entrada:** `Assets/ArenaScene.unity` (única escena).

**Secuencia de arranque:**
1. `Awake()` → Singleton + `EnsureCoreReferences()` (crea Control, HugResolver, ArenaManager, ScorePersistence, HugFeedback, AudioManager, ScreenshotTool)
2. `Start()` → Valida arena, configura respawns, `EnsurePresentationHelpers()` (crea HUD, MainMenuUI, SceneDecorator, UILayoutManager), muestra menú con música
3. Usuario presiona "Jugar" → `BeginMatchFromMenu()` → música de partida → `RunMatchLoop()`
4. Al terminar → se guarda puntaje → música de menú → pantalla de resultados

---

## 9. Convenciones del Código

| Aspecto | Convención |
|---|---|
| **Prefijo** | `AC_` (Arena Competitiva) |
| **Namespaces** | No se usan |
| **Idioma** | Dominio en español (`puntajeJugador1`, `vidasJugador1`), técnica en inglés (`moveSpeed`, `dashCooldown`) |
| **Async** | Exclusivamente corrutinas (`IEnumerator` + `StartCoroutine`) |
| **Referencias** | Singleton (`AC_GameManager.Instance`) o referencias serializadas |
| **Input** | `Input.GetKey`/`GetKeyDown` con `KeyCode` públicos |
| **Física** | `CharacterController.Move()`, `Physics.OverlapBox`, `Physics.Raycast` |
| **UI** | `UnityEngine.UI.Text` + `Canvas`. Soporte dual: procedural (default) o prefab |
| **Persistencia** | `PlayerPrefs` + `JsonUtility` |

---

## 10. Code Smells Resueltos

1. ✅ **GameManager Dios:** Partido en 6 subsistemas. 802 → ~550 líneas.
2. ⚠️ **Acoplamiento al Singleton:** Mitigado. Los subsistemas acceden al GM por `Instance`, pero la lógica está encapsulada.
3. ⚠️ **Detección de caída:** Sigue siendo frame a frame. Aceptable para 2 jugadores.
4. ✅ **UI procedural:** `AC_MainMenuUI` soporta prefab (`menuPrefab`). `AC_UILayoutManager` tiene flag `usarPrefab`.
5. ✅ **Spanglish:** Unificado. Dominio = español, técnica = inglés.
6. ✅ **Scripts duplicados:** Eliminados los 10 scripts stale de la raíz.
7. ⚠️ **Sin tests:** Sigue sin tests automatizados.
8. ✅ **Sin persistencia:** `AC_ScorePersistence` con PlayerPrefs + JSON.
9. ✅ **Sin feedback:** `AC_HugFeedback` con shake de cámara y flash.
10. ✅ **Sin audio:** `AC_AudioManager` con hooks null-safe listos para recibir clips.
11. ✅ **Sin ambiente visual:** Tema Dojo Japonés aplicado — pabellón de madera, faroles, exterior con montañas y árboles. Sin afectar física ni gameplay.
