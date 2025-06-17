// Copyright (c) Reality Collab, HCC.
using UnityEngine;

public enum EdgeDimensionType { Length, Breadth }

public class MeasurePoint : MonoBehaviour
{
    [Tooltip("The structure this measure point belongs to.")]
    public StructureScript parentStructure;

    [Tooltip("The other MeasurePoint that forms this edge.")]
    public MeasurePoint pair; // Crucial: Assign this in the Inspector!

    [Tooltip("Is this point for measuring Length or Breadth of the parent structure?")]
    public EdgeDimensionType dimensionType;
}