using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DamageSegmentation.Utils
{
    public class DictionaryClassNames
    {
        public string dataSet;
        public string GetName(int classIndex)
        {
            if (dataSet == "COCO")
            {
                return detectableObjectsCOCO[classIndex];
            }
            else if (dataSet == "cracks")
            {
                return detectableObjectsCracks[classIndex];
            }
            else if (dataSet == "spalling")
            {
                return detectableObjectsSpalling[classIndex];
            }
            else if (dataSet == "rust")
            {
                return detectableObjectsRust[classIndex];
            }
            else if (dataSet == "efflorescence")
            {
                return detectableObjectsEfflorescence[classIndex];
            }
            else if (dataSet == "exposedrebars")
            {
                return detectableObjectsExposedRebars[classIndex];
            }
            else if (dataSet == "fivedamages")
            {
                return detectableObjectsFiveDamages[classIndex];
            }
            else
            {
                return null;
            }

        }

        private static List<string> detectableObjectsCOCO = new()
        {
            "Person",
            "Bicycle",
            "Car",
            "Motorcycle",
            "Airplane",
            "Bus",
            "Train",
            "Truck",
            "Boat",
            "Traffic light",
            "Fire hydrant",
            "Stop sign",
            "Parking meter",
            "Bench",
            "Bird",
            "Cat",
            "Dog",
            "Horse",
            "Sheep",
            "Cow",
            "Elephant",
            "Bear",
            "Zebra",
            "Giraffe",
            "Backpack",
            "Umbrella",
            "Handbag",
            "Tie",
            "Suitcase",
            "Frisbee",
            "Skis",
            "Snowboard",
            "Sports ball",
            "Kite",
            "Baseball bat",
            "Baseball glove",
            "Skateboard",
            "Surfboard",
            "Tennis racket",
            "Bottle",
            "Wine glass",
            "Cup",
            "Fork",
            "Knife",
            "Spoon",
            "Bowl",
            "Banana",
            "Apple",
            "Sandwich",
            "Orange",
            "Broccoli",
            "Carrot",
            "Hot dog",
            "Pizza",
            "Donut",
            "Cake",
            "Chair",
            "Couch",
            "Potted plant",
            "Bed",
            "Dining table",
            "Toilet",
            "TV",
            "Laptop",
            "Mouse",
            "Remote",
            "Keyboard",
            "Cell phone",
            "Microwave",
            "Oven",
            "Toaster",
            "Sink",
            "Refrigerator",
            "Book",
            "Clock",
            "Vase",
            "Scissors",
            "Teddy bear",
            "Hair drier",
            "Toothbrush"
        };

        private static List<string> detectableObjectsCracks = new()
        {
            "crack"
        };

        private static List<string> detectableObjectsSpalling = new()
        {
            "spalling"
        };

        private static List<string> detectableObjectsRust = new()
        {
            "rust"
        };

        private static List<string> detectableObjectsEfflorescence = new()
        {
            "efflorescence"
        };

        private static List<string> detectableObjectsExposedRebars = new()
        {
            "exposedrebars"
        };

        private static List<string> detectableObjectsFiveDamages = new()
        {
            "crack",
            "spalling",
            "rust",
            "efflorescence",
            "exposedrebars"
        };
    }
}