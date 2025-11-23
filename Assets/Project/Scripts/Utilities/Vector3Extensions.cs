// Utility extension methods for Vector3
// Required by SeatedMode and other First Hand systems

using UnityEngine;

namespace Oculus.Interaction.ComprehensiveSample
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// Returns a new Vector3 with the Y component set to the specified value
        /// </summary>
        public static Vector3 SetY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }

        /// <summary>
        /// Returns a new Vector3 with the X component set to the specified value
        /// </summary>
        public static Vector3 SetX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }

        /// <summary>
        /// Returns a new Vector3 with the Z component set to the specified value
        /// </summary>
        public static Vector3 SetZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }
    }
}