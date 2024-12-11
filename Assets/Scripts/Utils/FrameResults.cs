using System.Collections;
using System.Collections.Generic;
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
        public void SpawnResultsDisplayer(Texture2D texture, Transform cameraTransform)
        {
            // Instantiate Results Displayer prefab
            Renderer resultsDisplayerSpawned = Object.Instantiate(resultsDisplayerPrefab);

            // Assign texture to the spawned displayer
            resultsDisplayerSpawned.material.mainTexture = texture;


            // Set the position of the quad to be 1 unit in front of the camera - temporary
            Vector3 positionInFrontOfCamera = cameraTransform.position + cameraTransform.forward * 1.0f;
            resultsDisplayerSpawned.transform.position = positionInFrontOfCamera;

            // Make sure the quad faces the camera
            resultsDisplayerSpawned.transform.rotation = Quaternion.LookRotation(cameraTransform.forward);
        }
    }
}
