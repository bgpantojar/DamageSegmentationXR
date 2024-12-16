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
            //Vector3 positionInFrontOfCamera = cameraTransform.position + cameraTransform.forward * 1.0f;
            //resultsDisplayerSpawned.transform.position = positionInFrontOfCamera;

            // Make sure the quad faces the camera
            //resultsDisplayerSpawned.transform.rotation = Quaternion.LookRotation(cameraTransform.forward);

            
            // Get the camera #! This can inprove I think -- TO REVISIT!!
            Camera camera = Camera.main;

            // Set the position of the displayer to the camera's near plane
            float distanceToNearPlane = 1.0f; // offset 
            float distanceCameraEyes = 0.1f; // to spawn the displayer approx at front  of the eyes instead of at front of the camera.
            Vector3 positionInFrontOfCamera = camera.transform.position + camera.transform.forward * distanceToNearPlane - camera.transform.up * distanceCameraEyes;
            resultsDisplayerSpawned.transform.position = positionInFrontOfCamera;

            // Align the displayer's rotation with the camera's forward direction
            resultsDisplayerSpawned.transform.rotation = camera.transform.rotation;

            // Scale the quad to match the camera's field of view
            float quadWidth = 2.0f * distanceToNearPlane * Mathf.Tan(64.69f * 0.5f * Mathf.Deg2Rad); // using HFOV from hololens documentation
            float quadHeight = quadWidth * (504.0f / 896.0f);
            
            resultsDisplayerSpawned.transform.localScale = new Vector3(quadWidth, quadHeight, 1.0f);
        }
    }
}
