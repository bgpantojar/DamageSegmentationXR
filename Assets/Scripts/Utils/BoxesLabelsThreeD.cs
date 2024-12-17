using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        public (TextMeshPro, List<LineRenderer>) SpawnClassText(List<TextMeshPro> classTextList, TextMeshPro classTextPrefab, LineRenderer lineRendererPrefab, Vector2Int yoloInputImageSize, BoundingBox box, Transform cameraTransform, Vector2 realImageSize, float fv, float cx, float cy, float minSameObjectDistance, float distanceCamEye)
        {
            // Get the coordinate of the bounding box center in 3D space using ray tracing method
            Vector3 XYthreeD = getXYThreeD(cameraTransform, box.x, box.y, yoloInputImageSize, realImageSize, fv, cx, cy);     

            // Check if the label corresponds to a new Object or to a one already labeled based on a predefined min distance
            var alreadyLabeled = classTextList.FirstOrDefault(
                classT => classT.text == box.className &&
                Vector3.Distance(XYthreeD, classT.transform.position) < minSameObjectDistance);
            
            // Instantiate classText object if the object has not been labeled
            if (!alreadyLabeled)
            {
                TextMeshPro classText = UnityEngine.Object.Instantiate(classTextPrefab, classTextPrefab.transform.position, Quaternion.identity);
                classText.transform.position = XYthreeD;
                classText.text = box.className;
                classText.transform.LookAt(cameraTransform); // Make the text always face the camera
                classText.transform.Rotate(0, 180, 0);  // Make the text readable left to right

                // Spawn bounding box
                List<LineRenderer> lineRenderers = SpawnClassBox(lineRendererPrefab, cameraTransform, box, yoloInputImageSize, realImageSize, fv, cx, cy, distanceCamEye);

                return (classText, lineRenderers);
            }
            else
            {
                return (null, null);
            }
        }

        public Vector3 getXYThreeD(Transform cameraTransform, float bx, float by, Vector2 yoloInputImageSize, Vector2 realImageSize, float fv, float cx, float cy, float distanceCamEye = 0.08f, bool isBox = false)
        {
            

            // Flip vertically image coordinates as in the image space the origin is at the top and increases downwards
            float y = yoloInputImageSize.y - by;
            float x = bx;

            // Computed x and y in the realImage frame extracted from camera
            var xImage = ((float)x / (float)yoloInputImageSize.x) * (float)realImageSize.x;
            var yImage = ((float)y / (float)yoloInputImageSize.y) * (float)realImageSize.y;

            // Normalize image coordinates using intrinsic parameters (so normalized fv is 1)
            var xImageNorm = (xImage - cx) / fv;
            var yImageNorm = (yImage - cy) / fv;
            var nfv = (float)fv / (float)fv;

            if (isBox)
            {
                // Project the bounding box to a plane places 1m at front of the camera and corrected vertically by the distance between eye and camera
                Vector3 coordPlanefvat1WorldSpace = cameraTransform.position + nfv * cameraTransform.forward + cameraTransform.right * xImageNorm + cameraTransform.up * yImageNorm - cameraTransform.up * distanceCamEye;
                return coordPlanefvat1WorldSpace;
            }
            else
            {
                // Construct the ray direction in camera space
                Vector3 rayDirCameraSpace = new Vector3(xImageNorm, yImageNorm, nfv);
                rayDirCameraSpace.Normalize(); // Optional, depends on raycasting method

                // Transform the ray direction to world space
                Vector3 rayDirWorldSpace = cameraTransform.rotation * rayDirCameraSpace;
            
                // Camera origin
                Vector3 rayOriginWorldSpace = cameraTransform.position;

                // Cast the ray onto the spatial map
                Ray ray = new Ray(rayOriginWorldSpace, rayDirWorldSpace);
                var XYthreeD = Vector3.zero;
                if (Physics.Raycast(ray, out RaycastHit hitInfo)) // this is to test in play mode. Comment to deploy in hololens
                //if (Physics.Raycast(ray, out RaycastHit hitInfo, 10, LayerMask.GetMask("Spatial Mesh"))) // Uncomment to deploy in hololens. With this rays only hit on Spatial Mesh
                {
                    XYthreeD = hitInfo.point; // 3D position in space
                }
                return XYthreeD;
            }
        }

            public List<LineRenderer> SpawnClassBox(LineRenderer lineRendererPrefab, Transform cameraTransform, BoundingBox box, Vector2 yoloInputImageSize, Vector2 realImageSize, float fv, float cx, float cy, float distanceCamEye)
        {
            // Get 2D coordinates of the bounding box corners
            float roundPixel = 1.0f; // To help avoiding detections outside image plane
            Vector2 topLeft = new Vector2(box.x - (box.width / 2) + roundPixel, box.y - (box.height / 2) + roundPixel);
            Vector2 topRight = new Vector2(box.x + (box.width / 2) - roundPixel, box.y - (box.height / 2) + roundPixel);
            Vector2 bottomRight = new Vector2(box.x + (box.width / 2) - roundPixel, box.y + (box.height / 2) - roundPixel);
            Vector2 bottomLeft = new Vector2(box.x - (box.width / 2) + roundPixel, box.y + (box.height / 2) - roundPixel);
            
            // Get the coordinate of the bounding box corners in 3D space using ray tracing method
            var positionInSpaceTopLeft = getXYThreeD(cameraTransform, topLeft.x, topLeft.y, yoloInputImageSize, realImageSize, fv, cx, cy, distanceCamEye, true);
            var positionInSpaceTopRight = getXYThreeD(cameraTransform, topRight.x, topRight.y, yoloInputImageSize, realImageSize, fv, cx, cy, distanceCamEye, true);
            var positionInSpaceBottomRight = getXYThreeD(cameraTransform, bottomRight.x, bottomRight.y, yoloInputImageSize, realImageSize, fv, cx, cy, distanceCamEye, true);
            var positionInSpaceBottomLeft = getXYThreeD(cameraTransform, bottomLeft.x, bottomLeft.y, yoloInputImageSize, realImageSize, fv, cx, cy, distanceCamEye, true);

            // Build Vector3 with bounding box corners
            Vector3[] boundingBoxCorners3D = new Vector3[] { positionInSpaceTopLeft, positionInSpaceTopRight, positionInSpaceBottomRight, positionInSpaceBottomLeft, positionInSpaceTopLeft };
            
            List<LineRenderer> lineRenderers = new List<LineRenderer>();

            // Spawn bounding box line
            for (int i = 0; i < boundingBoxCorners3D.Length - 1; i++)
            {
                Vector3 start = boundingBoxCorners3D[i];
                Vector3 end = boundingBoxCorners3D[i + 1];

                LineRenderer lineRenderer = UnityEngine.Object.Instantiate(lineRendererPrefab);
                lineRenderer.transform.position = start;

                // Calculate direction and rotation
                Vector3 direction = (end - start).normalized;
                lineRenderer.transform.rotation = Quaternion.LookRotation(direction);

                // Calculate scale
                float distance = Vector3.Distance(start, end);
                lineRenderer.transform.localScale = new Vector3(lineRenderer.transform.localScale.x, lineRenderer.transform.localScale.y, distance);

                // Add lineRenderer to the list for this bounding box
                lineRenderers.Add(lineRenderer);
            }

            return lineRenderers;
        }
    }
}