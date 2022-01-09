using UnityEngine;

namespace Utils
{
    public static class VectorUtils 
    {
        public static Vector3 XZPlane(this Vector3 vec)
        {
            return new Vector3(vec.x,0,vec.z);
        }
    }
}
