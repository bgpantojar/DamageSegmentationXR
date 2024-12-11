using UnityEngine;

namespace DamageSegmentationXR.Utils
{
    public static class CameraExtensions
    {
        public static Transform CopyCameraTransForm(this Camera camera)
        {
            var g = new GameObject
            {
                transform =
                {
                    position = camera.transform.position,
                    rotation = camera.transform.rotation,
                    localScale = camera.transform.localScale
                }
            };
            return g.transform;
        }
    }
}