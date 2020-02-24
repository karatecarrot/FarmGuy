using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Dynamite : MonoBehaviour
{
    public int3 explosionRadius;
    public WorldGenerator world;

    public bool explode;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            explode = true;
        }

        if(explode)
        {
            for (int x = 0; x < explosionRadius.x; x++)
            {
                for (int y = 0; y < explosionRadius.y; y++)
                {
                    for (int z = 0; z < explosionRadius.z; z++)
                    {
                        if (world.CheckForVoxel(transform.position + -transform.up))
                        {
                            Debug.Log("Contains Voxel");
                            world.GetChunkFromVector3(transform.position + -transform.up).EditVoxel(transform.position + -transform.up, 0);
                        }
                    }
                }
            }

            explode = false;
        }
    }
}
