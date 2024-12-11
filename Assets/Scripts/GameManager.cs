using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using DamageSegmentationXR.Utils;
using Unity.Sentis;
using System.Linq;

public class GameManager : MonoBehaviour
{
    private WebCamTexture webCamTexture;    
    [SerializeField]
    private Vector2Int requestedCameraSize = new(896, 504);
    [SerializeField]
    private int cameraFPS = 4;
    [SerializeField]
    private Vector2Int yoloInputImageSize = new(320, 320);
    [SerializeField]
    public Renderer resultsDisplayerPrefab;
    private FrameResults frameResultsDisplayer;
    private List<Transform> cameraTransformPool = new List<Transform>();
    private int maxCameraTransformPoolSize = 5;
    [SerializeField]
    private Renderer inputDisplayerRenderer;
    private Texture2D storedTexture;
    private Transform storedCameraTransform;

    // Start is called before the first frame update
    private async void Start()
    {
        // Initialize the ResultDisplayer object
        frameResultsDisplayer = new FrameResults(resultsDisplayerPrefab);

        // Access to the device camera image information
        webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
        webCamTexture.Play();
        await StartInferenceAsync();
    }

    // Asynhronous inference function
    private async Task StartInferenceAsync()
    {
        // Create a RenderTexture with the input size of the yolo model
        await Task.Delay(1000);
        var renderTexture = new RenderTexture(yoloInputImageSize.x, yoloInputImageSize.y, 24);

        // Variables to control time to spawn results
        //float lastSpawnTime = Time.time; // Keep track of the last spawn time
        //float spawnInterval = 5.0f; // Interval to spawn the results displayer

        while (true)
        {
            // Copying transform parameters of the device camera to a Pool
            cameraTransformPool.Add(Camera.main.CopyCameraTransForm());
            var cameraTransform = cameraTransformPool[^1];
            
            // Copying pixel data from webCamTexture to a RenderTexture - Resize the texture to the input size
            //Graphics.Blit(webCamTexture, renderTexture);
            Graphics.Blit(inputDisplayerRenderer.material.mainTexture, renderTexture); //use this for debugging. comment this for building the app
            await Task.Delay(32);

            // Convert RenderTexure to a Texture2D
            var texture = renderTexture.ToTexture2D();
            await Task.Delay(32);

            // Check if it's time to spawn
            //if (Time.time - lastSpawnTime >= spawnInterval)
            //{
            //    lastSpawnTime = Time.time; // Reset the timer
            //
            //    // Spawn results displayer
            //    frameResultsDisplayer.SpawnResultsDisplayer(texture, cameraTransform);
            //}

            // Set results data parameters that are callable from OnButtonClick functions
            SetResultsData(texture, cameraTransform);

            // Destroy the oldest cameraTransform gameObject from the Pool
            if (cameraTransformPool.Count > maxCameraTransformPoolSize)
            {
                Destroy(cameraTransformPool[0].gameObject);
                cameraTransformPool.RemoveAt(0);
            }
        }
    }

    // Method to store the data needed to call a function without parameters (OnButtonClick)
    public void SetResultsData(Texture2D texture, Transform cameraTransform)
    {
        // Access to texture and cameraTransform info and stored it in variables accessible from OnButtonClick functions
        storedTexture = texture;
        storedCameraTransform = cameraTransform;
    }

    // Public method without parameters to be called from UI Button
    public void OnButtonClickSpawnResultsDisplayer()
    {
        // Spawn results displayer using stored texture and cameraTransform
        frameResultsDisplayer.SpawnResultsDisplayer(storedTexture, storedCameraTransform);        
    }
}