using System.Collections;
using UnityEngine;
using VehiclePhysics;

namespace EnElCamino
{
    /// <summary>
    /// Initializes the untouched VPP vehicle instance for direct testing.
    /// The vehicle keeps VPStandardInput as its only driving input component.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class VppDirectControlSetup : MonoBehaviour
    {
        private VPVehicleController vehicle;
        private VPStandardInput nativeInput;

        private void Awake()
        {
            vehicle = FindFirstObjectByType<VPVehicleController>();
            if (vehicle == null)
                return;

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
        }

        private void SetDrive()
        {
            int[] input = vehicle.data.Get(Channel.Input);
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
