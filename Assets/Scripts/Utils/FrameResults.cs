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
            float distanceToNearPlane = camera.nearClipPlane + 1.0f; // Slightly offset to avoid clipping
            Vector3 positionInFrontOfCamera = camera.transform.position + camera.transform.forward * distanceToNearPlane;
            resultsDisplayerSpawned.transform.position = positionInFrontOfCamera;

            // Align the displayer's rotation with the camera's forward direction
            resultsDisplayerSpawned.transform.rotation = camera.transform.rotation;

            // Scale the quad to match the camera's field of view
            //float quadHeight = 2.0f * distanceToNearPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            //float quadHeight = 2.0f * distanceToNearPlane * Mathf.Tan(42.46f * 0.5f * Mathf.Deg2Rad); // computed from fx fy cx fy from paper
            //float quadHeight = 2.0f * distanceToNearPlane * Mathf.Tan(23.46f * 0.5f * Mathf.Deg2Rad); // computed from fx fy cx fy from fmm from specs
            //float quadWidth = 2.0f * distanceToNearPlane * Mathf.Tan(39.53f * 0.5f * Mathf.Deg2Rad); // computed from fx fy cx fy from fmm from specs
            float quadWidth = 2.0f * distanceToNearPlane * Mathf.Tan(64.69f * 0.5f * Mathf.Deg2Rad); // in hololens documentation
            //float quadWidth = quadHeight * camera.aspect;
            //float quadWidth = quadHeight * (1024.0f / 540.0f);
            //float quadWidth = quadHeight * (896.0f / 504.0f);
            //float quadHeight = quadWidth * (504.0f / 896.0f);
            float quadHeight = quadWidth * (1278.0f / 2272.0f);
            resultsDisplayerSpawned.transform.localScale = new Vector3(quadWidth, quadHeight, 1.0f);
            

        }
    }
}
