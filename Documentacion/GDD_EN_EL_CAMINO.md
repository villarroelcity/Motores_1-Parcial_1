# En el Camino

## Modalidad

Proyecto individual de Ezequiel Godoy para el Segundo Parcial de Motores de Desarrollo 1 – Unity.

## Propuesta

“En el Camino” es una aventura de exploración en tercera persona. Un viajero llega casi sin combustible a una estación rural. El surtidor no funciona porque el lugar está sin energía, así que el jugador debe explorar la estación, encender el generador, cargar combustible y volver a la ruta.

## Alcance del vertical slice

La experiencia se concentra en una parada rural completa: movimiento a pie, cámara Cinemachine en tercera persona, una estación construida con primitivas, un generador interactuable, un surtidor, una interacción mediante Raycast, cajas físicas, un prefab instanciado, iluminación dinámica, UI uGUI, audio procedural, partículas y un desafío final de escape con IA.

El jugador debe recuperar la energía, entrar al taller, abrir el portón, tomar y llenar el bidón, cargar el auto y conducir hasta la salida. Al iniciar la conducción se activa una amenaza que persigue el vehículo durante una ventana de tiempo.

## Bucle jugable

Explorar la estación → encender el generador → entrar al taller → abrir el portón → tomar el bidón → llenar el bidón → cargar el auto → escapar a la ruta.

## Sistemas implementados

- Movimiento con `CharacterController` e Input System.
- Cámara de seguimiento con Cinemachine 3 y `CinemachineBrain`.
- Interacción mediante Raycast y objetivos secuenciales.
- Física de cajas y vehículo.
- UI uGUI con objetivos, mensajes, controles, temporizador y estados de victoria/derrota.
- Iluminación dinámica de día a noche y luces de advertencia durante el escape.
- Audio procedural: ambiente continuo y feedback sonoro para eventos importantes.
- Partículas de feedback generadas en runtime.
- IA de persecución basada en estados para el desafío final.

## Desafío final

Cuando el vehículo arranca se genera una amenaza detrás del auto. La amenaza persigue al vehículo y provoca Game Over si alcanza la distancia de captura. El jugador debe llegar al trigger `SalidaAlCamino`; la secuencia combina cámara, UI, audio, partículas, luces y gameplay.

## Estado actual

La escena inicia correctamente en Play Mode y fue verificada sin errores de compilación. La rama de trabajo es `parcial-2`. Falta realizar una prueba completa de build ejecutable y reemplazar los assets provisionales por arte final y audio definitivo.
