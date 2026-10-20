# Informe de testing — Segundo Parcial

## Versión

Vertical slice de “En el Camino”, rama `parcial-2`.

## Pruebas realizadas

- El proyecto abre en Unity `6000.3.5f2` y la escena `EnElCamino` carga correctamente.
- El Editor entra y sale de Play Mode sin errores de compilación.
- El HUD muestra objetivos, mensajes y temporizador al iniciar.
- Cinemachine genera la cámara de gameplay y mantiene el encuadre del jugador.
- El flujo de eventos puede avanzar desde el generador hasta el arranque del vehículo.
- El desafío actual combina 60 segundos de combustible del generador con la salida antes de que anochezca. La persecución fue retirada por decisión de diseño; el temporizador no constituye por sí mismo un sistema de IA.
- Se verificaron las referencias de UI durante Play Mode.
- Persisten advertencias de APIs obsoletas del Nature Starter Kit y código de editor sin uso; esta revisión no equivale a una build validada.

## Regresión previa de portón y audio VPP — 20/10/2026

La siguiente prueba automatizada se ejecutó antes del rediseño de la bajada. Sus resultados de escalera no validan el flujo actual de colgarse y descender. Se retiró del menú de Unity la entrada personalizada que iniciaba la prueba.

- PASS: señal de audio no nula del motor durante la llegada (`AudioSource.GetOutputData`).
- PASS: el clip reproducido pertenece al paquete: `Assets/Vehicle Physics Pro/Sample Assets/Art/Audio/8000-clickfix.wav`.
- No aplica al flujo actual: la prueba antigua validaba tomar y colocar la escalera; ahora no se recoge ni se mueve la escalera.
- PASS: pulsar la tecla sin conectar la estación no la deja trabada.
- PASS: el selector de interacción elige la caja eléctrica; usarla enchufa el generador y da energía a la estación.
- PASS: el tiempo de combustible queda en 60 segundos en Play Mode y en la escena guardada.
- PASS: tras conectar la caja, el selector elige la tecla junto al portón.
- PASS: ON abre el portón y desactiva el collider `PortonTaller`.
- PASS: OFF cierra; ON permite abrirlo de nuevo.
- PASS: después de subir al auto, el motor gira aproximadamente a 1098 RPM y su AudioSource reproduce una señal de audio no nula.
- PASS: Game Over pausa el audio general.

Se revisó además un render del modelo texturado de la tecla con sus rótulos. El informe bruto se genera en `Library/GateAudioRegression.txt`; la vista previa en `Library/GateSwitchPreview.png`. No sustituye un recorrido manual ni una prueba de build.

## Compilación de esta revisión

- PASS: `Assembly-CSharp.csproj` compila con 0 errores.
- PASS: `Assembly-CSharp-Editor.csproj` compila con 0 errores.
- Pendiente: probar en Play Mode la llegada del vehículo y la nueva animación de descenso; esta compilación no es una build ejecutable de Unity.

## Pruebas pendientes

- Completar el recorrido usando teclado y mouse manualmente.
- Verificar audio en una máquina sin dispositivos Android conectados.
- Definir e integrar la IA obligatoria de la consigna, respetando la decisión de retirar el fantasma perseguidor.
- Llegar a `SalidaAlCamino` con el vehículo y confirmar victoria.
- Ejecutar una build Windows o WebGL.
- Probar la experiencia con el framerate y resolución de entrega.

## Estado

La escena funciona en el Editor y la integración inicial del vertical slice está operativa. La validación de build, audio final y recorrido manual completo queda pendiente.
