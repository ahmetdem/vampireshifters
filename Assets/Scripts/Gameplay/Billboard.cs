using UnityEngine;

namespace Gameplay
{
    public class Billboard : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // Make the object look away from the camera's forward vector
            // This effectively makes the quad/text face the camera perfectly
            transform.forward = _mainCamera.transform.forward;
        }
    }
}
