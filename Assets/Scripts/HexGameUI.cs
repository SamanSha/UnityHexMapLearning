using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour {

    public HexGrid grid;

    HexCell currentCell;

    HexUnit selectedUnit;

    void Update () {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButtonDown(0)) {
                DoeSelection();
            }
            else if (selectedUnit) {
                if (Input.GetMouseButtonDown(1)) {
                    DoMove();
                }
                else {
                    DoPathfinding();
                }
            }
        }    
    }

    public void SetEditMode (bool toggle) {
        enabled = !toggle;
        grid.ShowUI(!toggle);
        grid.ClearPath();
        if (toggle) {
            Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        }
        else {
            Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
        }
    }

    bool UpdateCurrentCell () {
        HexCell cell =
            grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell) {
            currentCell = cell;
            return true;
        }
        return false;
    }

    void DoeSelection () {
        grid.ClearPath();
        UpdateCurrentCell();
        if (currentCell) {
            selectedUnit = currentCell.Unit;
        }
    }

    void DoPathfinding () {
        if (UpdateCurrentCell()) {
            if (currentCell && selectedUnit.IsValidDestination(currentCell)) {
                grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
            }
            else {
                grid.ClearPath();
            }
        }
    }

    void DoMove () {
        if (grid.HasPath) {
            selectedUnit.Travel(grid.GetPath());
            grid.ClearPath();
        }
    }
}
