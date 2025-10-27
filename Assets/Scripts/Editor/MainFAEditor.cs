using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainFA))]
public class MainFAEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MainFA fa = target as MainFA;

        if (GUILayout.Button("New Seeds"))
        {
            Undo.RecordObject(fa, "New Seeds");
            fa.NewSeeds();
            EditorUtility.SetDirty(fa);
        }
        if (GUILayout.Button("Fracture"))
        {
            Undo.RecordObject(fa, "Fracture");
            fa.FractureMesh();
            EditorUtility.SetDirty(fa);
        }
    }
}