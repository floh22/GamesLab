using UnityEngine;
using Object = System.Object;

namespace Utils
{
    public static class ValidationUtils
    {
        public static void RequireNonNull(Object obj)
        {
            if (obj == null)
            {
                Debug.LogError("Required object is null!");
            }
        }
    }
}