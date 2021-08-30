namespace Games.NoSoySauce.Avatars.Calibration.Body.Samples
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    ///     Controls a slider which reflects the progress of collecting samples during calibration step.
    /// </summary>
    public class CalibrationSamplingProgressSlider : MonoBehaviour
    {
        public BodyCalibrationProfileCreator calibrationProfileCreator;
        public Slider slider;

        public void LateUpdate()
        {
            UpdateSlider();
        }

        public void UpdateSlider()
        {
            slider.value = calibrationProfileCreator.SamplingProgress;
        }
    }
}