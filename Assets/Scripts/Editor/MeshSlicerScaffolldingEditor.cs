using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshSlicerScaffolding))]
public class MeshSlicerScaffolldingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MeshSlicerScaffolding meshSlicer = target as MeshSlicerScaffolding;

        if (GUILayout.Button("Slice mesh"))
        {
            Undo.RecordObject(meshSlicer, "Slice");
            meshSlicer.SliceMesh();
            EditorUtility.SetDirty(meshSlicer);
        }
    }
}
