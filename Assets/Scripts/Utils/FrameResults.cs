using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

namespace DamageSegmentationXR.Utils
{
    public class FrameResults
    {
        private Renderer resultsDisplayerPrefab;

        // Constructor to pass dependencies
        public FrameResults(Renderer prefab)
        {
            resultsDisplayerPrefab = prefab;
        }

        // Method to spawn results displayer
        public void SpawnResultsDisplayer(Texture2D texture, Transform cameraTransform, Tensor<float> segmentation, BoundingBox[] boundingBoxes)
        {
            // Instantiate Results Displayer prefab
            Renderer resultsDisplayerSpawned = Object.Instantiate(resultsDisplayerPrefab);

            // Draw bounding boxes on texture
            DrawBoundingBoxes(boundingBoxes, texture);

            // Assign texture to the spawned displayer
            resultsDisplayerSpawned.material.mainTexture = texture;


            // Set the position of the quad to be 1 unit in front of the camera - temporary
            //Vector3 positionInFrontOfCamera = cameraTransform.position + cameraTransform.forward * 1.0f;
            //resultsDisplayerSpawned.transform.position = positionInFrontOfCamera;

            // Make sure the quad faces the camera
            //resultsDisplayerSpawned.transform.rotation = Quaternion.LookRotation(cameraTransform.forward);

            
            // Get the camera #! This can inprove I think -- TO REVISIT!!
            Camera camera = Camera.main;

            // Set the position of the displayer to the camera's near plane
            float distanceToNearPlane = 1.0f; // offset image plane 1 unit at front of the camera.
            float distanceCameraEyes = 0.08f; // to spawn the displayer approx at front  of the eyes instead of at front of the camera.
            Vector3 positionInFrontOfCamera = camera.transform.position + camera.transform.forward * distanceToNearPlane - camera.transform.up * distanceCameraEyes;
            resultsDisplayerSpawned.transform.position = positionInFrontOfCamera;

            // Align the displayer's rotation with the camera's forward direction
            resultsDisplayerSpawned.transform.rotation = camera.transform.rotation;

            // Scale the quad to match the camera's field of view
            float quadWidth = 2.0f * distanceToNearPlane * Mathf.Tan(64.69f * 0.5f * Mathf.Deg2Rad); // using HFOV from hololens documentation
            float quadHeight = quadWidth * (504.0f / 896.0f);
            
            resultsDisplayerSpawned.transform.localScale = new Vector3(quadWidth, quadHeight, 1.0f);
        }

        void DrawBoundingBoxes(BoundingBox[] boundingBoxes, Texture2D texture)
        {
            // Checking outputs
            foreach (BoundingBox box in boundingBoxes)
            {
                //classesString += box.className + " "; // Adding space between detected classes
                //Debug.Log(box.yoloClassName);
                DrawBoundingBox(texture, box.x, box.y, box.width, box.height, box.className, 0.2f, 0.2f, 0.8f);
            }
        }

        public static void DrawBoundingBox(Texture2D texture, float x, float y, float w, float h, string className, float r, float g, float b)
        {
            //Debug.Log("Hola bbx " + texture);
            // Convert color components to a Color object
            Color boundingBoxColor = new Color(r, g, b);

            // Flip the y-coordinate (Unity texture coordinate system)
            float flippedY = texture.height - y;

            // Calculate width and height of the bounding box in pixel space
            int boxWidth = Mathf.RoundToInt(w);
            int boxHeight = Mathf.RoundToInt(h);

            // Calculate the starting position (top-left corner) of the bounding box
            int startX = Mathf.RoundToInt(x - boxWidth / 2);
            //int startY = Mathf.RoundToInt(y - boxHeight / 2);
            int startY = Mathf.RoundToInt(flippedY - boxHeight / 2);

            // Clamp values to ensure they are within the texture bounds
            startX = Mathf.Clamp(startX, 0, texture.width - 1);
            startY = Mathf.Clamp(startY, 0, texture.height - 1);
            boxWidth = Mathf.Clamp(boxWidth, 0, texture.width - startX);
            boxHeight = Mathf.Clamp(boxHeight, 0, texture.height - startY);

            // Draw top and bottom horizontal borders of the bounding box
            for (int i = startX; i < startX + boxWidth; i++)
            {
                // Top border
                texture.SetPixel(i, startY, boundingBoxColor);
                // Bottom border
                texture.SetPixel(i, startY + boxHeight - 1, boundingBoxColor);
            }

            // Draw left and right vertical borders of the bounding box
            for (int i = startY; i < startY + boxHeight; i++)
            {
                // Left border
                texture.SetPixel(startX, i, boundingBoxColor);
                // Right border
                texture.SetPixel(startX + boxWidth - 1, i, boundingBoxColor);
            }

            // Draw class name label at the bottom left of the bounding box
            int labelX = startX;
            int labelY = startY + boxHeight - 1;

            //Debug.Log($"This is a {yoloClassName}");
            DrawTextOnTexture(texture, className, labelX, labelY, boundingBoxColor);

            // Apply the changes to the texture
            texture.Apply();
        }

        public static void DrawTextOnTexture(Texture2D texture, string text, int x, int y, Color color)
        {
            // This function draws a basic representation of text on the texture.
            // Each character is drawn as a small rectangle, just for illustration purposes.

            int fontSize = 5; // The size of each character

            foreach (char character in text)
            {
                for (int i = 0; i < fontSize; i++)
                {
                    for (int j = 0; j < fontSize; j++)
                    {
                        int pixelX = x + i;
                        int pixelY = y - j; // Adjusting to ensure it stays within the bounds

                        // Ensure we're within the bounds of the texture
                        if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
                        {
                            texture.SetPixel(pixelX, pixelY, color);
                        }
                    }
                }
                x += fontSize + 1; // Move the starting x position for the next character
            }
        }

    }
}
