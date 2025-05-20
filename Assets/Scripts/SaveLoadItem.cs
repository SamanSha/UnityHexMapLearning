using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadItem : MonoBehaviour {
    
    public SaveLoadMenu menu;

    public string MapName {
        get {
            return mapName;
        }
        set {
            mapName = value;
            transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = mapName;
        }
    }

    string mapName;

    public void Select () {
        menu.SelectItem(mapName);
    }
}
