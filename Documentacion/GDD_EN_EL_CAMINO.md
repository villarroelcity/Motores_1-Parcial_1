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

El protagonista es un pirata del asfalto sospechado de robar el auto que conduce. En un camino rural al anochecer, se queda sin combustible frente a una estación abandonada. Debe resolver el recorrido del taller, devolver energía a la estación y abastecer el vehículo antes de que oscurezca. Al continuar por la ruta aparece una emboscada policial: la persecución es una sorpresa y el HUD no la anticipa.

## Bucle principal

Llegada con motor encendido → corte de combustible y conducción con el envión → exploración a pie → taller → generador → tablero eléctrico → portón → bidón → surtidor → vehículo → ruta → emboscada policial → escape.

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
| 8 | Llegar a `SalidaAlCamino` | Oculta el reloj de anochecer y permite continuar conduciendo |
| 9 | Pasar por la zona de emboscada | Inicia la persecución y su reloj de 60 segundos |
| 10 | Sacar 200 metros de ventaja al patrullero | Victoria |

Derrotas vigentes: agotar el tiempo hasta la ruta, agotar el generador antes de resolver el taller, una caída peligrosa, captura por contacto de la patrulla, agotar los 60 segundos de persecución y alcanzar el 100% de daño del auto. Victoria y derrota vuelven a estar habilitadas después de la prueba de grabación. `GAME OVER` detiene la partida y deja disponible `REINICIAR`.

El tiempo inicial hasta alcanzar la ruta es de **120 segundos (dos minutos)**. El generador tiene 30 segundos de combustible. Su cuenta interna continúa aunque se oculte el indicador tras llenar el bidón. Al agotarse se apagan el audio y la electricidad, sin desenchufar el cable. Alimenta también las luces exteriores. Al alcanzar la ruta desaparece el reloj de anochecer; el ciclo ambiental continúa sin saltar de golpe a la noche y sin causar derrota por ese reloj.

## Sistemas implementados

- Movimiento con `CharacterController`; caminar y correr con `Shift`.
- Cámara de tercera persona con `CamaraJugador` y detección de obstáculos.
- Interacción por Raycast e interfaz `IInteractable`.
- Flujo de misión secuencial en `GameFlow`.
- Auto con Vehicle Physics Pro; llegada, carga y control reunidos en `CarDriver`.
- Interacciones de generador, tablero, interruptor, portón, bidón, surtidor y salida.
- UI uGUI adaptada a pantalla para objetivos, mensajes, tiempo y combustible del generador.
- Ciclo tarde–noche con Enviro 3, iluminación y audio ambiental/espacial.
- Audio por mixer, efectos de interacción, motor VPP, música adaptativa y silenciamiento al finalizar.
- Físicas, colliders, triggers, cajas físicas y partículas de feedback.
- Acceso de la estación reunido en `Camino`: los antiguos `Camino (2)` y `Camino (3)` se unen al tramo central mediante cortes diagonales. La malla estática `Camino_Unido` conserva el material y los extremos hacia la ruta, con un `MeshCollider` coincidente y sin cajas de colisión superpuestas.

## Dirección visual y sonora

La estación usa una estética rural low-poly con vegetación de Nature Starter Kit 2, entorno de Enviro 3, modelos externos de generador/bidón/tablero y surtidor 3D. El patrullero de la persecución se distingue del auto del jugador mediante materiales propios. El arte se utiliza como apoyo del gameplay: el techo, el hueco, el tablero y el portón hacen visible la ruta de misión.

El ambiente exterior, generador, surtidor, portón, acciones del auto, música de exploración y mensajes de victoria/derrota se organizan mediante `EnElCamino_Mixer`.

## IA — patrullero

La amenaza es `PatrulleroIA`, oculto junto a la ruta. La persecución se activa al atravesar su trigger, sin exigir superar una velocidad. La patrulla comienza a incorporarse al detectar al jugador aproximándose unos cuatro segundos antes; el trigger inicia el HUD y el reloj. La edición Community de VPP permite un solo vehículo VPP activo: ese controlador queda reservado al jugador. La patrulla usa cuatro `WheelCollider` de Unity guardados en la escena, con dirección delantera, suspensión, torque y freno. Las ruedas, sus modelos y la fuente de sirena se asignan desde el Inspector; no se construyen al iniciar.

La IA compara corredores y predice hasta tres segundos el movimiento del tráfico. Ajusta velocidad según la distancia longitudinal con signo: si sobrepasa al jugador, frena en lugar de acelerar alejándose. Puede retroceder tras detenerse para recuperar la posición. Si el jugador sale al pasto, amplía la búsqueda fuera de la calzada y apunta hacia su posición prevista, evitando el límite que antes la retenía sobre el asfalto. Vuelve al comportamiento de ruta cuando ambos vehículos regresan a ella. La aceleración, recuperación y evasión siguen pendientes de validación jugable; no se considera cerrada la IA.

Para rodear tráfico lento o detenido, la predicción parte de la velocidad actual y simula giro delantero y aceleración, en lugar de suponer 12 m/s desde reposo. Evalúa ambos costados y las banquinas, busca completar el cambio lateral antes del obstáculo y mantiene ese espacio hasta haberlo rebasado. A baja velocidad limita la aceleración durante la maniobra; si está demasiado cerca, intenta retroceder solo cuando el sensor trasero deja espacio. Al acercarse a un jugador detenido considera también la separación lateral, no solo la longitudinal. El alcance de detección contempla la velocidad relativa del tráfico de frente.

Las balizas son dos cubos de malla guardados en la escena, `PatrulleroIA/BalizaIzquierda` y `BalizaDerecha`. Se pueden mover, rotar y escalar con el juego detenido. El código solo alterna sus luces y su emisión. La sirena es 3D, con volumen máximo hasta 8 m y atenuación lineal hasta silencio a 220 m.

## Tráfico y conducción

El tráfico empieza al recuperar el control tras cargar combustible. Usa carrocerías VPP de coupé y camioneta, colores distintos y circulación en ambos sentidos a unos 60 km/h. Los vehículos aparecen lejos, con intervalos y separaciones aleatorias. La base se redujo de 24 a 19 vehículos por sentido (aproximadamente 20% menos), de 5 a 4 iniciales por sentido y de 100 a 125 m de separación base. Son cuerpos dinámicos con movimiento recto; en esta ruta plana se limita su movimiento vertical para evitar que se apilen.

Los dos modelos y cuatro colores se guardan como ocho prefabs en `Assets/PreparedGameplay/Prefabs`. La escena contiene una reserva de 64 instancias inicialmente desactivadas: el gestor mezcla sus referencias, las posiciona lejos y las activa, sin instanciar autos ni agregar componentes durante la partida. La reserva es capacidad disponible, no cantidad de vehículos activos al mismo tiempo. Colliders, altura, ruedas, materiales y faros quedan preparados en edición.

El control de crucero considera otros vehículos y no el suelo. Un roce entre autos de tráfico provoca una pausa breve y permite retomar la marcha; un impacto del jugador desactiva su avance automático. Se desactivan los componentes VPP que estos autos no usan. La altura inicial se corrige a partir de los límites mundiales del modelo, manteniendo sus ruedas sobre el asfalto. El collider de carrocería usa fricción baja para no quedar frenado contra el piso con el motor simplificado. La prevención de colas y choques secundarios debe verificarse nuevamente en juego.

El jugador conserva las físicas y la dirección de VPP. La transmisión es automática: mantener abajo/S frena primero y luego selecciona reversa al detenerse; arriba/W vuelve a avanzar. El pasto reduce el agarre respecto del asfalto y aplica pequeños impulsos verticales alternados cerca de las ruedas para representar irregularidades. Los impulsos aumentan con la velocidad, requieren contacto real con el pasto y se suspenden al quedar en el aire o volver al asfalto. La patrulla comparte esa irregularidad y pierde adherencia lateral fuera de la ruta. Los faros originales permanecen encendidos y se complementan con haces de luz sobre el suelo.

El daño del jugador se acumula por impactos normales a la carrocería superiores a 3 m/s; los apoyos en el suelo y pequeños roces no suman daño. Cada impacto suma aproximadamente 2 puntos por m/s sobre ese umbral, con máximo de 65 por evento y un intervalo mínimo de 0,35 s entre registros. A 100 puntos el vehículo queda destruido y termina la partida. El daño persiste al ocultarse su indicador.

Si VPP cala el motor durante la conducción, el puente solicita un nuevo arranque nativo tras confirmar el estado durante 0,35 s. Solo puede hacerlo con combustible, motor solicitado, daño menor a 100 y partida activa. Conserva la dirección, transmisión y física del asset; no reinicializa el controlador ni fuerza las RPM. La secuencia inicial sin combustible no usa esta recuperación.

## Interfaz y controles

La UI usa uGUI y se adapta al tamaño de la pantalla. Incluye objetivos durante un máximo de 10 segundos, mensajes breves, reloj de anochecer, combustible del generador, `VENTAJA` y espejo retrovisor. La primera misión aparece al bajar del auto. No muestra el cartel de velocidad, la telemetría, el título del juego ni avisos que revelen la emboscada. Los textos de gameplay no llevan tildes.

El recordatorio de controles ocupa una sola línea horizontal al pie derecho, con ancho adaptable al Canvas y ajuste de fuente entre 10 y 15. No permite saltos automáticos ni desbordamiento vertical. La configuración se guarda en el Canvas, sin un script que vuelva a imponer sus dimensiones al habilitar el HUD.

El espejo muestra solo la imagen, sin marco ni texto. Las referencias temporales del editor se retiraron. `VENTAJA 0 / 200 m | TIEMPO 1:00` aparece en una sola línea roja, anclada abajo con margen para no recortarse. La barra `DANO` aparece solo al recibir un impacto y se oculta tras cinco segundos sin nuevos golpes, aunque su valor interno sigue acumulado.

El menú solicitado contiene únicamente:

- Pausa y continuación con `ESC`.
- Reinicio de partida con el botón `REINICIAR` o la tecla `R`.
- Pantalla de `CONTROLES`.

Los botones, paneles, `EventSystem` e `InputSystemUIInputModule` están guardados en la escena. Los botones usan eventos persistentes configurados en el Inspector. `GameUI` solo controla contenido, visibilidad, pausa y resultados; no crea ni elimina la UI ni impone posiciones durante la partida. El espejo usa una cámara y un asset RenderTexture previamente conectados. Al producirse cualquier `GAME OVER` se detienen los audios de juego y queda disponible `REINICIAR`. El cartel siempre dice únicamente `GAME OVER`, sin agregar el motivo de derrota.

Al confirmar la victoria se establece inmediatamente `Time.timeScale = 0`: quedan congeladas la física y las animaciones que usan tiempo de juego. La cámara deja de seguir al vehículo y el menú no puede reanudar una partida finalizada. El reinicio restaura la escala de tiempo normal.

## Criterios de entrega

Quedan por producir y validar: build jugable, video de hasta cinco minutos, prueba final completa y documentación de testing actualizada. La lista operativa está en [Informe_Testing_EN_EL_CAMINO.md](Informe_Testing_EN_EL_CAMINO.md).

## Actualización del 22–23 de octubre de 2026

Se revisaron los commits existentes y los cambios pendientes de escena, conducción, tráfico, patrulla, HUD, iluminación e interacciones. La compilación de `Assembly-CSharp.csproj` pasa sin errores; siguen apareciendo advertencias de APIs obsoletas del paquete Nature Starter Kit. Esto verifica compilación, no sustituye la prueba completa de persecución, colisiones y audio en una build.

## Depuración de construcción de objetos — 23/10/2026

Los 17 scripts propios pasan de 5.537 a 4.181 líneas, una reducción de 1.356 líneas (24,5%). `GameUI` pasa de 942 a 313. Se guardaron en edición la UI, el modelo del jugador, el agarre del bidón, fuentes de audio, sirena, ruedas de la patrulla, luces, materiales físicos y variantes de tráfico. Los pasos usan un WAV, no un sonido generado al jugar. Los recursos quedan organizados bajo `Assets/PreparedGameplay`: 35 materiales compartidos, tres materiales físicos, ocho prefabs, un RenderTexture y un WAV.

No queda construcción de GameObjects, `AddComponent`, generación de AudioClip, RenderTexture o PhysicsMaterial en los scripts de gameplay. Se conserva la copia interna del ScriptableObject de tiempo de Enviro para no modificar el asset original: no es un objeto de escena. Las herramientas temporales de migración se retiraron de `Assets/Editor` después de guardar y comprobar las referencias.

Se mantienen la cámara personalizada y las animaciones procedurales existentes del jugador y portón; migrarlas a Cinemachine/Animator sería otro cambio de comportamiento que requiere validación específica. No se alteraron las decisiones de persecución, la dirección VPP ni el control de crucero del tráfico.

