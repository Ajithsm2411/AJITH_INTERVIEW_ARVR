using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterviewRoom
{
    /// <summary>
    /// Smooth free-fly camera allowing the user to fly around the interview room,
    /// orbit, and inspect details. Supports both new Input System and legacy input.
    /// </summary>
    public class FreeFlyCamera : MonoBehaviour
    {
        public float moveSpeed = 4.0f;
        public float sprintMultiplier = 2.0f;
        public float lookSensitivity = 1.5f;

        private float yaw;
        private float pitch;

        private void Start()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;
        }

        private void Update()
        {
            // Only control when active
            if (!enabled) return;

            Vector3 move = Vector3.zero;
            bool isSprinting = false;
            bool isLooking = false;
            Vector2 lookDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) move += transform.forward;
                if (kb.sKey.isPressed) move -= transform.forward;
                if (kb.aKey.isPressed) move -= transform.right;
                if (kb.dKey.isPressed) move += transform.right;
                if (kb.eKey.isPressed || kb.spaceKey.isPressed) move += Vector3.up;
                if (kb.qKey.isPressed || kb.leftCtrlKey.isPressed) move -= Vector3.up;
                isSprinting = kb.leftShiftKey.isPressed;
            }
            if (mouse != null)
            {
                isLooking = mouse.rightButton.isPressed;
                lookDelta = mouse.delta.ReadValue() * 0.1f;
            }
#else
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;
            isSprinting = Input.GetKey(KeyCode.LeftShift);
            isLooking = Input.GetMouseButton(1);
            lookDelta = new Vector2(Input.GetAxis("Mouse X") * 2f, Input.GetAxis("Mouse Y") * 2f);
#endif

            if (isLooking)
            {
                yaw += lookDelta.x * lookSensitivity;
                pitch -= lookDelta.y * lookSensitivity;
                pitch = Mathf.Clamp(pitch, -85f, 85f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1.0f);
            if (move.sqrMagnitude > 0.001f)
            {
                transform.position += move.normalized * (currentSpeed * Time.deltaTime);
            }
        }
    }
}
