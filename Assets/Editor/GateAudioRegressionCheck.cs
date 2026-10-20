using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EnElCamino;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VehiclePhysics;

namespace EnElCamino.Editor
{
    [InitializeOnLoad]
    public static class GateAudioRegressionCheck
    {
        private const string RunningKey = "EnElCamino.GateAudioCheck.Running";
        private static double started;
        private static double driveStarted;
        private static int phase;
        private static bool introAudio;
        private static readonly List<string> results = new List<string>();
        private static readonly List<string> failures = new List<string>();

        static GateAudioRegressionCheck()
        {
            EditorApplication.playModeStateChanged += ModeChanged;
        }

        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            StationPowerControlsSetup.Apply();
            RenderSwitchPreview();
            SessionState.SetBool(RunningKey, true);
            EditorApplication.isPlaying = true;
        }

        public static void PreviewSwitch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            StationPowerControlsSetup.Apply();
            RenderSwitchPreview();
        }

        private static void ModeChanged(PlayModeStateChange mode)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                started = EditorApplication.timeSinceStartup;
                phase = 0;
                introAudio = false;
                results.Clear();
                failures.Clear();
                Application.logMessageReceived += RuntimeLog;
                EditorApplication.update += Tick;
            }
            if (mode == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(RunningKey, false);
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= RuntimeLog;
            }
        }

        private static void RuntimeLog(string condition, string stack, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error) && failures.Count < 15)
                failures.Add(condition + " " + stack);
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                double elapsed = EditorApplication.timeSinceStartup - started;
                VPVehicleController car = UnityEngine.Object.FindFirstObjectByType<VPVehicleController>();
                VPAudio audio = car != null ? car.GetComponentInChildren<VPAudio>() : null;
                AudioSource engine = audio != null ? audio.engine.audioSource : null;
                if (elapsed > 1.8 && elapsed < 6.5 && HasOutput(engine)) introAudio = true;
                if (phase == 0 && elapsed > 8.5)
                {
                    Check(introAudio, "Motor VPP audible durante la llegada");
                    Check(engine != null && AssetDatabase.GetAssetPath(engine.clip).StartsWith("Assets/Vehicle Physics Pro/"), "Clip original del paquete VPP");
                    results.Add("VPP clip=" + (engine != null ? AssetDatabase.GetAssetPath(engine.clip) : "null") + "; rpm=" + (car != null ? car.data.Get(Channel.Vehicle, VehicleData.EngineRpm) : -1));
                    GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
                    LadderInteractable ladder = null;
                    foreach (LadderInteractable candidate in UnityEngine.Object.FindObjectsByType<LadderInteractable>(FindObjectsSortMode.None))
                    {
                        if (new SerializedObject(candidate).FindProperty("portableExterior").boolValue) { ladder = candidate; break; }
                    }
                    LadderPlacementInteractable placement = UnityEngine.Object.FindFirstObjectByType<LadderPlacementInteractable>();
                    Check(ladder != null && placement != null, "Escalera exterior y punto de colocación conectados");
                    if (ladder != null && placement != null)
                    {
                        ladder.Interact();
                        ladder.Interact();
                        placement.Interact();
                        placement.Interact();
                    }
                    Check(flow != null && flow.insideGarage, "Descenso por escalera habilita acceso al taller");
                    GeneratorInteractable generator = UnityEngine.Object.FindFirstObjectByType<GeneratorInteractable>();
                    generator.Interact();
                    Check(flow.generatorOn && generator.Prompt == "Generador encendido", "El generador enciende y arranca el conteo");
                    GateSwitchInteractable gate = UnityEngine.Object.FindFirstObjectByType<GateSwitchInteractable>();
                    GarageDoorInteractable door = UnityEngine.Object.FindFirstObjectByType<GarageDoorInteractable>();
                    gate.Interact();
                    Check(!gate.IsOn && !door.IsOpen, "Tecla no queda trabada si se pulsa sin energía");
                    RaycastInteractor interactor = UnityEngine.Object.FindFirstObjectByType<RaycastInteractor>();
                    MethodInfo select = typeof(RaycastInteractor).GetMethod("FindInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
                    StationPowerBoxInteractable powerBox = UnityEngine.Object.FindFirstObjectByType<StationPowerBoxInteractable>();
                    MovePlayerNear(interactor, powerBox.transform);
                    IInteractable selected = select.Invoke(interactor, null) as IInteractable;
                    Check(ReferenceEquals(selected, powerBox), "E selecciona la caja eléctrica al acercarse");
                    selected?.Interact();
                    Check(flow.generatorOn && flow.stationPowerConnected, "Enchufar generador habilita la energía de la estación");
                    Check(Mathf.Approximately(new SerializedObject(flow).FindProperty("generatorFuelSeconds").floatValue, 60f), "Combustible del generador configurado a 60 segundos");
                    MovePlayerNear(interactor, gate.transform, gate.transform.forward);
                    selected = select.Invoke(interactor, null) as IInteractable;
                    Check(ReferenceEquals(selected, gate), "E selecciona la tecla junto al portón");
                    selected?.Interact();
                    Check(gate.IsOn && door.IsOpen && flow.garageOpen, "Tecla ON abre portón");
                    foreach (Collider collider in door.DoorObject.GetComponentsInChildren<Collider>(true)) Check(!collider.enabled, "Paso del portón libre: " + collider.name);
                    gate.Interact();
                    Check(!gate.IsOn && !door.IsOpen && !flow.garageOpen, "Tecla OFF cierra portón");
                    gate.Interact();
                    Check(gate.IsOn && door.IsOpen && flow.garageOpen, "Tecla vuelve a abrir sin bloquearse");
                    flow.carHasFuel = true;
                    car.GetComponent<CarDriver>().EnterCar();
                    driveStarted = EditorApplication.timeSinceStartup;
                    phase = 1;
                }
                if (phase == 1 && EditorApplication.timeSinceStartup - driveStarted > 3.5)
                {
                    Check(HasOutput(engine), "Motor VPP audible después de subir al auto");
                    results.Add("Conducción RPM=" + car.data.Get(Channel.Vehicle, VehicleData.EngineRpm) + "; playing=" + (engine != null && engine.isPlaying) + "; volume=" + (engine != null ? engine.volume : -1f));
                    UnityEngine.Object.FindFirstObjectByType<GameFlow>().GameOver();
                    Check(AudioListener.pause, "Game over silencia el resto del audio");
                    Finish();
                }
                if (elapsed > 35) throw new TimeoutException("La prueba superó los 35 segundos.");
            }
            catch (Exception exception)
            {
                failures.Add(exception.ToString());
                Finish();
            }
        }

        private static bool HasOutput(AudioSource source)
        {
            if (source == null || !source.enabled || !source.isPlaying || source.volume < 0.001f) return false;
            float[] samples = new float[256];
            source.GetOutputData(samples, 0);
            double energy = 0;
            foreach (float sample in samples) energy += sample * sample;
            return energy > 0.00000001;
        }

        private static void MovePlayerNear(RaycastInteractor interactor, Transform target, Vector3? facing = null)
        {
            CharacterController controller = interactor.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            Vector3 direction = facing ?? Vector3.forward;
            interactor.transform.position = target.position - direction * 0.7f - Vector3.up * 0.6f;
            interactor.GetComponent<PlayerController>()?.ResetFallTracking();
            if (controller != null) controller.enabled = true;
        }

        private static void Check(bool condition, string description)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + description);
            if (!condition) failures.Add(description);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= RuntimeLog;
            string report = string.Join("\n", results) + "\n\n" + string.Join("\n", failures);
            File.WriteAllText("Library/GateAudioRegression.txt", report);
            Debug.Log("[GateAudioCheck] " + (failures.Count == 0 ? "PASS" : "FAIL") + "\n" + report);
            EditorApplication.isPlaying = false;
        }

        private static void RenderSwitchPreview()
        {
            GateSwitchInteractable gate = UnityEngine.Object.FindFirstObjectByType<GateSwitchInteractable>();
            if (gate == null) return;
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject preview = UnityEngine.Object.Instantiate(gate.gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(preview, scene);
            preview.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (Transform child in preview.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            GameObject cameraObject = new GameObject("SwitchPreviewCamera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.cameraType = CameraType.Preview;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.105f, 0.12f);
            camera.cullingMask = 1 << 31;
            camera.transform.position = new Vector3(0.45f, 0.22f, -1f);
            camera.transform.LookAt(new Vector3(0f, 0.02f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 0.36f;
            GameObject lightObject = new GameObject("SwitchPreviewLight", typeof(Light));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
            RenderTexture render = new RenderTexture(640, 640, 24);
            camera.targetTexture = render;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = render;
            Texture2D image = new Texture2D(640, 640, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
            image.Apply();
            File.WriteAllBytes("Library/GateSwitchPreview.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(render);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
