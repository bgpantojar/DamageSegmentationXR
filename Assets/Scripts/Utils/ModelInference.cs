using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using DamageSegmentationXR.Utils;
using DamageSegmentation.Utils;
using System.Data;
using System.Threading.Tasks;
using UnityEngine.Timeline;
using static UnityEngine.UI.GridLayoutGroup;
using TMPro;

namespace DamageSegmentationXR.Utils
{
    public class ModelInference
    {
        private DictionaryClassNames classNames;
        private Model runtimeModel;
        private Worker workerSegment;

        // Constructor to pass dependencies
        public ModelInference(ModelAsset modelAsset)
        {
            // Load model
            runtimeModel = ModelLoader.Load(modelAsset);

            // Create an inference engine (a worker) that runs in CPU
            workerSegment = new Worker(runtimeModel, BackendType.CPU);

            // Initialize yoloClassNames
            classNames = new DictionaryClassNames();
            classNames.dataSet = "COCO";
        }

        public async Task<(BoundingBox[], Tensor<float>)> ExecuteInference(Texture2D inputImage, float confidenceThreshold, float iouThreshold)
        {

            // Convert a texture to a tensor
            Tensor<float> inputTensor = TextureConverter.ToTensor(inputImage);

            // To run the model, use the schedule method
            //workerSegment.Schedule(inputTensor);

            // Get the output
            //Tensor<float> outputTensorSegment0 = workerSegment.PeekOutput("output0") as Tensor<float>;
            //Debug.Log("Got the detection outputTensor0" + outputTensorSegment0);
            //Tensor<float> outputTensorSegment1 = workerSegment.PeekOutput("output1") as Tensor<float>;
            //Debug.Log("Got the segmentation outputTensor1" + outputTensorSegment1);

            await Task.Delay(32);

            // Run the model with the inpuTensor using the ForwadAsync.
            (Tensor<float> outputTensorSegment0, Tensor<float> outputTensorSegment1) = await ForwardAsync(workerSegment, inputTensor);
            //Debug.Log("Got the detection outputTensor0" + outputTensorSegment0);
            //Debug.Log("Got the segmentation outputTensor1" + outputTensorSegment1);

            //CPU-accessible copy
            Tensor<float> resultsSegment0 = outputTensorSegment0.ReadbackAndClone();
            Tensor<float> resultsSegment1 = outputTensorSegment1.ReadbackAndClone();

            // Extract Bounding Boxes 
            BoundingBox[] boundingBoxes = ExtractBoundingBoxesConfidence(resultsSegment0, classNames, confidenceThreshold);
            //Debug.Log($"Number of bounding boxes that meet the confidence criteria {boundingBoxes.Length}");
            //foreach (BoundingBox box in boundingBoxes)
            //{
            //    Debug.Log($"Initial bbxs. This is a {box.className} located at x {box.x} y {box.y}");                
            //}

            // Filter Bounding Boxes considering overlapping with IOU
            BoundingBox[] filteredBoundingBoxes = FilterBoundingBoxesIoU(boundingBoxes, iouThreshold);
            //Debug.Log($"Number of filtered bounding boxes removing those that considerably overlap {filteredBoundingBoxes.Length}");

            // Dispose Tensor Data
            outputTensorSegment0.Dispose();
            outputTensorSegment1.Dispose();
            resultsSegment0.Dispose();
            //resultsSegment1.Dispose();
            inputTensor.Dispose();

            return (filteredBoundingBoxes, resultsSegment1);
        }

        // Nicked from https://github.com/Unity-Technologies/barracuda-release/issues/236#issue-1049168663
        public async Task<(Tensor<float>, Tensor<float>)> ForwardAsync(Worker workerSegment, Tensor<float> inputTensor)
        {
            var executor = workerSegment.ScheduleIterable(inputTensor);
            var it = 0;
            bool hasMoreWork;
            do
            {
                hasMoreWork = executor.MoveNext();
                if (++it % 20 == 0)
                {
                    await Task.Delay(32);
                }
            } while (hasMoreWork);

            // Fetch the two output tensors
            Tensor<float> outputTensor0 = workerSegment.PeekOutput("output0") as Tensor<float>;
            Tensor<float> outputTensor1 = workerSegment.PeekOutput("output1") as Tensor<float>;

            return (outputTensor0, outputTensor1);
        }

        // Extract bounding boxes that meet with conficenceThreshold criteria
        public BoundingBox[] ExtractBoundingBoxesConfidence(Tensor<float> result, DictionaryClassNames classNames, float confidenceThreshold = 0.2f)
        {
            // Get the number of attributes and number of bounding boxes output by the model
            int numAttributes = result.shape[1]; // 116 attributes per box x, y, w, h, 80p, 32c
            int numBoxes = result.shape[2]; // 2100 predicted boxes for yolo11n-seg-320
                                           
            // Create empty list to store the extracted bounding boxes that meet confidenceThreshold requirement
            List<BoundingBox> boxes = new List<BoundingBox>();

            // Iterate through each predicted box
            for (int i = 0; i < numBoxes; i++)
            {
                // Find the class with the highest probability
                int bestClassIndex = -1;
                float maxClassProbability = 0.0f;
                for (int c = 4; c < numAttributes - 32; c++) // Class probabilities(confidence) start at 5th index
                {
                    float classProbability = result[0, c, i];
                    if (classProbability > maxClassProbability)
                    {
                        maxClassProbability = classProbability;
                        bestClassIndex = c - 4; // Class index (0-based)
                    }
                }

                // Only consider boxes with confidence above the threshold
                if (maxClassProbability > confidenceThreshold)
                {
                    // Extract the bounding box coordinates
                    float xCenter = result[0, 0, i]; // x center
                    float yCenter = result[0, 1, i]; // y center
                    float width = result[0, 2, i];   // width
                    float height = result[0, 3, i];  // height

                    // Get the name of the class
                    string className = classNames.GetName(bestClassIndex);

                    // Create a bounding box object and add it to the list
                    BoundingBox box = new BoundingBox
                    {
                        x = xCenter,
                        y = yCenter,
                        width = width,
                        height = height,
                        classIndex = bestClassIndex,
                        className = className,
                        classProbability = maxClassProbability,

                    };
                    // Fill the maskCoefficients list with values from resultTensor[0, 84:116, b]
                    for (int j = 0; j < 32; j++)
                    {
                        // Get the value from the result tensor at the specified position
                        float coefficient = result[0, numAttributes - 32 + j, i];

                        // Set the value in the maskCoefficients list
                        box.maskCoefficients[j] = coefficient;
                    }
                    boxes.Add(box);
                }
            }
            
            // Dispose tensor
            result.Dispose();

            // Convert the list to an array and return
            return boxes.ToArray();
        }

        // Filter out bounding boxes by checking the overlap among them using IoU.
        public static BoundingBox[] FilterBoundingBoxesIoU(BoundingBox[] boundingBoxes, float iouThreshold = 0.4f)
        {
            List<BoundingBox> filteredBoxes = new List<BoundingBox>();
            bool[] isRemoved = new bool[boundingBoxes.Length]; // Track which boxes are removed

            // Iterate through each bounding box to compare with others
            for (int i = 0; i < boundingBoxes.Length; i++)
            {
                if (isRemoved[i]) continue; // Skip if the box is already marked for removal

                BoundingBox currentBox = boundingBoxes[i];
                for (int j = i + 1; j < boundingBoxes.Length; j++)
                {
                    if (isRemoved[j]) continue; // Skip if the box is already marked for removal

                    BoundingBox compareBox = boundingBoxes[j];
                    float iou = CalculateIoU(currentBox, compareBox);

                    if (iou > iouThreshold)
                    {
                        // Keep the box with the higher confidence
                        if (currentBox.classProbability >= compareBox.classProbability)
                        {
                            isRemoved[j] = true; // Mark the box with lower confidence for removal
                        }
                        else
                        {
                            isRemoved[i] = true; // Mark the current box for removal
                            break;
                        }
                    }
                }
            }

            // Collect all boxes that are not removed
            for (int i = 0; i < boundingBoxes.Length; i++)
            {
                if (!isRemoved[i])
                {
                    filteredBoxes.Add(boundingBoxes[i]);
                }
            }

            // Return filteredBoxes as Array
            return filteredBoxes.ToArray();
        }

        // Compute the IoU between two images to support the filtering of bounding boxes that overlap
        private static float CalculateIoU(BoundingBox boxA, BoundingBox boxB)
        {
            // Calculate intersection area
            float xA = Mathf.Max(boxA.x, boxB.x);
            float yA = Mathf.Max(boxA.y, boxB.y);
            float xB = Mathf.Min(boxA.x + boxA.width, boxB.x + boxB.width);
            float yB = Mathf.Min(boxA.y + boxA.height, boxB.y + boxB.height);

            float intersectionWidth = Mathf.Max(0, xB - xA);
            float intersectionHeight = Mathf.Max(0, yB - yA);
            float intersectionArea = intersectionWidth * intersectionHeight;

            // Calculate area of each box
            float boxAArea = boxA.width * boxA.height;
            float boxBArea = boxB.width * boxB.height;

            // Calculate union area
            float unionArea = boxAArea + boxBArea - intersectionArea;

            // Calculate IoU
            return intersectionArea / unionArea;
        }
    }
}