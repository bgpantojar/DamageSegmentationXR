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
    private readonly List<TextMeshPro> classTextList = new();
    private int maxClassTextListSize = 5;

    // Start is called before the first frame update
    private async void Start()
    {
        // Initialize the ModelInference object
        modelInference = new ModelInference(modelAsset);

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
        await Task.Delay(1000);

        // Getting the image dimensions and properties from device camera
        var realImageSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
        var focalLenght = Camera.main.focalLength;
        var horizontalFoV = Camera.main.GetHorizontalFieldOfView();
        var verticalFoV = Camera.main.fieldOfView;
        //Debug.Log($"Real image size {realImageSize}. Focal lenght {focalLenght}. FoV (H {horizontalFoV} V {verticalFoV}.");
        //TextMeshPro classText = Instantiate(classTextPrefab);
        //classText.text = $"Real camera size {realImageSize}. Focal lenght {focalLenght}. FoV (H {horizontalFoV} V {verticalFoV}.";

        // Compute camera intrinsics
        //float fx = realImageSize.x / (2 * Mathf.Tan(Mathf.Deg2Rad * horizontalFoV / 2));
        //float fx = realImageSize.x / (2 * Mathf.Tan(Mathf.Deg2Rad * verticalFoV / 2));
        //float fy = realImageSize.y / (2 * Mathf.Tan(Mathf.Deg2Rad * verticalFoV / 2));
        //float cx = realImageSize.x / 2;
        //float cy = realImageSize.y / 2;
        //Debug.Log($"fx {fx}, fy {fy}, cx {cx}, cy {cy}");
        //float fx = 1390.0f; // paper
        //float fy = 1390.0f;
        //float cx = 1024.0f;
        //float cy = 540.0f;
        //float fx = 1246.7f;// based on fmm = 4.87
        //float fy = 1227.2f;
        //float cx = 448.0f;
        //float cy = 252.0f;
        float fx = 1793.85f;// based on HFOV specs = 64.69
        float fy = 1793.85f;
        float cx = 1136.0f;
        float cy = 639.0f;

        // Spawn temp classText for debugging
        //TextMeshPro classTextRay = Instantiate(classTextPrefab, classTextPrefab.transform.position, Quaternion.identity);

        // Create a RenderTexture with the input size of the yolo model
        var renderTexture = new RenderTexture(yoloInputImageSize.x, yoloInputImageSize.y, 24);

        // Variables to control time to spawn results
        //float lastSpawnTime = Time.time; // Keep track of the last spawn time
        //float spawnInterval = 5.0f; // Interval to spawn the results displayer
        var count = 0;
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
            //Debug.Log($"Number of detected objects {filteredBoundingBoxes.Length}");
            foreach (BoundingBox box in filteredBoundingBoxes) 
            {
                // Instantiate classText object
                //TextMeshPro classText = Instantiate(classTextPrefab, classTextPrefab.transform.position, Quaternion.identity);
                //Debug.Log($"This is a {box.className} located at x {box.x} y {box.y}");
                //SpawnClassText(box, classText, cameraTransform, realImageSize, focalLenght, fx, fy, cx, cy, count);
                TextMeshPro classText = SpawnClassText(box, cameraTransform, realImageSize, focalLenght, fx, fy, cx, cy, count);
                classTextList.Add(classText);
            }

            // Spawn classText gameObject for the detected object
            //SpawnClassText(classTextRay, cameraTransform, realImageSize, focalLenght, fx, fy, cx, cy, count, 160f, 160f);
            //count++;

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
            //Debug.Log($"Number of spawned classText {classTextList.Count} {classTextList.Count > maxClassTextListSize}");
            if (classTextList.Count > maxClassTextListSize)
            {
                //Debug.Log($"Destroying {classTextList.Count}");
                for (int i = 0; i < classTextList.Count - maxClassTextListSize; i++)
                {
                    Destroy(classTextList[i].gameObject);
                    classTextList.RemoveAt(i);
                }
                //Debug.Log($"Destroyed? {classTextList.Count}");
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

    //public void SpawnClassText( BoundingBox box, TextMeshPro classText, Transform cameraTransform, Vector2 realImageSize, float focalLenght, float fx, float fy, float cx, float cy, int count)
    public TextMeshPro SpawnClassText(BoundingBox box, Transform cameraTransform, Vector2 realImageSize, float focalLenght, float fx, float fy, float cx, float cy, int count)
    {
        // Flip vertically image coordinates as in the image space the origin is at the top and increases downwards
        float y = yoloInputImageSize.y - box.y;
        float x = box.x;

        // Computed x and y in the realImage frame extracted from camera
        //var xImage = ((float)x / (float)yoloInputImageSize.x) * (float)realImageSize.x;
        //var yImage = ((float)y / (float)yoloInputImageSize.y) * (float)realImageSize.y;
        //var xImage = ((float)x / (float)yoloInputImageSize.x) * 1504.0f; //dimensions photo taken with hololens
        //var yImage = ((float)y / (float)yoloInputImageSize.y) * 846.0f;
        var xImage = ((float)x / (float)yoloInputImageSize.x) * (2.0f * cx); // paper values
        var yImage = ((float)y / (float)yoloInputImageSize.y) * (2.0f * cy);

        // Step 1: Normalize image coordinates using intrinsic parameters
        var xImageNorm = (xImage - cx) / fx;
        var yImageNorm = (yImage - cy) / fy;
        //var xImageNorm = (xImage - 752.0f) / fx;
        //var yImageNorm = (yImage - 423.2f) / fy;

        // Step 2: Construct the ray direction in camera space
        Vector3 rayDirCameraSpace = new Vector3(xImageNorm, yImageNorm, 1.0f);
        //Debug.Log($"rayDirCameraSpace {rayDirCameraSpace}");
        rayDirCameraSpace.Normalize(); // Optional, depends on raycasting method
        //Debug.Log($"rayDirCameraSpaceNormalized {rayDirCameraSpace}");

        // Step 3: Transform the ray direction to world space
        Vector3 rayDirWorldSpace = cameraTransform.rotation * rayDirCameraSpace;
        Vector3 rayOriginWorldSpace = cameraTransform.position;
        
        // Step 4: Cast the ray onto the spatial map
        Ray ray = new Ray(rayOriginWorldSpace, rayDirWorldSpace);
        var XYthreeD = Vector3.zero;
        if (Physics.Raycast(ray, out RaycastHit hitInfo)) // this is to test in play mode. Comment to deploy in hololens
        //if (Physics.Raycast(ray, out RaycastHit hitInfo, 10, LayerMask.GetMask("Spatial Mesh"))) // Uncomment to deploy in hololens
        //if (Physics.SphereCast(ray, 0.15f, out var hitInfo, 10, LayerMask.GetMask("Spatial Mesh")))
        {
            XYthreeD = hitInfo.point; // 3D position in space
        }
        Debug.Log($"XYthreeD {XYthreeD} {count}");

        // Instantiate classText object
        TextMeshPro classText = Instantiate(classTextPrefab, classTextPrefab.transform.position, Quaternion.identity);
        classText.transform.position = XYthreeD;
        //classText.text = $"Hit! {XYthreeD} {count} {rayDirWorldSpace} {rayOriginWorldSpace}";
        classText.text = box.className;
        classText.transform.LookAt(cameraTransform); // Make the text always face the camera
        classText.transform.Rotate(0, 180, 0);  // Make the text readable left to right
        
        return classText;
    }
}