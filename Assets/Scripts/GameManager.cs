using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using DamageSegmentationXR.Utils;
using Unity.Sentis;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using System.IO;

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
    public float distanceCamEye = 0.08f;
    private Tensor<float> storedSegmentation;
    private BoundingBox[] storedfilteredBoundingBoxes;
    private bool enableInference = false; // Default value to start inference
    public enum DataSet
    {
        COCO,
        Cracks
    }
    public DataSet selectedDataSet;
    [SerializeField]
    private TextMeshPro performanceText;
    private string logFilePath;
    private string currentInspectionFolder;
    private bool enableTimeLog = false;
    private List<PreviousInspection> loadedInspections = new List<PreviousInspection>(); // List to track loaded previous inspections.
    
    // Start is called before the first frame update
    private async void Start()
    {
        // Ensure the "Documents" directory exists
        string documentsPath = Application.persistentDataPath + "/Documents/";
        if (!Directory.Exists(documentsPath))
        {
            Directory.CreateDirectory(documentsPath);
        }

        // Create inspection folder         
        // Look for existing folders whose names start with "inspection_"
        var directories = Directory.GetDirectories(documentsPath, "inspection_*");
        int nextId = directories.Length + 1;
        string inspectionID = "inspection_" + nextId;
        currentInspectionFolder = Path.Combine(documentsPath, inspectionID);
        Directory.CreateDirectory(currentInspectionFolder);
        //Debug.Log("Created inspection folder: " + currentInspectionFolder);

        // Generate a timestamped filename for each session if enableTimeLog is true
        if (enableTimeLog)
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss"); // e.g., 20240203_153045
            logFilePath = currentInspectionFolder + $"/performance_log_{modelAsset.name}_{timestamp}.txt";
            //performanceText.text = $"logFilePath: {logFilePath}";
        }

        // Initialize the ModelInference object
        string dataSet="";
        if (selectedDataSet == DataSet.COCO)
        {
            dataSet = "COCO";
        }
        else if (selectedDataSet == DataSet.Cracks)
        {
            dataSet = "cracks";
        }
        modelInference = new ModelInference(modelAsset, dataSet);

        // Initialize the ResultDisplayer object
        frameResultsDisplayer = new FrameResults(resultsDisplayerPrefab);

        // Pass folder information to the FrameResults instance so that it can save files there.
        frameResultsDisplayer.InspectionFolderPath = currentInspectionFolder;
        frameResultsDisplayer.InspectionID = inspectionID;

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
        //performanceText.text = $"image size: {realImageSize}";
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
            if (enableInference) // Perform inference only if enabled
            {
                float fullLoopStartTime = Time.realtimeSinceStartup; // Start full loop timer

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
                float inferenceStartTime = Time.realtimeSinceStartup; // Start inference timer
                (BoundingBox[] filteredBoundingBoxes, Tensor<float> segmentation) = await modelInference.ExecuteInference(texture, confidenceThreshold, iouThreshold);
                float inferenceTime = Time.realtimeSinceStartup - inferenceStartTime; // Inference duration

                foreach (BoundingBox box in filteredBoundingBoxes)
                {
                    // Instantiate classText object
                    (TextMeshPro classText, List<LineRenderer> lineRenderers) = boxesLabelsThreeD.SpawnClassText(classTextList, classTextPrefab, lineRendererPrefab, yoloInputImageSize, box, cameraTransform, realImageSize, fv, cx, cy, minSameObjectDistance, distanceCamEye);
                    if (classText != null)
                    {
                        classTextList.Add(classText);
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
                SetResultsData(texture, cameraTransform, segmentation, filteredBoundingBoxes);

                // Dispose segmentation tensor
                segmentation.Dispose();

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

                float fullLoopTime = Time.realtimeSinceStartup - fullLoopStartTime; // Full loop duration

                // Log to file if ebableTimeLog is true
                if (enableTimeLog)
                {
                    LogPerformance(inferenceTime, fullLoopTime);

                    // Display TimeDebug results
                    performanceText.text = $"Inference: {inferenceTime:F4}s\nFull Loop: {fullLoopTime:F4}s";
                }

                
            }
            else
            {
                // Wait before checking again to avoid a tight loop
                await Task.Delay(100);
            }
        }
    }

    // Method to store the data needed to call a function without parameters (OnButtonClick)
    public void SetResultsData(Texture2D texture, Transform cameraTransform, Tensor<float> segmentation, BoundingBox[] filteredBoundingBoxes)
    {
        // Access to texture and cameraTransform info and stored it in variables accessible from OnButtonClick functions
        storedTexture = texture;
        storedCameraTransform = cameraTransform;
        storedfilteredBoundingBoxes = filteredBoundingBoxes;

        // Clone the segmentation tensor to ensure its values persist and not vanishes to be able to display the segmentation results
        storedSegmentation = segmentation.ReadbackAndClone();
    }

    // Public method without parameters to be called from UI Button
    public void OnButtonClickSpawnResultsDisplayer()
    {
        // Spawn results displayer using stored texture and cameraTransform
        frameResultsDisplayer.SpawnResultsDisplayer(storedTexture, storedCameraTransform, storedSegmentation, storedfilteredBoundingBoxes, distanceCamEye, HFOV);
    }

    // Public method  to be called from UI Button - Destroying last ResultsDisplayer
    public void OnButtonClickDestroyResultsDisplayer()
    {
        frameResultsDisplayer.DestroyLastResultsDisplayer();
    }

    // Public method  to be called from UI Button - locate last ResultsDisplayer in a grid array
    public void OnButtonClickLocateResultsDisplayer()
    {
        frameResultsDisplayer.LocateLastResultsDisplayerInGrid();
    }

    // Public method to toggle the inference process
    public void ToggleInference(bool isEnabled)
    {
        enableInference = isEnabled;
        //Debug.Log($"enableInference {enableInference}");
        
    }

    // Public method without parameters to be called from UI Button2
    public void OnButtonClickSpawnResultsDisplayer2()
    {
        // Update texture in the input debugger displayer
        inputDisplayerRenderer.material.mainTexture = storedTexture;
    }
    private void LogPerformance(float inferenceTime, float fullLoopTime)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true)) // Append mode
        {
            string logEntry = $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} - " +
                              $"Model: {modelAsset.name}, Input Size: {yoloInputImageSize.x}x{yoloInputImageSize.y}, " +
                              $"Inference Time: {inferenceTime:F4} sec, Full Loop Time: {fullLoopTime:F4} sec";
            writer.WriteLine(logEntry);
        }

        //Debug.Log($"Logged Performance - {logFilePath}");
    }

    
    // Methods to load previous inspections.
    // Called from a UI button. The parameter "inspectionIndex" (0 to 3) corresponds to one of the four buttons.
    public void OnButtonClickLoadInspection(int inspectionIndex)
    {
        // Get all inspection folders from Documents.
        string documentsPath = Application.persistentDataPath + "/Documents/";
        var inspectionDirs = Directory.GetDirectories(documentsPath, "inspection_*")
                                      .OrderByDescending(d => d) 
                                      .ToList();

        // We want to load one of the last four inspections.
        if (inspectionIndex < inspectionDirs.Count)
        {
            string folderPath = inspectionDirs[inspectionIndex+1];
            //Debug.Log($"folder Path Previous inspection {folderPath}");
            string inspectionID = Path.GetFileName(folderPath);

            // Create a new PreviousInspection object.
            PreviousInspection pi = new PreviousInspection(folderPath, inspectionID, currentInspectionFolder, resultsDisplayerPrefab);
            pi.LoadInspection();
            loadedInspections.Add(pi);
        }
        else
        {
            Debug.LogError("No inspection folder available for index " + inspectionIndex);
        }
    }

    // Called from a UI button to destroy all loaded previous inspections.
    public void OnButtonClickDestroyPreviousInspections()
    {
        foreach (var pi in loadedInspections)
        {
            pi.DestroyInspection();
        }
        loadedInspections.Clear();
    }

}