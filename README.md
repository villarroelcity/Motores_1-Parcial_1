# En el camino — Segundo Parcial

Proyecto individual de **Motores de Desarrollo 1**, hecho en Unity 6. Es un juego en tercera persona que combina exploración, interacción con objetos y conducción.

## De qué trata

El protagonista es un ladrón que acaba de robar un auto y está escapando por una ruta rural. Se queda sin combustible y logra detenerse cerca de una estación de servicio abandonada. Tiene una **ventana de oportunidad de dos minutos** para conseguir nafta y regresar a la ruta. Durante ese tiempo nadie lo está persiguiendo.

Para poder cargar combustible tiene que entrar al taller por un hueco en el techo, encender un generador, conectar el tablero eléctrico, abrir el portón y encontrar un bidón. El generador solo tiene **30 segundos de combustible**, así que debe llenar el bidón en el surtidor antes de que se apague. Si lo consigue, puede llevar la nafta al auto, arrancarlo y salir de la estación. Todavía tiene que llegar a la ruta dentro de los dos minutos.

Cuando vuelve al camino parece que ya escapó, pero un patrullero escondido reconoce el auto robado y comienza a perseguirlo. El objetivo final es sacar **200 metros de ventaja en un minuto** mientras esquiva el tráfico y evita chocar. Si no lo logra, pierde; la avería del auto es la explicación prevista en la historia.

## Objetivos del juego

1. Subir al techo y entrar al taller por el hueco.
2. Encender el generador y conectar la electricidad.
3. Abrir el portón y recoger el bidón.
4. Llenar el bidón en el surtidor antes de que se apague el generador.
5. Cargar el auto, arrancarlo y llegar a la ruta antes de que terminen los dos minutos.
6. Escapar de la patrulla sacándole 200 metros de ventaja dentro del minuto de persecución.

Los relojes cumplen funciones distintas: el de dos minutos comienza cuando el auto se queda sin combustible; el de treinta segundos comienza al encender el generador; el de un minuto comienza con la persecución. Si el bidón ya está lleno, que se apague el generador no provoca una derrota inmediata.

## Controles

| Tecla | Acción |
| --- | --- |
| `WASD` o flechas | Mover al personaje y conducir |
| `Shift` | Correr cuando está a pie |
| `E` | Interactuar con objetos cercanos |
| `S` o flecha abajo (en el auto) | Frenar y luego poner reversa |
| `Espacio` (en el auto) | Freno del vehículo |
| `Esc` | Pausar o reanudar |
| `R` | Reiniciar desde el menú de pausa o resultado |

La interfaz muestra el objetivo actual y los indicadores de tiempo, combustible del generador, daño y distancia durante la persecución.

## Sistemas del proyecto

- **Movimiento y animación:** personaje con `CharacterController` y `Animator`.
- **Vehículos y física:** conducción del auto con Vehicle Physics Pro, colisiones y sistema de daño.
- **IA y tráfico:** patrulla que sigue al jugador mediante ruedas físicas y navegación, con vehículos circulando por la ruta.
- **Cámara:** seguimiento del jugador y del auto con Cinemachine, además de retrovisor.
- **Interacciones:** detección por raycast, generador, tablero, portón, bidón y surtidor.
- **Audio:** sonidos ambientales, motor, generador, surtidor, sirena y música de exploración y persecución.
- **Efectos y escenario:** iluminación de tarde a noche, humo, polvo sobre el pasto, chispas y vegetación.
- **Interfaz:** objetivos, relojes, menú de pausa, daño y pantallas de resultado.

## Abrir el proyecto

1. Usar **Unity 6000.3.5f2**.
2. Descargar o clonar la rama `parcial-2`.
3. Abrir la carpeta del proyecto desde Unity Hub.
4. Importar los assets comerciales necesarios desde las licencias correspondientes.
5. Abrir `Assets/Scenes/EnElCamino.unity` y esperar a que finalice la importación.

Los paquetes comerciales no se incluyen completos en GitHub. El proyecto utiliza, entre otros, **Vehicle Physics Pro**, **Nature Starter Kit 2** y **Loader Heavy Machinery**. La instalación y las dependencias se explican en [Instalación y entrega](Documentacion/INSTALACION_Y_ENTREGA.md) y [Assets y dependencias](Documentacion/ASSETS_Y_DEPENDENCIAS.md).

## Estado actual

El recorrido, los objetivos y los sistemas principales están preparados en la escena y en el código. **Todavía falta validar una partida completa en Unity y probar la build final de Windows**, especialmente el funcionamiento de la persecución, el tráfico, las colisiones y los resultados.

También hay un detalle pendiente entre historia e implementación: durante la persecución el HUD todavía habla de una reserva de combustible y, al agotarse el minuto, el código muestra `Game Over`. La avería del auto está planteada como causa narrativa de esa derrota, pero no tiene una secuencia visual o sonora específica implementada.

Para la entrega del Segundo Parcial también quedan pendientes el video de gameplay, la publicación o build ejecutable y el cierre del informe de testing.

## Documentación

- [GDD — diseño e historia del juego](Documentacion/GDD_EN_EL_CAMINO.md)
- [Informe de testing](Documentacion/Informe_Testing_EN_EL_CAMINO.md)
- [Estado frente a la consigna del Segundo Parcial](Documentacion/ESTADO_PARCIAL_2.md)
- [Mapa de scripts](Documentacion/MAPA_SCRIPTS.md)
- [Instalación y entrega](Documentacion/INSTALACION_Y_ENTREGA.md)
- [Assets, dependencias y créditos](Documentacion/ASSETS_Y_DEPENDENCIAS.md)

## Ramas

- `main`: prototipo inicial correspondiente al Primer Parcial.
- `parcial-2`: desarrollo del vertical slice para el Segundo Parcial.

Los recursos de terceros y sus licencias se detallan en la documentación del proyecto.
