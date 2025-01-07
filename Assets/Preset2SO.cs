using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "preset2/asset", menuName = "preset2")]
public class Preset2SO : ScriptableObject
{
    public string name;
    public int type;
    public float scint;
    public Vector2 screenpos;
    public float pickoverlinear;
    public Vector2 root;
}

