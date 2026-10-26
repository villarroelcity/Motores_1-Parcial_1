# Informe de testing — En el Camino

## Alcance de esta revisión

Última revisión: 26/10/2026. Rama: `parcial-2`. El registro comienza el 20/10. Las verificaciones de editor no reemplazan una prueba manual de punta a punta ni una build de entrega.

## Verificaciones técnicas registradas

- La escena principal configurada para build es `Assets/Scenes/EnElCamino.unity`.
- El flujo de misión tiene ocho pasos y bloquea los interactuables que no correspondan al objetivo actual.
- El generador usa 30 segundos de combustible y el HUD dispone de un indicador específico mientras está activo.
- El portón requiere estación energizada; la interacción abre y cierra su collider de paso.
- El vehículo se configura con transmisión automática de Vehicle Physics Pro.
- El motor se apaga antes de que finalice la llegada automática; el jugador recibe control al detenerse el auto.
- Victoria y cualquier Game Over pausan las demás fuentes de audio antes de reproducir el feedback final.
- Se retiraron scripts de editor que reconfiguraban la escena automáticamente al abrirla o cambiar Play Mode. Se conserva sólo el ajuste de espera diurna en edición.

## Prueba manual obligatoria antes de entregar

| Caso | Pasos | Resultado esperado | Estado |
| --- | --- | --- | --- |
| Llegada | Ejecutar desde escena limpia | Auto llega encendido, pierde motor y el jugador aparece junto a él | Pendiente |
| Movimiento | Caminar y mantener `Shift` | La carrera es perceptiblemente más rápida que la caminata | Pendiente |
| Taller | Subir, entrar por el hueco y bajar | Sin flotación ni Game Over en la bajada controlada | Pendiente |
| Caída | Caer fuera de la bajada controlada | `GAME OVER`, audio global detenido | Pendiente |
| Energía | Generador → tablero → interruptor | El portón sólo abre con energía y antes del fin del combustible | Pendiente |
| Combustible | Bidón → surtidor → vehículo | Modelo del bidón acompaña al jugador sin flotar y habilita arrancar | Pendiente |
| Salida | Conducir a `SalidaAlCamino` | Oculta el reloj; se sigue conduciendo sin anticipar la emboscada | Pendiente |
| Persecución | Pasar el trigger, obtener 200 m de ventaja o agotar 60 s | Victoria por ventaja; derrota por captura o tiempo | Pendiente |
| Daño | Chocar contra vehículos y obstáculos | Suma daño; barra visible 5 s tras cada golpe; derrota al llegar a 100% | Pendiente |
| Motor tras un choque | Chocar sin alcanzar 100% de daño y volver a acelerar | Si VPP cala el motor, rearranca con combustible; conserva dirección y reversa | Pendiente |
| Cartel de derrota | Perder por daño, tiempo, captura y caída | Solo `GAME OVER` y botón `REINICIAR`, sin motivo adicional | Pendiente |
| Evasión lenta | Detener al jugador junto a una cola de tráfico | Patrulla busca costado libre o banquina; no queda frenada indefinidamente detrás | Pendiente |
| Sirena | Comparar sonido cerca, a 100 m y a más de 220 m | Volumen decreciente hasta silencio | Pendiente |
| Balizas | Mover los dos cubos de la patrulla con el juego detenido | Posiciones conservadas al iniciar; destello sin recrear geometría | Pendiente |
| Tráfico | Circular en ambos sentidos y provocar un roce entre NPC | Marcha normal; roce no genera detención permanente de ambos | Pendiente |
| Tiempo inicial | Iniciar una partida nueva | El reloj comienza en 2:00; la persecución conserva su minuto propio | Pendiente |
| Congelación por victoria | Sacar 200 m de ventaja | Se muestra la victoria y se congelan inmediatamente autos, física y cámara | Pendiente |
| Tráfico reducido | Iniciar conducción y recorrer ambos carriles | Menor cantidad y mayor separación; base de 19 por sentido y 125 m | Pendiente |
| Persecución fuera de ruta | Salir al pasto durante la persecución y regresar | Patrulla sigue fuera de calzada y luego retoma la ruta | Pendiente |
| Irregularidad del pasto | Conducir sobre pasto despacio, rápido, y volver al asfalto | Sacudidas leves según velocidad; ninguna estando quieto, en el aire o en asfalto | Pendiente |
| Empalmes de Camino | Recorrer los dos codos entre la ruta y la estación | Malla continua, sin caras solapadas, huecos ni cajas de colisión sobrantes | Pendiente |
| Tiempo | Dejar agotar el reloj | Atardecer a noche, `GAME OVER`, audio detenido | Pendiente |
| Resolución | Probar 16:9 y una resolución menor | HUD sin recortes ni solapamientos | Pendiente |
| Recordatorio de controles | Abrir la escena y cambiar el tamaño de Game | Una línea horizontal al pie derecho, sin letras apiladas en Scene ni Game | Pendiente |
| Build | Compilar y ejecutar build Windows | Mismo flujo y audio que en editor | Pendiente |

## Riesgos conocidos

- La IA está integrada, pero su evasión, recuperación y conducción cuando el jugador sale al pasto siguen sin validación final.
- La validación en una build Windows es obligatoria y todavía está pendiente.
- Los paquetes comerciales deben estar instalados localmente antes de abrir la escena; ver la guía de instalación.

## Evidencia a guardar al cerrar el parcial

- Captura del inspector con la escena en Build Settings.
- Video del recorrido completo, incluida una derrota y la victoria.
- Build final con fecha/versionado.
- Resultado de esta tabla actualizado con PASS/FAIL y correcciones aplicadas.

## Revisión del 23/10/2026

Se inspeccionó el video de 108 segundos `Desktop 2026.10.23 - 14.55.07.05.mp4`. Las observaciones del usuario sobre aceleración de la patrulla, pérdida de seguimiento, tráfico detenido y HUD motivaron cambios posteriores; ese video no valida la versión actual. La compilación externa de Unity mediante `dotnet build Assembly-CSharp.csproj` termina con 0 errores y 17 advertencias de APIs obsoletas de terceros. No se ejecutó Play ni una build para validar las últimas modificaciones de sirena y balizas. Los casos de la tabla continúan pendientes.

También se inspeccionó el video de 24 segundos `Desktop 2026.10.23 - 18.02.54.06.mp4`: muestra tráfico detenido y la pantalla de victoria sin congelación. Se corrigieron el cálculo de altura mundial al crear tráfico y su material de contacto, se bajó el tiempo inicial a 120 s y se agregó la pausa inmediata de victoria. Los cambios compilan sin errores; su validación física y visual posterior sigue pendiente.

Se inspeccionó el video de 15,67 segundos `Desktop 2026.10.23 - 18.38.41.07.mp4`: el jugador ya está detenido al comenzar y la derrota aparece al agotarse el minuto de persecución. No incluye el choque previo, por lo que no confirma por sí solo la causa del motor detenido. Se agregó recuperación mediante el arranque nativo ante un calado informado por VPP y se unificó el texto final en `GAME OVER`.

Se ejecutaron pruebas aisladas en .NET con los métodos reales de recuperación y planificación, sustituyendo los tipos matemáticos y el mundo físico de Unity. Pasaron diez comprobaciones de arranque: demora mínima, rearranque con daño de 65, intervalo entre intentos y bloqueos por falta de combustible, daño total, derrota, victoria, motor no solicitado, motor funcionando y arranque en curso. El planificador encontró corredores junto a un auto a 10, 12 y 18 m, una banquina ante dos autos detenidos y un paso ante tráfico de frente. También mantuvo el lado elegido, conservó el espacio al circular junto al obstáculo, lo liberó después de rebasarlo y evaluó la marcha atrás. A 6 m no se considera viable girar directamente: debe verificarse la recuperación con retroceso.

Estas pruebas no ejecutan suspensión, contacto de neumáticos, sensores físicos ni audio de Unity. No se abrió Play ni se validó una build con estos cambios: quedan pendientes el choque que cala el motor, la recuperación en un atasco real y la aproximación al costado del jugador.

Se corrigió el texto `HUD/Controles` para estirar su rectángulo horizontalmente con márgenes de 24, fuente adaptable de 10 a 15 y sin ajuste automático de línea ni desbordamiento vertical. Se verificaron los valores serializados de anclaje, tamaño y overflow; `Assembly-CSharp.csproj` compila con 0 errores. Se conserva pendiente la comprobación visual en el editor y en Game: no se inició Play para esta corrección.

## Validación de objetos preparados en edición — 23/10/2026

Se ejecutó Unity 6000.3.5f2 en batchmode, sin Play, para guardar los objetos que antes se construían durante la partida. Una segunda apertura de la escena comprobó referencias, scripts presentes, acciones persistentes de los botones, acciones del Input System, conexión cámara/RenderTexture, agarre del bidón, fuentes de audio, cuatro ruedas de patrulla y 64 autos de reserva con sus colliders, alturas y faros.

Se invocaron las acciones de Continuar, Controles y Volver, además de pausa, victoria y Game Over, en memoria del editor. Se verificó que el cartel de derrota no incluye el motivo. Se activaron dos autos de la reserva y se comprobó velocidad de 60 km/h con signos opuestos, altura restringida y ausencia de nuevos GameObjects. Las pruebas funcionales se hicieron después de guardar la escena; sus paneles y autos activos no se guardaron. La revisión final registró 8.842 comprobaciones, incluyendo los controles de presencia de scripts por objeto.

Se compararon 24 métodos con la versión anterior: dirección y transferencia de control VPP, recuperación del motor, adherencia, persecución, evasión, movimiento y colisiones del tráfico, temporizadores y secuencia de combustible permanecen sin cambios en sus cuerpos. `Assembly-CSharp.csproj` compila con 0 errores y 17 advertencias obsoletas de terceros. No se ejecutaron simulación física completa, audio real, reinicio por carga de escena ni recorrido jugable: esos casos de la tabla siguen pendientes.

Durante la preparación se consolidaron 768 materiales en 35, reemplazando referencias antes de retirar 733 copias idénticas generadas en esta tarea. No se eliminaron materiales originales del usuario; se conserva un respaldo temporal de las copias generadas. Los scripts temporales de migración y validación se retiraron del proyecto.

## Música y decisiones de patrulla — 24/10/2026

Se quitó el disparador de tensión asociado a los 20 segundos finales del anochecer. `AudioController` mantiene la pista detenida y en silencio hasta recibir la notificación que la patrulla emite después de iniciar su sirena; el clip comienza desde cero. Se mantienen los cambios previos del usuario en audio y HUD.

La patrulla regula su velocidad antes de la alarma, determina el sentido inicial de ruta con la velocidad real del jugador, conserva avance/reversa dentro de una zona de tolerancia y frena antes de cambiar de marcha. El planificador usa la marcha seleccionada y el objetivo previsto fuera de ruta; se redujo la resistencia a corregir el carril hacia el jugador. No se modificaron el tráfico ni los controles VPP del jugador.

Pasaron 34 comprobaciones aisladas: se extrajeron los métodos reales `PursuitSpeed` y `SelectDriveDirection` y se ejecutaron en memoria con `Math.Clamp` como equivalente de `Mathf.Clamp`. Se comprobaron incorporación, alcance, frenado al sobrepasar, límite de velocidad y estabilidad de marcha junto a 90 grados y al objetivo. Se inspeccionaron además los disparadores y el orden sirena/notificación. La compilación del gameplay termina con 0 errores y 17 advertencias obsoletas de terceros. Esto no valida todavía la conducción física, la evasión con tráfico ni la sincronización audible en Play.

Prueba jugable pendiente: llegar a los últimos 20 segundos sin activar la persecución (sin tensión), cruzar el trigger (sirena y comienzo de tensión), variar la velocidad, detenerse, salir al pasto y volver a la ruta, con tráfico alrededor. Confirmar que la patrulla recupera al jugador sin alternar marchas ni alejarse durante la incorporación.

## Atajos de VPP — 24/10/2026

Se revisaron el prefab Sport Coupe, el adaptador `UnityInput` instalado y las lecturas de comandos en la versión local de `VPStandardInput`, `VPResetVehicle` y `VPVisualEffects`. Enter estaba asignado a respawn; Shift al embrague, no al freno de mano. Los métodos de lectura contenían además cambios manuales, arranque, luces, bloqueo Ctrl+Espacio y retarder del teclado numérico.

`AutoJugador` deshabilita los componentes de reset y las teclas de luces, encendido y N/R; mantiene bloqueados embrague y cambios manuales al recuperar el control. Desvincula el eje duplicado de freno de mano para impedir Ctrl+Espacio, pero conserva Espacio mediante el `externalHandbrake` existente. Restaura ese mapa al destruir el auto. Antes del paso físico borra solamente Clutch, GearShift y Retarder: no modifica dirección, pedales, freno de mano, encendido ni selección automática. No quedan modificaciones en el SDK del asset ni se agregan scripts u objetos.

Pasaron 32 comprobaciones aisladas sobre los métodos reales con tipos sustitutos: reset, faros, mapa de freno y restauración, teclas manuales, dos transferencias de control, caja automática y limpieza exclusiva de canales auxiliares. El gameplay compila sin errores; continúan las 17 advertencias obsoletas de terceros. Pendiente en Play: comprobar los atajos tanto durante el envión inicial como al volver a subir después de cargar combustible, junto con reversa, Espacio, Shift a pie y acciones de pausa/reinicio.

## Revisión final de limpieza y renombrado — 24/10/2026

Se conservaron los GUID de los cinco scripts renombrados y se actualizaron tipos en código, escena, ocho prefabs y botones persistentes. Los nombres principales de jerarquía se comprobaron como Juego, AutoJugador, UI y Trafico. El generador mantiene su BoxCollider como trigger.

Se guardaron en la escena los bloqueos de reset, teclas de encendido/N/R/luces y cambios manuales VPP. Se quitaron esas búsquedas/configuraciones repetidas del gameplay. Solo queda el manejo interno necesario de Ctrl+Espacio y comandos auxiliares. Se eliminó reflexión de faros/telemetría usando la API pública instalada; se mantuvo la reflexión necesaria para los internos del motor VPP.

El reloj independiente de Enviro queda asignado como asset. Se comprobó en memoria que Juego utiliza ese asset y restaura el original al terminar. Los cambios de color/emisión usan MaterialPropertyBlock: la prueba del cambio visual no incrementó la cantidad de materiales cargados. Se comprobaron el encendido de la geometría original de faros y el bloqueo de telemetría. Se quitaron motivos de derrota que la UI ignoraba y se corrigieron dos erratas visibles.

Unity 6000.3.5f2, en batchmode y sin Play, registró **8.874 comprobaciones** en la última ejecución funcional: ausencia de scripts perdidos, referencias serializadas, nombres, bloqueos VPP persistentes, 64 referencias únicas e inactivas de tráfico, ocho prefabs, acciones de botones, materiales y reloj. No se guardaron los estados usados por las pruebas en memoria. Los scripts temporales de revisión se retiraron al finalizar.

La auditoría encontró 0 CinemachineBrain activos, 0 Animator activos y 3 emisores climáticos de Enviro (lluvia, salpicaduras y nieve). Esa presencia no prueba que las partículas se utilicen en el recorrido real. La cámara y las animaciones propias siguen funcionando por código, sin conversión automática durante esta limpieza.

Se compararon 21 métodos de conducción, llegada, entrada al auto, motor, IA, tráfico, repostaje, cámara y resultado contra el respaldo tomado al comenzar esta revisión, normalizando solamente los nombres de tipos. Sus cuerpos no cambiaron. La importación final sin herramientas temporales y ambas compilaciones finalizaron correctamente; permanecen las advertencias obsoletas de Nature Starter Kit.

El gameplay pasa de 4.268 a 4.191 líneas, con 17 archivos. La búsqueda en scripts propios no encuentra creación de GameObjects/componentes, Instantiate, generación de texturas/materiales/audio ni Renderer.material/materials. El alcance excluye el código interno de paquetes externos. La matriz manual anterior continúa pendiente: no se ejecutaron Play, simulación física completa, audio real, build ni recorrido de persecución en esta revisión. Los faltantes contra la consigna oficial se detallan en [ESTADO_PARCIAL_2.md](ESTADO_PARCIAL_2.md).

## Cinemachine, partículas y entorno nativo — 24/10/2026

Se integró Cinemachine 3.1.7 con un Brain y una cámara virtual con Follow, Rotation Composer y Deoccluder. `CamaraJugador` pasa de 138 a 37 líneas; sus llamadas públicas existentes transfieren el objetivo sin intervenir en VPP. Se quitaron Enviro y su reloj de la escena y de los scripts propios. El sol, Skybox Procedural, ambiente y niebla usan el reloj de `Juego`. El paquete Enviro permanece instalado como respaldo. Las texturas del terreno que todavía dependían de sus muestras se sustituyeron por el pasto de Nature Starter Kit, conservando el GUID del material.

Se ejecutó el pipeline real de Cinemachine en el editor: cambio auto/jugador/auto, traslación y giro de 90 grados del objetivo. Una pared aislada de prueba hizo pasar la cámara de `(0, 10002.77, -4.11)` a `(0, 10001.79, -1.70)`, delante del obstáculo. Se comprobó el bloqueo del Brain con victoria y derrota. La pared y los estados de prueba no se guardaron.

Los tres emisores guardados en la escena producen partículas al simularse: 8 de humo, 11 de polvo y 28 chispas en los intervalos de prueba. Se comprobó el apagado de emisión del humo y la condición real del polvo: control del auto, velocidad mayor a 10 km/h y pasto; sin emisión detenido o sobre asfalto. Las chispas se conectaron al contacto aceptado por daño; la llegada real de un choque todavía debe validarse en Play.

El proyecto usa Built-in aunque tiene instalado URP. Los materiales de partículas se prepararon con `Particles/Standard Unlit`, evitando shaders URP incompatibles. Se revisaron renders diurno, nocturno y del humo del generador obtenidos con la cámara real en editor. Los emisores, materiales y textura están preparados en edición; no se crean objetos en gameplay. La iluminación decrece gradualmente y alcanzar la ruta no reinicia el reloj ni produce derrota posterior por anochecer.

Los scripts propios quedan en 4.056 líneas y 17 archivos (135 líneas menos). La última verificación funcional registró 8.325 comprobaciones, incluyendo ausencia de scripts perdidos y de dependencias de Enviro en la escena. Las herramientas temporales se retiran al finalizar. No se ejecutaron Play, física completa ni build: siguen pendientes el recorrido, techos/paredes reales, reingreso al auto, impactos, audio y video que demuestre los efectos durante gameplay. Las pruebas y capturas del editor no se consideran una partida grabada.

La importación final sin herramientas temporales terminó correctamente. Las compilaciones de gameplay y editor finalizaron con 0 errores; permanecen las 17 advertencias conocidas de APIs obsoletas de Nature Starter Kit. La búsqueda final en scripts propios no encuentra construcción de objetos/componentes/materiales ni referencias a Enviro.

## Prueba parcial en Play y retiro de diagnóstico — 24/10/2026

La validación se interrumpió por pedido del usuario, que realizará las pruebas. Se recorrieron en Play la llegada, escalera, descenso, generador, tablero, portón y toma del bidón mediante entradas de teclado automatizadas. Se comprobó el bloqueo de movimiento durante el llenado, la derrota al agotar el generador y el reinicio con R. No se completaron repostaje, conducción posterior, IA ni colisiones con tráfico; esos casos siguen pendientes.

Se retiró de `CamaraJugador` la llamada a `CinemachineBrain.ManualUpdate`, que generaba un error en Play porque el Brain usa actualización automática. El cambio de objetivo sigue invalidando el estado anterior para que Cinemachine recalcule el encuadre. Se conservaron esta corrección y los objetos de gameplay; se retiraron las herramientas, registros y respaldos temporales de diagnóstico.

## Encuadre del retrovisor — 24/10/2026

El cálculo anterior incluía todos los Renderer del auto, por lo que los límites de las partículas podían modificar la altura y distancia del espejo. Se sustituyó por un offset fijo de `(0, 1.65, -2.7)` relativo al rumbo interpolado visible del auto, con inclinación descendente de 6 grados y FOV vertical de 35 grados. Se conservan la posición y tamaño del panel y su inversión horizontal de espejo. La compilación finalizó con 0 errores y las 17 advertencias conocidas del paquete de vegetación. No se entró en Play; el usuario validará el encuadre durante la persecución y al circular por pasto.

## Navegación y conducción de patrulla — 24/10/2026

Se revisó el video de 44 segundos `Desktop 2026.10.24 - 19.52.00.01.mp4`. La patrulla se aproxima, pierde seguimiento al desplazarse el jugador hacia la banquina y el recorrido termina por tiempo. Ese video evidencia un fallo de la versión anterior; no valida la nueva.

Se consultaron Unidad 2, páginas 18–31 (NavMesh, superficies, agentes y obstáculos), Clase 5, páginas 4–8 (fuerzas y FixedUpdate), y Clase 6, páginas 7–8 (KISS y herramientas integradas). Se revisaron [CarAI-Unity de Ahmed Saed, MIT](https://github.com/Ahmedsaed/CarAI-Unity) y el [tutorial oficial de WheelColliders](https://docs.unity3d.com/6000.0/Documentation/Manual/WheelColliderTutorial.html). La ficha de Asset Store enlazada por CarAI no resultó accesible; el código y su licencia se consultaron en GitHub. No se importó un paquete sin comprobarlo ni se incorporaron modelos ajenos.

Se sustituyó el planificador de corredores por AI Navigation. La escena guarda `NavegacionPatrulla`, su NavMeshSurface, el asset RutaPatrulla y el agente Patrulla. El asfalto tiene costo 1 y el pasto 2,5. La superficie cubre el terreno completo, aproximadamente 2.908 × 17.048 m. Los vehículos y el personaje se excluyen del horneado; los 64 autos de tráfico tienen NavMeshObstacle con carving móvil. Se desactivaron los cuatro WheelColliders originales adicionales de la patrulla y se mantienen únicamente sus cuatro ruedas de conducción asignadas. La lógica de movimiento del tráfico y el VPP del jugador no se cambiaron.

La incorporación usa un punto fijo y espera al vehículo que se aproxima; al pasar el jugador comienza el alcance, evitando una vuelta prematura hacia el auto que todavía viene detrás. El control de velocidad contempla curvas y frenada. La marcha cambia después de frenar, con una zona de tolerancia, y la recuperación comprueba espacio libre antes de retroceder. La navegación no mueve el Transform del auto: conduce mediante las ruedas físicas.

Se ejecutaron ocho escenarios de 25 segundos con PhysX y los métodos reales de la patrulla en edición, sin Play. El objetivo fue un Rigidbody con trayectoria impuesta y sin colisión con la patrulla, para medir seguimiento sin finalizar por captura. Se forzó la renovación del plan cada diez pasos y se descontó manualmente el tiempo de recuperación, porque Physics.Simulate en edición no avanza el reloj de gameplay. Se usaron pasos de 0,02 s.

| Escenario aislado | Resultado medido al finalizar |
| --- | --- |
| Objetivo en recta a 18 m/s | Separación 2,60 m; patrulla a 18,09 m/s |
| Objetivo frena y se detiene | Patrulla vuelve al objetivo y queda a 2,02 m, casi detenida |
| Objetivo se desplaza 24 m al pasto | Lo sigue y termina a 3,23 m |
| Objetivo detenido detrás | Recuperación a 2,03 m; patrulla casi detenida |
| Salida desde el escondite real | Alcanza al objetivo; separación final 2,65 m |
| Incorporación sin alarma | Avanza unos 8,8 m y espera detenida; no continúa alejándose |
| Alarma mientras el jugador aún se aproxima | Espera el paso, arranca y termina a 2,61 m |
| Jugador frena antes de pasar | Vuelve hacia su posición y termina a 2,03 m |

También se verificó un desvío alrededor de un auto detenido: el NavMesh generó cuatro esquinas, la patrulla se apartó 3,48 m y volvió al seguimiento con separación final de 2,59 m. Las comprobaciones de penetración entre colliders registraron cero solapamientos; durante el paso junto a la carrocería quedó un margen lateral mínimo de 0,68 m entre sus AABB. La prueba no certifica todos los contactos posibles con tráfico en movimiento.

No se ejecutó una partida completa ni se validaron simultáneamente música, captura, reloj, tráfico móvil y maniobras imprevistas. El usuario realiza esa validación. Se retiran las herramientas y archivos temporales tras guardar la navegación; no se agregan scripts de gameplay. Si se cambian obstáculos estáticos, volver a hornear NavegacionPatrulla en Unity.

La importación final, ya sin la herramienta temporal, y las compilaciones de gameplay/editor finalizaron con 0 errores. Permanecen las 17 advertencias conocidas de Nature Starter Kit. El controlador de patrulla queda en 557 líneas y el proyecto conserva sus 17 scripts de gameplay.

## Tirón al iniciar la sirena — 24/10/2026

La inspección del importador real de Unity encontró `preloadAudioData=false` y `loadInBackground=false` tanto en tension.mp3 como en sirena_patrulla.mp3. La sirena conservaba un metadato antiguo con la precarga en otra ubicación; Unity la interpretaba como desactivada. Ambos clips usan Decompress On Load. La música dura 87,04 segundos y ocupa 30.707.712 bytes descomprimida; la sirena dura 6,06 segundos y ocupa 2.138.112 bytes.

En edición, sin Play y sin reproducir audio, se descargaron los datos y se midió LoadAudioData: la llamada síncrona de la música bloqueó 164,29 ms y la de sirena 6,26 ms. Es una causa comprobada de tirón; no es una captura del tiempo total del frame de persecución. Se activaron Preload Audio Data y Load In Background con AudioImporter.SaveAndReimport, conservando compresión, calidad, canales, clips y referencias. Tras reimportar, el importador confirmó ambos indicadores activos y ambos clips llegaron a Loaded. La petición posterior con precarga solicitada retornó en 0,011 y 0,008 ms respectivamente; esas cifras no representan la duración de la decodificación en segundo plano.

La corrección usa los ajustes nativos descritos en la [documentación de importación de audio de Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html). No reproduce la música ni la sirena por anticipado. Se conserva el evento de tensión ligado al inicio de la sirena. Las balizas ya tenían sombras desactivadas; no se modificaron iluminación, cámaras, navegación, controles ni escena. La herramienta de medición y su salida temporal se retiran. Falta verificar en una partida real si hay otros picos simultáneos de renderizado o navegación.

## Incorporación, Animator y continuidad narrativa — 25/10/2026

Se confirmó con el usuario que la patrulla debe incorporarse paralela al tránsito. Su orientación guardada ya era paralela, pero el punto de incorporación avanzaba apenas 8 m para corregir unos 5 m laterales, y la frenada por proximidad la dejaba cruzada. Se amplió a 35 m y se separó la corrección de carril/rumbo de la incorporación del seguimiento NavMesh posterior. La maniobra termina alineada, no únicamente cerca de un punto. Si necesita corregir el último ángulo, avanza despacio hasta alinearse; no rota ni teletransporta el Rigidbody.

La prueba con un obstáculo estático que representa un auto reveló además frenada tardía y activación de la reversa al esperar. Se cambió la distancia de frenado para calcularla con 4 m/s² en lugar de 8 y se exige velocidad deseada mayor a 1 m/s antes de declarar atasco. Esperar deliberadamente frente a tráfico no dispara esa recuperación. Se conserva la lógica de movimiento de los autos de tráfico.

Se migraron las cuatro poses del personaje a `Assets/PreparedGameplay/Animaciones/`: Reposo, Caminar, Colgado y Caida, enlazadas en Jugador.controller. El Animator está guardado sobre Jugador, no tiene root motion y usa Estado/Ritmo. Caminar contiene los eventos de pasos. El script conserva movimiento, caída e interacción, pero deja de calcular la pose de cada extremidad. Queda en 181 líneas y no se agrega un script de gameplay. Las curvas constantes se redujeron a dos claves. Se comprobó en un grafo de animación manual, en edición, que los cuatro estados se activan, que Caminar mueve el brazo y que la raíz no cambia de posición.

La comparación de todos los bloques Transform anteriores y posteriores a la migración no encontró cambios. Se conservaron las posiciones, rotaciones y escalas guardadas por el usuario. Los colliders del auto ya tenían el tag Player; se verificaron y no se atribuye el tirón de cámara a un tag faltante. El Rigidbody del auto tiene interpolación activa. Se configuró el Brain en LateUpdate y Deoccluder con SmoothingTime 0,2, Damping 0,5 y DampingWhenOccluded 0,08, manteniendo el zoom ante paredes y el encuadre. Esto es un ajuste de configuración pendiente de comprobación visual durante la conducción; no demuestra por sí solo que todos los sacudones hayan desaparecido.

EditorSettings.asyncShaderCompilation está activo. La descripción del cian temporal coincide con el shader provisional documentado por Unity durante la [compilación asíncrona en el editor](https://docs.unity3d.com/6000.0/Documentation/Manual/AsynchronousShaderCompilation-introduction.html). No se deshabilitó esa compilación ni se reemplazaron los materiales: desactivarla puede bloquear el editor al preparar variantes. Debe verificarse una build para distinguir este comportamiento del editor de cualquier defecto visual adicional.

La consigna exige Sistema de Animación, sin pedir Animator por nombre. Se actualizó el estado del parcial para reflejar la integración nativa. Se reutiliza el objetivo temporal para mostrar una victoria aparente al llegar a la ruta sin pausar ni marcar finished. La emboscada reemplaza ese cartel y el reloj existente de persecución se rotula RESERVA. La explicación narrativa de los 60 segundos se muestra al comenzar la alarma; los objetivos anteriores no revelan a la patrulla. No se agregó un reloj, una pantalla ni una secuencia de pausa.

Las comprobaciones se ejecutan en edición y simulación aislada de PhysX, sin entrar en Play. La validación del recorrido, tráfico móvil simultáneo, animaciones con bidón, cámara y sorpresa narrativa sigue a cargo del usuario. Las herramientas y archivos temporales se retiran al terminar.

La última ejecución, con la configuración final, terminó correctamente: incorporación libre con error lateral 0,26 m y ángulo 2,995 grados, sin cruzar al carril contrario. Tras poner el objetivo 30 m delante y activar el seguimiento, la patrulla volvió a acelerar hasta 11,01 m/s en 1,2 s. Con el bloque estático de 2,1 × 1,5 × 4,5 m se registraron cero solapamientos de carrocería; la patrulla esperó detenida. Al retirarlo terminó la incorporación con ángulo 2,14 grados. Las rutas se renovaron cada diez pasos y se usó Physics.Simulate de 0,02 s; no representan una partida con tráfico móvil ni una medición visual de la cámara.

La llamada real a ReachRoad dejó roadReached=true, finished=false y Time.timeScale=1: el cartel de victoria aparente no congela la partida. Los cuatro estados del grafo Animator se activaron sin mover la raíz del personaje.

La importación final sin la herramienta temporal terminó con código 0. Compilaciones finales: gameplay, 0 errores y las 17 advertencias conocidas de Nature Starter Kit; editor, 0 errores y 0 advertencias. El proyecto conserva 17 scripts de gameplay y una herramienta permanente de editor.

## Video de animación, alcance, polvo y tráfico — 25/10/2026

Se revisó `Desktop 2026.10.25 - 03.29.12.03.mp4`, de 139,62 segundos, mediante muestras cada 8 segundos y una captura de mayor resolución alrededor de 1:20. Se observa la secuencia a pie, conducción y persecución, con vehículos que quedan en contacto/intersección en la ruta. El video anterior no valida los cambios siguientes.

Se redujo Caminar a velocidad base 0,6 y Reposo a 0,75. Un grafo Animator en edición confirmó reproducción 0,6 al caminar y 1,08 con Ritmo 1,8 al correr; antes eran 1 y 1,8. Se conservan movimiento y eventos de pasos.

El límite de alcance de la patrulla calculaba la frenada usando la velocidad absoluta del objetivo dentro de la raíz cuadrada, lo que reducía mucho la velocidad de cierre cuando ambos iban rápido. Ahora limita la velocidad relativa. Se toma el avance del objetivo sobre el rumbo de la patrulla, evitando que un desplazamiento lateral reduzca artificialmente esa lectura a cero. El cierre cercano se limita suavemente, manteniendo la dirección y los sensores existentes.

La polvareda pasó a espacio mundial, con vida de 2,1–3 s, mayor tamaño, crecimiento, dispersión, alfa suave y emisión según distancia. La simulación estacionaria de 2,5 s produjo 73 partículas y tamaño máximo de 3,11 m; la emisión por distancia agrega 6 partículas por metro al circular. El máximo queda en 900. Se prepararon dos emisores de escape sobre AutoJugador; la prueba de encendido produjo 14 partículas por emisor a 0,8 s, y al apagar y avanzar la simulación 2 s quedaron en cero. No se genera ningún emisor durante la partida. Se revisó un render de editor con efectos activados expresamente para evaluar volumen y posición; esa captura sobre asfalto no representa la condición de emisión de gameplay.

Los 64 vehículos de reserva tienen un único Rigidbody, sin WheelColliders nativos y con bloqueo vertical/rotaciones X-Z. Por ello no se atribuyó el problema a una segunda simulación VPP. Se sustituyó MoveRotation por torque limitado y se redujo la velocidad de despenetración a 1 m/s. Se renueva la cesión mientras hay contacto con tráfico por delante; un contacto recibido por detrás no obliga al líder a detenerse. El sensor deja de descartar superficies inclinadas de vehículos como si fueran suelo. La activación de tráfico comprueba espacio libre antes de consumir una entrada de la reserva.

En PhysX, sin Play y con pasos de 0,02 s, una fila de tres vehículos durante 15 s terminó detenida detrás del líder, sin solapamientos mayores a 5 mm ni variación de altura significativa (menos de 0,000001 m). Un coupé y una camioneta a 60 km/h en sentidos opuestos pasaron sin solapamientos en 7 s. Una activación sobre otro auto fue rechazada sin avanzar el índice de la reserva. Estos casos comprueban sensores, movimiento y prevención de aparición sobre autos; no certifican todos los impactos con el jugador ni la entrega de callbacks de contacto en una partida real. La verificación de choques y estabilidad con todos los sistemas activos sigue pendiente del usuario.

En la configuración final, a 30 m/s del objetivo, los márgenes de cierre calculados fueron 1,95 m/s a 5 m de separación entre centros, 3 m/s a 10 m y 4 m/s a 25 m. La emisión aleatoria de polvo produjo entre 70 y 73 partículas estacionarias y tamaños máximos de 2,84–3,11 m en las dos ejecuciones. Estas mediciones no son velocidades de impacto ni resultados de una persecución completa.

La importación final sin herramientas temporales y ambas compilaciones terminaron con 0 errores. Persisten las 17 advertencias conocidas de Nature Starter Kit en gameplay; editor sin advertencias. Se retiraron las capturas, el respaldo y las herramientas temporales. Se mantienen 17 scripts de gameplay.

## Navegación desactivada — 24/10/2026

Se revisó el video de 30,65 segundos `Desktop 2026.10.24 - 22.12.53.02.mp4`: se activan sirena y HUD, pero la patrulla permanece junto al escondite. En la escena guardada se encontró el NavMeshSurface de NavegacionPatrulla con `m_Enabled: 0`. El código instalado de Unity confirma que OnDisable retira los datos de navegación; sin ruta, la patrulla aplicaba freno.

Se reactivó la superficie y se enlazó explícitamente a PatrulleroIA. Awake habilita el objeto y su superficie, con búsqueda de respaldo para una escena que todavía no tenga serializada esa referencia. El ajuste de espera del editor oculta solamente el dibujo del NavMesh al abrir la escena o salir de Play, manteniendo activa la navegación. La propiedad de visualización es interna en esta versión de Unity, por lo que se consulta por reflexión únicamente desde el script de editor.

Se verificaron el enlace serializado y la superficie habilitada. Gameplay y editor compilan con 0 errores; se mantienen las advertencias conocidas del paquete de vegetación. No se ejecutó una nueva partida: falta comprobar esta corrección durante la persecución real.

## Aceleración, limpieza y shaders — 26/10/2026

Con Unity cerrado y sin Play se comprobó la escena guardada: cero scripts perdidos, 64 autos en la reserva con sus cuatro modelos de rueda visibles y sin controladores VPP de tráfico. La patrulla conserva cuatro ruedas físicas y sus modelos. Los cuatro clips del personaje resuelven todas las rutas después de renombrar PlayerModel a ModeloJugador. Se conservan todos los transform explícitos supervivientes de la escena guardada por el usuario, incluidas las posiciones de los escapes.

Se eliminó el tope recto involuntario de sqrt(9 / 0,006), aproximadamente 139 km/h, y el frenado doble por esquina. Subir solo el torque no mejoró el alcance: la rueda patinaba. El corte de torque por deslizamiento longitudinal mayor o igual a 0,4 y la rigidez longitudinal 1,7 sí redujeron la separación máxima. Se ensayó un objetivo que comienza 60 m delante, con velocidad constante y sin colisionar con él. La patrulla arranca detenida y replantea cada 0,2 s. PhysX avanza en pasos de 0,02 s.

| Objetivo | Separación máxima entre centros | Tiempo hasta menos de 5,5 m |
| --- | --- | --- |
| 144 km/h, recta | 120,82 m | 19,04 s |
| 180 km/h, recta | 144,57 m | 21,22 s |
| 198 km/h, recta | 160,83 m | 22,72 s |
| 108 km/h, desvío lateral de 24 m al pasto | 91,89 m | 19,02 s |

La incorporación desde la posición real del escondite terminó con error lateral de 0,45 m y ángulo de 2,91 grados respecto a la ruta. En un ensayo adicional con un auto detenido, la ruta consultada permaneció recta: la patrulla frenó antes del auto, sin penetración, pero no completó el adelantamiento. Por ello **la evasión de tráfico no se da por validada**. Debe verificarse durante gameplay junto con la actualización de los obstáculos de navegación, choques y recuperación. Los ensayos de alcance no representan tráfico simultáneo ni velocidades de impacto.

El acceso directo al agarre público de VPP produjo multiplicador 1 en asfalto, 0,45 en pasto y restauración a 1; se recuperó también el clip original de arranque. La polvareda estacionaria a 3 s produjo 144 partículas y tamaño máximo de 4,59 m. Se revisó un render fuera de pantalla con el emisor activado expresamente: trail marrón y mayor volumen, sin material cian. No equivale a una partida grabada.

Se renderizaron 20 vistas para registrar y precalentar 31 variantes de 11 shaders. La colección está referenciada por Graphics Settings y se deshabilitó el shader provisional asíncrono del editor. Esto trata una causa comprobable del cian; no descarta otros defectos visuales que solo aparezcan durante la partida. Referencias: [compilación asíncrona](https://docs.unity3d.com/6000.0/Documentation/Manual/AsynchronousShaderCompilation-introduction.html), [colecciones de variantes](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-variant-collections.html) y [deslizamiento de WheelCollider](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/WheelHit-forwardSlip.html).

La importación final sin herramienta temporal terminó con código 0. Gameplay compiló con 0 errores y las 17 advertencias conocidas de Nature Starter Kit; editor, con 0 errores y 0 advertencias. Siguen 17 scripts propios y una herramienta permanente de editor. Se retiraron el diagnóstico, respaldos, capturas y registros temporales. Las posiciones guardadas por el usuario se incluyeron en la escena conservada.


## Persecución del video 01:12:41 — 26/10/2026

Se revisó `Desktop 2026.10.26 - 01.12.41.02.mp4` (40,37 s), con muestras generales, detalle de la persecución y una captura completa. Se observa incorporación de patrulla, salida del jugador al pasto, retorno a la ruta y ventaja creciente. La simulación anterior que empezaba con ambos autos ya colocados en un tramo lejano no representaba esa secuencia.

El diagnóstico nuevo abre la escena guardada en cada caso. La patrulla parte de su escondite; el objetivo comienza 220 m atrás en el carril de avance. Se usa la cota real de Persecucion (z mínimo 431,9) para activar el estado de alarma. PhysX avanza a 0,02 s hasta 35 s después de ese punto. Se desactiva la interpolación solo en la simulación sin render, para leer poses físicas por paso; no cambia la configuración guardada. La maniobra de pasto desplaza al objetivo unos 15 m lateralmente y luego lo devuelve al carril. El tráfico usa dos coupés y dos camionetas de la reserva, a 60 km/h, distribuidos entre ambos sentidos.

| Objetivo y caso | Separación longitudinal máxima | Alcance a menos de 5,5 m después de los primeros 4 s |
| --- | --- | --- |
| 144 km/h, recta | 5,19 m | 4,02 s |
| 198 km/h, recta | 15,83 m | 7,38 s |
| 198 km/h, pasto y regreso | 84,02 m | 19,86 s |
| 144 km/h, tráfico | 19,46 m | 4,02 s |
| 198 km/h, pasto, regreso y tráfico | 157,41 m | 28,70 s |
| 234 km/h, recta | 31,06 m | 11,60 s |

Los seis casos terminaron sin llegar a los 200 m y cumplieron las aserciones de alcance, penetración máxima menor a 3 cm y desplazamiento por paso menor a 2 m. La penetración medida contra tráfico fue cero. El mayor desplazamiento registrado fue 1,40 m en 20 ms, coherente con la velocidad física. Se siguen simulando los casos después del alcance para observar estabilidad; en gameplay una captura válida termina la partida.

Estas pruebas usan un objetivo de velocidad impuesta y sin colliders, no un conductor VPP ni la entrega completa de callbacks y condiciones de resultado. Comprueban seguimiento, maniobra física y separación en los escenarios descritos; no certifican una partida completa, todas las variantes de tráfico ni un bloqueo total. Se conservan pendientes la validación manual con controles reales y la build de entrega.

Se consultaron de nuevo [NavMeshObstacle de Unity](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshObstacle.html) y [CarAI-Unity](https://github.com/Ahmedsaed/CarAI-Unity). No se importó otro controlador ni se copió un paquete. La corrección usa los componentes existentes y las consultas físicas de Unity.

La importación final sin diagnóstico temporal terminó con código 0. Compilaron gameplay y editor con 0 errores; gameplay mantiene las 17 advertencias conocidas de Nature Starter Kit y editor no presenta advertencias. Se retiraron la herramienta, sus registros y las capturas temporales. La escena no cambió y el ajuste previo del usuario en CieloRuta.mat se conserva aparte del cambio de IA.
