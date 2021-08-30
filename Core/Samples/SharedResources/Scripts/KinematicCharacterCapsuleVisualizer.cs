namespace Games.NoSoySauce.Avatars.Samples
{
    using KinematicCharacterController;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///     Used to draw a capsule gizmo and preview mesh for <see cref="KinematicCharacterMotor" />
    /// </summary>
    public class KinematicCharacterCapsuleVisualizer : MonoBehaviour
    {
        public enum DisplayMode
        {
            Gizmo,
            Mesh,
            Combined
        }

        public KinematicCharacterMotor motor;
        public Material capsuleMeshMaterial;
        public DisplayMode mode = DisplayMode.Gizmo;

        private GameObject capsuleObject;

        private void Reset() { motor = GetComponent<KinematicCharacterMotor>(); }

    #if UNITY_EDITOR
        private void OnEnable()
        {
            if (!capsuleObject && motor)
            {
                capsuleObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsuleObject.name = "Character Debug Capsule";
                capsuleObject.hideFlags = HideFlags.NotEditable;

                var capsuleCollider = capsuleObject.GetComponent<Collider>();
                if (capsuleCollider)
                {
                    // Have to disable it first: otherwise, it will interfere with the Character during the first frame, as destroy operation is not immediate
                    capsuleCollider.enabled = false;
                    Destroy(capsuleCollider);
                }

                if (capsuleMeshMaterial)
                {
                    var capsuleRenderer = capsuleObject.GetComponent<MeshRenderer>();
                    if (capsuleRenderer) capsuleRenderer.material = capsuleMeshMaterial;
                }

                capsuleObject.transform.SetParent(motor.transform);
            }
        }

        private void OnDisable()
        {
            if (capsuleObject) Destroy(capsuleObject);
        }

        private void Update()
        {
            if (capsuleObject && motor)
            {
                bool shouldCapsuleMeshBeEnabled = mode == DisplayMode.Mesh || mode == DisplayMode.Combined;
                capsuleObject.SetActive(shouldCapsuleMeshBeEnabled);

                if (shouldCapsuleMeshBeEnabled)
                {
                    capsuleObject.transform.localPosition = new Vector3
                    {
                        x = 0f,
                        y = motor.Capsule.height / 2f,
                        z = 0f
                    };
                    capsuleObject.transform.localScale = new Vector3
                    {
                        x = motor.Capsule.radius * 2f,
                        y = motor.Capsule.height / 2f,
                        z = motor.Capsule.radius * 2f
                    };
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!motor) return;
            bool shouldCapsuleGizmoBeEnabled = mode == DisplayMode.Gizmo || mode == DisplayMode.Combined;
            if (!shouldCapsuleGizmoBeEnabled) return;

            var capsuleColor = Color.yellow;
            var capsule = motor.Capsule;
            var capsuleTransform = capsule.transform;
            DrawWireCapsule(capsuleTransform.TransformPoint(capsule.center), capsuleTransform.rotation, capsule.radius, capsule.height, capsuleColor);
        }

        // Source: https://forum.unity.com/threads/drawing-capsule-gizmo.354634/
        private static void DrawWireCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color)
        {
            var originalColor = Handles.color;

            Handles.color = color;
            var angleMatrix = Matrix4x4.TRS(position, rotation, Handles.matrix.lossyScale);

            using (new Handles.DrawingScope(angleMatrix))
            {
                float pointOffset = (height - radius * 2) / 2;

                //draw sideways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, radius);
                Handles.DrawLine(new Vector3(0, pointOffset, -radius), new Vector3(0, -pointOffset, -radius));
                Handles.DrawLine(new Vector3(0, pointOffset, radius), new Vector3(0, -pointOffset, radius));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, radius);
                //draw frontways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, radius);
                Handles.DrawLine(new Vector3(-radius, pointOffset, 0), new Vector3(-radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(radius, pointOffset, 0), new Vector3(radius, -pointOffset, 0));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, radius);
                //draw center
                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, radius);
            }

            Handles.color = originalColor;
        }
    #endif
    }
}