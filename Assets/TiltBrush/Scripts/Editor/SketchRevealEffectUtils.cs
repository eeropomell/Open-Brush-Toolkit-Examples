using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SketchRevealEffect))]
public class SketchRevealEffectUtils : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SketchRevealEffect sketchRevealEffectBase = (SketchRevealEffect)target;

        if (sketchRevealEffectBase == null)
        {
            return;
        }

        // calculates the real sketch time of the sketch, and sets TotalSketchTime to that value
        if (GUILayout.Button("Get Actual Sketch Time"))
        {
            sketchRevealEffectBase.totalSketchTime = sketchRevealEffectBase.CalculateTotalSketchTime();
        }

        // this is useful if meshes are deleted, because the SketchRevealEffect methods won't work if the strokes list changes suddenly
        // i.e. use this button if something isn't working
        if (GUILayout.Button("Reinitialize Strokes array"))
        {
            sketchRevealEffectBase.UpdateStrokesArray();
        }
    }

}