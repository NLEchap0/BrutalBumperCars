using System.Collections;
using UnityEngine;

public class MenuNavigationManager : MonoBehaviour
{
    public static MenuNavigationManager Instance;

    [Header("Impostazioni Generali")]
    [SerializeField] private float durataTransizioneMenu = 0.35f;
    [SerializeField] private Canvas canvasPrincipale;

    [Header("Impostazioni Transizione Garage")]
    [SerializeField] private float durataUscitaMenu = 0.5f;
    [SerializeField] private float ritardoEntrataGarage = 1.0f;
    [SerializeField] private float durataEntrataGarage = 0.75f;

    [Header("Impostazioni Statistiche Dettagliate")]
    [Tooltip("Tempo di discesa/risalita tra Y: 1340 e Y: 0")]
    [SerializeField] private float durataAnimazioneStatistiche = 0.45f;

    private bool isAnimating = false;
    public bool IsAnimating => isAnimating;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasPrincipale == null)
            canvasPrincipale = GetComponentInParent<Canvas>();
    }

    private float GetLarghezzaCanvas()
    {
        if (canvasPrincipale != null)
            return canvasPrincipale.GetComponent<RectTransform>().rect.width;

        return Screen.width;
    }

    private float GetAltezzaCanvas()
    {
        if (canvasPrincipale != null)
            return canvasPrincipale.GetComponent<RectTransform>().rect.height;

        return Screen.height;
    }

    public void TransizioneAvanti(GameObject paginaDaChiudere, GameObject paginaDaAprire, GameObject elementoExtra = null)
    {
        if (isAnimating) return;

        float distanza = GetLarghezzaCanvas();

        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(-distanza, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, new Vector2(0f, rectApri.anchoredPosition.y), durataTransizioneMenu, false, true));
        }

        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 target = new Vector2(distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, target, durataTransizioneMenu, true, false));
        }

        if (elementoExtra != null)
        {
            elementoExtra.SetActive(true);
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            rectExtra.anchoredPosition = new Vector2(-distanza, rectExtra.anchoredPosition.y);
            StartCoroutine(Muovi(rectExtra, new Vector2(0f, rectExtra.anchoredPosition.y), durataTransizioneMenu, false, false));
        }
    }

    public void TransizioneIndietro(GameObject paginaDaChiudere, GameObject paginaDaAprire, GameObject elementoExtra = null)
    {
        if (isAnimating) return;

        float distanza = GetLarghezzaCanvas();

        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 target = new Vector2(-distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, target, durataTransizioneMenu, true, false));
        }

        if (elementoExtra != null)
        {
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            Vector2 target = new Vector2(-distanza, rectExtra.anchoredPosition.y);
            StartCoroutine(Muovi(rectExtra, target, durataTransizioneMenu, true, false));
        }

        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(distanza, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, new Vector2(0f, rectApri.anchoredPosition.y), durataTransizioneMenu, false, true));
        }
    }

    public void FaiUscireASinistra(GameObject pagina)
    {
        if (pagina == null || isAnimating) return;

        float distanza = GetLarghezzaCanvas();
        RectTransform rect = pagina.GetComponent<RectTransform>();
        Vector2 target = new Vector2(-distanza, rect.anchoredPosition.y);
        StartCoroutine(Muovi(rect, target, durataTransizioneMenu, true, true));
    }

    public void TransizioneAvvioPartita(GameObject menu, GameObject garage, GameObject stats, GameObject background, Animator cameraAnimator = null)
    {
        if (isAnimating) return;
        StartCoroutine(SequenzaAvvioPartita(menu, garage, stats, background, cameraAnimator));
    }

    private IEnumerator SequenzaAvvioPartita(GameObject menu, GameObject garage, GameObject stats, GameObject background, Animator cameraAnimator)
    {
        isAnimating = true;
        float largh = GetLarghezzaCanvas();
        float alt = GetAltezzaCanvas();

        if (menu != null)
        {
            RectTransform rectMenu = menu.GetComponent<RectTransform>();
            Vector2 targetMenu = new Vector2(-largh, rectMenu.anchoredPosition.y);
            StartCoroutine(Muovi(rectMenu, targetMenu, durataUscitaMenu, true, false));
        }

        float tempoDiAttesa = ritardoEntrataGarage;

        if (cameraAnimator != null)
        {
            yield return null;
            AnimatorStateInfo stateInfo = cameraAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length > 0)
            {
                tempoDiAttesa = stateInfo.length * 0.85f;
            }
        }

        yield return new WaitForSecondsRealtime(tempoDiAttesa);

        if (garage != null)
        {
            garage.SetActive(true);
            RectTransform rectGarage = garage.GetComponent<RectTransform>();
            rectGarage.anchoredPosition = new Vector2(-largh, rectGarage.anchoredPosition.y);
            StartCoroutine(Muovi(rectGarage, new Vector2(0f, rectGarage.anchoredPosition.y), durataEntrataGarage, false, false));
        }

        float targetY = 1340f;
        float startY = targetY + alt;

        if (stats != null)
        {
            stats.SetActive(true);
            RectTransform rectStats = stats.GetComponent<RectTransform>();
            rectStats.anchoredPosition = new Vector2(rectStats.anchoredPosition.x, startY);
            StartCoroutine(Muovi(rectStats, new Vector2(rectStats.anchoredPosition.x, targetY), durataEntrataGarage, false, false));
        }

        if (background != null)
        {
            background.SetActive(true);
            RectTransform rectBg = background.GetComponent<RectTransform>();
            rectBg.anchoredPosition = new Vector2(rectBg.anchoredPosition.x, startY);
            StartCoroutine(Muovi(rectBg, new Vector2(rectBg.anchoredPosition.x, targetY), durataEntrataGarage, false, false));
        }

        yield return new WaitForSecondsRealtime(durataEntrataGarage);
        isAnimating = false;
    }

    /// <summary>
    /// Fa scendere le statistiche e il BG da Y: 1340 a Y: 0 per coprire il menu del garage.
    /// </summary>
    public void ApriStatisticheDettagliate(GameObject stats, GameObject background)
    {
        if (isAnimating) return;

        // Porta il BG e le statistiche in primo piano rispetto agli altri elementi del canvas
        if (background != null)
        {
            background.transform.SetAsLastSibling();
            RectTransform rectBg = background.GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(rectBg.anchoredPosition.x, 0f);
            StartCoroutine(Muovi(rectBg, targetPos, durataAnimazioneStatistiche, false, false));
        }

        if (stats != null)
        {
            stats.transform.SetAsLastSibling();
            RectTransform rectStats = stats.GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(rectStats.anchoredPosition.x, 0f);
            StartCoroutine(Muovi(rectStats, targetPos, durataAnimazioneStatistiche, false, true));
        }
    }

    /// <summary>
    /// Riporta le statistiche e il BG da Y: 0 di nuovo a Y: 1340.
    /// </summary>
    public void ChiudiStatisticheDettagliate(GameObject stats, GameObject background)
    {
        if (isAnimating) return;

        float targetY = 1340f;

        if (background != null)
        {
            RectTransform rectBg = background.GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(rectBg.anchoredPosition.x, targetY);
            StartCoroutine(Muovi(rectBg, targetPos, durataAnimazioneStatistiche, false, false));
        }

        if (stats != null)
        {
            RectTransform rectStats = stats.GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(rectStats.anchoredPosition.x, targetY);
            StartCoroutine(Muovi(rectStats, targetPos, durataAnimazioneStatistiche, false, true));
        }
    }

    private IEnumerator Muovi(RectTransform rect, Vector2 destinazione, float durata, bool disattivaAllaFine, bool gestisceLock)
    {
        if (gestisceLock) isAnimating = true;

        Vector2 start = rect.anchoredPosition;
        float t = 0f;

        while (t < durata)
        {
            t += Time.unscaledDeltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t / durata);
            rect.anchoredPosition = Vector2.Lerp(start, destinazione, smooth);
            yield return null;
        }

        rect.anchoredPosition = destinazione;

        if (disattivaAllaFine)
        {
            rect.gameObject.SetActive(false);
        }

        if (gestisceLock) isAnimating = false;
    }
}