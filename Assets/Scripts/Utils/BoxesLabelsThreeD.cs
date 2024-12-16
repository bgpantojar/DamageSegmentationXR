using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace DamageSegmentationXR.Utils
{
    public class BoxesLabelsThreeD
    {
        // Constructor to pass dependencies
        public BoxesLabelsThreeD()
        {
            
        }

        public TextMeshPro SpawnClassText(TextMeshPro classTextPrefab, Vector2Int yoloInputImageSize, BoundingBox box, Transform cameraTransform, Vector2 realImageSize, float fv, float cx, float cy, int count)
        {
            // Flip vertically image coordinates as in the image space the origin is at the top and increases downwards
            float y = yoloInputImageSize.y - box.y;
            float x = box.x;

            // Computed x and y in the realImage frame extracted from camera
            var xImage = ((float)x / (float)yoloInputImageSize.x) * (float)realImageSize.x;
            var yImage = ((float)y / (float)yoloInputImageSize.y) * (float)realImageSize.y;

            // Normalize image coordinates using intrinsic parameters (so normalized fv is 1)
            var xImageNorm = (xImage - cx) / fv;
            var yImageNorm = (yImage - cy) / fv;
            var nfv = (float)fv / (float)fv;

            // Construct the ray direction in camera space
            Vector3 rayDirCameraSpace = new Vector3(xImageNorm, yImageNorm, nfv);
            rayDirCameraSpace.Normalize(); // Optional, depends on raycasting method

            // Transform the ray direction to world space
            Vector3 rayDirWorldSpace = cameraTransform.rotation * rayDirCameraSpace;
            Vector3 rayOriginWorldSpace = cameraTransform.position;

            // Cast the ray onto the spatial map
            Ray ray = new Ray(rayOriginWorldSpace, rayDirWorldSpace);
            var XYthreeD = Vector3.zero;
            if (Physics.Raycast(ray, out RaycastHit hitInfo)) // this is to test in play mode. Comment to deploy in hololens
            //if (Physics.Raycast(ray, out RaycastHit hitInfo, 10, LayerMask.GetMask("Spatial Mesh"))) // Uncomment to deploy in hololens. With this rays only hit on Spatial Mesh
            {
                XYthreeD = hitInfo.point; // 3D position in space
            }

            // Instantiate classText object
            TextMeshPro classText = UnityEngine.Object.Instantiate(classTextPrefab, classTextPrefab.transform.position, Quaternion.identity);
            classText.transform.position = XYthreeD;
            classText.text = box.className;
            classText.transform.LookAt(cameraTransform); // Make the text always face the camera
            classText.transform.Rotate(0, 180, 0);  // Make the text readable left to right

            return classText;
        }
    }
}
