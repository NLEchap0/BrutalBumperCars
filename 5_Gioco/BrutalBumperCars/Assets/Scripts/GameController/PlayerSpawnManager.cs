using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Lista punti di spaw")]
    [SerializeField] private List<Transform> spawnPoints;

    [SerializeField] private Rigidbody player;

    [Header("Lista delle auto bot")]
    [SerializeField] private List<GameObject> bots;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       

        int rnd = Random.Range(0, spawnPoints.Count);
        
        bots[rnd].SetActive(false); //per il prodotto finale cambiare in Destroy(bots[rnd]);

        player.position = spawnPoints[rnd].position;
        player.rotation = spawnPoints[rnd].rotation;
                

    }
}
