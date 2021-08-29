using UnityEngine;

public class PlaneGenerator : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;

    [SerializeField, Range(1,64)] int quadsPerAxis = 1;

    private void CreatePlane()
    {
        var origin          = new Vector3(-0.5f, 0.0f, -0.5f);
        var verticesPerAxis = quadsPerAxis + 1;

        var uvStart = Vector2.zero;
        var uvEnd   = Vector2.one;

        var vertices = new Vector3[verticesPerAxis * verticesPerAxis];
        var normals  = new Vector3[vertices.Length];
        var uvs      = new Vector2[vertices.Length];
        var indices  = new int[(verticesPerAxis - 1) * (verticesPerAxis - 1) * 4];
        var normal   = Vector3.up;

        // Vertices
        for (var i = 0; i < vertices.Length; i++)
        {
            var i0     = i  / verticesPerAxis;
            var i1     = i  % verticesPerAxis;
            var localU = i0 / (verticesPerAxis - 1f);
            var localV = i1 / (verticesPerAxis - 1f);

            vertices[i] = origin + localU * Vector3.right + localV * Vector3.forward;
            normals[i]  = normal;
            uvs[i].x    = Mathf.Lerp(uvStart.x, uvEnd.x, localU);
            uvs[i].y    = Mathf.Lerp(uvStart.y, uvEnd.y, localV);
        }

        // Quads
        var vertexIndex = 0;

        for (var y = 0; y < verticesPerAxis - 1; ++y)
        {
            for (var x = 0; x < verticesPerAxis - 1; ++x)
            {
                var slash = x % 2 == 0 && y % 2 == 0;

                if (slash)
                {
                    indices[vertexIndex++] = (y + 0) * verticesPerAxis + (x + 0);
                    indices[vertexIndex++] = (y + 0) * verticesPerAxis + (x + 1);
                    indices[vertexIndex++] = (y + 1) * verticesPerAxis + (x + 1);
                    indices[vertexIndex++] = (y + 1) * verticesPerAxis + (x + 0);
                }
                else // backslash
                {
                    indices[vertexIndex++] = (y + 0) * verticesPerAxis + (x + 1);
                    indices[vertexIndex++] = (y + 1) * verticesPerAxis + (x + 1);
                    indices[vertexIndex++] = (y + 1) * verticesPerAxis + (x + 0);
                    indices[vertexIndex++] = (y + 0) * verticesPerAxis + (x + 0);
                }
            }
        }

        var mesh = new Mesh()
        {
            name = $"Plane_{verticesPerAxis}x{verticesPerAxis}"
        };

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetIndices(indices, MeshTopology.Quads, 0);

        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }

    private void OnValidate()
    {
        CreatePlane();
    }
}
