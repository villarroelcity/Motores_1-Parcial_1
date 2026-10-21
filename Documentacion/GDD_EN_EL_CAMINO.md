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

En un camino rural al anochecer, el auto se queda sin combustible frente a una estación abandonada. Para escapar, el jugador debe atravesar el taller, devolver energía a la estación y abastecer el vehículo antes de que oscurezca.

## Bucle principal

Llegada automática → exploración a pie → acceso al taller por el techo → generador → tablero eléctrico → portón → bidón → surtidor → vehículo → salida.

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
| 8 | Llegar a la salida | Victoria |

Derrotas vigentes: agotar el tiempo general/noche, agotar el combustible del generador antes de resolver el taller y caer por el hueco del techo. El mensaje final se normaliza como `GAME OVER` y detiene la mezcla de audio antes de reproducir su feedback.

## Sistemas implementados

- Movimiento con `CharacterController`; caminar y correr con `Shift`.
- Cámara de tercera persona con Cinemachine.
- Interacción por Raycast e interfaz `IInteractable`.
- Flujo de misión secuencial en `GameFlow`.
- Auto con Vehicle Physics Pro; llegada, carga y control reunidos en `CarDriver`.
- Interacciones de generador, tablero, interruptor, portón, bidón, surtidor y salida.
- UI uGUI adaptada a pantalla para objetivos, mensajes, tiempo y combustible del generador.
- Ciclo tarde–noche con Enviro 3, iluminación y audio ambiental/espacial.
- Audio por mixer, efectos de interacción, motor VPP, música adaptativa y silenciamiento al finalizar.
- Físicas, colliders, triggers, cajas físicas y partículas de feedback.

## Dirección visual y sonora

La estación usa una estética rural low-poly con vegetación de Nature Starter Kit 2, entorno de Enviro 3, modelos externos de generador/bidón/tablero y surtidor 3D. El arte se utiliza como apoyo del gameplay: el techo, el hueco, el tablero y el portón hacen visible la ruta de misión.

El ambiente exterior, generador, surtidor, portón, acciones del auto, música de exploración y mensajes de victoria/derrota se organizan mediante `EnElCamino_Mixer`.

## IA — pendiente de definición e integración

La consigna exige IA y navegación. El código de una amenaza que no se activaba fue retirado durante la limpieza para no conservar referencias falsas ni un prefab inexistente. Antes de la entrega se debe elegir una amenaza coherente, darle una presencia visual, configurar NavMesh y comprobar una condición de derrota reproducible. No debe presentarse como funcional hasta probar el recorrido completo.

## Criterios de entrega

Quedan por producir y validar: build jugable, video de hasta cinco minutos, prueba final completa, documentación de testing actualizada y el desafío de IA integrado. La lista operativa está en [Informe_Testing_EN_EL_CAMINO.md](Informe_Testing_EN_EL_CAMINO.md).

