using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MenuNavigationManager : MonoBehaviour
{
    public static MenuNavigationManager Instance;

    [Header("Impostazioni Generali")]
    [SerializeField] private float durataTransizioneMenu = 0.35f;
    [SerializeField] private Canvas canvasPrincipale;

    [Header("Impostazioni Transizione Garage")]
    [SerializeField] private float durataUscitaMenu = 0.5f;
    [Tooltip("Tempo di attesa prima dell'entrata del garage se non viene rilevata l'animazione della camera")]
    [SerializeField] private float ritardoEntrataGarage = 1.2f;
    [Tooltip("Durata dell'animazione di discesa/entrata degli elementi del garage")]
    [SerializeField] private float durataEntrataGarage = 0.85f;

    [Header("Impostazioni Statistiche Dettagliate")]
    [Tooltip("Coordinata Y di riposo (in alto dove sporge la freccia) per statistiche e BG")]
    [SerializeField] private float yPosizioneNascostaStatistiche = 1340f;
    [Tooltip("Tempo di discesa/risalita tra Y nascosta e Y: 0")]
    [SerializeField] private float durataAnimazioneStatistiche = 0.45f;

    private bool isAnimating = false;
    public bool IsAnimating => isAnimating;

    private bool statisticheAperte = false;
    public bool StatisticheAperte => statisticheAperte;

    private Coroutine coroutineStatistiche;

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

    private void Update()
    {
        bool clickPremuto = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clickPremuto = true;
            mousePos = Mouse.current.position.ReadValue();
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            clickPremuto = true;
            mousePos = Input.mousePosition;
        }
#endif

        if (clickPremuto)
        {
            if (EventSystem.current == null) return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                Debug.Log($"<color=cyan>[DEBUG RAYCAST] Cliccato su: <b>{results[0].gameObject.name}</b></color>");
            }
        }
    }

    private float GetLarghezzaCanvas()
    {
        if (canvasPrincipale != null)
            return canvasPrincipale.GetComponent<RectTransform>().rect.width;

        return Screen.width;
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

    public void TransizioneAvvioPartita(GameObject menu, GameObject garage, GameObject stats, GameObject background, Animator cameraAnimator = null)
    {
        StopAllCoroutines();
        isAnimating = false;
        statisticheAperte = false;
        StartCoroutine(SequenzaAvvioPartita(menu, garage, stats, background, cameraAnimator));
    }

    private IEnumerator SequenzaAvvioPartita(GameObject menu, GameObject garage, GameObject stats, GameObject background, Animator cameraAnimator)
    {
        isAnimating = true;
        float largh = GetLarghezzaCanvas();

        // 1. Menu principale esce a sinistra
        if (menu != null)
        {
            RectTransform rectMenu = menu.GetComponent<RectTransform>();
            Vector2 targetMenu = new Vector2(-largh, rectMenu.anchoredPosition.y);
            StartCoroutine(Muovi(rectMenu, targetMenu, durataUscitaMenu, true, false));
        }

        // Il BG esce a sinistra se visibile col menu principale
        if (background != null)
        {
            if (background.TryGetComponent<Image>(out var imgBg))
            {
                imgBg.raycastTarget = false;
            }

            RectTransform rectBg = background.GetComponent<RectTransform>();
            if (Mathf.Abs(rectBg.anchoredPosition.y) < 50f)
            {
                Vector2 targetBg = new Vector2(-largh, rectBg.anchoredPosition.y);
                StartCoroutine(Muovi(rectBg, targetBg, durataUscitaMenu, false, false));
            }
        }

        // 2. Attesa animazione telecamera
        float tempoDiAttesa = ritardoEntrataGarage;

        if (cameraAnimator != null)
        {
            yield return null;

            if (cameraAnimator.IsInTransition(0))
            {
                AnimatorStateInfo nextState = cameraAnimator.GetNextAnimatorStateInfo(0);
                if (nextState.length > 0)
                {
                    tempoDiAttesa = nextState.length;
                }
            }
            else
            {
                AnimatorStateInfo currentState = cameraAnimator.GetCurrentAnimatorStateInfo(0);
                if (currentState.length > 0)
                {
                    tempoDiAttesa = currentState.length;
                }
            }
        }

        yield return new WaitForSecondsRealtime(tempoDiAttesa);

        // 3. Entrata Garage
        if (garage != null)
        {
            garage.SetActive(true);
            RectTransform rectGarage = garage.GetComponent<RectTransform>();
            rectGarage.anchoredPosition = new Vector2(-largh, rectGarage.anchoredPosition.y);
            StartCoroutine(Muovi(rectGarage, new Vector2(0f, rectGarage.anchoredPosition.y), durataEntrataGarage, false, false));
        }

        // 4. Ripristino X a 0 e posizionamento di BG e Statistiche in alto fuori schermo
        if (background != null)
        {
            background.SetActive(true);
            background.transform.SetAsLastSibling();
            RectTransform rectBg = background.GetComponent<RectTransform>();
            // Posizionamento esplicito su X: 0f e Y nascosta
            rectBg.anchoredPosition = new Vector2(0f, yPosizioneNascostaStatistiche);
        }

        if (stats != null)
        {
            stats.SetActive(true);
            stats.transform.SetAsLastSibling();
            RectTransform rectStats = stats.GetComponent<RectTransform>();
            rectStats.anchoredPosition = new Vector2(rectStats.anchoredPosition.x, yPosizioneNascostaStatistiche);
        }

        yield return new WaitForSecondsRealtime(durataEntrataGarage);
        isAnimating = false;
    }

    public void ToggleStatisticheDettagliate(GameObject stats, GameObject background)
    {
        if (statisticheAperte)
        {
            ChiudiStatisticheDettagliate(stats, background);
        }
        else
        {
            ApriStatisticheDettagliate(stats, background);
        }
    }

    public void ApriStatisticheDettagliate(GameObject stats, GameObject background)
    {
        statisticheAperte = true;
        if (coroutineStatistiche != null)
        {
            StopCoroutine(coroutineStatistiche);
        }
        coroutineStatistiche = StartCoroutine(SequenzaStatistiche(stats, background, 0f));
    }

    public void ChiudiStatisticheDettagliate(GameObject stats, GameObject background)
    {
        statisticheAperte = false;
        if (coroutineStatistiche != null)
        {
            StopCoroutine(coroutineStatistiche);
        }
        coroutineStatistiche = StartCoroutine(SequenzaStatistiche(stats, background, yPosizioneNascostaStatistiche));
    }

    private IEnumerator SequenzaStatistiche(GameObject stats, GameObject background, float targetY)
    {
        if (background != null)
        {
            background.SetActive(true);
            background.transform.SetAsLastSibling();
            RectTransform rectBg = background.GetComponent<RectTransform>();
            // Garantisce sempre X: 0f durante l'apertura e la chiusura
            Vector2 targetPosBg = new Vector2(0f, targetY);
            StartCoroutine(Muovi(rectBg, targetPosBg, durataAnimazioneStatistiche, false, false));
        }

        if (stats != null)
        {
            stats.SetActive(true);
            stats.transform.SetAsLastSibling();
            RectTransform rectStats = stats.GetComponent<RectTransform>();
            Vector2 targetPosStats = new Vector2(rectStats.anchoredPosition.x, targetY);
            StartCoroutine(Muovi(rectStats, targetPosStats, durataAnimazioneStatistiche, false, false));
        }

        yield return new WaitForSecondsRealtime(durataAnimazioneStatistiche);
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