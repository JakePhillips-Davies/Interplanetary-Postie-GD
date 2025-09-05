using System;
using System.Collections.Generic;
using Godot;

namespace InterplanetaryPostieGD.Assets.Scripts.Terrain.Quadtree;

public enum FaceDirection
{
	Up,
	Down,
	Right,
	Left,
	Forward,
	Back
}

[Tool]
public partial class CelestialTerrainFace : Node3D
{
#region Variables

	public          TerrainQuadNode  rootQuadtreeNode { get; private set; }
	[Export] public CelestialTerrain celestialTerrain { get; private set; }
	[Export] public FaceDirection    faceDirection    { get; private set; }
	[Export] public Texture2D        heightmap        { get; private set; }
	public          Image            heightmapImg     { get; private set; }
	[Export] public Texture2D        colourMap        { get; private set; }
	public          int              mapWidth         { get; private set; }
	public          double           quadToMapRatio   { get; private set; }

#endregion


#region Godot Methods

	public override void _Ready() {
		heightmapImg = heightmap.GetImage();
		
		mapWidth       = heightmapImg.GetWidth();
		quadToMapRatio = (mapWidth - 1) / (celestialTerrain.radius * 2);

		Axis axis = GetAxis();
		rootQuadtreeNode = new TerrainQuadNode(null, this, new Vector2(0, 0), axis);
	}

	public override void _PhysicsProcess(double delta) {
		UpdateTerrainNode(rootQuadtreeNode);
	}

#endregion

#region Tree Traversal

	public static void UpdateTerrainNode(TerrainQuadNode _node) {
		int state = 0;
		state += (_node.nodeState == QuadNodeState.Leaf) ? 1 : 0;
		state += (_node.IsInSplitRange()) ? 2 : 0;
		// Out of range, branch = 0
		// Out of range, leaf = 1
		// In range, branch = 2
		// In range, leaf = 3
		switch (state) {
			case 0: // Out of splitting range and is a branch, needs to kill its children and regen as a leaf.
				_node.ResetNode();
				_node.SetupNode();
				return;

			case 1: // Out of range and is a leaf, it's in intended state leave it be.
				break;

			case 2: // In range and a branch, it's in intended state leave it be.
				break;

			case 3: // In range and is a leaf, needs to kill its terrain and regen as a branch.
				_node.ResetNode();
				_node.SetupNode();
				return;
		}

		// Update children
		if (_node.children == null) return;
		foreach (TerrainQuadNode child in _node.children) {
			UpdateTerrainNode(child);
		}
	}

#endregion

#region Misc

	public Axis GetAxis() {
		return faceDirection switch {
			FaceDirection.Up => new Axis {
				Up      = celestialTerrain.axis.Up,
				Right   = celestialTerrain.axis.Right,
				Forward = celestialTerrain.axis.Forward
			},
			FaceDirection.Down => new Axis {
				Up      = -celestialTerrain.axis.Up,
				Right   = celestialTerrain.axis.Right,
				Forward = -celestialTerrain.axis.Forward
			},
			FaceDirection.Right => new Axis {
				Up      = celestialTerrain.axis.Right,
				Right   = -celestialTerrain.axis.Forward,
				Forward = -celestialTerrain.axis.Up
			},
			FaceDirection.Left => new Axis {
				Up      = -celestialTerrain.axis.Right,
				Right   = celestialTerrain.axis.Forward,
				Forward = -celestialTerrain.axis.Up
			},
			FaceDirection.Forward => new Axis {
				Up      = celestialTerrain.axis.Forward,
				Right   = celestialTerrain.axis.Right,
				Forward = -celestialTerrain.axis.Up
			},
			FaceDirection.Back => new Axis {
				Up      = -celestialTerrain.axis.Forward,
				Right   = -celestialTerrain.axis.Right,
				Forward = -celestialTerrain.axis.Up
			},
			_ => throw new ArgumentOutOfRangeException()
		};
	}

#endregion
}