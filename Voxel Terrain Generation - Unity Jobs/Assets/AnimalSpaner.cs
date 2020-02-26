using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalSpaner : MonoBehaviour
{
    public int chanceToSpawn;

    public GameObject[] animals;

    Transform spawnPoint;

    // Start is called before the first frame update
    void Start()
    {
        spawnPoint = gameObject.transform;
        SpawnChance();
    }

    void SpawnChance()
    {
        float spawnPercent;
        spawnPercent = Random.Range(1, 101);

        if (spawnPercent >= chanceToSpawn)
        {
            ChooseAnimal();
        }
    }

    void ChooseAnimal()
    {
        float animalPercent;
        animalPercent = Random.Range(1, 101);
        if (animalPercent <= 40)
        {
            Instantiate(animals[0], spawnPoint);
        }
        else if (animalPercent <= 60 && animalPercent >= 40)
        {
            Instantiate(animals[1], spawnPoint);
        }
        else if (animalPercent <= 90 && animalPercent >= 60)
        {
            Instantiate(animals[2], spawnPoint);
        }
        else if (animalPercent <= 95 && animalPercent >= 90)
        {
            Instantiate(animals[3], spawnPoint);
        }
        else if (animalPercent <= 100 && animalPercent >= 96)
        {
            RareAnimal();
        }

    }

    void RareAnimal()
    {
        float rareChance;
        rareChance = Random.Range(1, 101);
        if (rareChance <= 50)
        {
            Instantiate(animals[4], spawnPoint);
        }
        else if (rareChance >= 50 && rareChance <= 90)
        {
            Instantiate(animals[5], spawnPoint);
        }
        else if (rareChance >= 91 && rareChance <= 100)
        {
            Instantiate(animals[6], spawnPoint);
        }
    }
}