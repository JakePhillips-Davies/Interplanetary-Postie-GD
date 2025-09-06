using System;
using System.Linq;
using Godot;
using Godot.NativeInterop;
using InterplanetaryPostieGD.Assets.Scripts.Terrain.Quadtree;
using Array = Godot.Collections.Array;

namespace InterplanetaryPostieGD.Assets.Scripts.Terrain;

[Tool]
public partial class TerrainChunk : MeshInstance3D
{
#region Variables

	[Export] public Vector3                centre               { get; private set; }
	[Export] public Vector2                quadPosition         { get; private set; }
	[Export] public double                 quadWidth            { get; private set; }
	public          Axis                   axis                 { get; private set; }
	[Export] public Vector2                cubemapPosition      { get; private set; }
	[Export] public ArrayMesh              arrayMesh            { get; private set; }
	[Export] public CelestialTerrain       celestialTerrain     { get; private set; }
	[Export] public CelestialTerrainFace   celestialTerrainFace { get; private set; }
	private         DebugDraw3DScopeConfig drawConfig;

	private Mesh mesh;

#endregion

#region Godot Methods

	public override void _Ready() {
		drawConfig = DebugDraw3D.ScopedConfig().SetThickness(0);

		Position = centre;

		// RegenerateMesh();
		RegenerateMeshSt();
	}

	public override void _PhysicsProcess(double delta) {
		// DebugDraw3D.DrawLine(GlobalPosition, GlobalPosition + Position.Normalized() * 10000);
	}

#endregion

#region Setup

	public void _Init(
		CelestialTerrainFace _celestialTerrainFace,
		Vector3              _centre,
		Vector2              _quadPosition,
		double               _quadWidth,
		Axis                 _axis
	) {
		celestialTerrain     = _celestialTerrainFace.celestialTerrain;
		celestialTerrainFace = _celestialTerrainFace;
		centre               = _centre;
		quadPosition         = _quadPosition;
		quadWidth            = _quadWidth;
		axis                 = _axis;
		cubemapPosition      = (quadPosition + new Vector2(celestialTerrain.radius - quadWidth / 2, celestialTerrain.radius - quadWidth / 2)) * celestialTerrainFace.quadToMapRatio;

		celestialTerrainFace.AddChild(this);
	}

#endregion

#region Mesh Gen

	private void RegenerateMesh() {
		if (arrayMesh == null) {
			arrayMesh = new ArrayMesh();
			Mesh      = arrayMesh;

			StandardMaterial3D material = (StandardMaterial3D)celestialTerrain.material.Duplicate();
			material.AlbedoTexture = celestialTerrainFace.colourMap;

			MaterialOverride = material;
		}

		arrayMesh.ClearSurfaces();
		Array surfaceArray = CreateTerrainChunk();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
	}

	private void RegenerateMeshSt() {
		StandardMaterial3D material = (StandardMaterial3D)celestialTerrain.material.Duplicate();
		material.AlbedoTexture = celestialTerrainFace.colourMap;

		MaterialOverride = material;

		CreateTerrainChunkSt();
	}

	private Array CreateTerrainChunk() {
		Array surfaceArray = [];
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		FastNoiseLite noise = celestialTerrain.noise;

		int       resolution = 32;
		Vector3[] vertices   = new Vector3[resolution * resolution];
		Vector3[] normals    = new Vector3[resolution * resolution];
		Vector2[] uvs        = new Vector2[resolution * resolution];
		int[]     indices    = new int[resolution * resolution * 6];
		Vector3   surfacePoint;
		Vector2   quadPoint;
		Vector2   cubemapPoint;
		Color     heightCol;
		Color     y0Lerp;
		Color     y1Lerp;
		double    height, lat, lon;
		double    vertStep = quadWidth / (resolution - 1);
		double    cubeStep = quadWidth * celestialTerrainFace.quadToMapRatio / (resolution - 1);
		int       vertOffset, indexOffset, xf, yf;

		for (int x = 0; x < resolution; x++) {
			for (int y = 0; y < resolution; y++) {
				vertOffset  = (x * resolution + y);
				indexOffset = 6 * (x * resolution + y);

				cubemapPoint = new Vector2(cubeStep * x, cubeStep * y) + cubemapPosition;
				xf           = Mathf.FloorToInt(cubemapPoint.X);
				yf           = Mathf.FloorToInt(cubemapPoint.Y);

				if ((xf < celestialTerrainFace.mapWidth - 1) && (yf < celestialTerrainFace.mapWidth - 1)) {
					y0Lerp    = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 1, yf + 0), cubemapPoint.X - xf);
					y1Lerp    = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 1).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 1, yf + 1), cubemapPoint.X - xf);
					heightCol = y0Lerp.Lerp(y1Lerp, cubemapPoint.Y - yf);
				}
				else if ((xf < celestialTerrainFace.mapWidth - 1) && (yf == celestialTerrainFace.mapWidth - 1)) {
					heightCol = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 1, yf + 0), cubemapPoint.X - xf);
				}
				else if ((xf == celestialTerrainFace.mapWidth - 1) && (yf < celestialTerrainFace.mapWidth - 1)) {
					heightCol = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 1), cubemapPoint.Y - yf);
				}
				else heightCol = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0);

				// height = celestialTerrain.radius;

				quadPoint    =  quadPosition + new Vector2(x * vertStep - quadWidth / 2, y * vertStep - quadWidth / 2);
				surfacePoint =  TerrainQuadNode.QuadToSphere(quadPoint, axis, celestialTerrain.radius);
				height       =  (heightCol.Luminance * celestialTerrain.maxElevation) + celestialTerrain.radius;
				height       += noise.GetNoise3D(surfacePoint.X, surfacePoint.Y, surfacePoint.Z) * celestialTerrain.noiseGain;

				surfacePoint = surfacePoint.Normalized() * height - centre;

				vertices[vertOffset] = surfacePoint;
				normals[vertOffset]  = surfacePoint.Normalized() + centre; // calculated later
				uvs[vertOffset]      = (cubemapPoint + Vector2.One / 2) / (celestialTerrainFace.mapWidth);

				// Indices, only done if not on the right or bottom edge
				// First tri: 0 - 2 - 3 (2 here = the resolution, as it's done one full line to get back to that spot)
				// 0-2
				//  \|
				// 1 3
				// Second tri: 0 - 1 - 3
				// 0 2
				// |\
				// 1-3
				if ((x < resolution - 1) && (y < resolution - 1)) {
					indices[indexOffset + 0] = vertOffset + 0;
					indices[indexOffset + 1] = vertOffset + resolution + 1;
					indices[indexOffset + 2] = vertOffset + resolution;
					indices[indexOffset + 3] = vertOffset + 0;
					indices[indexOffset + 4] = vertOffset + 1;
					indices[indexOffset + 5] = vertOffset + resolution + 1;
				}
			}
		}

		// Calculate normals by running through each triangle, adding that tri's normal to each of it's verts and normalizing all normals after.
		Vector3 a, b, c, normal;
		for (int i = 0; i < indices.Length; i += 3) {
			a = vertices[indices[i + 0]] + centre;
			b = vertices[indices[i + 1]] + centre;
			c = vertices[indices[i + 2]] + centre;

			normal = -(b - a).Cross(c - a);

			normals[indices[i + 0]] += normal * normal.LengthSquared();
			normals[indices[i + 1]] += normal * normal.LengthSquared();
			normals[indices[i + 2]] += normal * normal.LengthSquared();
		}

		// Normalize
		for (int i = 0; i < normals.Length; i++) {
			normals[i] = normals[i].Normalized();
		}


		surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices;
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals;
		surfaceArray[(int)Mesh.ArrayType.TexUV]  = uvs;
		surfaceArray[(int)Mesh.ArrayType.TexUV2] = uvs;
		surfaceArray[(int)Mesh.ArrayType.Index]  = indices;

		return surfaceArray;
	}

	private void CreateTerrainChunkSt() {
		SurfaceTool st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		FastNoiseLite noise = celestialTerrain.noise;

		int     resolution = 16;
		Vector3 surfacePoint;
		Vector2 quadPoint;
		Vector2 cubemapPoint;
		Color   heightCol;
		Color   y0Lerp;
		Color   y1Lerp;
		double  height;
		double  vertStep = quadWidth / (resolution - 1);
		double  cubeStep = quadWidth * celestialTerrainFace.quadToMapRatio / (resolution - 1);
		int     vertOffset, xf, yf;

		for (int x = 0; x < resolution; x++) {
			for (int y = 0; y < resolution; y++) {
				vertOffset = (x * resolution + y);

				cubemapPoint = new Vector2(cubeStep * x, cubeStep * y) + cubemapPosition;
				xf           = Mathf.FloorToInt(cubemapPoint.X);
				yf           = Mathf.FloorToInt(cubemapPoint.Y);

				if ((xf < celestialTerrainFace.mapWidth - 1) && (yf < celestialTerrainFace.mapWidth - 1)) {
					y0Lerp    = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 1, yf + 0), cubemapPoint.X - xf);
					y1Lerp    = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 1).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 1, yf + 1), cubemapPoint.X - xf);
					heightCol = y0Lerp.Lerp(y1Lerp, cubemapPoint.Y - yf);
				}
				else if ((xf < celestialTerrainFace.mapWidth - 1) && (yf == celestialTerrainFace.mapWidth - 1)) {
					heightCol = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 1, yf + 0), cubemapPoint.X - xf);
				}
				else if ((xf == celestialTerrainFace.mapWidth - 1) && (yf < celestialTerrainFace.mapWidth - 1)) {
					heightCol = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0).Lerp(celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 1), cubemapPoint.Y - yf);
				}
				else heightCol = celestialTerrainFace.heightmapImg.GetPixel(xf + 0, yf + 0);

				// height = celestialTerrain.radius;

				quadPoint    =  quadPosition + new Vector2(x * vertStep - quadWidth / 2, y * vertStep - quadWidth / 2);
				surfacePoint =  TerrainQuadNode.QuadToSphere(quadPoint, axis, celestialTerrain.radius);
				height       =  (heightCol.Luminance * celestialTerrain.maxElevation) + celestialTerrain.radius;
				height       += noise.GetNoise3D(surfacePoint.X, surfacePoint.Y, surfacePoint.Z) * celestialTerrain.noiseGain;

				surfacePoint = surfacePoint.Normalized() * height - centre;
				
				if (x == 1 && y == 1) GD.Print(heightCol.Luminance);

				st.SetUV((cubemapPoint + Vector2.One / 2) / (celestialTerrainFace.mapWidth));
				st.SetUV2((cubemapPoint + Vector2.One / 2) / (celestialTerrainFace.mapWidth));
				st.SetSmoothGroup(UInt32.MaxValue);
				st.AddVertex(surfacePoint);

				// Indices, only done if not on the right or bottom edge
				// First tri: 0 - 2 - 3 (2 here = the resolution, as it's done one full line to get back to that spot)
				// 0-2
				//  \|
				// 1 3
				// Second tri: 0 - 1 - 3
				// 0 2
				// |\
				// 1-3
				if ((x < resolution - 1) && (y < resolution - 1)) {
					st.AddIndex(vertOffset + 0);
					st.AddIndex(vertOffset + resolution + 1);
					st.AddIndex(vertOffset + resolution);
					st.AddIndex(vertOffset + 0);
					st.AddIndex(vertOffset + 1);
					st.AddIndex(vertOffset + resolution + 1);
				}
			}
		}

		st.GenerateNormals();
		st.GenerateTangents();
		mesh = st.Commit();

		Mesh = mesh;
	}

#endregion
}