# Mapa simple de scripts

Este archivo resume los scripts propios de `Assets/Scripts` después de la limpieza. Los scripts de Vehicle Physics Pro, Enviro 3 y Nature Starter Kit 2 quedan fuera de esta lista porque pertenecen a paquetes externos.

Estado actual: 14 scripts propios.

## Flujo principal

| Script | Responsabilidad |
| --- | --- |
| `GameFlow` | Estado de misión, objetivos, victoria/derrota, apagado general del audio y reloj tarde-noche iniciado al agotarse el auto. |

## Jugador y cámara

| Script | Responsabilidad |
| --- | --- |
| `PlayerController` | Movimiento con `CharacterController`, gravedad, correr con Shift, modelo low-poly, animación, pasos y caída por el hueco. |
| `CamaraJugador` | Seguimiento del objetivo y reducción de distancia cuando una pared obstruye la cámara. |
| `DetectorDeInteracciones` | Busca el objeto interactivo más cercano, ejecuta la tecla E y contiene el contrato pequeño `IInteractable`. |

## Secuencia del auto y misión

| Script | Responsabilidad |
| --- | --- |
| `CarDriver` | Hace llegar el auto, corta el combustible, permite cargarlo/subir y entrega el control a los controles nativos de Vehicle Physics Pro. No duplica la física del asset. |
| `CheckPoint` | Comprueba la llegada a la salida. |
| `Surtidor` | Recoger, llevar en la mano, llenar el bidón y operar el surtidor mediante su modo `pumpMode`. |

## Interacciones del escenario

| Script | Responsabilidad |
| --- | --- |
| `Escalera` | Subir al techo, interactuar con el hueco y ejecutar la caída controlada al taller. |
| `Generador` | Encendido, loop del motor e indicador del generador. |
| `TableroElectrico` | Conecta el generador al tablero, controla su estado y dibuja cable, enchufe e indicador. |
| `Interruptores` | Interruptor del portón. |
| `PortonTaller` | Animación lenta de apertura del portón. |

## Audio y presentación

| Script | Responsabilidad |
| --- | --- |
| `AudioController` | Ambiente, efectos de interacción y transición entre música de exploración y tensión. |
| `GameUI` | Objetivos, mensajes, reloj y combustible del generador con Canvas Scaler. |

## Limpieza aplicada

- Se eliminaron `PhysicsCrate`, `VoiceNarration` y `CinemachineFollowSetup`: no tenían uso real en la escena o eran un componente de compatibilidad desactivado.
- Se eliminaron `VppDirectControlSetup` y `VerticalSliceDirector`: el primero duplicaba la configuración que ya realiza `CarDriver`; el segundo repetía mensajes y feedback que ya resuelve `GameFlow`.
- Se eliminaron eventos sin suscriptores y métodos de audio vacíos.
- Se quitaron campos muertos del controlador del auto sin modificar la ruta de control de Vehicle Physics Pro.
- Se unificaron llegada/interacción del auto en `CarDriver`, bidón/surtidor en `Surtidor`, reloj en `GameFlow`, animación en `PlayerController`, cable en `TableroElectrico`, audio/música en `AudioController` y el hueco en `Escalera`.
- Se conserva `Assets/Editor/DaylightStandbySetup.cs` porque solo fija una hora de día en el editor y evita que la escena cambie de horario cada vez que se abre o se detiene Play.

Los scripts restantes representan sistemas visibles del juego: flujo y tiempo, jugador y cámara, interacción, auto, UI, audio, generador/energía, portón y salida. Los componentes de Vehicle Physics Pro, Enviro 3 y Nature Starter Kit 2 siguen siendo externos y no se cuentan dentro de los scripts propios.

`CarDriver` sigue siendo el script más largo porque adapta la secuencia inicial y la interacción al componente interno de Vehicle Physics Pro. Esa parte no conviene reemplazarla por un controlador casero: duplicaría la física del paquete y volvería a aparecer el problema de que el auto gira como un bloque.

## Criterio de la materia

La organización sigue una responsabilidad por script: entrada y movimiento separados de cámara, UI, audio, interacciones y flujo. Se usan `Update` para entrada y lógica, `FixedUpdate` para el bus de física del vehículo, `LateUpdate` para cámara y eventos para comunicar cambios entre sistemas. Esto mantiene el código simple y explicable para el nivel de Motores 1.
