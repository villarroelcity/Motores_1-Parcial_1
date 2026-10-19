# En el Camino — Segundo Parcial

Vertical slice individual para Motores de Desarrollo 1 – Unity.

El jugador llega casi sin combustible a una estación de servicio rural. Para volver al camino debe explorar el edificio, encender el generador, usar el surtidor y salir por la zona marcada. Al arrancar el auto comienza un desafío final de escape con amenaza, temporizador, audio, luces y partículas.

## Controles

- `WASD` o flechas: mover al personaje.
- Mouse: orientar la cámara en tercera persona.
- `E`: interactuar con el generador y el surtidor.

Durante el desafío final, el objetivo es llegar a `SalidaAlCamino` antes de que la amenaza alcance el vehículo.

## Escena

La escena principal es `Assets/Scenes/EnElCamino.unity`. El proyecto usa `CharacterController`, cámara Cinemachine 3, Raycast, interacción, física, trigger de salida, UI uGUI, iluminación dinámica, audio procedural, partículas, una IA de persecución y un prefab de combustible.

El edificio de la estación es un espacio cerrado con paredes y techo, conectado con una ruta de salida. El objetivo de esta entrega es consolidar una experiencia vertical slice en una única parada jugable.
