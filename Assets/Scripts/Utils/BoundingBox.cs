using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DamageSegmentationXR.Utils
{
    public class BoundingBox
    {
        public float x { get; set; }       // x coordinate of the box
        public float y { get; set; }       // y coordinate of the box
        public float width { get; set; }   // Width of the box
        public float height { get; set; }  // Height of the box
        public int classIndex { get; set; }   // Class index with the highest probability
        public string className { get; set; } // Name of the class using class map
        public float classProbability { get; set; } // class probability of the predicted class (highest probability)
        public List<float> maskCoefficients { get; set; } = new List<float>(new float[32]); // List of size 32 for mask coefficients
    }
}