using System.IO;
using UnityEditor;
using UnityEngine;

public class ScreenshotWithGizmos : MonoBehaviour
{
    public int screenshotWidth = 1920;
    public int screenshotHeight = 1080;
    public string screenshotName = "ScreenshotWithGizmos.png";

    private Camera screenshotCamera;

    void Start()
    {
        Invoke(nameof(TakeScreenshotWithGizmos), 0.01f);
    }

    public void TakeScreenshotWithGizmos()
    {
        StartCoroutine(CaptureScreenshotWithGizmos());
    }

    private System.Collections.IEnumerator CaptureScreenshotWithGizmos()
    {
        var mainCamera = Camera.main;
        var renderTexture = new RenderTexture(screenshotWidth, screenshotHeight, 24, RenderTextureFormat.ARGB32);

        // Configure the main camera for screenshot
        mainCamera.targetTexture = renderTexture;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0, 0, 0, 0); // Transparent background
        mainCamera.Render();

        // Capture the main camera view
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(screenshotWidth, screenshotHeight, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, screenshotWidth, screenshotHeight), 0, 0);
        texture.Apply();

        // Render Gizmos to the same RenderTexture
        Handles.BeginGUI();
        Handles.DrawGizmos(mainCamera);
        Handles.EndGUI();

        // Save the screenshot as a PNG with transparency
        byte[] pngData = texture.EncodeToPNG();
        string filePath = Path.Combine(Application.dataPath, screenshotName);
        File.WriteAllBytes(filePath, pngData);
        Debug.Log($"Screenshot saved to {filePath}");

        // Clean up
        mainCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(texture);

        yield return null;
    }
}