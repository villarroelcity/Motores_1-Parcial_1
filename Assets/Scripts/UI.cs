using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EnElCamino
{
    // Los objetos y su aspecto se editan en el Canvas, no se construyen al jugar.
    public class UI : MonoBehaviour
    {
        [SerializeField] private Juego gameFlow;
        [SerializeField] private CanvasGroup gameplayVisibility;
        [SerializeField] private Text objectiveText;
        [SerializeField] private RectTransform objectivePanel;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;
        [SerializeField] private RectTransform clockPanel;
        [SerializeField] private GameObject completedPanel;
        [SerializeField] private Text finalText;
        [SerializeField] private GameObject outcomeRestartButton;
        [SerializeField] private GameObject generatorFuelPanel;
        [SerializeField] private Text generatorFuelText;
        [SerializeField] private GameObject pursuitDistancePanel;
        [SerializeField] private Text pursuitDistanceText;
        [SerializeField] private GameObject damagePanel;
        [SerializeField] private Text damageText;
        [SerializeField] private RectTransform damageFill;
        [SerializeField] private GameObject rearViewPanel;
        [SerializeField] private Camera rearViewCamera;
        [SerializeField] private AutoJugador rearViewTarget;
        [SerializeField] private Vector3 rearViewOffset = new Vector3(0f, 1.65f, -2.7f);
        [SerializeField] private float rearViewPitch = 6f;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject controlsPanel;
        private bool gameplayVisible;
        private bool paused;
        private float objectiveExpiresAt;
        private float messageExpiresAt;
        private int displayedGeneratorFuelSeconds = -1;
        private int pursuitCurrentMeters;
        private int pursuitRequiredMeters = 200;
        private int pursuitSecondsRemaining = -1;

        private void Awake()
        {
            pausePanel.SetActive(false);
            controlsPanel.SetActive(false);
            completedPanel.SetActive(false);
            damagePanel.SetActive(false);
            HidePrompt();
            HideMessage();
            HideObjective();
            HideGeneratorFuelTime();
            SetGameplayVisible(false);
        }

        private void SetOverlayVisible(bool visible)
        {
            gameplayVisible = visible;
            gameplayVisibility.alpha = visible ? 1f : 0f;
            gameplayVisibility.interactable = visible;
            gameplayVisibility.blocksRaycasts = visible;
        }

        public void SetGameplayVisible(bool visible)
        {
            SetOverlayVisible(visible);
            if (!visible) { HideMessage(); HidePursuitDistance(); }
            else ShowCountdown();
        }

        private bool IsGameOverOrFinished()
        {
            return gameFlow != null && (gameFlow.gameOver || gameFlow.finished);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (controlsPanel != null && controlsPanel.activeSelf)
                {
                    controlsPanel.SetActive(false);
                    return;
                }
                TogglePause();
            }

            if (Keyboard.current.rKey.wasPressedThisFrame &&
                (paused || completedPanel != null && completedPanel.activeSelf))
                RestartGame();
        }

        private void TogglePause()
        {
            if (!gameplayVisible || pausePanel == null) return;
            if (gameFlow != null && (gameFlow.gameOver || gameFlow.finished)) return;

            paused = !paused;
            pausePanel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        public void ResumeGame()
        {
            if (IsGameOverOrFinished()) return;
            if (!paused) return;
            paused = false;
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public void RestartGame()
        {
            paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                SceneManager.LoadScene(activeScene.path, LoadSceneMode.Single);
                return;
            }

            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
        }

        public void SetObjective(string text)
        {
            if (IsGameOverOrFinished()) return;
            if (objectiveText == null) return;
            objectiveText.text = text;
            objectiveText.enabled = true;
            if (!objectiveText.gameObject.activeSelf) objectiveText.gameObject.SetActive(true);
            if (objectivePanel != null && !objectivePanel.gameObject.activeSelf)
                objectivePanel.gameObject.SetActive(true);
            objectiveExpiresAt = Time.time + 10f;
        }

        public void HideObjective()
        {
            if (objectivePanel != null) objectivePanel.gameObject.SetActive(false);
        }

        public void ShowPrompt(string text)
        {
            if (IsGameOverOrFinished()) return;
            if (!gameplayVisible) return;
            if (promptText == null) return;
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
        }

        public void ShowMessage(string text)
        {
            if (IsGameOverOrFinished()) return;
            if (!gameplayVisible) return;
            if (messageText == null) return;
            messageText.text = text;
            messageText.gameObject.SetActive(true);
            messageExpiresAt = Time.time + 4.5f;
        }

        public void HideMessage()
        {
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        public void SetGeneratorFuelTime(float secondsRemaining)
        {
            if (IsGameOverOrFinished()) return;
            int seconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            if (seconds != displayedGeneratorFuelSeconds)
            {
                generatorFuelText.text = $"COMBUSTIBLE EN\nEL GENERADOR\n{seconds / 60}:{seconds % 60:00}";
                displayedGeneratorFuelSeconds = seconds;
            }
            generatorFuelText.enabled = true;
            if (!generatorFuelText.gameObject.activeSelf) generatorFuelText.gameObject.SetActive(true);
            generatorFuelPanel.SetActive(true);
        }

        public void HideGeneratorFuelTime()
        {
            if (generatorFuelPanel != null) generatorFuelPanel.SetActive(false);
            displayedGeneratorFuelSeconds = -1;
        }

        public void SetPursuitDistance(float currentDistance, float requiredDistance)
        {
            if (IsGameOverOrFinished()) return;
            pursuitCurrentMeters = Mathf.Max(0, Mathf.FloorToInt(currentDistance));
            pursuitRequiredMeters = Mathf.Max(1, Mathf.CeilToInt(requiredDistance));
            RefreshPursuitDisplay();
        }

        public void SetPursuitTimeRemaining(float secondsRemaining)
        {
            if (IsGameOverOrFinished()) return;
            pursuitSecondsRemaining = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            RefreshPursuitDisplay();
        }

        private void RefreshPursuitDisplay()
        {
            SetOverlayVisible(true);
            if (pursuitDistanceText == null || rearViewPanel == null) return;

            string timer = pursuitSecondsRemaining >= 0
                ? $"    |    RESERVA {pursuitSecondsRemaining / 60}:{pursuitSecondsRemaining % 60:00}"
                : string.Empty;
            pursuitDistanceText.text = $"VENTAJA {pursuitCurrentMeters} / {pursuitRequiredMeters} m{timer}";
            pursuitDistancePanel.SetActive(true);
            rearViewPanel.SetActive(true);
            if (rearViewCamera != null) rearViewCamera.enabled = true;
        }

        public void HidePursuitDistance()
        {
            if (pursuitDistancePanel != null) pursuitDistancePanel.SetActive(false);
            if (rearViewPanel != null) rearViewPanel.SetActive(false);
            if (rearViewCamera != null) rearViewCamera.enabled = false;
        }

        private void UpdateDamageDisplay()
        {
            if (damagePanel == null) return;
            if (rearViewTarget == null) rearViewTarget = FindFirstObjectByType<AutoJugador>();
            bool visible = gameplayVisible && !IsGameOverOrFinished() && rearViewTarget != null &&
                rearViewTarget.HasPlayerControl && rearViewTarget.Damage > 0f && Time.time < rearViewTarget.DamageHudUntil;
            damagePanel.SetActive(visible);
            if (!visible) return;
            damageText.text = $"DAÑO {Mathf.CeilToInt(rearViewTarget.Damage)}%";
            damageFill.anchorMax = new Vector2(Mathf.Clamp01(rearViewTarget.Damage / 100f), 1f);
        }

        private void UpdateRearViewCamera()
        {
            if (rearViewCamera == null || !rearViewCamera.enabled) return;
            if (rearViewTarget == null) rearViewTarget = FindFirstObjectByType<AutoJugador>();
            if (rearViewTarget == null) return;

            Vector3 forward = Vector3.ProjectOnPlane(rearViewTarget.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) return;

            // El humo, polvo y chispas no deben mover el encuadre del espejo.
            Vector3 mirrorPosition = rearViewTarget.transform.position +
                Quaternion.LookRotation(forward, Vector3.up) * rearViewOffset;
            rearViewCamera.transform.SetPositionAndRotation(
                mirrorPosition,
                Quaternion.LookRotation(-forward, Vector3.up) * Quaternion.Euler(rearViewPitch, 0f, 0f));
        }

        public void ShowControls() => controlsPanel.SetActive(true);
        public void HideControls() => controlsPanel.SetActive(false);
        public void HideCountdown() => clockPanel.gameObject.SetActive(false);
        public void ShowCountdown() => clockPanel.gameObject.SetActive(true);

        private void LateUpdate()
        {
            if (messageText.gameObject.activeSelf && Time.time >= messageExpiresAt) HideMessage();
            if (objectivePanel.gameObject.activeSelf && Time.time >= objectiveExpiresAt) HideObjective();
            UpdateRearViewCamera();
            UpdateDamageDisplay();
        }

        public void ShowCompleted() => ShowOutcome("TE ESCAPASTE DE LA POLICIA\nVICTORIA...!!!", false);
        public void ShowGameOver() => ShowOutcome("GAME OVER", true);

        private void ShowOutcome(string title, bool lost)
        {
            paused = true;
            Time.timeScale = 0f;
            if (lost) AudioListener.pause = true;
            SetOverlayVisible(true);
            pausePanel.SetActive(false);
            controlsPanel.SetActive(false);
            finalText.text = title;
            completedPanel.SetActive(true);
            completedPanel.transform.SetAsLastSibling();
            outcomeRestartButton.SetActive(lost);
            HideGeneratorFuelTime();
            HidePursuitDistance();
            HideObjective();
            HidePrompt();
            HideMessage();
            damagePanel.SetActive(false);
        }
    }
}
