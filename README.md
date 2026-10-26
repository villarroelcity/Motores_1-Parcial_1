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
- `E`: interactuar con el objeto cercano indicado en pantalla.
- En el vehículo: WASD/flechas para conducir, abajo/S para frenar y luego retroceder; Espacio conserva el freno existente.
- `Esc`: pausa. `R`: reinicio desde pausa o resultado.

La cámara usa Cinemachine: `CinemachineBrain` en `CamaraPrincipal` y `CamaraCinemachine` con seguimiento, encuadre y acercamiento ante paredes. `CamaraJugador` solo cambia el objetivo entre jugador y auto. El cielo y el anochecer usan herramientas nativas de Unity, sin Enviro en la escena.

## Recorrido actual

1. El auto llega encendido, pierde potencia y se detiene antes de la estación.
2. El personaje baja, sube al techo y entra al taller por el hueco.
3. Enciende el generador, conecta el tablero eléctrico y abre el portón.
4. Recoge el bidón, lo carga en el surtidor y abastece el auto.
5. Conduce hasta `SalidaAlCamino` antes de que se cumpla el tiempo.
6. Continúa por la ruta, supera la emboscada y saca 200 m de ventaja antes de agotar los 60 segundos de persecución.

El HUD muestra tanto el tiempo general como el combustible restante del generador cuando está en marcha. Todas las derrotas y la victoria silencian las demás fuentes de audio.

## Estado de la entrega

La IA y la persecución están integradas, pero su validación jugable final sigue pendiente. Cinemachine, el Animator del personaje y los emisores de humo del generador y ambos escapes, polvareda del pasto y chispas de impacto están preparados en la escena. La revisión de requisitos y entregables está en [ESTADO_PARCIAL_2.md](Documentacion/ESTADO_PARCIAL_2.md). Las pruebas de editor no sustituyen el recorrido en Play ni una build.

Revisión del 26/10: jerarquía depurada y nombres principales en castellano, mayor polvareda, precarga de shaders y revisión de persecución tras el video. Permanecen 17 scripts propios, ahora con 3.697 líneas frente a las 3.894 del comienzo de la limpieza. Se conserva el control VPP del jugador. Las seis simulaciones de alcance, pasto y tráfico están registradas en el informe; la partida completa sigue pendiente de validación manual.

- Diseño: [GDD_EN_EL_CAMINO.md](Documentacion/GDD_EN_EL_CAMINO.md)
- Pruebas y pendientes: [Informe_Testing_EN_EL_CAMINO.md](Documentacion/Informe_Testing_EN_EL_CAMINO.md)
- Assets y dependencias: [ASSETS_Y_DEPENDENCIAS.md](Documentacion/ASSETS_Y_DEPENDENCIAS.md)

