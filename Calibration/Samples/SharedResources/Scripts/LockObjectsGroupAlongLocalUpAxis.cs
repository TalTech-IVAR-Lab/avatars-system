namespace Games.NoSoySauce.Avatars.Calibration.Body.Samples
{
    using System.Collections.Generic;
    using Malimbe.XmlDocumentationAttribute;
    using UnityEngine;
    using Zinnia.Process;

    /// <summary>
    ///     Locks a group of objects relative to he given object.
    /// </summary>
    /// <remarks>
    ///     Used in VR Avatar Calibration System Sample to keep the rig visualizer's head always in place
    ///     on the vertical axis of the calibration dummy.
    /// </remarks>
    public class LockObjectsGroupAlongLocalUpAxis : MonoBehaviour, IProcessable
    {
        /// <summary>
        /// Object which will be locked to the local up axis.
        /// </summary>
        [field: DocumentedByXml]
        public Transform mainObject;

        /// <summary>
        /// Objects which will be constrained by the main object.
        /// </summary>
        [field: DocumentedByXml]
        public List<Transform> dependentObjects = new List<Transform>();

        public void Process()
        {
            LockObjects();
        }

        private void LockObjects()
        {
            if (!mainObject) return;

            // Calculate displacement from desired (locked) position
            Vector3 displacedPosition = mainObject.localPosition;
            var lockedPosition = new Vector3(0f, displacedPosition.y, 0f);
            Vector3 displacementVector = displacedPosition - lockedPosition;
            Vector3 displacementVectorWorld = mainObject.parent.TransformVector(displacementVector);

            // Remove displacement from main and dependent objects
            mainObject.position -= displacementVectorWorld;
            foreach (Transform dependee in dependentObjects)
            {
                if (dependee) dependee.position -= displacementVectorWorld;
            }
        }
    }
}