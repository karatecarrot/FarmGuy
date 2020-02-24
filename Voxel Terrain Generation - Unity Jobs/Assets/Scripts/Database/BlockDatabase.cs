using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public struct Block
{
    public string name;
    public int ID;
    public bool isStackable;
    public bool isSolidBlock;
    public bool renderSurroundingFaces;
    public float transparency;

    public Block(int id, string blockName, bool stackable, bool solid, bool renderFaces, float globalLight, int back = 0, int front = 0, int top = 0, int bottom = 0, int left = 0, int right = 0)
    {
        name = blockName;
        ID = id;
        isStackable = stackable;
        isSolidBlock = solid;
        renderSurroundingFaces = renderFaces;
        transparency = globalLight;

        backFace = back;
        frontFace = front;
        topFace = top;
        bottomFace = bottom;
        leftFace = left;
        rightFace = right;
    }

    [Header("Texture Values")]
    public int backFace;
    public int frontFace;
    public int topFace;
    public int bottomFace;
    public int leftFace;
    public int rightFace;

    public int GetTextureID (int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFace;
            case 1:
                return frontFace;
            case 2:
                return topFace;
            case 3:
                return bottomFace;
            case 4:
                return leftFace;
            case 5:
                return rightFace;
            default:
                Debug.LogWarning("Error in GetTextureID, invalid faceIndex");
                return 0;
        }
    }
}

public class BlockDatabase : MonoBehaviour
{
    public static BlockDatabase instance;

    public Material blockMaterialAtlas;
    [Space]
    public List<Block> blockDatabase = new List<Block>();

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More then on instance of - BlockDatabase on (" + instance.gameObject.name + ") Gameobject");
            return;
        }
        else
            instance = this;

        GetDatabase("Assets/Resources/BlockData.txt");
    }

    private void GetDatabase(string path)
    {
        // Create streamReader to read text of txt object at 'path'
        StreamReader reader = new StreamReader(path);

        // Goto function call
        AddItem:

        // Adds the block to a list off the text object in the 'path'
        blockDatabase.Add(new Block(
            int.Parse(reader.ReadLine().Replace("id: ", "")),
            reader.ReadLine().Replace("name: ", ""),
            bool.Parse(reader.ReadLine().Replace("stackable: ", "")),
            bool.Parse(reader.ReadLine().Replace("solid: ", "")),
            bool.Parse(reader.ReadLine().Replace("renderFaces: ", "")),

            float.Parse(reader.ReadLine().Replace("transparency: ", "")),

            int.Parse(reader.ReadLine().Replace("back: ", "")),
            int.Parse(reader.ReadLine().Replace("front: ", "")),
            int.Parse(reader.ReadLine().Replace("top: ", "")),
            int.Parse(reader.ReadLine().Replace("bottom: ", "")),
            int.Parse(reader.ReadLine().Replace("left: ", "")),
            int.Parse(reader.ReadLine().Replace("right: ", ""))
            ));

        // Checks to see if there are more blocks left to add to the list
        string lineReader = reader.ReadLine();
        if (lineReader == ",")
        {
            goto AddItem;
        }
        else if (lineReader == ";")
        {
            reader.Close();
        }
        else
            Debug.LogWarning("BlockDatabase.txt does not have correct line endings");
    }
}
