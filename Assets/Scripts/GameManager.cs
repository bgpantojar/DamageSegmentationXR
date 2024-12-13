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

        Debug.Log($"Real image size {realImageSize}. Focal lenght {focalLenght}. FoV (H {horizontalFoV} V {verticalFoV}.");
        TextMeshPro classText = Instantiate(classTextPrefab);
        classText.text = $"Real camera size {realImageSize}. Focal lenght {focalLenght}. FoV (H {horizontalFoV} V {verticalFoV}.";

        // Compute camera intrinsics
        float fx = realImageSize.x / (2 * Mathf.Tan(Mathf.Deg2Rad * horizontalFoV / 2));
        float fy = realImageSize.y / (2 * Mathf.Tan(Mathf.Deg2Rad * verticalFoV / 2));
        float cx = realImageSize.x / 2;
        float cy = realImageSize.y / 2;
        Debug.Log($"fx {fx}, fy {fy}, cx {cx}, cy {cy}");

        // Assuming x, y detection cordinates.
        var x = 160;
        var y = 160;
        // x and y in the realImage
        var xImage = ((float)x / (float)yoloInputImageSize.x) * (float)realImageSize.x;
        var yImage = ((float)y / (float)yoloInputImageSize.y) * (float)realImageSize.y;
        // Step 1: Normalize image coordinates using intrinsic parameters
        var xImageNorm = (xImage - cx) / fx;
        var yImageNorm = (yImage - cy) / fy;
        // Step 2: Construct the ray direction in camera space
        Vector3 rayDirCameraSpace = new Vector3(xImageNorm, yImageNorm, 1.0f);
        rayDirCameraSpace.Normalize(); // Optional, depends on your raycasting method
        // Step 3: Transform the ray direction to world space
        var cameraTransformtemp = Camera.main.CopyCameraTransForm();
        Vector3 rayDirWorldSpace = cameraTransformtemp.rotation * rayDirCameraSpace;
        Vector3 rayOriginWorldSpace = cameraTransformtemp.position;
        // Step 4: Cast the ray onto the spatial map
        Ray ray = new Ray(rayOriginWorldSpace, rayDirWorldSpace);
        var XYthreeD = Vector3.zero;
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            XYthreeD = hitInfo.point; // 3D position in space
        }
        Debug.Log($"x {x} y {y} xImage {xImage} yImage {yImage} xImageNorm {xImageNorm} yImageNorm {yImageNorm} " +
            $"rayDirCameraSpace {rayDirCameraSpace} rayDirWorldSpace {rayDirWorldSpace} rayOriginWorldSpace {rayOriginWorldSpace} XYthreeD {XYthreeD}");
        TextMeshPro classTextRay = Instantiate(classTextPrefab, XYthreeD, Quaternion.identity);
        classTextRay.text = $"Hit!";
        classTextRay.transform.LookAt(Camera.main.transform); // Make the text always face the camera
        classTextRay.transform.Rotate(0, 180, 0); // Invert the text rotation by 180 degrees to make it readable (as LookAt will make it face away)
        //classText.text = $"3D {XYthreeD}";
        classText.text = $"x {x} y {y} xImage {xImage} yImage {yImage} xImageNorm {xImageNorm} yImageNorm {yImageNorm} " +
            $"rayDirCameraSpace {rayDirCameraSpace} rayDirWorldSpace {rayDirWorldSpace} rayOriginWorldSpace {rayOriginWorldSpace} XYthreeD {XYthreeD}";

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

            // Temp ray casting
            // Step 3: Transform the ray direction to world space
            Vector3 rayDirWorldSpacet = cameraTransform.rotation * rayDirCameraSpace;
            Vector3 rayOriginWorldSpacet = cameraTransform.position;
            // Step 4: Cast the ray onto the spatial map
            Ray rayt = new Ray(rayOriginWorldSpacet, rayDirWorldSpacet);
            var XYthreeDt = Vector3.zero;
            if (Physics.Raycast(rayt, out RaycastHit hitInfot, 10, LayerMask.GetMask("Spatial Mesh")))
            {
                XYthreeDt = hitInfot.point; // 3D position in space
            }
            Debug.Log($"XYthreeD {XYthreeDt} {count}");
            classTextRay.transform.position = XYthreeDt;
            classTextRay.text = $"Hit! {XYthreeDt} {count} {rayDirWorldSpacet} {rayOriginWorldSpacet}";
            classTextRay.transform.LookAt(cameraTransform); // Make the text always face the camera
            classTextRay.transform.Rotate(0, 180, 0);
            count++;

            // Copying pixel data from webCamTexture to a RenderTexture - Resize the texture to the input size
            Graphics.Blit(webCamTexture, renderTexture);
            //Graphics.Blit(inputDisplayerRenderer.material.mainTexture, renderTexture); //use this for debugging. comment this for building the app
            await Task.Delay(32);

            // Convert RenderTexure to a Texture2D
            var texture = renderTexture.ToTexture2D();
            await Task.Delay(32);

            // Execute inference using as inputImage the 2D texture
            await modelInference.ExecuteInference(texture, confidenceThreshold, iouThreshold);

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