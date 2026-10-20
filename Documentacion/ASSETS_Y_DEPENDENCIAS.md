# Assets y dependencias

## Contenido que permanece en el proyecto

| Recurso | Uso dentro de la escena |
| --- | --- |
| Vehicle Physics Pro | Física, transmisión automática y audio del vehículo |
| Enviro 3 — Sky and Weather | Ciclo de tarde a noche, cielo e iluminación |
| Nature Starter Kit 2 | Árboles, vegetación y materiales del terreno |
| Fuel Pump 3D | Modelo visible del surtidor; se conserva su prefab, malla, materiales y texturas requeridos |
| Modelos externos | Generador portátil, bidón, interruptor y tablero usados por los prefabs/escena |
| `Assets/Audio` | Música, ambiente, efectos e `EnElCamino_Mixer` |

## Limpieza realizada

- Se quitaron las escenas demo, guía, perfil de postprocesado, modelo de colina y materiales de suelo que pertenecían exclusivamente a la demostración de Fuel Pump 3D.
- Se eliminaron nueve herramientas de editor de una sola ejecución que cambiaban la escena, materiales, audio, portón o surtidor al abrir Unity. Estos cambios ya están guardados en la escena; mantener las herramientas hacía que una prueba pudiera alterar otra.
- Se mantiene `DaylightStandbySetup` porque su única responsabilidad es mostrar una hora diurna estable cuando la escena está detenida en el editor.

## Dependencias no versionadas

Por licencia y tamaño, las carpetas de Vehicle Physics Pro, Enviro 3 y Nature Starter Kit 2 están ignoradas por Git. Cada equipo que clone el repositorio debe importarlas desde una licencia válida del Asset Store antes de abrir la escena. No se deben copiar sus archivos a GitHub.

Los modelos externos que sí están en `Assets/ThirdParty/ExternalModels` conservan su archivo de licencias/créditos asociado.

## Convenciones de repositorio

- Escena productiva: `Assets/Scenes/EnElCamino.unity`.
- Código de gameplay: `Assets/Scripts`.
- Materiales propios: `Assets/Materials`.
- Audio y mixer: `Assets/Audio`.
- Documentación de entrega: `Documentacion`.
- No versionar: `Library`, `Temp`, `Logs`, `.vs`, builds locales ni paquetes comerciales.

