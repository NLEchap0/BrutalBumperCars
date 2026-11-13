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
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            {
                if (i == rnd)
                {
                    bots[i].SetActive(false);

                    player.position = spawnPoints[i].position;
                    player.rotation = spawnPoints[i].rotation;
                }
            }
        }

    }
}
