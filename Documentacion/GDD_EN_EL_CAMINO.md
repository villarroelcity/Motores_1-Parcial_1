# GDD — En el Camino

Última revisión del diseño narrativo y la progresión: 30/10/2026. Las secciones fechadas al final documentan cambios anteriores; para las reglas vigentes, consultar primero las secciones principales.

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

*En el camino* es un juego de exploración, interacción y conducción en tercera persona. El protagonista es un ladrón que acaba de robar un auto y huye por una ruta rural. En medio de la fuga se queda sin combustible y alcanza una estación de servicio abandonada. Tiene una ventana de oportunidad de dos minutos para conseguir nafta, volver al vehículo y salir a la ruta antes de que anochezca. Durante esta primera parte nadie lo está persiguiendo.

Para reabastecerse debe entrar a un taller por el techo, recuperar la energía eléctrica de la estación mediante un generador con apenas 30 segundos de combustible, encontrar un bidón vacío y llenarlo en el surtidor antes de que el generador se apague.

Cuando por fin regresa a la ruta, cree que consiguió escapar. Pero un patrullero oculto, alertado por el robo, reconoce el vehículo y comienza a perseguirlo. La fuga termina con un desafío de conducción: sacar 200 metros de ventaja antes de que se agote un minuto. Si lo consigue, gana; si no, la historia plantea que el auto sufre una avería y la fuga fracasa.

## Historia y progresión

### Primera etapa: ventana de oportunidad (2 minutos)

El auto comienza la escena circulando por la ruta. Al quedarse sin combustible pierde potencia, se apaga y termina deteniéndose por inercia junto a la estación abandonada. El reloj de **2:00** empieza cuando comienza el corte de combustible, no al aparecer el policía.

El jugador baja y debe resolver la situación contrarreloj. El desafío en esta etapa consiste en explorar la estación, acceder al taller, habilitar el surtidor y volver a conducir. No hay una persecución activa durante esta fase. El plazo termina al cruzar la salida hacia la ruta: si no llega a ese punto antes de que se agote el reloj de anochecer, pierde.

### Segunda etapa: recuperar combustible (30 segundos de generador)

El taller no tiene un acceso directo utilizable desde afuera. El jugador debe subir al techo y descender por un hueco para entrar. Allí encuentra un generador con una reserva limitada de combustible. Después de encenderlo tiene **30 segundos** para conectar la electricidad desde el tablero, accionar el interruptor del portón, recoger el bidón vacío y cargarlo en el surtidor.

El generador es necesario para que el surtidor funcione. Si se apaga **antes de llenar el bidón**, termina la partida. Si el bidón ya está lleno cuando se agota, el generador puede apagarse sin provocar una derrota inmediata. El jugador todavía debe llevar el combustible al auto, cargarlo, arrancarlo y alcanzar la ruta dentro de los dos minutos de la primera etapa. Los dos relojes son independientes: encender el generador no reinicia la cuenta general.

### Tercera etapa: salida a la ruta y emboscada policial

Al pasar por `SalidaAlCamino`, el juego muestra una **victoria aparente** y oculta el reloj de anochecer, pero no termina la partida. El jugador sigue conduciendo y no recibe un aviso anticipado de la patrulla.

Un policía permanece escondido cerca de la ruta. Reconoce el auto robado y, cuando se activa la zona de persecución, comienza el desafío final. La patrulla se incorpora al camino, enciende la sirena y persigue al jugador entre el tráfico.

### Desafío final: escapar del patrullero (1 minuto)

El jugador dispone de **60 segundos** para ganar **200 metros de ventaja** respecto del patrullero. Debe conducir, esquivar el tráfico y elegir si se mantiene sobre el asfalto o intenta escapar por el pasto.

- **Victoria:** alcanzar los 200 metros de ventaja durante la persecución.
- **Derrota por tiempo:** agotarse el minuto sin alcanzar la distancia requerida. En la historia, el auto sufre una avería y el ladrón no consigue escapar.
- **Otras derrotas:** colisión de captura con la patrulla, daño acumulado del vehículo del 100 % y las condiciones de derrota de la etapa anterior.

**Estado de implementación:** el minuto, el límite de 200 metros y el Game Over al agotarse el tiempo están programados. Sin embargo, el código aún describe el reloj como «RESERVA» y avisa que queda un minuto de combustible; además, al terminar el tiempo llama a `GameOver()`, sin una secuencia específica de avería. Antes de la entrega conviene alinear esos textos y, si se desea mostrar la falla, implementar su efecto visual o sonoro. El GDD describe la intención narrativa sin presentar esa avería como un efecto ya implementado.

## Bucle jugable

Conducir hasta quedarse sin combustible → bajar del auto → explorar la estación → entrar por el techo al taller → encender el generador → conectar el tablero eléctrico → abrir el portón → recoger el bidón → cargarlo en el surtidor → cargar el auto → arrancar y salir a la ruta → emboscada policial → evasión y resultado final.

La UI guía las interacciones del taller y muestra los relojes correspondientes. La patrulla se revela recién en la última parte, para que el desafío final cambie el ritmo de exploración a conducción bajo presión.

## Objetivos y condiciones

| Paso | Acción del jugador | Resultado |
| --- | --- | --- |
| 1 | Entrar al taller por el techo y descender por el hueco | Accede al generador y a la zona del bidón |
| 2 | Encender el generador | Comienza su cuenta de 30 segundos |
| 3 | Conectar la electricidad desde el tablero | Habilita la energía de la estación |
| 4 | Accionar el interruptor del portón | Abre la salida del taller |
| 5 | Recoger el bidón vacío | Puede trasladarlo hasta el surtidor |
| 6 | Llenar el bidón antes de que se apague el generador | Asegura el combustible para el auto |
| 7 | Llevar el bidón al vehículo y cargarlo | Permite volver a arrancar |
| 8 | Salir de la estación y alcanzar `SalidaAlCamino` antes de los 2 minutos | Supera la primera etapa; aparece una victoria aparente |
| 9 | Avanzar hasta la zona de emboscada | Se activa la persecución y el reloj de 60 segundos |
| 10 | Ganar 200 metros de ventaja sobre el patrullero | Victoria definitiva |

## Reglas de tiempo y condiciones de derrota

| Límite | Cuándo empieza | Qué sucede al agotarse |
| --- | --- | --- |
| Ventana de oportunidad: 120 segundos | Cuando el auto comienza a quedarse sin combustible durante la llegada | Si aún no llegó a `SalidaAlCamino`, `Game Over` |
| Generador: 30 segundos | Al encenderlo dentro del taller | Se corta la energía; si todavía no llenó el bidón, `Game Over` |
| Persecución: 60 segundos | Al activar la persecución mientras conduce | Si no alcanzó 200 m de ventaja, `Game Over`; la avería es la explicación narrativa prevista |

Además se pierde por una caída peligrosa, captura del patrullero o daño del auto del 100 %. La victoria final congela el juego y muestra el resultado correspondiente; las derrotas presentan `GAME OVER` y permiten reiniciar.

## Sistemas implementados

Controles del vehículo: `WASD` o flechas para dirección, aceleración, freno y reversa automática; `Espacio` conserva el freno existente. Se deshabilitan los atajos de demostración VPP: respawn con `Enter`, embrague con `Shift`, cambios manuales con números/Tab/CapsLock/PageUp/PageDown, selección de N/R, encendido con K, conmutación de faros con L, retarder con teclado numérico y bloqueo de estacionamiento con Ctrl+Espacio. `Shift` sigue siendo correr a pie; `ESC` y `R` conservan las funciones del menú. No se cambia la física, la respuesta de dirección/pedales ni la caja automática VPP.

- Movimiento con `CharacterController`; caminar y correr con `Shift`.
- Cámara de tercera persona con `CamaraJugador` y detección de obstáculos.
- Interacción por Raycast e interfaz `IInteractable`.
- Flujo de misión secuencial en `Juego`.
- Auto con Vehicle Physics Pro; llegada, carga y control reunidos en `AutoJugador`.
- Interacciones de generador, tablero, interruptor, portón, bidón, surtidor y salida.
- UI uGUI adaptada a pantalla para objetivos, mensajes, tiempo y combustible del generador.
- Ciclo tarde–noche con sol, Skybox Procedural y niebla nativos de Unity; audio ambiental/espacial.
- Audio por mixer, efectos de interacción, motor VPP, música adaptativa y silenciamiento al finalizar.
- Físicas, colliders, triggers y colisiones de vehículos. Partículas de gameplay: humo del generador y de ambos escapes del auto, polvareda al conducir sobre pasto y chispas de impacto.
- Acceso de la estación reunido en `Camino`: los antiguos `Camino (2)` y `Camino (3)` se unen al tramo central mediante cortes diagonales. La malla estática `Camino_Unido` conserva el material y los extremos hacia la ruta, con un `MeshCollider` coincidente y sin cajas de colisión superpuestas.

## Dirección visual y sonora

La estación usa una estética rural low-poly con vegetación de Nature Starter Kit 2, cielo e iluminación nativos de Unity, modelos externos de generador/bidón/tablero y surtidor 3D. El patrullero de la persecución se distingue del auto del jugador mediante materiales propios. El arte se utiliza como apoyo del gameplay: el techo, el hueco, el tablero y el portón hacen visible la ruta de misión.

El ambiente exterior, generador, surtidor, portón, acciones del auto, música de exploración y mensajes de victoria/derrota se organizan mediante `EnElCamino_Mixer`.

## IA — patrullero

La amenaza es `PatrulleroIA`, oculto junto a la ruta. La persecución se activa al atravesar su trigger, sin exigir superar una velocidad. La patrulla comienza a incorporarse al detectar al jugador aproximándose unos cuatro segundos antes; el trigger inicia el HUD y el reloj. La edición Community de VPP permite un solo vehículo VPP activo: ese controlador queda reservado al jugador. La patrulla usa cuatro `WheelCollider` de Unity guardados en la escena, con dirección delantera, suspensión, torque y freno. Las ruedas, sus modelos y la fuente de sirena se asignan desde el Inspector; no se construyen al iniciar.

La música de tensión comienza desde el inicio del clip cuando la patrulla enciende su sirena, con la transición de volumen existente. No se reproduce anticipadamente ni se dispara por los últimos 20 segundos del reloj de anochecer. Antes de la alarma, la patrulla se incorpora hacia un punto fijo de la ruta y espera, evitando que el punto de incorporación avance con el propio auto. Si la alarma comienza antes de que el jugador la pase, espera su paso sin dar una vuelta prematura; si el jugador se detiene, vuelve a perseguir su posición. Frena antes de invertir la marcha y conserva una zona de tolerancia para no alternar avance y reversa a cada paso.

La patrulla usa AI Navigation de Unity para buscar una ruta hacia la posición prevista del jugador, con anticipación máxima de 0,6 segundos. `NavegacionPatrulla` contiene un NavMeshSurface horneado sobre el terreno; el asfalto tiene menor costo que el pasto. La navegación decide por dónde pasar y las ruedas físicas ejecutan la maniobra: no hay NavMeshAgent desplazando el auto ni teletransporte. La selección anterior de corredores se retiró.

La superficie queda enlazada a la patrulla y se habilita al iniciar la partida. Ocultar el dibujo violeta en Scene afecta solo a la visualización del editor; deshabilitar NavMeshSurface retiraría las rutas de la IA.

Los 64 autos de reserva tienen NavMeshObstacle con carving móvil para que la ruta de la patrulla rodee sus carrocerías. Se conserva el movimiento existente del tráfico. La patrulla también usa un sensor físico delantero para regular la velocidad ante un bloqueo y una comprobación trasera antes de retroceder. El seguimiento limita la velocidad según curva, distancia de frenado y movimiento del objetivo; una maniobra corta de reversa puede recuperar a un jugador que quedó detrás.

La referencia consultada fue CarAI-Unity de Ahmed Saed (MIT), que separa la ruta NavMesh de la conducción con WheelColliders. Se adaptó ese enfoque al controlador existente, sin importar su patrulla aleatoria ni agregar scripts de gameplay. La navegación se prepara y guarda en Unity; el auto del jugador conserva VPP. Las pruebas físicas aisladas de alcance, frenado, pasto, reversa y salida del escondite no sustituyen la validación jugable con todo el tráfico activo.

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

El retrovisor sigue una posición fija detrás del auto, con horizonte estable, inclinación de 6 grados hacia la calzada y campo de visión vertical de 35 grados. Su encuadre no depende de los límites del humo, polvo ni chispas. `Rear View Offset` y `Rear View Pitch` se ajustan en el componente UI del Inspector; la lente pertenece a `CamaraEspejoRetrovisor`.

El menú solicitado contiene únicamente:

- Pausa y continuación con `ESC`.
- Reinicio de partida con el botón `REINICIAR` o la tecla `R`.
- Pantalla de `CONTROLES`.

Los botones, paneles, `EventSystem` e `InputSystemUIInputModule` están guardados en la escena. Los botones usan eventos persistentes configurados en el Inspector. `UI` solo controla contenido, visibilidad, pausa y resultados; no crea ni elimina la UI ni impone posiciones durante la partida. El espejo usa una cámara y un asset RenderTexture previamente conectados. Al producirse cualquier `GAME OVER` se detienen los audios de juego y queda disponible `REINICIAR`. El cartel siempre dice únicamente `GAME OVER`, sin agregar el motivo de derrota.

Al confirmar la victoria se establece inmediatamente `Time.timeScale = 0`: quedan congeladas la física y las animaciones que usan tiempo de juego. La cámara deja de seguir al vehículo y el menú no puede reanudar una partida finalizada. El reinicio restaura la escala de tiempo normal.

## Criterios de entrega

Quedan por producir y validar: build jugable, video de hasta cinco minutos, prueba final completa y documentación de testing actualizada. La lista operativa está en [Informe_Testing_EN_EL_CAMINO.md](Informe_Testing_EN_EL_CAMINO.md).

La comparación completa con la consigna oficial está en [ESTADO_PARCIAL_2.md](ESTADO_PARCIAL_2.md). Cinemachine está integrado con seguimiento, encuadre y resolución de paredes; `CamaraJugador` solo transfiere el objetivo y bloquea el seguimiento al terminar. Los cinco emisores propios responden al gameplay y están guardados en la escena. Se comprobaron en editor; falta registrar su uso y validar la cámara en un recorrido jugable completo. El personaje usa Animator con clips editables; la consigna exige Sistema de Animación, sin nombrar Animator ni NavMesh expresamente. También debe aclararse si la instanciación de prefabs en edición y la reserva de tráfico satisfacen el punto de instanciación o si se exige crear objetos durante la partida.

## Actualización del 22–23 de octubre de 2026

Se revisaron los commits existentes y los cambios pendientes de escena, conducción, tráfico, patrulla, HUD, iluminación e interacciones. La compilación de `Assembly-CSharp.csproj` pasa sin errores; siguen apareciendo advertencias de APIs obsoletas del paquete Nature Starter Kit. Esto verifica compilación, no sustituye la prueba completa de persecución, colisiones y audio en una build.

## Depuración de construcción de objetos — 23/10/2026

Los 17 scripts propios pasan de 5.537 a 4.181 líneas, una reducción de 1.356 líneas (24,5%). `UI` pasa de 942 a 313. Se guardaron en edición la UI, el modelo del jugador, el agarre del bidón, fuentes de audio, sirena, ruedas de la patrulla, luces, materiales físicos y variantes de tráfico. Los pasos usan un WAV, no un sonido generado al jugar. Los recursos quedan organizados bajo `Assets/PreparedGameplay`: 35 materiales compartidos, tres materiales físicos, ocho prefabs, un RenderTexture y un WAV.

No queda construcción de GameObjects, `AddComponent`, generación de AudioClip, RenderTexture o PhysicsMaterial en los scripts de gameplay. Se conserva la copia interna del ScriptableObject de tiempo de Enviro para no modificar el asset original: no es un objeto de escena. Las herramientas temporales de migración se retiraron de `Assets/Editor` después de guardar y comprobar las referencias.

Se mantienen la cámara personalizada y las animaciones procedurales existentes del jugador y portón; migrarlas a Cinemachine/Animator sería otro cambio de comportamiento que requiere validación específica. No se alteraron las decisiones de persecución, la dirección VPP ni el control de crucero del tráfico.

## Última depuración y nombres — 24/10/2026

Los cinco archivos solicitados pasan a `Trafico`, `TraficoAutos`, `AutoJugador`, `Juego` y `UI`, conservando GUID y referencias. Los objetos principales son `Trafico`, `AutoJugador`, `Juego` y `UI`; los autos de la reserva ya usan nombres `Trafico_...`. El mapa actualizado contiene 17 scripts propios, no 14.

Esta revisión reduce el gameplay de 4.268 a 4.191 líneas (77 menos), sin agregar scripts. Se retiraron configuración repetida de VPP, un método sin llamadas, un wrapper de audio y la sobrecarga/motivos de derrota que ya no se mostraban. Los bloqueos configurables de VPP quedaron guardados en la escena; se mantienen únicamente las excepciones internas de freno de estacionamiento y retarder. Faros y telemetría usan la API pública del asset sin reflexión.

El reloj de partida ahora es un asset preparado en `Assets/PreparedGameplay/Entorno/RelojPartida.asset`: sustituye la copia interna del reloj mencionada en la revisión anterior. Los scripts propios no crean GameObjects, componentes, cámaras, materiales, texturas ni AudioClips, y no usan Instantiate. Los colores y la emisión se cambian con un MaterialPropertyBlock compartido sobre los materiales existentes; no hay copias implícitas con Renderer.material/materials. El SDK de los paquetes conserva su implementación interna.

## Cinemachine, partículas y entorno nativo — 24/10/2026

Esta integración posterior sustituye la cámara personalizada y el reloj de Enviro descritos en las entradas históricas anteriores. La cámara principal tiene `CinemachineBrain`; `CamaraCinemachine` contiene `CinemachineCamera`, `CinemachineFollow`, `CinemachineRotationComposer` y `CinemachineDeoccluder`. Sigue la rotación y posición del jugador o auto, conserva el encuadre anterior y se acerca cuando una pared tapa al objetivo. Los colliders del jugador y su auto usan el tag Player para que Deoccluder no confunda el propio objetivo con una pared. La transferencia a pie/auto sigue usando las mismas llamadas del juego.

Los objetos `GeneradorAuxiliar/HumoGenerador`, `AutoJugador/PolvoPasto` y `AutoJugador/ChispasImpacto` se editan con el Inspector en Stop. El humo funciona mientras está encendido el generador y deja de emitirse al apagarse. El polvo requiere control del auto, pasto y velocidad superior a 10 km/h. Las chispas usan el punto del contacto aceptado por el sistema de daño. Materiales y textura están preparados bajo `Assets/PreparedGameplay/Efectos`; los emisores no se crean en ejecución.

Se quitó la instancia de Enviro de la escena y sus referencias en scripts propios. `Juego` utiliza el mismo reloj de partida para regular el sol, luz ambiente, niebla y exposición del Skybox Procedural. Llegar a la ruta oculta el reloj sin reiniciarlo ni saltar a la noche; después de alcanzar la ruta el anochecer ya no produce derrota. Los faros existentes se conservan. No se incluyen las nubes ni el clima de Enviro en esta versión simplificada. El paquete instalado se conserva para recuperación, pero la escena ya no depende de él. El asset de reloj Enviro sin uso se retiró con respaldo previo.

Se reemplazaron las dos texturas de muestra de Enviro del suelo por la textura de pasto del Nature Starter Kit ya instalado. `PastoRuta.mat` conserva el GUID del material anterior; colliders y adherencia permanecen iguales.

Los 17 scripts de gameplay pasan de 4.191 a 4.056 líneas: 135 menos incluso con los efectos nuevos. No se agregan scripts de runtime ni construcción de objetos. Se comprobaron el pipeline real de Cinemachine, la pared, el giro, cambio de objetivo, bloqueo de cámara al finalizar, emisión de partículas y continuidad del reloj en editor. Las capturas revisadas son renders del editor; no sustituyen el video ni la prueba de conducción completa.

Referencias técnicas: [Cinemachine Follow](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineFollow.html) y [Deoccluder](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDeoccluder.html).

## Precarga del audio de persecución — 24/10/2026

La sirena y la música de tensión precargan sus datos en segundo plano desde sus ajustes de importación. Se evita decodificar ambos clips de forma síncrona al activarse la persecución. La precarga es silenciosa: la sirena inicia con la alarma y la música de tensión sigue comenzando con ese mismo evento. No se agregan objetos ni scripts de gameplay.

## Revisión de historia, animaciones y cámara — 25/10/2026

Los dos relojes representan problemas distintos: el primero limita la búsqueda para evitar quedar varado en la estación al anochecer; el segundo expresa la escasa reserva recuperada bajo la exigencia de la huida. Cuando suena la sirena, el HUD muestra `RESERVA 1:00` y pide sacar 200 m de ventaja antes de agotar esa reserva. Se reutiliza el contador de persecución de 60 segundos. La victoria aparente usa el objetivo temporal habitual, durante un máximo de 10 segundos, y la persecución lo reemplaza si comienza antes. Solo la victoria final por escapar congela la partida.

Esta revisión amplía el punto de incorporación de la patrulla de 8 a 35 m hacia delante sobre el carril del jugador. Durante la incorporación corrige distancia lateral y rumbo de la ruta; no busca un bloqueo perpendicular. Solo pasa al seguimiento tras alinearse a menos de 0,6 m del centro del carril y aproximadamente 3 grados del rumbo, con el jugador ya pasado o detenido. Mantiene su ubicación inicial y el seguimiento NavMesh de la persecución.

`Jugador` tiene Animator sin root motion y el controlador `Assets/PreparedGameplay/Animaciones/Jugador.controller`. Sus estados Reposo, Caminar, Colgado y Caida se editan en Animator y Animation. `Estado` selecciona la pose y `Ritmo` regula la caminata según el desplazamiento real. Los pasos son eventos del clip Caminar. Al cargar combustible se congela el Animator; el descenso controlado mantiene sus animaciones aunque bloquee el movimiento. `PlayerController` queda en 181 líneas y deja de calcular oscilaciones de extremidades por frame.

El Brain se actualiza en LateUpdate sobre el Rigidbody interpolado del auto. Deoccluder conserva la detección inmediata de paredes y añade 0,2 s de retención de distancia, amortiguación de recuperación 0,5 y de entrada 0,08. No se cambia el offset ni la lente elegidos. La configuración se comprobó en edición; falta la validación visual en gameplay.

## Ajustes de conducción y efectos — 25/10/2026

La velocidad base del estado Caminar baja a 0,6: caminar y correr animan un 40% más despacio, sin reducir el desplazamiento del CharacterController. Reposo baja a 0,75. Los eventos del clip mantienen los pasos sincronizados, sin agregar lógica de animación.

La patrulla calcula la velocidad del objetivo sobre su rumbo y limita la velocidad de acercamiento relativa, evitando retenerse excesivamente a pocos metros. El alcance cercano añade hasta 3 m/s; la recuperación más fuerte se incorpora gradualmente por encima de 20 m de separación. Se conserva el sensor para frenar ante tráfico y la dirección anterior.

La polvareda utiliza partículas en espacio mundial de 2,1–3 s, crecimiento hasta unos 3 m y emisión de 30 por segundo más 6 por metro recorrido, con máximo de 900. Sigue dependiendo de conducir sobre pasto a más de 10 km/h. Ambos `HumoEscapeIzquierdo` y `HumoEscapeDerecho`, hijos de AutoJugador, son emisores guardados en la escena y editables en Stop. Emiten vapor gris tenue mientras se solicita motor y dejan disipar las partículas al apagarlo. Comparten el material preparado; no se crean efectos durante el gameplay.

El tráfico aplica torque para recuperar el rumbo, en vez de imponer la rotación. Cede mientras mantenga contacto con otro auto por delante, permitiendo que el que recibe un toque desde atrás avance. El sensor reconoce vehículos incluso cuando alcanza una superficie inclinada de su carrocería. Antes de activar una entrada de la reserva se verifica un espacio libre de 20 m de largo; una posición ocupada no consume esa entrada. Estos ajustes conservan velocidad, cantidad, modelos y circulación en ambos sentidos.

## Organización del historial — 25/10/2026

Los cambios acumulados del 24 y 25 se separaron por tema en `parcial-2`: renombrado de controladores conservando GUID, entorno nativo y flujo, Cinemachine/retrovisor, Animator, navegación de patrulla, contactos/aparición de tráfico, partículas, inicio de música y precarga del audio. La documentación se mantiene en un commit propio. Se conservó el historial anterior; la escena compartida se dividió junto a las referencias de cada integración. No se incorporaron herramientas temporales ni paquetes comerciales al repositorio.

## Alcance de patrulla, jerarquía y polvo — 26/10/2026

La patrulla conservaba un límite involuntario de unos 139 km/h incluso en recta, además de otro freno por esquinas que duplicaba el límite de curva. Se retiran esos topes redundantes; el máximo configurado queda en 72 m/s. Las ruedas cortan momentáneamente el motor al superar 0,4 de deslizamiento longitudinal para recuperar tracción, con rigidez longitudinal 1,7. Se mantienen frenada ante obstáculos, incorporación paralela, cierre gradual cercano, 200 m para escapar y 60 segundos de reserva. El coste de navegar por pasto baja mientras el jugador esté fuera de la ruta, para seguirlo sin insistir en volver al asfalto.

Se retiraron más de 1.400 nodos y ramas sin uso, principalmente variantes ocultas de ruedas del tráfico, antiguos ayudantes de VPP, marcadores y UI obsoleta. Patrulla y tráfico conservan sus carrocerías, ruedas visibles, luces y colliders; no conservan controladores VPP adicionales ni telemetría. La patrulla usa solamente sus cuatro WheelColliders. Los objetos principales pasan a nombres como `Generador`, `CamaraPrincipal`, `SistemaEventos`, `ModeloJugador`, `MusicaPersecucion` y `AutoTrafico_00`. Los cuatro clips del Animator apuntan a `ModeloJugador` después del cambio de nombre. Se conservan las posiciones elegidas por el usuario, incluidos los escapes.

La polvareda aumenta a 50 partículas por segundo y 8 por metro, vida de 2,5–3,8 s, tamaño inicial de 0,9–1,5 m, crecimiento y máximo de 1.200. El emisor sigue preparado en Unity y solo se habilita sobre pasto, conduciendo a más de 10 km/h.

Se guardaron 31 variantes de 11 shaders en `VariantesEscena.shadervariants`, incluidas vistas diurnas, nocturnas, luces y efectos. Se precargan desde Graphics Settings y en espera del editor. Se deshabilita la compilación asíncrona del editor para evitar el material provisional cian. Una variante nueva todavía puede producir espera al compilarse por primera vez; la precarga no certifica ausencia de tirones en todo el recorrido.

Los 17 scripts pasan de 3.894 a 3.617 líneas: 277 menos. Se sustituyó reflexión innecesaria por acceso público a VPP para agarre y audio, se retiraron búsquedas de respaldo y estados sin uso, y se trasladó configuración física a los componentes guardados. También se quitaron comentarios históricos. Se conserva el pequeño acceso al motor privado de VPP y el bloqueo de sus atajos internos porque siguen siendo necesarios. No se crean GameObjects, componentes, materiales ni emisores durante gameplay. El control nativo del auto del jugador se conserva.


## Persecución tras el video de las 01:12 — 26/10/2026

La patrulla anticipa la incorporación y acelera cuando el jugador se acerca; deja de frenar en el punto de espera durante la alarma. El estado de incorporación termina cuando el jugador la pasa, sin exigir que coincidan simultáneamente dos tolerancias de alineación. La sirena y la música mantienen su trigger existente.

Se sigue un punto previsto delante del jugador cuando el corredor físico está libre. NavMesh queda como respaldo ante obstáculos reales, evitando los rodeos innecesarios del mapa horneado sobre tramos despejados. Aumenta la distancia de anticipación de dirección con la velocidad. Los sensores de frenado miran la trayectoria elegida y acotan su alcance al objetivo, evitando frenar por objetos lejanos fuera del recorrido.

La patrulla busca vehículos lentos delante, elige un lado con espacio y mantiene ese lado hasta quedar 8 m por delante del auto adelantado. Comprueba el acceso diagonal y el corredor lateral antes de iniciar la maniobra. Si queda detenida por un bloqueo puede iniciar recuperación aunque el sensor haya reducido la velocidad deseada a cero. La comprobación de espacio trasero sigue activa.

Se aplica un par estabilizador para amortiguar el giro excesivo y se suaviza el cambio de agarre pasto/asfalto. La patrulla deja de recibir los impulsos artificiales de baches del jugador; mantiene suspensión y contacto mediante sus WheelColliders. El jugador conserva esos baches y sus controles VPP. El límite de patrulla continúa en 72 m/s, la victoria en 200 m y la reserva en 60 s.

Solo cambia PatrulleroIA: no se agregan scripts ni objetos de gameplay. El archivo queda en 549 líneas y el total propio en 3.697. Las pruebas aisladas incorporan salida real, desvío al pasto, regreso y tráfico de ambos sentidos; la validación jugable sigue a cargo del usuario.
