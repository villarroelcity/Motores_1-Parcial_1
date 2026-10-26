# Mapa de scripts propios

Revisión: 26/10/2026. Hay **17 archivos de gameplay**, 3.697 líneas y una herramienta permanente de editor. La limpieza de esta fecha comenzó con 3.894 líneas. Los scripts de VPP y otros paquetes no se cuentan como propios.

| Script | Responsabilidad |
| --- | --- |
| `Juego` | Misiones, energía, tiempo, persecución y resultados de partida. |
| `AutoJugador` | Llegada, combustible, entrada al auto, daño, emisión de escapes/polvo/chispas y puente a los controles nativos VPP. |
| `UI` | Objetivos, mensajes, relojes, espejo, daño, pausa, controles y reinicio. |
| `Trafico` | Activa y coloca los autos de una reserva preparada en la escena. |
| `TraficoAutos` | Movimiento recto, ruedas y colisiones del tráfico. Incluye la utilidad compartida de luces/materiales. |
| `PatrulleroIA` | Incorporación anticipada, seguimiento físico directo con respaldo NavMesh, adelantamiento por un corredor libre, recuperación, sirena y balizas. |
| `PlayerController` | Movimiento a pie, carrera, caída, parámetros del Animator y eventos de pasos. Las poses están en clips editables en Unity. |
| `CamaraJugador` | Puente a Cinemachine: cambia el objetivo jugador/auto y congela la cámara al finalizar. El seguimiento y las paredes son componentes Cinemachine guardados en Unity. |
| `DetectorDeInteracciones` | Raycast, selección por cercanía y tecla E. Incluye `IInteractable`. |
| `CheckPoint` | Detecta los triggers de salida a la ruta y persecución. |
| `Surtidor` | Recoger/transportar el bidón y llenarlo en el surtidor. |
| `Escalera` | Subida al techo y descenso controlado por el hueco. |
| `Generador` | Encendido, audio, humo preparado en la escena y apagado del generador. |
| `TableroElectrico` | Alimentación de la estación, indicador y llave térmica. El cable ya está en la escena. |
| `Interruptores` | Interacción con el interruptor del portón. |
| `PortonTaller` | Apertura del portón y su secuencia de movimiento. |
| `AudioController` | Ambiente, portón y transición musical al comenzar la sirena. |

`Assets/Editor/DaylightStandbySetup.cs` mantiene la hora diurna de espera, oculta el dibujo de navegación y precalienta la colección de shaders en el editor. No forma parte del gameplay ni crea objetos.

## Nombres cambiados

| Antes | Archivo actual | Objeto principal de jerarquía |
| --- | --- | --- |
| `TrafficManager` | `Trafico.cs` | `Trafico` |
| `TrafficVehicle` | `TraficoAutos.cs` | Instancias de la reserva bajo `Trafico` |
| `CarDriver` | `AutoJugador.cs` | `AutoJugador` |
| `GameFlow` | `Juego.cs` | `Juego` |
| `GameUI` | `UI.cs` | `UI` |

Los cinco `.meta` mantienen sus GUID: no son componentes nuevos ni copias. Se actualizaron tipos y referencias de escena, prefabs y botones. Los demás nombres de archivos no se cambiaron fuera de lo solicitado.

Otros nombres de jerarquía: `CamaraPrincipal`, `SistemaEventos`, `Generador`, `ModeloJugador`, `MusicaExploracion`, `MusicaPersecucion`, `AudioDerrota`, `AudioVictoria`, `PanelVentaja`, `PanelRetrovisor` y `AutoTrafico_00` a `AutoTrafico_63`. Los nombres internos de mallas y del SDK se conservan para no romper referencias.

## Criterio de limpieza

- Los objetos, cámaras, UI, colliders, ruedas, audio, luces y reserva de tráfico están preparados en Unity.
- No hay `new GameObject`, `CreatePrimitive`, `AddComponent` ni `Instantiate` en los scripts propios de gameplay.
- El ciclo tarde-noche usa el sol, niebla y Skybox Procedural de Unity, sin dependencias Enviro en scripts propios. El antiguo reloj Enviro fue retirado.
- Color y emisión usan un MaterialPropertyBlock compartido: no clonan materiales con `Renderer.material` ni `Renderer.materials`.
- Los atajos configurables VPP están desactivados en la escena. Solo queda código para Ctrl+Espacio y comandos internos sin ajuste en el Inspector.
- Se conserva la física VPP del jugador. La cámara usa Cinemachine, el personaje usa Animator y la patrulla combina NavMesh con WheelColliders.

La integración de cámara sustituyó el seguimiento propio por Cinemachine. Los emisores ya están en la escena: AutoJugador controla polvo, impactos y ambos escapes; Generador controla su humo. No se crean cámaras ni partículas GameObject durante la partida.

La patrulla ahora consulta un NavMesh horneado y conduce con sus cuatro WheelColliders. `NavegacionPatrulla` y los NavMeshObstacle del tráfico son componentes de Unity guardados en edición. El planificador de corredores anterior se retiró; siguen siendo 17 scripts de gameplay.

Se usa `Update` para entrada/lógica, `FixedUpdate` para física y `LateUpdate` para cámara. Los pendientes del parcial están en [ESTADO_PARCIAL_2.md](ESTADO_PARCIAL_2.md).
