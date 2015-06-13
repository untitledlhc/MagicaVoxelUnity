﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(MVVoxModelVoxel))]
[CanEditMultipleObjects]
public class MVVoxModelVoxelInspector : Editor {

	public override void OnInspectorGUI ()
	{

		AU.AUEditorUtility.ColoredHelpBox (Color.yellow, "Combining multiple voxels into one");

		GUILayout.BeginHorizontal ();

		if (GUILayout.Button ("Combine Selected")) {
			if (this.targets != null && this.targets.Length > 1)
				CombineVoxels (System.Array.ConvertAll(this.targets, item => (MVVoxModelVoxel)item));
		}

		if (GUILayout.Button ("Combine All")) {
			MVVoxModel voxModel = (this.target as MVVoxModelVoxel).parentVoxModel;
			MVVoxModelVoxel[] allVoxels = voxModel.GetComponentsInChildren<MVVoxModelVoxel> ();
			CombineVoxels (allVoxels);
		}

		GUILayout.EndHorizontal ();
	}

	public static void CombineVoxels(MVVoxModelVoxel[] voxels) {
		if (voxels != null && voxels.Length > 0) {
			MVVoxelChunk chunk = new MVVoxelChunk ();
			MVVoxModel model = voxels [0].parentVoxModel;
			MVVoxelChunk origChunk = model.vox.voxelChunk;

			chunk.voxels = new byte[origChunk.sizeX, origChunk.sizeY, origChunk.sizeZ];
			foreach (MVVoxModelVoxel v in voxels) {
				chunk.voxels [v.voxel.x, v.voxel.y, v.voxel.z] = v.voxel.colorIndex;
			}

			MVImporter.GenerateFaces (chunk);
			Mesh[] meshes = MVImporter.CreateMeshesFromChunk (chunk, model.vox.palatte, model.sizePerVox);

			if (meshes.Length > 1) {
				Debug.LogError ("[MVCombine] Currently does not support combining voxels into multiple meshes, please reduce the number of voxels you are trying to combine");
				return;
			}

			string filename = System.IO.Path.GetFileNameWithoutExtension (model.filePath);

			Material mat = (model.defaultMaterial != null) ? model.defaultMaterial : new Material (Shader.Find ("MagicaVoxel/FlatColor"));

			int index = 0;
			foreach (Mesh mesh in meshes) {
				GameObject go = new GameObject ();
				go.name = string.Format ("{0} ({1})", filename, index);
				go.transform.SetParent (model.gameObject.transform);
				go.transform.localPosition = Vector3.zero;

				MeshFilter mf = go.AddComponent<MeshFilter> ();
				mf.mesh = mesh;

				MeshRenderer mr = go.AddComponent<MeshRenderer> ();
				mr.material = mat;

				MVVoxModelMesh voxMesh = go.AddComponent<MVVoxModelMesh> ();
				voxMesh.voxels = voxels.Select( o => o.voxel ).ToArray();

				if(model.addBoxColliders)
					go.AddComponent<BoxCollider> ();

				Selection.activeGameObject = go;

				index++;
			}


			foreach (MVVoxModelVoxel v in voxels) {
				GameObject.DestroyImmediate (v.gameObject);
			}
		}
		else {
			Debug.LogError("[MVCombine] Invalid voxels");
		}
	}

}