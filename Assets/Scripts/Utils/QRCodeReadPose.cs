using UnityEngine;
using Microsoft.MixedReality.OpenXR;
using TMPro;
using System;
using System.IO;
using System.Linq;

public class QRCodeReadPose : MonoBehaviour
{
    [SerializeField] private ARMarkerManager markerManager;

    public TextMeshPro m_TextMeshPro;

    private bool _isScanning = false;

    private void Start()
    {
        if (markerManager == null)
        {
            Debug.LogError("ARMarkerManager is not assigned.");
            return;
        }

        // Subscribe to the markersChanged event
        markerManager.markersChanged += OnMarkersChanged;
        
    }

    
    // Called by your dynamic menu button to initiate a one-time QR scan.    
    public void StartQRCodeScan()
    {
        _isScanning = true;
        if (m_TextMeshPro != null)
        {
            m_TextMeshPro.text = "Scanning for QR Code...";
        }
        Debug.Log("QR Code scanning started.");
    }

    // Processes marker changes only when scanning is enabled.
    // <param name="args">Event arguments with added, updated, and removed markers.</param>
    private void OnMarkersChanged(ARMarkersChangedEventArgs args)
    {
        if (!_isScanning)
            return; // Process markers only if scanning is active

        // Check for newly added markers first
        if (args.added != null)
        {
            foreach (var marker in args.added)
            {
                ProcessMarker(marker);
                _isScanning = false;
                return; // Only process one marker per scan
            }
        }
        // If no added markers, check for updated markers
        if (args.updated != null)
        {
            foreach (var marker in args.updated)
            {
                ProcessMarker(marker);
                _isScanning = false;
                return; // Only process one marker per scan
            }
        }

        // clear any UI elements or logs associated with marker
        if (args.removed != null)
        {
            foreach (var removedMarker in args.removed)
            {
                HandleRemovedMarker(removedMarker);
            }
        }
    }

    // Retrieves the QR marker's decoded text and transform data,
    // displays it on the UI, and saves it to a text file.
    // <param name="marker">The ARMarker to process.</param>
    private void ProcessMarker(ARMarker marker)
    {
        // Retrieve the decoded string from the marker (e.g., the QR content)
        string qrCodeString = marker.GetDecodedString();

        // Retrieve the transform information from the marker
        Vector3 qrPosition = marker.transform.position;
        Quaternion qrRotation = marker.transform.rotation;
        Vector3 qrScale = marker.transform.localScale;

        // Build the information string
        string info = $"QR Code Data: {qrCodeString}\n" +
                      $"Position: {qrPosition}\n" +
                      $"Rotation: {qrRotation.eulerAngles}\n" +
                      $"Scale: {qrScale}";

        // Log the transform information to the console
        Debug.Log("QR Code Transform Information:\n" + info);

        // Spawn a new TextMeshPro object at the marker's position and rotation.
        // mainText is expected to be a prefab containing a TextMeshPro component.
        if (m_TextMeshPro != null)
        {
            // Instantiate a new TextMeshPro instance directly from the prefab.
            //TextMeshPro spawnedText = Instantiate(m_TextMeshPro, qrPosition, qrRotation);
            TextMeshPro spawnedText = Instantiate(m_TextMeshPro, qrPosition, Quaternion.identity);
            spawnedText.transform.LookAt(Camera.main.transform);
            spawnedText.transform.Rotate(0, 180f, 0); // Rotate so the text faces the camera
            //spawnedText.transform.localScale = qrScale;
            spawnedText.text = info;
            Destroy(spawnedText.gameObject, 5f);
        }
        else
        {
            Debug.LogError("No mainText prefab assigned.");
        }

        // Save the information to a text file
        SaveInfoToFile(info);
    }

    // Saves the provided information string to a text file in the "Documents" folder
    // inside the persistent data path. A timestamp is appended to the filename for uniqueness.
    // <param name="info">The string information to save.</param>
    private void SaveInfoToFile(string info)
    {
        try
        {
            string baseFolder = "";
            
            // Get all inspection folders from Documents.
            string documentsPath = Application.persistentDataPath + "/Documents/";
            var inspectionDirs = Directory.GetDirectories(documentsPath, "inspection_*")
                                            .OrderByDescending(d => d)
                                            .ToList();
            // If there are any inspection folders, take the first one (which is the highest due to OrderByDescending)
            baseFolder = inspectionDirs.Count > 0 ? inspectionDirs[0] : documentsPath;            

            // Create a filename with a timestamp to avoid overwriting previous files.
            string fileName = "QRCodeData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            string filePath = Path.Combine(baseFolder, fileName);

            // Write the information to the file.
            File.WriteAllText(filePath, info);
            Debug.Log("QR Code data saved to " + filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error saving QR Code data: " + ex.Message);
        }
    }

    // Handles logic for removed markers.
    // Clears any displayed information when a marker is removed.
    // <param name="removedMarker">The removed ARMarker.</param>
    private void HandleRemovedMarker(ARMarker removedMarker)
    {
        Debug.Log($"QR Code Removed! Marker ID: {removedMarker.trackableId}");
        if (m_TextMeshPro != null)
        {
            m_TextMeshPro.text = string.Empty;
        }
    }
}