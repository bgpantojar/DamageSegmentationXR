using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PreviousInspection
{
    public string InspectionFolder { get; private set; }
    public string InspectionID { get; private set; }
    private Renderer resultsDisplayerPrefab;

    // List to hold references to spawned quad GameObjects.
    private List<GameObject> spawnedQuads = new List<GameObject>();

    public PreviousInspection(string folderPath, string inspectionID, Renderer prefab)
    {
        InspectionFolder = folderPath;
        InspectionID = inspectionID;
        resultsDisplayerPrefab = prefab;
    }

    
    // Loads all saved inspection results from the InspectionFolder.
    // It looks for files with the .jpg extension and the corresponding .txt files.
    // For each pair, it instantiates a quad, sets the texture, and applies the transform.
    public void LoadInspection()
    {
        // Get all JPG files in the inspection folder.
        string[] imageFiles = Directory.GetFiles(InspectionFolder, "*.jpg");

        foreach (string imagePath in imageFiles)
        {
            // Expect a matching .txt file with the same name (except extension)
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);
            string transformPath = Path.Combine(InspectionFolder, fileNameWithoutExtension + ".txt");

            // Load image as texture.
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2); // size will be replaced by loaded image dimensions.
            texture.LoadImage(imageBytes);

            // Create a new quad using the resultsDisplayerPrefab.
            Renderer quadRenderer = Object.Instantiate(resultsDisplayerPrefab);
            quadRenderer.material.mainTexture = texture;

            // Read and parse transform information if the text file exists.
            if (File.Exists(transformPath))
            {
                string transformText = File.ReadAllText(transformPath);
                // Expected format (as saved):
                // "Position: (x, y, z)
                //  Rotation: (x, y, z)
                //  Scale: (x, y, z)"
                // A simple parsing method is shown below.
                Vector3 position = Vector3.zero;
                Vector3 rotationEuler = Vector3.zero;
                Vector3 scale = Vector3.one;

                string[] lines = transformText.Split('\n');
                foreach (string line in lines)
                {
                    if (line.StartsWith("Position:"))
                    {
                        // Remove "Position:" and any parentheses then split by comma.
                        string posData = line.Replace("Position:", "").Replace("(", "").Replace(")", "");
                        string[] posValues = posData.Split(',');
                        if (posValues.Length >= 3)
                        {
                            float.TryParse(posValues[0], out float px);
                            float.TryParse(posValues[1], out float py);
                            float.TryParse(posValues[2], out float pz);
                            position = new Vector3(px, py, pz);
                        }
                    }
                    else if (line.StartsWith("Rotation:"))
                    {
                        string rotData = line.Replace("Rotation:", "").Replace("(", "").Replace(")", "");
                        string[] rotValues = rotData.Split(',');
                        if (rotValues.Length >= 3)
                        {
                            float.TryParse(rotValues[0], out float rx);
                            float.TryParse(rotValues[1], out float ry);
                            float.TryParse(rotValues[2], out float rz);
                            rotationEuler = new Vector3(rx, ry, rz);
                        }
                    }
                    else if (line.StartsWith("Scale:"))
                    {
                        string scaleData = line.Replace("Scale:", "").Replace("(", "").Replace(")", "");
                        string[] scaleValues = scaleData.Split(',');
                        if (scaleValues.Length >= 3)
                        {
                            float.TryParse(scaleValues[0], out float sx);
                            float.TryParse(scaleValues[1], out float sy);
                            float.TryParse(scaleValues[2], out float sz);
                            scale = new Vector3(sx, sy, sz);
                        }
                    }
                }

                // Apply the saved transform information.
                quadRenderer.transform.position = position;
                quadRenderer.transform.rotation = Quaternion.Euler(rotationEuler);
                quadRenderer.transform.localScale = scale;
            }
            else
            {
                Debug.LogWarning("No transform data found for " + imagePath);
            }
            // make these quads children of a common parent for easier management. TODO
            spawnedQuads.Add(quadRenderer.gameObject);
        }
    }

    // Destroys all the spawned quad GameObjects that represent the loaded inspection.
    public void DestroyInspection()
    {
        foreach (GameObject quad in spawnedQuads)
        {
            Object.Destroy(quad);
        }
        spawnedQuads.Clear();
    }
}