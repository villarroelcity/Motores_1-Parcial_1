# GDD — En el Camino

## Ficha

| Campo | Definición |
| --- | --- |
| Materia | Motores de Desarrollo 1 |
| Entrega | Segundo Parcial |
| Autor | Ezequiel Godoy |
| Género | Exploración y supervivencia ligera en tercera persona |
| Motor | Unity 6 |
| Escena principal | `Assets/Scenes/EnElCamino.unity` |

## High concept

En un camino rural al anochecer, el auto se queda sin combustible frente a una estación abandonada. Para escapar, el jugador debe atravesar el taller, devolver energía a la estación y abastecer el vehículo antes de que oscurezca.

## Bucle principal

Llegada automática → exploración a pie → acceso al taller por el techo → generador → tablero eléctrico → portón → bidón → surtidor → vehículo → salida.

La secuencia es guiada por objetivos del HUD. Cada interacción valida el paso anterior para que no se pueda completar el recorrido en un orden accidental.

## Objetivos y condiciones

| Paso | Acción | Resultado |
| --- | --- | --- |
| 1 | Subir al techo y usar el hueco | Acceso al taller |
| 2 | Encender el generador | Inicia el contador de combustible del generador |
| 3 | Conectar el generador al tablero | Energiza la estación |
| 4 | Accionar el interruptor del portón | Abre la salida del taller |
| 5 | Recoger el bidón | Permite usar el surtidor |
| 6 | Llenar el bidón | Combustible disponible para el auto |
| 7 | Cargar y arrancar el auto | Habilita la conducción |
| 8 | Llegar a la salida | Victoria |

Derrotas vigentes: agotar el tiempo general/noche, agotar el combustible del generador antes de resolver el taller y caer por el hueco del techo. El mensaje final se normaliza como `GAME OVER` y detiene la mezcla de audio antes de reproducir su feedback.

## Sistemas implementados

- Movimiento con `CharacterController`; caminar y correr con `Shift`.
- Cámara de tercera persona con Cinemachine.
- Interacción por Raycast e interfaz `IInteractable`.
- Flujo de misión secuencial en `GameFlow`.
- Auto con Vehicle Physics Pro; llegada, carga y control reunidos en `CarDriver`.
- Interacciones de generador, tablero, interruptor, portón, bidón, surtidor y salida.
- UI uGUI adaptada a pantalla para objetivos, mensajes, tiempo y combustible del generador.
- Ciclo tarde–noche con Enviro 3, iluminación y audio ambiental/espacial.
- Audio por mixer, efectos de interacción, motor VPP, música adaptativa y silenciamiento al finalizar.
- Físicas, colliders, triggers, cajas físicas y partículas de feedback.

## Dirección visual y sonora

La estación usa una estética rural low-poly con vegetación de Nature Starter Kit 2, entorno de Enviro 3, modelos externos de generador/bidón/tablero y surtidor 3D. El patrullero de la persecución se distingue del auto del jugador mediante materiales propios. El arte se utiliza como apoyo del gameplay: el techo, el hueco, el tablero y el portón hacen visible la ruta de misión.

El ambiente exterior, generador, surtidor, portón, acciones del auto, música de exploración y mensajes de victoria/derrota se organizan mediante `EnElCamino_Mixer`.

## IA — patrullero

La amenaza elegida es un patrullero (`PatrulleroIA`) que permanece oculto hasta que el jugador supera los 60 km/h y atraviesa el trigger de persecución. No usa el controlador VPP del jugador: se mueve con un `Rigidbody` sencillo para evitar la limitación de la edición Community de Vehicle Physics Pro, que permite un solo vehículo VPP activo en la escena.

Durante la persecución el patrullero sigue al auto del jugador, activa dos luces azules y una sirena espacial, actualiza la distancia de escape en el HUD y produce `GAME OVER` si alcanza al jugador, choca contra él o se agota el tiempo máximo de persecución. El jugador gana al mantener una distancia de 100 metros. El HUD incluye un indicador de distancia y un espejo retrovisor renderizado desde una cámara auxiliar.

La IA y sus condiciones deben validarse en una prueba completa: superar el límite de velocidad, activar el trigger, comprobar sirena/luces, escapar y comprobar también la derrota por alcance o choque.

## Interfaz y controles

La UI usa uGUI y se adapta al tamaño de la pantalla. Incluye objetivos, mensajes, reloj de tarde a noche, combustible restante del generador, velocidad del vehículo durante la conducción, distancia de escape y espejo retrovisor durante la persecución.

El menú solicitado contiene únicamente:

- Pausa y continuación con `ESC`.
- Reinicio de partida con el botón `REINICIAR` o la tecla `R`.
- Pantalla de `CONTROLES`.

Los botones se crean en tiempo de ejecución con `EventSystem` e `InputSystemUIInputModule`. Al producirse cualquier `GAME OVER` se detienen los audios de juego y queda disponible `REINICIAR`.

## Criterios de entrega

Quedan por producir y validar: build jugable, video de hasta cinco minutos, prueba final completa y documentación de testing actualizada. La lista operativa está en [Informe_Testing_EN_EL_CAMINO.md](Informe_Testing_EN_EL_CAMINO.md).

