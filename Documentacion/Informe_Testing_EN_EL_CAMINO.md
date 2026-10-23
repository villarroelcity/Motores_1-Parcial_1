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

### Comprobación en Play del 22/10/2026

- Tráfico: ocho vehículos con velocidad positiva sobre Z y ocho con velocidad negativa.
- Apoyo: distancia máxima entre la base del collider y la superficie de la ruta de 0,015 m.
- Impacto controlado: el vehículo corta su aceleración y conserva un Rigidbody dinámico.
- Espejo: producto escalar entre la dirección de su cámara y la del vehículo de -1 (mira hacia atrás).
- Patrullero: desplazamiento máximo observado entre muestras de 0,552 m durante la incorporación, sin ajuste instantáneo al borde de la ruta.
- Compilación correcta. La dificultad de evasión y la apariencia final del espejo requieren una prueba de conducción completa.

| Caso | Pasos | Resultado esperado | Estado |
| --- | --- | --- | --- |
| Llegada | Ejecutar desde escena limpia | Auto llega encendido, pierde motor y el jugador aparece junto a él | Pendiente |
| Movimiento | Caminar y mantener `Shift` | La carrera es perceptiblemente más rápida que la caminata | Pendiente |
| Taller | Subir, entrar por el hueco y bajar | Sin flotación ni Game Over en la bajada controlada | Pendiente |
| Caída | Caer fuera de la bajada controlada | `GAME OVER`, audio global detenido | Pendiente |
| Energía | Generador → tablero → interruptor | El portón sólo abre con energía y antes del fin del combustible | Pendiente |
| Combustible | Bidón → surtidor → vehículo | Modelo del bidón acompaña al jugador sin flotar y habilita arrancar | Pendiente |
| Salida | Conducir a `SalidaAlCamino` | Victoria, UI final y audio final sin sonidos residuales | Pendiente |
| Tiempo | Dejar agotar el reloj | Atardecer a noche, `GAME OVER`, audio detenido | Pendiente |
| Resolución | Probar 16:9 y una resolución menor | HUD sin recortes ni solapamientos | Pendiente |
| Build | Compilar y ejecutar build Windows | Mismo flujo y audio que en editor | Pendiente |

## Riesgos conocidos

- Falta integrar y probar la IA exigida por la consigna; no se considera cerrada.
- La validación en una build Windows es obligatoria y todavía está pendiente.
- Los paquetes comerciales deben estar instalados localmente antes de abrir la escena; ver la guía de instalación.

## Evidencia a guardar al cerrar el parcial

- Captura del inspector con la escena en Build Settings.
- Video del recorrido completo, incluida una derrota y la victoria.
- Build final con fecha/versionado.
- Resultado de esta tabla actualizado con PASS/FAIL y correcciones aplicadas.

