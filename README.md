# En el Camino

Prototipo individual para Motores de Desarrollo 1 – Unity.

El jugador llega casi sin combustible a una estación de servicio rural. Para volver al camino debe explorar el edificio, encender el generador, usar el surtidor y salir por la zona marcada.

## Controles

- `WASD` o flechas: mover al personaje.
- Mouse: orientar la cámara en tercera persona.
- `E`: interactuar con el generador y el surtidor.

## Escena

La escena principal es `Assets/Scenes/EnElCamino.unity`. El prototipo usa primitivas, un controlador con `CharacterController`, cámara de seguimiento, Raycast, interacción, física, trigger de salida e instanciación de un prefab de combustible.

El edificio de la estación es un espacio cerrado con paredes y techo. La conducción completa y otras paradas del viaje quedan fuera del alcance de este primer prototipo.
