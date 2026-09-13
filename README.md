# Núcleo Cero

Prototipo individual de Ezequiel Godoy para Motores de Desarrollo 1, Parcial 1.

Unity: **6000.3.5f2**, URP y New Input System.

## Estado actual

High Concept completo y greybox inicial guardado en `Assets/Scenes/NucleoCero.unity`.
La cápsula ya se mueve con New Input System y CharacterController. Tiene gravedad, giro hacia la dirección de avance y retorno al inicio si cae fuera del mapa. La cámara orbital sigue al personaje y se acerca ante obstáculos. La terminal permite restaurar energía con E: cambia a verde, genera un núcleo físico y desactiva la puerta. La zona cian detrás de la puerta detecta la llegada y muestra la finalización.

### Recorrido de esta versión

1. Entrar en Play y hacer clic en Game.
2. Acercarse al bloque cian situado a la izquierda y apuntar con la mira central hasta ver «E: restaurar energía».
3. Presionar E y observar el núcleo caer sobre su base a la derecha.
4. Cruzar la puerta abierta y entrar en la zona cian del fondo.
5. R recarga la escena para reiniciar; Esc libera el cursor.

`PlayerInteraction` lanza un Raycast desde la cámara y comprueba una distancia máxima de tres unidades desde el personaje al objeto. `IInteractable` define qué debe ofrecer un objeto interactivo: un mensaje y una acción. `TerminalInteractable` implementa ese contrato; su bandera Activated evita activaciones repetidas. `Instantiate` crea una copia del prefab NucleoEnergia, cuyo Rigidbody aplica gravedad y cuyo SphereCollider permite que se apoye sobre la base. `FinishTrigger.OnTriggerEnter` detecta al jugador y exige que la terminal esté activada.

El HUD usa Canvas y TextMeshPro con escala adaptable, controles arriba, mira central y objetivo abajo. Sus textos no capturan clics. No utiliza botones.

Pruebas de esta etapa en Play mediante comandos de Editor: raycast válido, pulsación E simulada en New Input System, bloqueo por distancia y pared, una única instancia ante activaciones repetidas, puerta abierta, caída y apoyo del núcleo tras simular física, trigger de llegada y render del HUD de finalización. No equivalen a un recorrido manual completo. Reinicio con R y build WebGL siguen pendientes de prueba.

### Controles y aprendizaje

Presionar Play y hacer clic en Game. WASD o flechas mueven al personaje; el mouse gira la cámara; Esc libera el cursor y suspende el movimiento; clic izquierdo lo captura nuevamente.

`PlayerMovement.cs` lee la acción Player/Move del asset InputSystem_Actions. Calcula una dirección relativa a la cámara, aplica gravedad y llama a CharacterController.Move. Multiplicar velocidad por Time.deltaTime convierte unidades por segundo en desplazamiento por frame. El CharacterController detecta paredes y suelo, pero no aplica gravedad por sí solo.

`ThirdPersonCamera.cs` lee el mouse mediante New Input System y sigue al personaje en LateUpdate, después de su movimiento. Un SphereCast detecta obstáculos entre el personaje y la posición deseada de la cámara. El jugador usa la capa Ignore Raycast para no bloquear su propia cámara.

En Inspector, seleccionar Player para ajustar Speed o Gravity; seleccionar Main Camera para ajustar Distance o Sensitivity. Hacer cambios permanentes fuera de Play. La cámara actual es un controlador propio básico; todavía no incorpora Cinemachine y deberá cotejarse con el sistema trabajado en clase.

Verificado en Editor: compilación sin errores, entrada W y desplazamiento, detección de suelo, bloqueo por pared, método de respawn y captura visual de cámara. Pendientes: prueba manual prolongada de controles, caída automática y comportamiento en una build WebGL.

Objetivo de publicación: WebGL cuando resulte viable, con Windows como alternativa aceptada por la consigna. Todavía no se generó una build.

## Cómo explorar esta primera escena

1. Abrir el proyecto con Unity 6000.3.5f2.
2. Abrir `Assets/Scenes/NucleoCero.unity`.
3. En Hierarchy, desplegar `01_Escenario` y seleccionar `Terminal_Provisional`.
4. En Inspector, observar Transform, Mesh Filter, Mesh Renderer y Box Collider.
5. Con el cursor sobre Scene, presionar F para enfocar el objeto seleccionado.

Transform define posición, rotación y escala. Mesh Filter contiene la geometría visible, Mesh Renderer la dibuja con un material y Box Collider define el volumen de colisión.

El piso mide 16 por 32 unidades. Como referencia de diseño, usamos aproximadamente una unidad por metro; la cápsula mide dos unidades de alto.

## Carpetas

- Assets/Scenes: niveles.
- Assets/Materials: apariencia de las superficies.
- Assets/Prefabs: objetos reutilizables, a completar.
- Assets/Scripts: comportamientos C#, a completar.
- Documentacion: High Concept y futura documentación de entrega.

Cada archivo `.meta` identifica un recurso para Unity. Debe acompañarlo en Git para conservar las referencias entre escenas, materiales y scripts.

## Git durante el desarrollo

Git guarda el historial local. GitHub aloja su copia remota. Un commit registra un cambio concreto; push envía los commits al remoto.

```powershell
git status
git add Assets Documentacion README.md
git diff --cached --stat
git commit -m "Describe el cambio realizado"
git push
```

Guardar las escenas antes de registrar cambios. Revisar lo preparado antes del commit. `.gitignore` excluye cachés, temporales y archivos de bloqueo de Office.

## Próximas etapas

Prueba manual del recorrido y reinicio; build WebGL; actualización del GDD y High Concept con el estado real; informe de testing y publicación. Cotejar la cámara propia con el sistema trabajado en clase.

El paquete Unity Pipeline permite operar el Editor local durante el desarrollo. No constituye una mecánica del juego.
