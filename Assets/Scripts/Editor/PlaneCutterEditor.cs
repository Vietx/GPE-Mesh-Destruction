using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlaneCutter))]
public class PlaneCutterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PlaneCutter planeCutter = target as PlaneCutter;

        if (GUILayout.Button("Slice mesh"))
        {
            Undo.RecordObject(planeCutter, "Slice");
            planeCutter.SliceMesh();
            EditorUtility.SetDirty(planeCutter);
        }
    }
}
