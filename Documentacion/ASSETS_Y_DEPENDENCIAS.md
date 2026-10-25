# Assets y dependencias

## Contenido que permanece en el proyecto

| Recurso | Uso dentro de la escena |
| --- | --- |
| Vehicle Physics Pro | Física, transmisión automática y audio del vehículo |
| Unity Built-in | Skybox Procedural, sol, niebla y anochecer gradual |
| Cinemachine 3.1.7 | Seguimiento, encuadre y resolución de paredes de la cámara principal |
| AI Navigation 2.0.9 | NavMesh de la patrulla y obstáculos del tráfico; navegación guardada en edición |
| Particle System de Unity | Humo del generador, polvo sobre pasto y chispas de impactos |
| Nature Starter Kit 2 | Árboles, vegetación y materiales del terreno |
| Fuel Pump 3D | Modelo visible del surtidor; se conserva su prefab, malla, materiales y texturas requeridos |
| Modelos externos | Generador portátil, bidón, interruptor y tablero usados por los prefabs/escena |
| `Assets/Audio` | Música, ambiente, efectos e `EnElCamino_Mixer` |

## Limpieza realizada

- Se quitaron las escenas demo, guía, perfil de postprocesado, modelo de colina y materiales de suelo que pertenecían exclusivamente a la demostración de Fuel Pump 3D.
- Se eliminaron nueve herramientas de editor de una sola ejecución que cambiaban la escena, materiales, audio, portón o surtidor al abrir Unity. Estos cambios ya están guardados en la escena; mantener las herramientas hacía que una prueba pudiera alterar otra.
- Se retiraron el auto y árboles de Kenney, junto con un bidón y un interruptor alternativos sin referencias. Se conserva el poste curvo de Kenney porque está instanciado en la escena.
- Se mantiene `DaylightStandbySetup` porque su única responsabilidad es mostrar una hora diurna estable cuando la escena está detenida en el editor.

## Dependencias no versionadas

Por licencia y tamaño, las carpetas de Vehicle Physics Pro, Enviro 3 y Nature Starter Kit 2 están ignoradas por Git. La escena actual usa VPP y Nature Starter Kit 2, que deben importarse desde una licencia válida. Enviro fue retirado de la escena y de los scripts propios; el paquete instalado no se borró y queda disponible localmente para restaurar la versión anterior. No se deben copiar paquetes comerciales a GitHub.

Los recursos propios nuevos están en `Assets/PreparedGameplay/Entorno/CieloRuta.mat` y `Assets/PreparedGameplay/Efectos` (textura suave y materiales de partículas Built-in). Los cinco emisores están guardados en la jerarquía: `GeneradorAuxiliar/HumoGenerador`, `AutoJugador/PolvoPasto`, `AutoJugador/ChispasImpacto`, `AutoJugador/HumoEscapeIzquierdo` y `AutoJugador/HumoEscapeDerecho`. El antiguo `RelojPartida.asset` de Enviro se retiró porque ya no tiene uso.

`Assets/PreparedGameplay/Animaciones/Jugador.controller` y sus cuatro clips contienen las poses Reposo, Caminar, Colgado y Caida del personaje. Se editan desde Animator/Animation y conservan los eventos de pasos; no requieren otro paquete de animación.

El material del terreno pasa a `Assets/Materials/PastoRuta.mat`, conservando su GUID. Utiliza `ground01.tga` de Nature Starter Kit 2; se retiraron las dos texturas de muestra de Enviro que todavía referenciaba. No se copiaron texturas comerciales a recursos propios ni se alteró la física del suelo.

Los modelos externos que sí están en `Assets/ThirdParty/ExternalModels` conservan su archivo de licencias/créditos asociado.

`Assets/PreparedGameplay/Navegacion/RutaPatrulla.asset` guarda la navegación de `NavegacionPatrulla`. El tipo de agente `Patrulla` y el área `Pasto` están configurados en Navigation. Los obstáculos del tráfico y exclusiones de vehículos/personaje del horneado quedan en la escena. Si se cambia la posición de árboles, paredones o suelo, volver a hornear ese NavMeshSurface desde Unity.

Referencia técnica consultada: [CarAI-Unity, Ahmed Saed](https://github.com/Ahmedsaed/CarAI-Unity), licencia MIT. No se incorporó el paquete completo ni se copiaron sus modelos: se utilizó como referencia para separar búsqueda de rutas y ruedas físicas. También se revisaron las secciones NavMesh de Unidad 2 y física/FixedUpdate de Clase 5.

La escena actual también referencia **Loader Heavy Machinery**, Asset Store ID **31696**, versión local **1.2.4**, en `Assets/_DLNK/HeavyMachinery`. Importar el paquete autorizado en esa ruta para conservar la referencia al prefab `HeavyMachinery_Stopped b`. Su contenido comercial y las copias automáticas de `Assets/_Recovery` permanecen locales y no se publican en GitHub.

## Convenciones de repositorio

- Escena productiva: `Assets/Scenes/EnElCamino.unity`.
- Código de gameplay: `Assets/Scripts`.
- Materiales propios: `Assets/Materials`.
- Audio y mixer: `Assets/Audio`.
- Documentación de entrega: `Documentacion`.
- No versionar: `Library`, `Temp`, `Logs`, `.vs`, builds locales ni paquetes comerciales.

