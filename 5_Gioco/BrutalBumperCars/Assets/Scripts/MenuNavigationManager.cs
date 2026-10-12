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

    [Header("Animazione Camera")]
    [Tooltip("Nome dello stato dell'animazione di andata della telecamera")]
    [SerializeField] private string nomeStatoCameraAndata = "MainCameraGameSelect";
    [Tooltip("Nome dello stato dedicato di ritorno (se esiste nell'Animator)")]
    [SerializeField] private string nomeStatoCameraRitorno = "MainCameraGameSelectReverse";

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

        if (elementoExtra != null)
        {
            elementoExtra.SetActive(true);
            elementoExtra.transform.SetAsFirstSibling(); // Sfondo dietro ai menu
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            // Resetta sia X che Y (azzerando eventuali offset verticali del garage)
            rectExtra.anchoredPosition = new Vector2(-distanza, 0f);
            StartCoroutine(Muovi(rectExtra, Vector2.zero, durataTransizioneMenu, false, false));
        }

        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            paginaDaAprire.transform.SetAsLastSibling();
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(-distanza, rectApri.anchoredPosition.y);
            Vector2 targetApri = new Vector2(0f, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, targetApri, durataTransizioneMenu, false, true));
        }

        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 targetChiudi = new Vector2(distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, targetChiudi, durataTransizioneMenu, true, false));
        }
    }

    public void TransizioneIndietro(GameObject paginaDaChiudere, GameObject paginaDaAprire, GameObject elementoExtra = null)
    {
        if (isAnimating) return;

        float distanza = GetLarghezzaCanvas();

        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 targetChiudi = new Vector2(-distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, targetChiudi, durataTransizioneMenu, true, false));
        }

        if (elementoExtra != null)
        {
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            Vector2 targetExtra = new Vector2(-distanza, 0f);
            StartCoroutine(Muovi(rectExtra, targetExtra, durataTransizioneMenu, true, false));
        }

        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            paginaDaAprire.transform.SetAsLastSibling();
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(distanza, rectApri.anchoredPosition.y);
            Vector2 targetApri = new Vector2(0f, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, targetApri, durataTransizioneMenu, false, true));
        }
    }

    public void FaiUscireASinistra(GameObject pagina)
    {
        if (pagina == null || isAnimating) return;

        float distanza = GetLarghezzaCanvas();
        RectTransform rect = pagina.GetComponent<RectTransform>();
        Vector2 targetUscita = new Vector2(-distanza, rect.anchoredPosition.y);
        StartCoroutine(Muovi(rect, targetUscita, durataTransizioneMenu, true, true));
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
        float alt = GetAltezzaCanvas();
        float startY = yPosizioneNascostaStatistiche + alt;

        // 1. Menu principale e BG escono a sinistra
        if (menu != null)
        {
            RectTransform rectMenu = menu.GetComponent<RectTransform>();
            Vector2 targetMenu = new Vector2(-largh, rectMenu.anchoredPosition.y);
            StartCoroutine(Muovi(rectMenu, targetMenu, durataUscitaMenu, true, false));
        }

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

        // 2. Telecamera in andata verso il Garage
        float tempoDiAttesa = ritardoEntrataGarage;

        if (cameraAnimator != null)
        {
            cameraAnimator.speed = 1f;
            cameraAnimator.Play(nomeStatoCameraAndata, 0, 0f);

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

        // 4. Discesa di Statistiche e BG dall'alto verso la posizione di riposo Y
        if (background != null)
        {
            background.SetActive(true);
            background.transform.SetAsLastSibling();
            RectTransform rectBg = background.GetComponent<RectTransform>();
            rectBg.anchoredPosition = new Vector2(0f, startY);
            StartCoroutine(Muovi(rectBg, new Vector2(0f, yPosizioneNascostaStatistiche), durataEntrataGarage, false, false));
        }

        if (stats != null)
        {
            stats.SetActive(true);
            stats.transform.SetAsLastSibling();
            RectTransform rectStats = stats.GetComponent<RectTransform>();
            rectStats.anchoredPosition = new Vector2(rectStats.anchoredPosition.x, startY);
            StartCoroutine(Muovi(rectStats, new Vector2(rectStats.anchoredPosition.x, yPosizioneNascostaStatistiche), durataEntrataGarage, false, false));
        }

        yield return new WaitForSecondsRealtime(durataEntrataGarage);
        isAnimating = false;
    }

    public void TransizioneRitornoAlMenu(GameObject menu, GameObject garage, GameObject stats, GameObject background, Animator cameraAnimator = null)
    {
        if (isAnimating) return;
        StopAllCoroutines();
        StartCoroutine(SequenzaRitornoAlMenu(menu, garage, stats, background, cameraAnimator));
    }

    private IEnumerator SequenzaRitornoAlMenu(GameObject menu, GameObject garage, GameObject stats, GameObject background, Animator cameraAnimator)
    {
        isAnimating = true;
        float largh = GetLarghezzaCanvas();
        float alt = GetAltezzaCanvas();
        float targetYSalita = yPosizioneNascostaStatistiche + alt;

        // FASE 1: Se le statistiche erano abbassate a Y: 0, le facciamo risalire prima
        if (statisticheAperte)
        {
            statisticheAperte = false;
            yield return StartCoroutine(SequenzaStatistiche(stats, background, yPosizioneNascostaStatistiche));
        }

        // FASE 2: USCITA GARAGE, STATISTICHE E BG
        if (garage != null)
        {
            RectTransform rectGarage = garage.GetComponent<RectTransform>();
            Vector2 targetGarage = new Vector2(-largh, rectGarage.anchoredPosition.y);
            StartCoroutine(Muovi(rectGarage, targetGarage, durataEntrataGarage, true, false));
        }

        if (stats != null)
        {
            RectTransform rectStats = stats.GetComponent<RectTransform>();
            Vector2 targetStats = new Vector2(rectStats.anchoredPosition.x, targetYSalita);
            StartCoroutine(Muovi(rectStats, targetStats, durataEntrataGarage, true, false));
        }

        if (background != null)
        {
            RectTransform rectBg = background.GetComponent<RectTransform>();
            Vector2 targetBg = new Vector2(0f, targetYSalita);
            StartCoroutine(Muovi(rectBg, targetBg, durataEntrataGarage, true, false));
        }

        yield return new WaitForSecondsRealtime(durataEntrataGarage);

        if (stats != null) stats.SetActive(false);
        if (background != null) background.SetActive(false);

        // FASE 3: REVERSE TELECAMERA
        if (cameraAnimator != null)
        {
            int hashRitorno = Animator.StringToHash(nomeStatoCameraRitorno);

            if (!string.IsNullOrEmpty(nomeStatoCameraRitorno) && cameraAnimator.HasState(0, hashRitorno))
            {
                cameraAnimator.speed = 1f;
                cameraAnimator.Play(nomeStatoCameraRitorno, 0, 0f);
                yield return null;

                float tempoClip = ritardoEntrataGarage;
                AnimatorStateInfo st = cameraAnimator.GetCurrentAnimatorStateInfo(0);
                if (st.length > 0) tempoClip = st.length;
                yield return new WaitForSecondsRealtime(tempoClip);
            }
            else
            {
                float durataCamera = ritardoEntrataGarage;
                AnimatorStateInfo st = cameraAnimator.GetCurrentAnimatorStateInfo(0);
                if (st.length > 0) durataCamera = st.length;

                float tempoPassato = 0f;
                while (tempoPassato < durataCamera)
                {
                    tempoPassato += Time.unscaledDeltaTime;
                    float progresso = Mathf.Clamp01(tempoPassato / durataCamera);
                    float normalizedTimeReverse = 1f - progresso;

                    cameraAnimator.Play(nomeStatoCameraAndata, 0, normalizedTimeReverse);
                    cameraAnimator.Update(0f);
                    yield return null;
                }

                cameraAnimator.Play(nomeStatoCameraAndata, 0, 0f);
                cameraAnimator.Update(0f);
            }

            cameraAnimator.speed = 0f;
        }
        else
        {
            yield return new WaitForSecondsRealtime(ritardoEntrataGarage);
        }

        // FASE 4: RIENTRO DEL MENU PRINCIPALE
        if (menu != null)
        {
            menu.SetActive(true);
            menu.transform.SetAsLastSibling();
            RectTransform rectMenu = menu.GetComponent<RectTransform>();
            rectMenu.anchoredPosition = new Vector2(-largh, rectMenu.anchoredPosition.y);
            StartCoroutine(Muovi(rectMenu, new Vector2(0f, rectMenu.anchoredPosition.y), durataUscitaMenu, false, false));
        }

        yield return new WaitForSecondsRealtime(durataUscitaMenu);
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