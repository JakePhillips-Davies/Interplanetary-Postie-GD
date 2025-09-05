using Godot;

namespace InterplanetaryPostieGD.Assets.Scripts.Terrain.Quadtree;

public enum QuadNodeState
{
	Branch,
	Leaf
}

public record Axis
{
	public required Vector3 Up;
	public required Vector3 Right;
	public required Vector3 Forward;
}

[Tool]
public class TerrainQuadNode
{
#region Variables

	public TerrainQuadNode      parent               { get; private set; }
	public TerrainQuadNode[]    children             { get; private set; }
	public CelestialTerrain     celestialTerrain     { get; private set; }
	public CelestialTerrainFace celestialTerrainFace { get; private set; }
	public Axis                 axis                 { get; private set; }
	public TerrainChunk         terrainChunk         { get; private set; }
	public QuadNodeState        nodeState            { get; private set; }
	public int                  depth                { get; private set; }
	public double               width                { get; private set; }
	public Vector3              spherePosition       { get; private set; }
	public Vector2              quadPosition         { get; private set; }

#endregion


#region Setup

	public TerrainQuadNode(TerrainQuadNode _parent, CelestialTerrainFace _celestialTerrainFace, Vector2 _quadPosition, Axis _axis) {
		parent               = _parent;
		celestialTerrain     = _celestialTerrainFace.celestialTerrain;
		celestialTerrainFace = _celestialTerrainFace;
		quadPosition         = _quadPosition;
		axis                 = _axis;

		// Sphere position
		spherePosition = QuadToSphere(quadPosition, _axis, celestialTerrain.radius);

		// Depth
		if (parent != null) depth = parent.depth + 1;
		else depth                = 1;

		// Width
		if (parent != null) width = parent.width / 2;
		else width                = celestialTerrain.radius * 2;

		SetupNode();
	}

	public void SetupNode() {
		ResetNode();
		
		if (IsInSplitRange()) {
			nodeState = QuadNodeState.Branch;
			SplitNode();
			return;
		}

		nodeState = QuadNodeState.Leaf;

		terrainChunk = new TerrainChunk();
		terrainChunk._Init(
			celestialTerrainFace,
			spherePosition,
			quadPosition,
			width,
			axis
		);
	}

#endregion

#region Node Splitting

	public void ResetNode() {
		if (children != null)
			foreach (TerrainQuadNode child in children) {
				child.ResetNode();
			}
		
		children = null;
		if (terrainChunk != null) terrainChunk.QueueFree();
		terrainChunk = null;
	}
	
	public bool IsInSplitRange() {
		double distanceSqr          = (spherePosition + celestialTerrain.GlobalPosition - celestialTerrain.focus.GlobalPosition).LengthSquared();
		double distanceThresholdSqr = Mathf.Pow((width * 2), 2);

		return ((distanceSqr <= distanceThresholdSqr) && (depth <= 12));
	}

	public void SplitNode() {
		children = new TerrainQuadNode[4];
		children[0] = new TerrainQuadNode(
			this,
			celestialTerrainFace,
			quadPosition + new Vector2(1, 1) * (width / 4),
			axis
		);
		children[1] = new TerrainQuadNode(
			this,
			celestialTerrainFace,
			quadPosition + new Vector2(-1, 1) * (width / 4),
			axis
		);
		children[2] = new TerrainQuadNode(
			this,
			celestialTerrainFace,
			quadPosition + new Vector2(1, -1) * (width / 4),
			axis
		);
		children[3] = new TerrainQuadNode(
			this,
			celestialTerrainFace,
			quadPosition +  new Vector2(-1, -1) * (width / 4),
			axis
		);
	}

#endregion

#region Misc

	public static Vector3 QuadToSphere(Vector2 _quadPosition, Axis _axis, double _radius) {
		Vector3 quadPos3 = _axis.Up * _radius + _axis.Right * _quadPosition.X + _axis.Forward * _quadPosition.Y;
		Vector3 spherePosition;
		Vector3 p  = quadPos3.Normalized();
		double  x2 = p.X * p.X;
		double  y2 = p.Y * p.Y;
		double  z2 = p.Z * p.Z;

		spherePosition = new Vector3(
			quadPos3.X * Mathf.Sqrt(1 - (y2 / 2) - (z2 / 2) + (y2 * z2) / 3),
			quadPos3.Y * Mathf.Sqrt(1 - (z2 / 2) - (x2 / 2) + (z2 * x2) / 3),
			quadPos3.Z * Mathf.Sqrt(1 - (x2 / 2) - (y2 / 2) + (x2 * y2) / 3)
		);

		spherePosition = spherePosition.Normalized() * _radius;

		return spherePosition;
	}

#endregion
}