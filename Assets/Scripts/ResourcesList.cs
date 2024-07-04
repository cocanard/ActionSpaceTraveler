using UnityEngine;

[CreateAssetMenu(fileName = "ResourcesList", menuName = "ResourcesList", order = 3)]
public class ResourcesList : ScriptableObject
{
    public GameObject[] ObjectArray;
    public Sprite[] SpriteArray;
}