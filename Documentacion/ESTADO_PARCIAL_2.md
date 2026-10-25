# Estado contra la consigna del Segundo Parcial

Revisión: 25/10/2026. Fuente: [consigna oficial aportada por el usuario](Consigna_Parcial2.txt), originalmente en `C:/Users/El Barto/Desktop/PARCIAL 2.txt`.

Se distingue presencia en código/escena de funcionamiento verificado en una partida. Este informe no certifica la aprobación.

## Resumen

El proyecto tiene recorrido estación-combustible-ruta-persecución, Cinemachine, partículas de gameplay, UI, audio, luces, daño, tráfico, prefabs y eventos. **No está listo para entregar:** falta validar el recorrido completo y la IA en juego, y cerrar los entregables. Debe aclararse el criterio de instanciación por la decisión de preparar los autos en edición.

## Sistemas base

| Punto de la consigna | Evidencia actual | Estado |
| --- | --- | --- |
| Organización | Carpetas propias, 17 scripts y mapa de responsabilidades | Implementado; revisar entrega limpia |
| GitHub y commits | Repositorio, rama `parcial-2` e historial de cambios | Cambios actuales pendientes de commit/push; colaboración no certificada |
| High Concept actualizado | GDD: robo del auto, estación y escape policial | Documentado |
| Movimiento | CharacterController, carrera y descenso | Implementado; prueba final pendiente |
| Física y colisiones | VPP, WheelColliders y Rigidbody del tráfico | Implementado; colisiones/manejo requieren prueba |
| Triggers y eventos | `CheckPoint`, `Juego` e interacciones | Implementado |
| Cámara funcional | Cinemachine sigue jugador/auto y se acerca ante paredes | Integrado; cambio de objetivo, giro y pared comprobados en editor; recorrido pendiente |
| Prefabs | Vehículos y ocho variantes propias de tráfico | Implementado |
| Raycast | Interacciones, cámara y sensores | Implementado |
| Daño | Impactos, HUD temporal y derrota a 100 | Implementado; prueba pendiente |
| Instanciación de objetos | Prefabs instanciados en edición y reserva de 64 autos; sin Instantiate en gameplay | Aclarar si el docente exige instanciar durante la partida; no se añadió contra el pedido de preparar todo en Unity |

## Evolución del escenario

| Punto | Evidencia | Estado |
| --- | --- | --- |
| Escenario avanzado | Estación, taller, techo, ruta, obstáculos, vegetación y patrulla | Presente; cierre visual/recorrido pendientes |
| Materiales y texturas | Entorno, vehículos y materiales propios | Presente |
| Terreno | Suelo, vegetación e irregularidad/agarre del pasto | Presente; verificar aspecto y conducción |
| Path Route y progresión | Taller, energía, bidón, combustible, ruta y persecución | Documentado; verificar que el recorrido sea legible |
| Cohesión y feedback | Ambientación rural, objetivos, sonido y luces | Revisión visual final pendiente |

## Sistemas obligatorios integrados

| Sistema | Evidencia | Estado |
| --- | --- | --- |
| Animación | Animator del personaje: Reposo, Caminar, Colgado y Caida; eventos de pasos | **Integrado** con clips guardados y sin root motion; cuatro estados comprobados en edición. La consigna no exige Animator por nombre. Validación jugable pendiente |
| Audio, música y ambiente | Mixer, pasos, motor, generador, surtidor, portón, sirena y música | Integrado; comprobar audio y mezcla |
| UI | HUD, objetivos, tiempo, daño, espejo, pausa, controles y reinicio | Integrado; probar resolución y botones en build |
| Cinemachine | Brain en la cámara principal; cámara virtual con Follow, Rotation Composer y Deoccluder | **Integrado y comprobado en editor**; prueba jugable final pendiente |
| Iluminación | Sol, Skybox Procedural, niebla, postes, tablero, faros y balizas | Enviro reemplazado por Unity nativo; transición y capturas diurna/nocturna comprobadas, recorrido pendiente |
| IA | Patrulla con NavMesh, WheelColliders y sensores; tráfico con obstáculos de navegación | Integrado; pruebas aisladas de seguimiento físico y desvío. Validar partida completa y tráfico en movimiento |
| Partículas | Humo del generador y ambos escapes; polvareda sobre pasto; chispas por impactos | **Integrado**; emisión/simulación y apagado de escapes comprobados; registrar efectos en video de gameplay |
| Eventos y secuencias | Llegada, combustible, energía, portón, repostaje, emboscada y resultados | Integrado; prueba completa pendiente |

La consigna exige IA, pero **no exige NavMesh expresamente**. No se marca faltante solo por usar WheelColliders y sensores.

## Desafío final

La persecución es la escape sequence integradora: sirena, música, tráfico, física, daño, HUD y resultados. Se gana al sacar 200 m y se pierde por captura, daño o agotar 60 segundos. La consigna permite un escape; no hace falta agregar un jefe. Falta comprobar victoria y derrota coherentes y reproducibles.

## Entregables

| Entrega | Pendiente |
| --- | --- |
| Video de máximo 5 minutos | Presentación con nombre/rol, evolución, gameplay, mapa/ruta, sistemas, desafío y explicación técnica; audio claro |
| itch.io y/o ejecutable | No se verificó una build final ni enlaces públicos. Preparar Windows/WebGL y ficha con Segundo Parcial, controles, objetivo, bugs y créditos |
| Publicación institucional | La sección de forma de entrega también menciona itch.io; confirmar canal final y subir los enlaces requeridos |
| GitHub | Implementación organizada en commits por tema en `parcial-2`; verificar acceso al repositorio y clonación con las dependencias indicadas |
| GDD | Existe y se actualizó. Completar plano/capturas del mapa y referencias visuales concretas |
| Testing | Existe; ejecutar casos pendientes y registrar resultados y feedback real |
| Más de 100 MB | Si aplica, adjuntar TXT con enlace público a Drive según la consigna |

## Prioridades

1. Probar Cinemachine en el recorrido completo, incluyendo techo, paredes y reingreso al auto.
2. Probar recorrido, repostaje/reingreso, reversa, atajos, tráfico y persecución.
3. Grabar humo del generador, polvo en pasto y chispas en impactos durante una partida. No confundir las capturas de editor con un recorrido jugable validado.
4. Aclarar instanciación frente a la reserva preparada en edición.
5. Cerrar build, video, mapa/referencias, testing, publicación y enlaces.

## Limpieza técnica

Los cinco scripts solicitados y sus objetos principales están renombrados, conservando GUID y referencias. Los objetos, cámara Cinemachine y emisores están preparados en Unity. El anochecer usa herramientas nativas; no queda Enviro en la escena ni en scripts propios. Los scripts propios no crean GameObjects, componentes, cámaras, texturas, materiales ni audio durante la partida. Color/emisión usan MaterialPropertyBlock sobre materiales existentes. Los paquetes externos conservan su implementación interna.
