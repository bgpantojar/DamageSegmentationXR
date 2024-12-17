using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using DamageSegmentationXR.Utils;
using Unity.Sentis;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;

public class GameManager : MonoBehaviour
{
    private WebCamTexture webCamTexture;    
    [SerializeField]
    private Vector2Int requestedCameraSize = new(896, 504);
    [SerializeField]
    private int cameraFPS = 4; // Higher FPS for smoothness, lower for efficiency
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
    private ModelInference modelInference;
    public ModelAsset modelAsset;
    public float confidenceThreshold = 0.2f;
    public float iouThreshold = 0.4f;
    [SerializeField]
    public TextMeshPro classTextPrefab;
    private BoxesLabelsThreeD boxesLabelsThreeD;
    public float HFOV = 64.69f;
    private readonly List<TextMeshPro> classTextList = new();
    private int maxClassTextListSize = 5;
    public float minSameObjectDistance = 0.3f;
    public LineRenderer lineRendererPrefab;
    private readonly List<List<LineRenderer>> lineRendererLists = new(); // List of lists for line renderers

    // Start is called before the first frame update
    private async void Start()
    {
        // Initialize the ModelInference object
        modelInference = new ModelInference(modelAsset);

        // Initialize the ResultDisplayer object
        frameResultsDisplayer = new FrameResults(resultsDisplayerPrefab);

        // Initialize the BoxesLabels3D Object
        boxesLabelsThreeD = new BoxesLabelsThreeD();

        // Access to the device camera image information
        webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
        webCamTexture.Play();
        await StartInferenceAsync();
    }

    // Asynhronous inference function
    private async Task StartInferenceAsync()
    {
        await Task.Delay(1000);

        // Getting the image dimensions and intrinsics from device camera
        var realImageSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
        var fv = realImageSize.x / (2 * Mathf.Tan(Mathf.Deg2Rad * HFOV / 2)); // virtual focal lenght assuming the image plane dimensions are realImageSize
        float cx = realImageSize.x / 2;
        float cy = realImageSize.y / 2;
        
        // Create a RenderTexture with the input size of the yolo model
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
            Graphics.Blit(webCamTexture, renderTexture);
            //Graphics.Blit(inputDisplayerRenderer.material.mainTexture, renderTexture); //use this for debugging. comment this for building the app
            await Task.Delay(32);

            // Convert RenderTexure to a Texture2D
            var texture = renderTexture.ToTexture2D();
            await Task.Delay(32);

            // Execute inference using as inputImage the 2D texture
            BoundingBox[] filteredBoundingBoxes = await modelInference.ExecuteInference(texture, confidenceThreshold, iouThreshold);
            
            foreach (BoundingBox box in filteredBoundingBoxes) 
            {
                // Instantiate classText object
                (TextMeshPro classText, List<LineRenderer> lineRenderers) = boxesLabelsThreeD.SpawnClassText(classTextList, classTextPrefab, lineRendererPrefab, yoloInputImageSize, box, cameraTransform, realImageSize, fv, cx, cy, minSameObjectDistance);                                                                       
                //TextMeshPro classText = boxesLabelsThreeD.SpawnClassText(classTextList, classTextPrefab, lineRendererPrefab, yoloInputImageSize, box, cameraTransform, realImageSize, fv, cx, cy, minSameObjectDistance);
                if (classText != null)
                {
                    classTextList.Add(classText);
                    //List<LineRenderer> lineRenderers = boxesLabelsThreeD.SpawnClassBox(lineRendererPrefab, cameraTransform, box, yoloInputImageSize, realImageSize, fv, cx, cy);
                    lineRendererLists.Add(lineRenderers);
                }
            }

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
            //Debug.Log($"Number of prefab text {classTextList.Count}");
            if (classTextList.Count > maxClassTextListSize)
            {
                for (int i = 0; i < classTextList.Count - maxClassTextListSize; i++)
                {
                    Destroy(classTextList[i].gameObject);
                    classTextList.RemoveAt(i);

                    // Destroy all line renderers associated with this detected object
                    foreach (var line in lineRendererLists[i])
                    {
                        Destroy(line.gameObject);
                    }
                    lineRendererLists.RemoveAt(i);

                }                
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

    // Public method without parameters to be called from UI Button2
    public void OnButtonClickSpawnResultsDisplayer2()
    {
        // Update texture in the input debugger displayer
        inputDisplayerRenderer.material.mainTexture = storedTexture;
    }
    
}