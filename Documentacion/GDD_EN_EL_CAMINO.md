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

Durante la persecución el patrullero sale desde su posición estacionada, gira y acelera para incorporarse a la ruta sin cambiar de posición instantáneamente. Apunta por delante del jugador para interceptarlo, esquiva obstáculos y ajusta su velocidad para seguirlo. Activa dos luces azules y una sirena espacial y actualiza la ventaja en el HUD. Un contacto aislado no termina la partida: hay captura cuando mantiene contacto durante 1,5 segundos con el jugador a menos de 12 km/h; también hay derrota si se agota el tiempo máximo de persecución. El jugador gana al sacar 200 metros de ventaja. El espejo usa una cámara orientada hacia atrás, fuera de la carrocería, con imagen reflejada horizontalmente y proporción 3:1.

El tráfico se activa al volver a arrancar el auto después de cargar combustible. Circulan al menos ocho carrocerías VPP por sentido a 60 km/h. Son cuerpos con masa y gravedad, apoyados sobre el asfalto según el tamaño de sus mallas. Al chocar dejan de acelerar y responden al impacto mediante la física. Los vehículos que siguen circulando se reciclan lejos del jugador para mantener tránsito de frente y en el mismo sentido.

La IA y sus condiciones deben validarse en una prueba completa: superar el límite de velocidad, activar el trigger, comprobar sirena/luces, escapar y comprobar también la derrota por alcance o choque.

## Interfaz y controles

La UI usa uGUI y se adapta al tamaño de la pantalla. Incluye objetivos, mensajes, reloj de tarde a noche, combustible restante del generador, velocidad del vehículo durante la conducción, indicador `VENTAJA` y espejo retrovisor durante la persecución. El espejo y el indicador tienen previsualizaciones editables dentro del Canvas (`RearViewMirrorPanel` y `PursuitDistancePanel`) para poder acomodarlos visualmente en la vista Scene; al ejecutar se leen sus posiciones y se reemplazan por el HUD funcional. El espejo muestra solo la imagen, sin marco ni texto.

El menú solicitado contiene únicamente:

- Pausa y continuación con `ESC`.
- Reinicio de partida con el botón `REINICIAR` o la tecla `R`.
- Pantalla de `CONTROLES`.

Los botones se crean en tiempo de ejecución con `EventSystem` e `InputSystemUIInputModule`. Al producirse cualquier `GAME OVER` se detienen los audios de juego y queda disponible `REINICIAR`.

## Criterios de entrega

Quedan por producir y validar: build jugable, video de hasta cinco minutos, prueba final completa y documentación de testing actualizada. La lista operativa está en [Informe_Testing_EN_EL_CAMINO.md](Informe_Testing_EN_EL_CAMINO.md).

