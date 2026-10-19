#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace EnElCamino.Editor
{
    public static class AudioParitySetup
    {
        private const string AudioRoot = "Assets/Audio/";

        [MenuItem("Audio/Replicar configuracion de Audio - Parcial 2")]
        public static void Apply()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioRoot + "Mixers/EnElCamino_Mixer.mixer");
            AudioMixerGroup sfx = FindGroup(mixer, "SFX");
            AudioMixerGroup music = FindGroup(mixer, "Music");
            AudioMixerGroup ambience = FindGroup(mixer, "Ambience");
            AudioMixerGroup voice = FindGroup(mixer, "Voice");

            SetupMusic(music);
            SetupVoice(voice);
            SetupFinalSources(sfx);
            SetupGenerator(sfx);
            SetupFuelPump(sfx);
            SetupCar(sfx);
            SetupGameplayController(mixer, ambience, sfx);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("AudioParitySetup: fuentes, loops, clips, mixer groups y voces replicados del proyecto de audio.");
        }

        private static void SetupMusic(AudioMixerGroup musicGroup)
        {
            GameObject explorationObject = EnsureObject("MusicSource");
            GameObject tensionObject = EnsureObject("MusicTensionSource");
            AudioSource exploration = ConfigureSource(EnsureAudioSource(explorationObject), musicGroup, true, false, 0.22f);
            AudioSource tension = ConfigureSource(EnsureAudioSource(tensionObject), musicGroup, true, false, 0f);

            AdaptiveMusicController adaptive = explorationObject.GetComponent<AdaptiveMusicController>();
            if (adaptive == null) adaptive = explorationObject.AddComponent<AdaptiveMusicController>();
            SetReference(adaptive, "explorationSource", exploration);
            SetReference(adaptive, "tensionSource", tension);
            SetReference(adaptive, "explorationClip", LoadClip("Music/exploracion.wav"));
            SetReference(adaptive, "tensionClip", LoadClip("Music/tension.mp3"));
        }

        private static void SetupVoice(AudioMixerGroup voiceGroup)
        {
            GameObject voiceObject = EnsureObject("VoiceNarration");
            AudioSource source = ConfigureSource(EnsureAudioSource(voiceObject), voiceGroup, false, false, 1f);
            VoiceNarration narration = voiceObject.GetComponent<VoiceNarration>();
            if (narration == null) narration = voiceObject.AddComponent<VoiceNarration>();
            SetReference(narration, "firstSpeaker", source);

            SerializedObject serialized = new SerializedObject(narration);
            SerializedProperty lines = serialized.FindProperty("firstSpeakerLines");
            lines.arraySize = 6;
            string[] names =
            {
                "Voice/estacion_abandonada.wav",
                "Voice/encontre_generador.wav",
                "Voice/aqui_hay_un_taller.wav",
                "Voice/conseguir_bidon.wav",
                "Voice/bidon_lleno.wav",
                "Voice/conseguir_combustible.wav"
            };
            for (int i = 0; i < names.Length; i++)
                lines.GetArrayElementAtIndex(i).objectReferenceValue = LoadClip(names[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameFlow flow = Object.FindFirstObjectByType<GameFlow>();
            if (flow != null) SetReference(flow, "voiceNarration", narration);
        }

        private static void SetupFinalSources(AudioMixerGroup sfxGroup)
        {
            GameObject gameOverObject = EnsureObject("GameOverSource");
            AudioSource gameOver = ConfigureSource(EnsureAudioSource(gameOverObject), sfxGroup, false, false, 1f);
            gameOver.clip = LoadClip("SFX/game_over.mp3");

            GameObject victoryObject = EnsureObject("VictorySource");
            AudioSource victory = ConfigureSource(EnsureAudioSource(victoryObject), sfxGroup, false, false, 0.9f);
            victory.clip = LoadClip("SFX/victoria.mp3");

            GameFlow flow = Object.FindFirstObjectByType<GameFlow>();
            if (flow != null)
            {
                SetReference(flow, "gameOverSource", gameOver);
                SetReference(flow, "victorySource", victory);
            }
        }

        private static void SetupGenerator(AudioMixerGroup sfxGroup)
        {
            GameObject generator = GameObject.Find("GeneradorAuxiliar");
            if (generator == null) return;

            GeneratorInteractable interactable = generator.GetComponentInChildren<GeneratorInteractable>(true);
            GameObject buttonObject = interactable != null ? interactable.gameObject : generator;
            AudioSource buttonSource = ConfigureSource(EnsureAudioSource(buttonObject), sfxGroup, false, false, 0.45f);
            Configure3DAudio(buttonSource, 1.5f, 16f);
            GameObject motorObject = EnsureObject("GeneratorMotorAudio", generator.transform);
            AudioSource motorSource = ConfigureSource(EnsureAudioSource(motorObject), sfxGroup, false, false, 0.32f);
            Configure3DAudio(motorSource, 2f, 20f);
            if (interactable == null) return;

            SetReference(interactable, "switchSound", LoadClip("SFX/switch_electricidad.mp3"));
            SetReference(interactable, "generatorStartSound", LoadClip("SFX/generador_arranque.wav"));
            SetReference(interactable, "generatorMotorLoop", LoadClip("SFX/generador_motor_loop.wav"));
            SetReference(interactable, "motorAudioSource", motorSource);
        }

        private static void SetupFuelPump(AudioMixerGroup sfxGroup)
        {
            GameObject pump = GameObject.Find("Surtidor");
            if (pump == null) return;
            ConfigureSource(EnsureAudioSource(pump), sfxGroup, false, false, 1f);
            FuelPumpInteractable interactable = pump.GetComponent<FuelPumpInteractable>();
            if (interactable != null) SetReference(interactable, "pumpSound", LoadClip("SFX/surtidor_on.mp3"));
        }

        private static void SetupCar(AudioMixerGroup sfxGroup)
        {
            GameObject car = GameObject.Find("VehiculoConRuedas");
            if (car == null) return;
            ConfigureSource(EnsureAudioSource(car), sfxGroup, false, false, 1f);
            SetReference(car.GetComponent<CarDriver>(), "accelerationSound", LoadClip("SFX/motor_acelerando.mp3"));
            SetReference(car.GetComponent<CarDriver>(), "engineLoopSound", LoadClip("SFX/motor_acelerando.mp3"));
            SetReference(car.GetComponent<CarInteractable>(), "startSound", LoadClip("SFX/motor_arranque.mp3"));
        }

        private static void SetupGameplayController(AudioMixer mixer, AudioMixerGroup ambienceGroup, AudioMixerGroup sfxGroup)
        {
            GameplayAudioController controller = Object.FindFirstObjectByType<GameplayAudioController>();
            if (controller == null) return;
            SetReference(controller, "mixer", mixer);
            SetReference(controller, "ambientClip", LoadClip("Ambience/ambiente_exterior.wav"));
            SetReference(controller, "garageOpen", LoadClip("SFX/sonido_porton.mp3"));
        }

        private static AudioSource ConfigureSource(AudioSource source, AudioMixerGroup group, bool loop, bool playOnAwake, float volume)
        {
            source.outputAudioMixerGroup = group;
            source.loop = loop;
            source.playOnAwake = playOnAwake;
            source.spatialBlend = 0f;
            source.volume = volume;
            return source;
        }

        private static void Configure3DAudio(AudioSource source, float minDistance, float maxDistance)
        {
            if (source == null) return;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
        }

        private static GameObject EnsureObject(string name, Transform parent = null)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null) return existing;
            GameObject created = new GameObject(name);
            if (parent != null) created.transform.SetParent(parent, false);
            return created;
        }

        private static AudioSource EnsureAudioSource(GameObject target)
        {
            AudioSource source = target.GetComponent<AudioSource>();
            return source != null ? source : target.AddComponent<AudioSource>();
        }

        private static AudioClip LoadClip(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + relativePath);
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
        {
            if (mixer == null) return null;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master/" + groupName);
            return groups.Length > 0 ? groups[0] : null;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            if (target == null) return;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
