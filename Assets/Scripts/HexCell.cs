using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.IO;

public class HexCell : MonoBehaviour {

    public HexCoordinates coordinates;

    public RectTransform uiRect;

    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    public HexGridChunk chunk;

    public int TerrainTypeIndex {
        get {
            return terrainTypeIndex;
        }
        set {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    int terrainTypeIndex;

    public int Elevation {
        get {
            return elevation;
        }
        set {
            if (elevation == value) {
                return;
            }
            int originalViewElevation = ViewElevation;
            elevation = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
            RefreshPosition();
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    int elevation = int.MinValue;

    public int Distance {
        get {
            return distance;
        }
        set {
            distance = value;
        }
    }

    int distance;

    public HexCell PathFrom { get; set; }

    public int SearchHeuristic { get; set; }

    public int SearchPriority {
        get {
            return distance + SearchHeuristic;
        }
    }

    public HexCell NextWithSamePriority { get; set; }

    public int SearchPhase { get; set; }

    public HexUnit Unit { get; set; }

    public HexCellShaderData ShaderData { get; set; }

    public int Index { get; set; }

    public bool IsVisible {
        get {
            return visibility > 0 && Explorable;
        }
    }

    int visibility;

    public bool IsExplored {
        get {
            return explored && Explorable;
        }
        private set {
            explored = value;
        }
    }

    public int ViewElevation {
        get {
            return elevation >= waterLevel ? elevation : waterLevel;
        }
    }

    public bool Explorable { get; set; }

    bool explored;

    public void SetLabel (string text) {
        TextMeshProUGUI label = uiRect.GetComponent<TextMeshProUGUI>();
        label.text = text;
    }

    void RefreshPosition () {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    public int WaterLevel {
        get {
            return waterLevel;
        }
        set {
            if (waterLevel == value) {
                return;
            }
            int originalViewElevation = ViewElevation;
            waterLevel = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
            ValidateRivers();
            Refresh();
        }
    }

    int waterLevel;

    public bool IsUnderwater {
        get {
            return waterLevel > elevation;
        }
    }

    public int GetElevationDifference (HexDirection direction) {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;

    public bool HasIncomingRiver {
        get {
            return hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver {
        get {
            return hasOutgoingRiver;
        }
    }

    public HexDirection IncomingRiver {
        get {
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver {
        get {
            return outgoingRiver;
        }
    }

    public bool HasRiver {
        get {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd {
        get {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    public float StreamBedY {
        get {
            return
                (elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public HexDirection RiverBeginOrEndDirection {
        get {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    public int UrbanLevel {
        get { 
            return urbanLevel;
        }
        set {
            if (urbanLevel != value) {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel {
        get {
            return farmLevel;
        }
        set {
            if (farmLevel != value) {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel {
        get {
            return plantLevel;
        }
        set {
            if (plantLevel != value) {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    int urbanLevel, farmLevel, plantLevel;

    public bool Walled {
        get {
            return walled;
        }
        set {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }

    bool walled;

    public int SpecialIndex {
        get {
            return specialIndex;
        }
        set {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    int specialIndex;

    public bool IsSpecial {
        get {
            return specialIndex > 0;
        }
    }

    [SerializeField]
    HexCell[] neighbors;

    public bool HasRiverThroughEdge (HexDirection direction) {
        return 
            hasIncomingRiver && incomingRiver == direction || 
            hasOutgoingRiver && outgoingRiver == direction;
    }

    public HexCell GetNeighbor (HexDirection direction) {
        return neighbors[(int)direction];
    }

    public void SetNeighbor (HexDirection direction, HexCell cell) {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType (HexDirection direction) {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation    
        );
    }

    public HexEdgeType GetEdgeType (HexCell otherCell) {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation    
        );
    }

    public float RiverSurfaceY {
        get {
            return
                (elevation + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public float WaterSurfaceY {
        get {
            return
                (waterLevel + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public void RemoveOutgoingRiver () {
        if (!hasOutgoingRiver) {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver () {
        if (!hasIncomingRiver) {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver () {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetOutgoingRiver (HexDirection direction) {
        if (hasOutgoingRiver && outgoingRiver == direction) {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor)) {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = direction;
        specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

        SetRoad((int)direction, false);
    }

    bool IsValidRiverDestination (HexCell neighbor) {
        return neighbor && (
            elevation >= neighbor.elevation || waterLevel == neighbor.elevation    
        );
    }

    void ValidateRivers () {
        if (
            hasOutgoingRiver && 
            !IsValidRiverDestination(GetNeighbor(outgoingRiver))
        ) {
            RemoveOutgoingRiver();
        }
        if (
            hasIncomingRiver && 
            !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
        ) {
            RemoveIncomingRiver();
        }
    }

    [SerializeField]
    bool[] roads;

    public bool HasRoads {
        get {
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i]) {
                    return true;
                }
            }
            return false;
        }
    }

    public bool HasRoadThroughEdge (HexDirection direction) {
        return roads[(int)direction];
    }

    public void AddRoad (HexDirection direction) {
        if (
            !roads[(int)direction] && !HasRiverThroughEdge(direction) && 
            !IsSpecial && !GetNeighbor(direction).IsSpecial && 
            GetElevationDifference(direction) <= 1    
        ) {
            SetRoad((int)direction, true);
        }
    }

    public void RemoveRoads () {
        for (int i = 0; i < neighbors.Length; i++) {
            if (roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    void SetRoad (int index, bool state) {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public void DisableHighlight () {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight (Color color) {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    public void IncreaseVisibility () {
        visibility += 1;
        if (visibility == 1) {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility () {
        visibility -= 1;
        if (visibility == 0) {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void ResetVisibility () {
        if (visibility > 0) {
            visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }

    void RefreshSelfOnly () {
        chunk.Refresh();
        if (Unit) {
            Unit.ValidateLocation();
        }
    }

    void Refresh () {
        if (chunk) {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++) {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
            if (Unit) {
                Unit.ValidateLocation();
            }
        }
    }

    public void Save (BinaryWriter writer) {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)(elevation + 127));
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);

        if (hasIncomingRiver) {
            writer.Write((byte)(incomingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++) {
            if (roads[i]) {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
        writer.Write(IsExplored);
    }

    public void Load (BinaryReader reader, int header) {
        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        if (header >= 4) {
            elevation -= 127;
        }
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++) {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }

        IsExplored = header >= 3 ? reader.ReadBoolean() : false;
        ShaderData.RefreshVisibility(this);
    }
}
