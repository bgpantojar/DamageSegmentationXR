using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using UnityEngine;

namespace DamageSegmentationXR.Utils
{
    public class FrameResults
    {
        private Renderer resultsDisplayerPrefab;
        private Vector3 gridOrigin = new Vector3(0.0f, 2.0f, 1.0f); // Starting position of the grid
        private Vector3 gridSpacing = new Vector3(0.1978f, 0.12f, 0.2f); // Spacing between grid cells        
        private int gridColumns = 3; // Number of columns in the grid
        private List<Renderer> resultsDisplayers = new List<Renderer>(); // List to track spawned ResultsDisplayers
        private int currentGridIndex = 0; // Index to track the next position in the grid

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
            foreach (BoundingBox box in boundingBoxes)
            {
                DrawBoundingBox(texture, box.x, box.y, box.width, box.height, box.className, 0.2f, 0.2f, 0.8f);
            }

            // Overlay segmentation mask
            texture = GenerateSegmentationMask(texture, boundingBoxes, segmentation);

            // Assign texture to the spawned displayer
            resultsDisplayerSpawned.material.mainTexture = texture;

            // Set the position of the displayer to the camera's near plane
            float distanceToNearPlane = 0.9f; // offset image plane 0.9 unit at front of the camera (not 1 to avoid overlap with 3D boxes).
            float distanceCameraEyes = 0.08f; // to spawn the displayer approx at front  of the eyes instead of at front of the camera.
            Vector3 positionInFrontOfCamera = cameraTransform.position + cameraTransform.forward * distanceToNearPlane - cameraTransform.up * distanceCameraEyes;
            resultsDisplayerSpawned.transform.position = positionInFrontOfCamera;

            // Align the displayer's rotation with the camera's forward direction
            resultsDisplayerSpawned.transform.rotation = cameraTransform.rotation;

            // Scale the quad to match the camera's field of view
            float quadWidth = 2.0f * distanceToNearPlane * Mathf.Tan(64.69f * 0.5f * Mathf.Deg2Rad); // using HFOV from hololens documentation
            float quadHeight = quadWidth * (504.0f / 896.0f);

            resultsDisplayerSpawned.transform.localScale = new Vector3(quadWidth, quadHeight, 1.0f);

            resultsDisplayers.Add(resultsDisplayerSpawned);
        }

        public static void DrawBoundingBox(Texture2D texture, float x, float y, float w, float h, string className, float r, float g, float b)
        {
            Color boundingBoxColor = new Color(r, g, b);

            // Flip the y-coordinate (Unity texture coordinate system)
            float flippedY = texture.height - y;

            // Calculate width and height of the bounding box in pixel space
            int boxWidth = Mathf.RoundToInt(w);
            int boxHeight = Mathf.RoundToInt(h);

            // Calculate the starting position (top-left corner) of the bounding box
            int startX = Mathf.RoundToInt(x - boxWidth / 2);
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

            // Draw class name label at the center of the bounding box
            int labelX = startX + (boxWidth / 2);
            int labelY = startY - 1 + (boxHeight / 2);
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

        Texture2D GenerateSegmentationMask(Texture2D inputImage, BoundingBox[] boundingBoxes, Tensor<float> segmentation)
        {
            // Get the original image dimensions
            int originalWidth = inputImage.width;
            int originalHeight = inputImage.height;

            // Extract the segmentation tensor (1, 32, 160, 160)
            int maskChannels = segmentation.shape[1]; // 32 channels
            int maskHeight = segmentation.shape[2];   // 160 height
            int maskWidth = segmentation.shape[3];    // 160 width
            int numDetections = boundingBoxes.Length; //detection.shape[2];   // Number of detections

            // Initialize a Texture2D to store the segmentation mask
            Texture2D maskTexture = new Texture2D(maskWidth, maskHeight, TextureFormat.RGBA32, false);
            Color[] maskPixels = new Color[maskWidth * maskHeight];

            // Initialize maskPixels with transparency
            for (int i = 0; i < maskPixels.Length; i++)
            {
                maskPixels[i] = new Color(0, 0, 0, 0); // Transparent background
            }

            // Loop through each detection
            for (int i = 0; i < numDetections; i++)
            {
                // Generate a random color for the object
                Color objectColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1.0f); // Random RGB with full alpha

                // Generate the mask for this detection considering the bounding box
                float[] maskData = new float[maskWidth * maskHeight];

                // Flip bounding box vertically to account for Unity's coordinate system. Need also to make it proporsional to the prediction mask
                float boxX = (1.0f * maskWidth / originalWidth) * boundingBoxes[i].x;// detection[0, 0, i];  // Center x in segmentation space
                float boxY = (1.0f * maskHeight / originalHeight) * (originalHeight - boundingBoxes[i].y);//detection[0, 1, i]);  // Center y, flipped
                float boxW = (1.0f * maskWidth / originalWidth) * boundingBoxes[i].width;// width;//detection[0, 2, i];  // Width
                float boxH = (1.0f * maskHeight / originalHeight) * boundingBoxes[i].height;// height;// detection[0, 3, i]; // Height

                // Define starting and ending coordinates for rendering masks based on bounding box
                int startX = Mathf.Clamp(Mathf.RoundToInt(boxX - boxW / 2), 0, maskWidth - 1);
                int endX = Mathf.Clamp(Mathf.RoundToInt(boxX + boxW / 2), 0, maskWidth - 1);
                int startY = Mathf.Clamp(Mathf.RoundToInt(boxY - boxH / 2), 0, maskHeight - 1);
                int endY = Mathf.Clamp(Mathf.RoundToInt(boxY + boxH / 2), 0, maskHeight - 1);

                for (int c = 0; c < maskChannels; c++)
                {
                    // Restrict the loops to bounding box limits
                    for (int y = startY; y <= endY; y++)
                    {
                        for (int x = startX; x <= endX; x++)
                        {
                            int index = y * maskWidth + x;

                            // Flip vertically: Use (maskHeight - 1 - y) instead of y
                            maskData[index] += boundingBoxes[i].maskCoefficients[c] * segmentation[0, c, maskHeight - 1 - y, x];

                        }
                    }
                }

                float maxValueMask = maskData.Max();
                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        int index = y * maskWidth + x;
                        if (Mathf.Clamp01(maskData[index]) > 0.9f) // Threshold to ignore low-confidence mask areas
                        {
                            maskPixels[index] = objectColor; // Assign object-specific color
                        }
                    }
                }
            }

            // Apply the pixels to the mask texture
            maskTexture.SetPixels(maskPixels);
            maskTexture.Apply();

            // Resize the mask to match the original input image dimensions
            Texture2D resizedMask = ResizeTexture(maskTexture, originalWidth, originalHeight);

            // Overlay the segmentation mask on the original image
            Texture2D outputTexture = OverlayMask(inputImage, resizedMask);
            return outputTexture;
        }

        Texture2D ResizeTexture(Texture2D source, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(width, height, source.format, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            return result;
        }

        Texture2D OverlayMask(Texture2D original, Texture2D mask)
        {
            Texture2D output = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
            Color[] originalPixels = original.GetPixels();
            Color[] maskPixels = mask.GetPixels();
            Color[] outputPixels = new Color[originalPixels.Length];
            //Debug.Log($"MASK ALFA {maskPixels[10].a}");
            for (int i = 0; i < originalPixels.Length; i++)
            {
                // Blend the mask with the original image
                if (maskPixels[i].a > 0)
                {
                    outputPixels[i] = Color.Lerp(originalPixels[i], maskPixels[i], 0.5f);
                    //Debug.Log("HOLA");
                }
                else
                {
                    outputPixels[i] = originalPixels[i];
                }

            }

            output.SetPixels(outputPixels);
            output.Apply();
            return output;
        }

        // Destroy the last spawned ResultsDisplayer
        public void DestroyLastResultsDisplayer()
        {
            if (resultsDisplayers.Count > 0)
            {
                Renderer lastDisplayer = resultsDisplayers[^1];
                Object.Destroy(lastDisplayer.gameObject);
                resultsDisplayers.RemoveAt(resultsDisplayers.Count - 1);
            }
            else
            {
                Debug.LogWarning("No ResultsDisplayers to destroy.");
            }
        }

        // Locate the last ResultsDisplayer in the grid
        public void LocateLastResultsDisplayerInGrid()
        {
            if (resultsDisplayers.Count > 0)
            {
                Renderer lastDisplayer = resultsDisplayers[^1];

                // Calculate grid position
                int row = currentGridIndex / gridColumns;
                int col = currentGridIndex % gridColumns;

                // Calculate position in the grid
                Vector3 gridPosition = gridOrigin +
                                       new Vector3(col * gridSpacing.x, row * gridSpacing.y, 0);

                // Update the ResultsDisplayer's position and scale
                lastDisplayer.transform.position = gridPosition;
                lastDisplayer.transform.localScale = new Vector3(0.1778f, 0.1f, lastDisplayer.transform.localScale.z); //10% of spawned Scale\
                lastDisplayer.transform.rotation = Quaternion.identity;

                // Move to the next grid position
                currentGridIndex++;
            }
            else
            {
                Debug.LogWarning("No ResultsDisplayers to locate in the grid.");
            }
        }

    }
}