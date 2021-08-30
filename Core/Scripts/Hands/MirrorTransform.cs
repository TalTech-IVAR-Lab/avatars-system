namespace Games.NoSoySauce.Everfight
{
    using UnityEngine;
    using Malimbe.XmlDocumentationAttribute;

    /// <summary>
    /// Makes transform mirror local values of another transform along the given axes.
    /// </summary>
    public class MirrorTransform : MonoBehaviour
    {
        public enum MirrorAxes
        {
            XY,
            XZ,
            YZ
        }

        /// <summary>
        /// The original transform.
        /// </summary>
        [field: DocumentedByXml]
        public Transform sourceTransform;
        /// <summary>
        /// Transform which will mirror <see cref="sourceTransform"/>.
        /// </summary>
        [field: DocumentedByXml]
        public Transform targetTransform;
        /// <summary>
        /// Axes along which the transform will be mirrored.
        /// </summary>
        [field: DocumentedByXml]
        public MirrorAxes mirrorAxes;

        private void OnValidate()
        {
            if (targetTransform == sourceTransform) targetTransform = null;
        }

        private void Update()
        {
            if (targetTransform == sourceTransform) return;
            if (!sourceTransform || !targetTransform) return;

            switch (mirrorAxes)
            {
                case MirrorAxes.XY:
                    {
                        targetTransform.localPosition = new Vector3(
                            sourceTransform.localPosition.x,
                            sourceTransform.localPosition.y,
                            -sourceTransform.localPosition.z
                            );
                        targetTransform.localRotation = Quaternion.Euler(
                            -sourceTransform.localRotation.eulerAngles.x,
                            -sourceTransform.localRotation.eulerAngles.y,
                            sourceTransform.localRotation.eulerAngles.z
                            );
                        targetTransform.localScale = new Vector3(
                            sourceTransform.localScale.x,
                            sourceTransform.localScale.y,
                            -sourceTransform.localScale.z
                            );
                        break;
                    }
                case MirrorAxes.XZ:
                    {
                        targetTransform.localPosition = new Vector3(
                            sourceTransform.localPosition.x,
                            -sourceTransform.localPosition.y,
                            sourceTransform.localPosition.z
                            );
                        targetTransform.localRotation = Quaternion.Euler(
                            -sourceTransform.localRotation.eulerAngles.x,
                            sourceTransform.localRotation.eulerAngles.y,
                            -sourceTransform.localRotation.eulerAngles.z
                            );
                        targetTransform.localScale = new Vector3(
                            sourceTransform.localScale.x,
                            -sourceTransform.localScale.y,
                            sourceTransform.localScale.z
                            );
                        break;
                    }
                case MirrorAxes.YZ:
                    {
                        targetTransform.localPosition = new Vector3(
                            -sourceTransform.localPosition.x,
                            sourceTransform.localPosition.y,
                            sourceTransform.localPosition.z
                            );
                        targetTransform.localRotation = Quaternion.Euler(
                            sourceTransform.localRotation.eulerAngles.x,
                            -sourceTransform.localRotation.eulerAngles.y,
                            -sourceTransform.localRotation.eulerAngles.z
                            );
                        targetTransform.localScale = new Vector3(
                            -sourceTransform.localScale.x,
                            sourceTransform.localScale.y,
                            sourceTransform.localScale.z
                            );
                        break;
                    }
            }
        }
    }
}