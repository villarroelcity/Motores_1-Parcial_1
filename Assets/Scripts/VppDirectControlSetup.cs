using System.Collections;
using UnityEngine;
using VehiclePhysics;

namespace EnElCamino
{
    /// <summary>
    /// Initializes the untouched VPP vehicle instance for direct testing.
    /// The vehicle keeps VPStandardInput as its only driving input component.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class VppDirectControlSetup : MonoBehaviour
    {
        private VPVehicleController vehicle;
        private VPStandardInput nativeInput;
        private GameFlow gameFlow;

        private void Awake()
        {
            vehicle = FindFirstObjectByType<VPVehicleController>();
            if (vehicle == null)
                return;

            gameFlow = FindFirstObjectByType<GameFlow>();

            nativeInput = vehicle.GetComponent<VPStandardInput>();
            vehicle.gearbox.type = Gearbox.Type.Automatic;
            SetDrive();

            if (nativeInput != null)
                StartCoroutine(KeepEngineRunning());
        }

        private void FixedUpdate()
        {
            if (vehicle == null)
                return;

            // Before the player boards, keep the scripted vehicle in Drive.
            // After boarding, VPStandardInput must own the complete input bus.
            if (gameFlow == null || !gameFlow.driving)
                SetDrive();
            else
                EnsureNativePlayerControl();
        }

        private void Update()
        {
            if (vehicle != null && (gameFlow == null || !gameFlow.driving))
                SetDrive();
        }

        private void SetDrive()
        {
            int[] input = vehicle.data.Get(Channel.Input);
            input[InputData.AutomaticGear] = 4;
        }

        private void EnsureNativePlayerControl()
        {
            if (gameFlow == null || !gameFlow.driving || nativeInput == null) return;

            // Reassert only the component state. Never write the input bus or
            // external pedal values here: VPP must read steering, throttle and
            // brake directly from its original keyboard bindings.
            vehicle.enabled = true;
            nativeInput.enabled = true;
        }

        private IEnumerator KeepEngineRunning()
        {
            nativeInput.externalIgnition = VPStandardInput.IgnitionKey.Start;
            yield return new WaitForSeconds(0.8f);
            nativeInput.externalIgnition = VPStandardInput.IgnitionKey.AccOn;
        }
    }
}
