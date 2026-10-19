# Informe de testing — Segundo Parcial

## Versión

Vertical slice de “En el Camino”, rama `parcial-2`.

## Pruebas realizadas

- El proyecto abre en Unity `6000.3.5f2` y la escena `EnElCamino` carga correctamente.
- El Editor entra y sale de Play Mode sin errores de compilación.
- El HUD muestra objetivos, mensajes y temporizador al iniciar.
- Cinemachine genera la cámara de gameplay y mantiene el encuadre del jugador.
- El flujo de eventos puede avanzar desde el generador hasta el arranque del vehículo.
- Al iniciar el vehículo se crea `AmenazaIA` y se activa el desafío final.
- Se verificaron las referencias de UI durante Play Mode.
- El proyecto registra únicamente un warning externo por conexión ADB a un emulador Android; no pertenece al gameplay.

## Pruebas pendientes

- Completar el recorrido usando teclado y mouse manualmente.
- Verificar audio en una máquina sin dispositivos Android conectados.
- Comprobar captura de la amenaza y pantalla de Game Over.
- Llegar a `SalidaAlCamino` con el vehículo y confirmar victoria.
- Ejecutar una build Windows o WebGL.
- Probar la experiencia con el framerate y resolución de entrega.

## Estado

La escena funciona en el Editor y la integración inicial del vertical slice está operativa. La validación de build, audio final y recorrido manual completo queda pendiente.
