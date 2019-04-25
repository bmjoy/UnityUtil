using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GeometryBuffer {

	private List<ObjectData> objects;
	public List<Vector3> vertices;
	public List<Vector2> uvs;
	public List<Vector3> normals;
	public int unnamedGroupIndex = 1; // naming index for unnamed group. like "Unnamed-1"
	
	private ObjectData current;
	private class ObjectData {
		public string name;
		public List<GroupData> groups;
		public List<FaceIndices> allFaces;
		public int normalCount;
		public ObjectData() {
			groups = new List<GroupData>();
			allFaces = new List<FaceIndices>();
			normalCount = 0;
		}
	}
	
	private GroupData curgr;
	private class GroupData {
		public string name;
		public string materialName;
		public List<FaceIndices> faces;
		public GroupData() {
			faces = new List<FaceIndices>();
		}
		public bool isEmpty { get { return faces.Count == 0; } }
	}
	
	public GeometryBuffer() {
		objects = new List<ObjectData>();
		ObjectData d = new ObjectData();
		d.name = "default";
		objects.Add(d);
		current = d;
		
		GroupData g = new GroupData();
		g.name = "default";
		d.groups.Add(g);
		curgr = g;
		
		vertices = new List<Vector3>();
		uvs = new List<Vector2>();
		normals = new List<Vector3>();
	}
	
	public void PushObject(string name) {
		//Debug.Log("Adding new object " + name + ". Current is empty: " + isEmpty);
		string currentMaterial = current.groups [current.groups.Count - 1].materialName;

		if(isEmpty) objects.Remove(current);
		
		ObjectData n = new ObjectData();
		n.name = name;
		objects.Add(n);

		GroupData g = new GroupData();
		g.materialName = currentMaterial;
		g.name = "default";
		n.groups.Add(g);
		
		curgr = g;
		current = n;
	}
	
	public void PushGroup(string name) {
		string currentMaterial = current.groups [current.groups.Count - 1].materialName;

		if(curgr.isEmpty) current.groups.Remove(curgr);
		GroupData g = new GroupData();
		g.materialName = currentMaterial;
		if (name == null) {
			name = "Unnamed-"+unnamedGroupIndex;
			unnamedGroupIndex++;
		}
		g.name = name;
		current.groups.Add(g);
		curgr = g;
	}
	
	public void PushMaterialName(string name) {
		//Debug.Log("Pushing new material " + name + " with curgr.empty=" + curgr.isEmpty);
		if(!curgr.isEmpty) PushGroup(name);
		if(curgr.name == "default") curgr.name = name;
		curgr.materialName = name;
	}
	
	public void PushVertex(Vector3 v) {
		vertices.Add(v);
	}
	
	public void PushUV(Vector2 v) {
		uvs.Add(v);
	}
	
	public void PushNormal(Vector3 v) {
		normals.Add(v);
	}
	
	public void PushFace(FaceIndices f) {
		curgr.faces.Add(f);
		current.allFaces.Add(f);
		if (f.vn >= 0) {
			current.normalCount++;
		}
	}
	
	public void Trace() {
		Debug.Log("OBJ has " + objects.Count + " object(s)");
		Debug.Log("OBJ has " + vertices.Count + " vertice(s)");
		Debug.Log("OBJ has " + uvs.Count + " uv(s)");
		Debug.Log("OBJ has " + normals.Count + " normal(s)");
		foreach(ObjectData od in objects) {
			Debug.Log(od.name + " has " + od.groups.Count + " group(s)");
			foreach(GroupData gd in od.groups) {
				Debug.Log(od.name + "/" + gd.name + " has " + gd.faces.Count + " faces(s)");
			}
		}
		
	}
	
	public int numObjects { get { return objects.Count; } }	
	public bool isEmpty { get { return vertices.Count == 0; } }
	public bool hasUVs { get { return uvs.Count > 0; } }
	public bool hasNormals { get { return normals.Count > 0; } }
	
	public static int MAX_VERTICES_LIMIT_FOR_A_MESH = 649990;

	public void PopulateMeshes(GameObject[] gs, Dictionary<string, Material> mats) {
		if(gs.Length != numObjects) return; // Should not happen unless obj file is corrupt...
		Debug.Log("PopulateMeshes GameObjects count:"+gs.Length);
		for(int i = 0; i < gs.Length; i++) {
			ObjectData od = objects[i];
			bool objectHasNormals = (hasNormals && od.normalCount > 0);
			
			if(od.name != "default") gs[i].name = od.name;
			Debug.Log("PopulateMeshes object name:"+od.name);

            Dictionary<string, int> vIdxCount = new Dictionary<string, int>();
            int vcount = 0;
            foreach (FaceIndices fi in od.allFaces)
            {
                string key = GetFaceIndicesKey(fi);
                int idx;
                // avoid duplicates
                if (!vIdxCount.TryGetValue(key, out idx))
                {
                    vIdxCount.Add(key, vcount);
                    vcount++;
                }
            }
            Vector3[] tvertices = new Vector3[vcount];
            Vector2[] tuvs = new Vector2[vcount];
            Vector3[] tnormals = new Vector3[vcount];
            
            

            foreach (FaceIndices fi in od.allFaces)
            {
                string key = GetFaceIndicesKey(fi);
                int k = vIdxCount[key];
                tvertices[k] = vertices[fi.vi];

                if (hasUVs)
                {
                    tuvs[k] = uvs[fi.vu];
                }
                if (hasNormals && fi.vn >= 0)
                {
                    tnormals[k] = normals[fi.vn];
                }
            }

            //Vector3[] tvertices = new Vector3[od.allFaces.Count];
            //Vector2[] tuvs = new Vector2[od.allFaces.Count];
            //Vector3[] tnormals = new Vector3[od.allFaces.Count];
            //int k = 0;
            //foreach (FaceIndices fi in od.allFaces)
            //{
            //    if (k >= MAX_VERTICES_LIMIT_FOR_A_MESH)
            //    {
            //        Debug.LogWarning("maximum vertex number for a mesh exceeded for object:" + gs[i].name);
            //        break;
            //    }
            //    tvertices[k] = vertices[fi.vi];
            //    if (hasUVs) tuvs[k] = uvs[fi.vu];
            //    if (hasNormals && fi.vn >= 0) tnormals[k] = normals[fi.vn];
            //    k++;
            //}

            Mesh m = (gs[i].GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
			m.vertices = tvertices;
			if(hasUVs) m.uv = tuvs;
			if(objectHasNormals) m.normals = tnormals;

			if(od.groups.Count == 1) {
				Debug.Log("PopulateMeshes only one group: "+od.groups[0].name);
				GroupData gd = od.groups[0];
				string matName = (gd.materialName != null) ? gd.materialName : "default"; // MAYBE: "default" may not enough.
				if (mats.ContainsKey(matName)) {
					gs[i].GetComponent<Renderer>().material = mats[matName];
					Debug.Log("PopulateMeshes mat:"+matName+" set.");
				}
				else {
					Debug.LogWarning("PopulateMeshes mat:"+matName+" not found.");
				}

                int[] triangles = new int[gd.faces.Count];
                for (int j = 0; j < triangles.Length; j++)
                {
                    FaceIndices fi = gd.faces[j];
                    string key = GetFaceIndicesKey(fi);

                    triangles[j] = vIdxCount[key];
                    //triangles[j] = j;
                }
				
				m.triangles = triangles;

			} else {
				int gl = od.groups.Count;
				Material[] materials = new Material[gl];
				m.subMeshCount = gl;
				int c = 0;
				
				Debug.Log("PopulateMeshes group count:"+gl);
				for(int j = 0; j < gl; j++) {
					string matName = (od.groups[j].materialName != null) ? od.groups[j].materialName : "default"; // MAYBE: "default" may not enough.
					if (mats.ContainsKey(matName)) {
						materials[j] = mats[matName];
						Debug.Log("PopulateMeshes mat:"+matName+" set.");
					}
					else {
						Debug.LogWarning("PopulateMeshes mat:"+matName+" not found.");
					}
					
					int l = od.groups[j].faces.Count + c;

                    int[] triangles = new int[od.groups[j].faces.Count];
                    for (int k = 0; k < triangles.Length; k++)
                    {
                        FaceIndices fi = od.groups[j].faces[k];
                        string key = GetFaceIndicesKey(fi);

                        triangles[k] = vIdxCount[key];
                        //triangles[j] = j;
                    }
                    m.SetTriangles(triangles, j);
				}
				
				gs[i].GetComponent<Renderer>().materials = materials;
			}
			if (!objectHasNormals) {
				m.RecalculateNormals();
			}
            //Solve(m);

        }
        //ObjLoader.instance.gameObject.SendMessage("OnInfoReceived", "Load mesh success!");
	}

    public static string GetFaceIndicesKey(FaceIndices fi)
    {
        return fi.vi.ToString() + "/" + fi.vu.ToString() + "/" + fi.vn.ToString();
    }

    public static void Solve(Mesh origMesh)
    {
        if (origMesh.uv == null || origMesh.uv.Length == 0)
        {
            Debug.LogWarning("Unable to compute tangent space vectors - texture coordinates not defined.");
            return;
        }
        if (origMesh.vertices == null || origMesh.vertices.Length == 0)
        {
            Debug.LogWarning("Unable to compute tangent space vectors - vertices not defined.");
            return;
        }
        if (origMesh.normals == null || origMesh.normals.Length == 0)
        {
            Debug.LogWarning("Unable to compute tangent space vectors - normals not defined.");
            return;
        }
        if (origMesh.triangles == null || origMesh.triangles.Length == 0)
        {
            Debug.LogWarning("Unable to compute tangent space vectors - triangles not defined.");
            return;
        }
        Vector3[] vertices = origMesh.vertices;
        Vector3[] normals = origMesh.normals;
        Vector2[] texcoords = origMesh.uv;
        int[] triangles = origMesh.triangles;
        int triVertCount = origMesh.triangles.Length;
        int maxVertIdx = -1;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (maxVertIdx < triangles[i])
            {
                maxVertIdx = triangles[i];
            }
        }
        if (vertices.Length <= maxVertIdx)
        {
            Debug.LogWarning("Unable to compute tangent space vectors - not enough vertices: " + vertices.Length.ToString());
            return;
        }
        if (normals.Length <= maxVertIdx)
        {
            Debug.LogWarning("Unable to compute tangent space vectors - not enough normals.");
            return;
        }
        if (texcoords.Length <= maxVertIdx)
        {
            Debug.LogWarning("Unable to compute tangent space vectors - not enough UVs.");
            return;
        }

        int vertexCount = origMesh.vertexCount;
        Vector4[] tangents = new Vector4[vertexCount];
        Vector3[] tan1 = new Vector3[vertexCount];
        Vector3[] tan2 = new Vector3[vertexCount];

        int triangleCount = triangles.Length / 3;
        int tri = 0;

        for (int i = 0; i < triangleCount; i++)
        {
            int i1 = triangles[tri];
            int i2 = triangles[tri + 1];
            int i3 = triangles[tri + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            Vector2 w1 = texcoords[i1];
            Vector2 w2 = texcoords[i2];
            Vector2 w3 = texcoords[i3];

            float x1 = v2.x - v1.x;
            float x2 = v3.x - v1.x;
            float y1 = v2.y - v1.y;
            float y2 = v3.y - v1.y;
            float z1 = v2.z - v1.z;
            float z2 = v3.z - v1.z;

            float s1 = w2.x - w1.x;
            float s2 = w3.x - w1.x;
            float t1 = w2.y - w1.y;
            float t2 = w3.y - w1.y;

            float r = 1.0f / (s1 * t2 - s2 * t1);
            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;

            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;

            tri += 3;
        }

        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 n = normals[i];
            Vector3 t = tan1[i];

            // Gram-Schmidt orthogonalize
            Vector3.OrthoNormalize(ref n, ref t);

            tangents[i].x = t.x;
            tangents[i].y = t.y;
            tangents[i].z = t.z;

            // Calculate handedness
            tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
        }

        origMesh.tangents = tangents;
    }
}