using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Perception : MonoBehaviour
{
	public Vector2 viewingRange = Vector2.zero; // cone, length and diameter
	public float hearingRange = 0f; // radius of a circle
	public HashSet<Awareness> awarenesses = new HashSet<Awareness>(); // list of awarenesses, known stealths and their types

	public Unit unit { get { return GetComponentInParent<Unit>(); } }

	Mesh mesh;
	public MeshRenderer meshRenderer;
	public MeshFilter meshFilter;

	void Start() {
		mesh = new Mesh {
			name = string.Format("{0} Perception Mesh", unit.name)
		};
	}

	public void SetViewMesh(Vector3[] vertices) {
		mesh.vertices = vertices;
		mesh.triangles = new int[] {
			0, 1, 2
		};
		mesh.normals = new Vector3[] {
			Vector3.back, Vector3.back, Vector3.back
		};
		mesh.uv = new Vector2[] {
			Vector2.zero, Vector2.right, Vector2.up
		};
		mesh.tangents = new Vector4[] {
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f)
		};
		meshFilter.mesh = mesh;
	}

	public void HideViewMesh() {
		mesh.triangles = new int[0];
		mesh.vertices = new Vector3[0];
	}
}