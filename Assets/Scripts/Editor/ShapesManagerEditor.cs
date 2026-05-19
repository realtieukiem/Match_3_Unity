using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShapesManager))]
public class ShapesManagerEditor : Editor
{
    private void OnSceneGUI()
    {
        ShapesManager manager = (ShapesManager)target;
        if (!manager.UseBoardLayoutFrame)
            return;

        Rect frame = manager.GetBoardLayoutFrame();
        Vector2 center = frame.center;
        Vector2 size = frame.size;

        Vector3 leftHandle = new Vector3(frame.xMin, center.y, 0f);
        Vector3 rightHandle = new Vector3(frame.xMax, center.y, 0f);
        Vector3 topHandle = new Vector3(center.x, frame.yMax, 0f);
        Vector3 bottomHandle = new Vector3(center.x, frame.yMin, 0f);
        Vector3 centerHandle = new Vector3(center.x, center.y, 0f);

        Handles.color = new Color(0f, 0.8f, 1f, 1f);
        Handles.DrawSolidRectangleWithOutline(new Vector3[]
        {
            new Vector3(frame.xMin, frame.yMin, 0f),
            new Vector3(frame.xMin, frame.yMax, 0f),
            new Vector3(frame.xMax, frame.yMax, 0f),
            new Vector3(frame.xMax, frame.yMin, 0f)
        }, new Color(0f, 0.8f, 1f, 0.05f), new Color(0f, 0.8f, 1f, 1f));

        EditorGUI.BeginChangeCheck();

        Vector3 newCenter = Handles.PositionHandle(centerHandle, Quaternion.identity);

        Vector3 newLeft = Handles.Slider(leftHandle, Vector3.right, HandleUtility.GetHandleSize(leftHandle) * 0.08f, Handles.CubeHandleCap, 0f);
        Vector3 newRight = Handles.Slider(rightHandle, Vector3.right, HandleUtility.GetHandleSize(rightHandle) * 0.08f, Handles.CubeHandleCap, 0f);
        Vector3 newTop = Handles.Slider(topHandle, Vector3.up, HandleUtility.GetHandleSize(topHandle) * 0.08f, Handles.CubeHandleCap, 0f);
        Vector3 newBottom = Handles.Slider(bottomHandle, Vector3.up, HandleUtility.GetHandleSize(bottomHandle) * 0.08f, Handles.CubeHandleCap, 0f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(manager, "Edit Board Layout Frame");

            Vector2 centerDelta = (Vector2)newCenter - center;
            if (centerDelta.sqrMagnitude > 0f)
            {
                manager.BoardLayoutFrameCenter += centerDelta;
            }
            else
            {
                float xMin = Mathf.Min(newLeft.x, frame.xMax - 0.1f);
                float xMax = Mathf.Max(newRight.x, frame.xMin + 0.1f);
                float yMin = Mathf.Min(newBottom.y, frame.yMax - 0.1f);
                float yMax = Mathf.Max(newTop.y, frame.yMin + 0.1f);

                size = new Vector2(Mathf.Max(0.1f, xMax - xMin), Mathf.Max(0.1f, yMax - yMin));
                center = new Vector2((xMin + xMax) / 2f, (yMin + yMax) / 2f);

                manager.BoardLayoutFrameCenter = center;
                manager.BoardLayoutFrameSize = size;
            }

            EditorUtility.SetDirty(manager);
        }
    }
}
