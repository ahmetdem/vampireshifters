using Unity.Netcode;
using Cinemachine;
using UnityEngine;

public class CameraFollow : NetworkBehaviour
{
    private CinemachineVirtualCamera _virtualCamera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Try to find it immediately (in case we join a game already in progress)
            AssignCamera();
        }
    }

    private void Update()
    {
        // If we are the owner, but we haven't found the camera yet (e.g., scene is still loading), keep trying
        if (IsOwner && _virtualCamera == null)
        {
            AssignCamera();
        }
    }

    private void AssignCamera()
    {
        // Find the player follow camera by tag to avoid picking up arena cameras (Boss/PvP)
        GameObject camObj = GameObject.FindWithTag("PlayerFollowCamera");
        CinemachineVirtualCamera vcam = camObj?.GetComponent<CinemachineVirtualCamera>();

        if (vcam != null)
        {
            _virtualCamera = vcam;
            _virtualCamera.Follow = transform;
            Debug.Log($"Camera found and assigned to {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[CameraFollow] PlayerFollowCamera not found! Make sure a CinemachineVirtualCamera is tagged 'PlayerFollowCamera'.");
        }
    }
}
