# Informe de testing — En el Camino

## Alcance de esta revisión

Fecha: 20/10/2026. Rama: `parcial-2`. Esta revisión ordena la base del proyecto y no reemplaza una prueba manual de punta a punta ni una build de entrega.

## Verificaciones técnicas registradas

- La escena principal configurada para build es `Assets/Scenes/EnElCamino.unity`.
- El flujo de misión tiene ocho pasos y bloquea los interactuables que no correspondan al objetivo actual.
- El generador usa 60 segundos de combustible y el HUD dispone de un indicador específico mientras está activo.
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

