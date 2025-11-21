using UnityEngine;
using UnityEditor;

public static class AutoMeshColliders
{
    [MenuItem("Tools/Colliders/Replace With MeshColliders (Selected Root)")]
    public static void ReplaceCollidersOnSelection() {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "No Selection",
                "Select the root environment object in the Hierarchy, then run this.",
                "OK"
            );
            return;
        }

        int count = 0;

        // Get all mesh filters under this root
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            GameObject go = mf.gameObject;

            // Skip if this looks like a character (has SkinnedMeshRenderer)
            if (go.GetComponent<SkinnedMeshRenderer>() != null)
                continue;

            // Optional: remove existing simple colliders on this object
            foreach (var col in go.GetComponents<Collider>())
            {
                if (!(col is MeshCollider))
                {
                    Object.DestroyImmediate(col);
                }
            }

            // Add or update MeshCollider
            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = go.AddComponent<MeshCollider>();
            }

            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;   // static environment pieces, not moving rigidbodies
            count++;
        }

        EditorUtility.DisplayDialog(
            "Done",
            $"Processed {count} MeshFilters under '{root.name}'.\n" +
            "MeshColliders have been added/updated.",
            "Nice"
        );
    }
}
