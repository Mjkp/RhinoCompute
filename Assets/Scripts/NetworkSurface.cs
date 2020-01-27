using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rhino;
using Rhino.Compute;

public class NetworkSurface : MonoBehaviour
{

    public string authToken;
    public Transform nestedPoints;
    private List<List<Vector3>> nestpts = new List<List<Vector3>>();

    void Start()
    {
        // windows server to use rhino compute sdk updated on unity
        ComputeServer.AuthToken = authToken;
        ComputeServer.WebAddress = "http://127.0.0.1:8081/";


        GenerateUnityMesh(GenerateRhinoMesh(UpdatePtList(nestedPoints, ref nestpts)));

    }

    void Update()
    {

    }

    List<List<Vector3>> UpdatePtList(Transform parentObj, ref List<List<Vector3>> mainList)
    {
        for(int i = 0; i< parentObj.childCount;i++)
        {
            List<Vector3> subList = new List<Vector3>();
            for(int j=0; j<parentObj.GetChild(i).childCount; j++)
            {
                Vector3 pos = parentObj.GetChild(i).GetChild(j).position;
                subList.Add(pos);
            }
            mainList.Add(subList);
        }
        return mainList;
    }

    static void DeleteMeshes(UnityEngine.Mesh[] meshobj)
    {
        foreach (var obj in meshobj)
        {
            Destroy(obj);
            Destroy(obj);
        }
        // to clean the memory 
        Resources.UnloadUnusedAssets();
    }

    static Rhino.Geometry.Mesh[] GenerateRhinoMesh(List<List<Vector3>> nestedPt)
    {
        var crvs = new List<Rhino.Geometry.Curve>();
        var pts = new List<Rhino.Geometry.Point3d>();
        for (int i=0; i< nestedPt.Count;i++)
        {
            for(int j=0; j< nestedPt[i].Count; j++)
            {
                var pt = new Rhino.Geometry.Point3d(nestedPt[i][j].x, nestedPt[i][j].z, nestedPt[i][j].y);
                pts.Add(pt);
            }
            var crv = CurveCompute.CreateInterpolatedCurve(pts.ToArray(), 3);
            crvs.Add(crv);
        }
        int err; 
        var srf = NurbsSurfaceCompute.CreateNetworkSurface(crvs, 0, 0.001, 0.001, 0.001, out err);

        if (err > 0) Debug.Log("failed to generate surface network");
        var brep = srf.ToBrep();
        var mesh = MeshCompute.CreateFromBrep(brep);
        return mesh;
    }


    static UnityEngine.Mesh[] GenerateUnityMesh(Rhino.Geometry.Mesh[] meshFromRhino)
    {
        foreach(var mesh in meshFromRhino)
        {
            Mesh meshObj = new Mesh();

            var vertices = new List<Vector3>();
            foreach (var meshVertex in mesh.Vertices)
            {
                var vertex = new Vector3(meshVertex.X, meshVertex.Z, meshVertex.Y);
                vertices.Add(vertex);
            }

            var triangles = new List<int>();
            foreach (var meshFace in mesh.Faces)
            {
                if (meshFace.IsTriangle)
                {
                    triangles.Add(meshFace.C);
                    triangles.Add(meshFace.B);
                    triangles.Add(meshFace.A);
                }
                else if (meshFace.IsQuad)
                {
                    triangles.Add(meshFace.C);
                    triangles.Add(meshFace.B);
                    triangles.Add(meshFace.A);
                    triangles.Add(meshFace.D);
                    triangles.Add(meshFace.C);
                    triangles.Add(meshFace.A);
                }
            }

            var normals = new List<Vector3>();
            foreach (var normal in mesh.Normals)
            {
                normals.Add(new Vector3(normal.X, normal.Z, normal.Y));
            }

            meshObj.vertices = vertices.ToArray();
            meshObj.triangles = triangles.ToArray();
            meshObj.normals = normals.ToArray();

            GameObject gb = new GameObject();
            gb.AddComponent<MeshFilter>().mesh = meshObj;
            gb.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        }

        return null;
    }

    /*
     * rhino mesh.vertices => unity points
     * unity points=> unity mesh visualizer
     * 
     */
}
