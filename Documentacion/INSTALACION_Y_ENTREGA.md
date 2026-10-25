# Instalación y entrega

## Requisitos locales

- Unity `6000.3.5f2`.
- Paquetes declarados en `Packages/manifest.json`.
- Importaciones locales con licencia válida: Vehicle Physics Pro y Nature Starter Kit 2; además de Loader Heavy Machinery para el obstáculo indicado en la lista de assets. Enviro no se usa en la escena actual.

## Apertura limpia

1. Clonar la rama `parcial-2`.
2. Abrir la carpeta raíz desde Unity Hub con la versión indicada.
3. Importar los paquetes comerciales usados si no están presentes en `Assets`; ver `ASSETS_Y_DEPENDENCIAS.md`.
4. Dejar terminar la importación y abrir `Assets/Scenes/EnElCamino.unity`.
5. Confirmar que sólo está agregada esa escena en Build Settings.

## Entrega final pendiente

1. Validar Cinemachine, IA y partículas en un recorrido reproducible. Cinemachine y emisores ya están integrados; la consigna no exige NavMesh expresamente.
2. Ejecutar la matriz manual de `Informe_Testing_EN_EL_CAMINO.md`.
3. Generar build Windows y probarlo fuera del Editor.
4. Grabar el video final de hasta cinco minutos.
5. Verificar que Git esté limpio, con código, escena, documentación y metadatos necesarios; no incluir carpetas regenerables ni assets comerciales.

La revisión contra la consigna oficial está en [ESTADO_PARCIAL_2.md](ESTADO_PARCIAL_2.md).

