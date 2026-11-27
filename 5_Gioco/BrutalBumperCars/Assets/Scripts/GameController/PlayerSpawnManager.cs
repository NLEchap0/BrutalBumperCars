using System.Collections.Generic;
using UnityEngine;

#region Gestisce lo spawn casuale del giocatore sostituendo il bot corrispondente nello slot scelto
public class PlayerSpawnManager : MonoBehaviour
{
    #region Dichiarazioni Parametri Inspector

    #region Punti di Spawn
    [Header("Lista punti di spaw")]
    [SerializeField] private List<Transform> spawnPoints;
    #endregion

    #region Riferimenti Veicoli
    [SerializeField] private GameObject player;

    [Header("Lista delle auto bot")]
    [SerializeField] private List<GameObject> bots;
    #endregion

    #endregion

    #region 1. Ciclo di Vita Unity (Start)

    #region Seleziona uno slot di partenza casuale per il giocatore e disattiva il bot assegnato a quello slot
    private void Start()
    {
        if (!CanAssignPlayerSpawn())
            return;

        int selectedSpawnIndex = SelectRandomSpawnIndex();
        DisableBotAtSlot(selectedSpawnIndex);
        MovePlayerToSpawnPoint(spawnPoints[selectedSpawnIndex]);
    }
    #endregion

    #endregion

    #region 2. Validazione e Selezione Slot

    private bool CanAssignPlayerSpawn()
    {
        return player != null
            && player.activeSelf
            && spawnPoints != null
            && spawnPoints.Count > 0;
    }

    private int SelectRandomSpawnIndex()
    {
        return Random.Range(0, spawnPoints.Count);
    }

    #endregion

    #region 3. Posizionamento Veicoli

    private void DisableBotAtSlot(int slotIndex)
    {
        if (bots != null && slotIndex >= 0 && slotIndex < bots.Count && bots[slotIndex] != null)
            bots[slotIndex].SetActive(false); // Per il prodotto finale cambiare in Destroy(bots[slotIndex]);
    }

    private void MovePlayerToSpawnPoint(Transform targetSpawnPoint)
    {
        if (targetSpawnPoint == null)
            return;

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        playerRigidbody.position = targetSpawnPoint.position;
        playerRigidbody.rotation = targetSpawnPoint.rotation;
    }

    #endregion
}
#endregion