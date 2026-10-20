# En el Camino — Parcial 2

Proyecto individual de **Motores de Desarrollo 1**. Es un vertical slice en tercera persona: el auto se queda sin combustible antes de llegar a una estación rural y el jugador debe recuperar la energía, conseguir combustible y volver a la ruta.

## Abrir el proyecto

1. Abrir la carpeta con **Unity 6000.3.5f2**.
2. Abrir `Assets/Scenes/EnElCamino.unity`.
3. Esperar a que Unity termine la importación antes de ejecutar Play.

Los paquetes de terceros de licencia comercial no se suben al repositorio. La lista de dependencias y el procedimiento de reinstalación están en [Documentacion/INSTALACION_Y_ENTREGA.md](Documentacion/INSTALACION_Y_ENTREGA.md).

## Controles

- `WASD` o flechas: mover al personaje.
- `Shift` sostenido: correr.
- `E`: interactuar con el objetivo que muestra el cursor.
- En el vehículo: controles configurados por Vehicle Physics Pro.

La cámara se sigue con Cinemachine; no hay una acción de mouse anunciada en el HUD.

## Recorrido actual

1. El auto llega encendido, pierde potencia y se detiene antes de la estación.
2. El personaje baja, sube al techo y entra al taller por el hueco.
3. Enciende el generador, conecta el tablero eléctrico y abre el portón.
4. Recoge el bidón, lo carga en el surtidor y abastece el auto.
5. Conduce hasta `SalidaAlCamino` antes de que se cumpla el tiempo.

El HUD muestra tanto el tiempo general como el combustible restante del generador cuando está en marcha. Todas las derrotas y la victoria silencian las demás fuentes de audio.

## Estado de la entrega

La escena y los assets realmente usados se conservan; se retiraron demos, material de muestra y automatizaciones de editor que reescribían la escena. La IA final todavía se debe definir e integrar como un desafío visible y comprobable; no se declara como terminada.

- Diseño: [GDD_EN_EL_CAMINO.md](Documentacion/GDD_EN_EL_CAMINO.md)
- Pruebas y pendientes: [Informe_Testing_EN_EL_CAMINO.md](Documentacion/Informe_Testing_EN_EL_CAMINO.md)
- Assets y dependencias: [ASSETS_Y_DEPENDENCIAS.md](Documentacion/ASSETS_Y_DEPENDENCIAS.md)

