# Núcleo Cero

Prototipo individual de Ezequiel Godoy para Motores de Desarrollo 1, Parcial 1.

Unity: **6000.3.5f2**, URP y New Input System.

## Estado actual

High Concept completo y greybox inicial guardado en `Assets/Scenes/NucleoCero.unity`.
La cápsula, terminal, puerta y meta son referencias de posición y tamaño: todavía no tienen comportamientos jugables. La cámara actual ofrece una vista general del nivel; la cámara de tercera persona está pendiente.

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

Controlador con New Input System y CharacterController; cámara en tercera persona; raycast e interacción; prefab de núcleo con Rigidbody; puerta y trigger final; pruebas, build, GDD, informe de testing y publicación.

El paquete Unity Pipeline permite operar el Editor local durante el desarrollo. No constituye una mecánica del juego.
