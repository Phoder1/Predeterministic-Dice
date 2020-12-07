using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DiceManager))]
public class StatsEditorScript : Editor {
    bool StatsDropdown;
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        StatsDropdown = EditorGUILayout.Foldout(StatsDropdown, "Stats");

        if (StatsDropdown) {
            for (int i = 0; i < 6; i++) {
                EditorGUILayout.LabelField((i+1)+ ":", DiceManager.dieStats[i].ToString());
            }
        }

    }
}

