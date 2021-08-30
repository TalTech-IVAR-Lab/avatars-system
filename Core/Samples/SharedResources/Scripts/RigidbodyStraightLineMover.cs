namespace Games.NoSoySauce.Avatars.Samples
{
    using UnityEngine;

    /// <summary>
    ///     Moves <see cref="Rigidbody" /> back and forth along a straight line.
    /// </summary>
    /// <remarks>
    ///     Used for testing character controller interaction with rigidbodies.
    /// </remarks>
    public class RigidbodyStraightLineMover : MonoBehaviour
    {
        public float amplitude = 3f;
        public Vector3 direction = Vector3.forward;
        public bool moveKinematic = false;

        private Rigidbody rb;
        private Vector3 initialPosition;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.position;
        }

        private void FixedUpdate()
        {
            var targetPosition = initialPosition + direction * amplitude * Mathf.Sin(Time.time);
            
            if (moveKinematic)
            {
                rb.isKinematic = true;
                rb.MovePosition(targetPosition);
            }
            else
            {
                rb.isKinematic = false;
                var currentPosition = rb.position;
                var requiredMotion = targetPosition - currentPosition;
                rb.velocity = requiredMotion / Time.deltaTime;
            }
        }
    }
}