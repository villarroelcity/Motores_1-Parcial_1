# En el Camino

Prototipo individual para Motores de Desarrollo 1 – Unity. - Primera etapa.

El jugador llega casi sin combustible a una estación de servicio rural abandonada. Para continuar con su viaje debera explorar la estacion de servicioy tratar de encender el generador para suministrar energia para poder hacer tratar de encender el surtidor de combustible. Una vez conseguido ello, deberar encontrar un bidon vacio para poder cargarlo de combustible ya que el auto quedo detenido.

## Controles

- `WASD` o flechas: mover al personaje.
- Mouse: orientar la cámara en tercera persona.
- `E`: interactuar con el generador y el surtidor.

## Escena

La escena principal es `Assets/Scenes/EnElCamino.unity`. El prototipo usa un controlador con `CharacterController`, cámara de seguimiento, Raycast, interacción, física, trigger de salida e instanciación de un prefab de combustible.

El edificio de la estación es un espacio cerrado con paredes y techo. La conducción completa y otras paradas del viaje quedan fuera del alcance de este primer prototipo.
