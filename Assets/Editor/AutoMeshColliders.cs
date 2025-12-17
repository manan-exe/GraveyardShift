using UnityEngine;
using UnityEditor;

//initially had a problem with the colliders for the environment
//i got the environment from the unity asset store
//the environment had colliders but they were all messed up and not flush with the ground
//it looked like the player was floating or inside the ground
//had to use a script to auto mesh colliders with map geometry
//there were way too many objects to do this manually
public static class AutoMeshColliders
{
    [MenuItem("Tools/Colliders/Replace With MeshColliders (Selected Root)")]
    public static void ReplaceCollidersOnSelection() {
        //grab a game object
        GameObject root = Selection.activeGameObject;
        //if no objectthen error out
        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "No Selection",
                "Select the root environment object in the Hierarchy, then run this.",
                "OK"
            );
            return;
        }

        //counter to see how many objects processed
        int count = 0;

        //get mesh filter components
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        //loop over all mesh filters
        foreach (MeshFilter mf in meshFilters)
        {
            //save the game object reference
            GameObject go = mf.gameObject;

            //dont interact with skinned mesh renderers because they might be character models
            if (go.GetComponent<SkinnedMeshRenderer>() != null)
                continue;

            //loop over existing colliders
            foreach (var col in go.GetComponents<Collider>())
            {
                //if is not a mesh collider destroy it
                //need to do this because the map came with messed up colliders so i couldnt just apply
                //  new colliders because it would conflict with the existing ones
                if (!(col is MeshCollider))
                {
                    Object.DestroyImmediate(col);
                }
            }

            //if mesh collider exists update it. if not then add one
            MeshCollider mc = go.GetComponent<MeshCollider>();
            //add one
            if (mc == null)
            {
                mc = go.AddComponent<MeshCollider>();
            }
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            count++;
        }

        //output summary
        EditorUtility.DisplayDialog(
            "Done",
            $"Processed {count} MeshFilters under '{root.name}'.\n" +
            "MeshColliders have been added/updated.",
            "Nice"
        );
    }
}
