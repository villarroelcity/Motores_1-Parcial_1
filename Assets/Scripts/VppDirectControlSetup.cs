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

            // VPP's automatic gearbox expects D (4) to be explicitly selected.
            SetDrive();
            EnsureNativePlayerControl();
        }

        private void Update()
        {
            if (vehicle != null)
            {
                SetDrive();
                EnsureNativePlayerControl();
            }
        }

        private void SetDrive()
        {
            int[] input = vehicle.data.Get(Channel.Input);
            input[InputData.AutomaticGear] = 4;
        }

        private void EnsureNativePlayerControl()
        {
            if (gameFlow == null || !gameFlow.driving || nativeInput == null) return;

            // Reassert only the VPP component state. Never write steering or
            // throttle here: those values must come directly from VPP's
            // original keyboard input and reach the front axle.
            vehicle.enabled = true;
            nativeInput.enabled = true;
            nativeInput.disableSteerInput = false;
            nativeInput.disableThrottleInput = false;
            nativeInput.disableBrakeInput = false;
            nativeInput.disableHandbrakeInput = false;
            nativeInput.disableClutchInput = false;
            nativeInput.disableGearShiftInputs = false;
            nativeInput.externalSteer = 0f;
            nativeInput.externalThrottle = 0f;
            nativeInput.externalBrake = 0f;
            nativeInput.externalHandbrake = 0f;

            int[] input = vehicle.data.Get(Channel.Input);
            input[InputData.Brake] = 0;
            input[InputData.Handbrake] = 0;
            input[InputData.Clutch] = 0;
            input[InputData.AutomaticGear] = 4;
        }

        private IEnumerator KeepEngineRunning()
        {
            nativeInput.externalIgnition = VPStandardInput.IgnitionKey.Start;
            yield return new WaitForSeconds(0.8f);
            nativeInput.externalIgnition = VPStandardInput.IgnitionKey.AccOn;
        }
    }
}
