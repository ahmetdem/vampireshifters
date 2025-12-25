using UnityEngine;

/// <summary>
/// Forces the window to maintain a 1:1 aspect ratio when resized.
/// Attach to a persistent GameObject (like GameBootstrap).
/// </summary>
public class AspectRatioEnforcer : MonoBehaviour
{
    [SerializeField] private int defaultSize = 900;
    
    private int lastWidth;
    private int lastHeight;

    private void Start()
    {
        // Set initial resolution
        Screen.SetResolution(defaultSize, defaultSize, FullScreenMode.Windowed);
        lastWidth = defaultSize;
        lastHeight = defaultSize;
    }

    private void Update()
    {
        // Only check in windowed mode
        if (Screen.fullScreenMode != FullScreenMode.Windowed) return;

        int currentWidth = Screen.width;
        int currentHeight = Screen.height;

        // Check if size changed
        if (currentWidth != lastWidth || currentHeight != lastHeight)
        {
            // Determine which dimension changed more
            int newSize;
            if (Mathf.Abs(currentWidth - lastWidth) > Mathf.Abs(currentHeight - lastHeight))
            {
                // Width changed more, use width as the new size
                newSize = currentWidth;
            }
            else
            {
                // Height changed more, use height as the new size
                newSize = currentHeight;
            }

            // Apply square resolution
            Screen.SetResolution(newSize, newSize, FullScreenMode.Windowed);
            lastWidth = newSize;
            lastHeight = newSize;
        }
    }
}
