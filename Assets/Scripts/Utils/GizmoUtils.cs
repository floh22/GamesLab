using UnityEditor;
using UnityEngine;

namespace Utils
{
    public static class GizmoUtils
    {
        public static void DrawPoint(Vector3 pos, float radius, Color color)
        {
#if UNITY_EDITOR
            Color c = Handles.color;
            Handles.color = color;
            Handles.DrawWireDisc(pos // position
                , Vector3.up        // normal
                , radius);                // radius
            Handles.color = c;            // color
#endif  
        }

        public static void DrawLine(Vector3 p1, Vector3 p2, float width, Color color)
        {
            int count = 1 + Mathf.CeilToInt(width); // how many lines are needed.
            if (count == 1)
            {
                Gizmos.DrawLine(p1, p2);
            }
            else
            {
                Camera c = Camera.current;
                if (c == null)
                {
                    Debug.LogError("Camera.current is null");
                    return;
                }

                Vector3 scp1 = c.WorldToScreenPoint(p1);
                Vector3 scp2 = c.WorldToScreenPoint(p2);

                Vector3 v1 = (scp2 - scp1).normalized; // line direction
                Vector3 n = Vector3.Cross(v1, Vector3.forward); // normal vector

                for (int i = 0; i < count; i++)
                {
                    Vector3 o = n * (0.99f * width * ((float)i / (count - 1) - 0.5f));
                    Vector3 origin = c.ScreenToWorldPoint(scp1 + o);
                    Vector3 destiny = c.ScreenToWorldPoint(scp2 + o);
                    Gizmos.color = color;
                    Gizmos.DrawLine(origin, destiny);
                }
            }
        }
    }
}
