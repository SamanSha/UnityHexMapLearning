using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMapMenu : MonoBehaviour {

    public HexGrid hexGrid;

    public HexMapGenerator mapGenerator;

    bool generateMaps = true;

    public void ToggleMapGeneration (bool toggle) {
        generateMaps = toggle;
    }

    public void Open () {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close () {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    void CreateMap (int x, int z) {
        if (generateMaps) {
            mapGenerator.GenerateMap(x, z);
        }
        else {
            hexGrid.CreateMap(x, z);
        }
        HexMapCamera.ValidatePosition();
        Close();
    }

    public void CreateSmallMap () {
        CreateMap(20, 15);
    }

    public void CreateMediumMap () {
        CreateMap(40, 30);
    }

    public void CreateLargeMap () {
        CreateMap(80, 60);
    }
}
