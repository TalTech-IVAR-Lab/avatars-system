namespace Games.NoSoySauce.Avatars.Calibration.Body.Samples
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Games.NoSoySauce.Universal.Coroutines;
    using Malimbe.XmlDocumentationAttribute;
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// Add cycling flickering periods to the given text to indicate loading.
    /// </summary>
    public class TextWithLoadingPeriods : MonoBehaviour
    {
        /// <summary>
        /// Text to add flickering periods to.
        /// </summary>
        [field: DocumentedByXml]
        public TextMeshProUGUI text;
        /// <summary>
        /// Flicker interval.
        /// </summary>
        [field: DocumentedByXml]
        public float interval = 1f;

        private string initialText;
        private BuffedCoroutine flickerCoroutine = new BuffedCoroutine(null);

        private void OnEnable()
        {
            initialText = text.text;
            flickerCoroutine = BuffedCoroutine.StartCoroutine(TextFlicker_Coroutine());
        }

        private void OnDisable()
        {
            flickerCoroutine.Stop();
            text.text = initialText;
        }

        private IEnumerator TextFlicker_Coroutine()
        {
            var delay = new WaitForSeconds(interval);
            
            while (true)
            {
                string currentText = initialText;
                
                for (int i = 0; i < 4; i++)
                {
                    text.SetText(currentText);
                    currentText += ".";
                    yield return delay;
                }
            }
        }
    }
}
