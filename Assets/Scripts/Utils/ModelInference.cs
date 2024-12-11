using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using DamageSegmentationXR.Utils;
using DamageSegmentation.Utils;
using System.Data;

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
            workerSegment = new Worker(this.runtimeModel, BackendType.CPU);

            // Initialize yoloClassNames
            classNames = new DictionaryClassNames();
            classNames.dataSet = "COCO";
        }

        public void ExecuteInference(Texture2D inputImage)
        {

            // Convert a texture to a tensor
            Tensor<float> inputTensor = TextureConverter.ToTensor(inputImage);

            // To run the model, use the schedule method
            workerSegment.Schedule(inputTensor);

            // Get the output
            Tensor<float> outputTensorSegment0 = workerSegment.PeekOutput("output0") as Tensor<float>;
            Debug.Log("Got the detection outputTensor0" + outputTensorSegment0);
            Tensor<float> outputTensorSegment1 = workerSegment.PeekOutput("output1") as Tensor<float>;
            Debug.Log("Got the segmentation outputTensor1" + outputTensorSegment1);

            // Dispose Tensor Data
            outputTensorSegment0.Dispose();
            outputTensorSegment1?.Dispose();
            inputTensor.Dispose();
        }
    }
}