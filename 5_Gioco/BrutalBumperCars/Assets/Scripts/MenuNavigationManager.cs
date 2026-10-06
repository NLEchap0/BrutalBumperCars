using System.Collections;
using UnityEngine;

public class MenuNavigationManager : MonoBehaviour
{
    public static MenuNavigationManager Instance;

    [Header("Impostazioni")]
    [SerializeField] private float durataAnimazione = 0.35f;
    [SerializeField] private Canvas canvasPrincipale;

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
        {
            return canvasPrincipale.GetComponent<RectTransform>().rect.width;
        }
        return Screen.width;
    }

    public void TransizioneAvanti(GameObject paginaDaChiudere, GameObject paginaDaAprire, GameObject elementoExtra = null)
    {
        if (isAnimating) return;

        float distanza = GetLarghezzaCanvas();

        // 1. Pagina che entra: parte da -distanza e arriva a 0
        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(-distanza, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, new Vector2(0f, rectApri.anchoredPosition.y), durataAnimazione, false, true));
        }

        // 2. Pagina che esce: parte dal centro (0) e va a +distanza
        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 target = new Vector2(distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, target, durataAnimazione, true, false));
        }

        // 3. Elemento Extra / BG
        if (elementoExtra != null)
        {
            elementoExtra.SetActive(true);
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            rectExtra.anchoredPosition = new Vector2(-distanza, rectExtra.anchoredPosition.y);
            StartCoroutine(Muovi(rectExtra, new Vector2(0f, rectExtra.anchoredPosition.y), durataAnimazione, false, false));
        }
    }

    public void TransizioneIndietro(GameObject paginaDaChiudere, GameObject paginaDaAprire, GameObject elementoExtra = null)
    {
        if (isAnimating) return;

        float distanza = GetLarghezzaCanvas();

        // 1. Pagina che esce: va a -distanza e poi si spegne
        if (paginaDaChiudere != null)
        {
            RectTransform rectChiudi = paginaDaChiudere.GetComponent<RectTransform>();
            Vector2 target = new Vector2(-distanza, rectChiudi.anchoredPosition.y);
            StartCoroutine(Muovi(rectChiudi, target, durataAnimazione, true, false));
        }

        // 2. Elemento Extra / BG
        if (elementoExtra != null)
        {
            RectTransform rectExtra = elementoExtra.GetComponent<RectTransform>();
            Vector2 target = new Vector2(-distanza, rectExtra.anchoredPosition.y);
            StartCoroutine(Muovi(rectExtra, target, durataAnimazione, true, false));
        }

        // 3. Pagina che rientra: parte da +distanza e va a 0
        if (paginaDaAprire != null)
        {
            paginaDaAprire.SetActive(true);
            RectTransform rectApri = paginaDaAprire.GetComponent<RectTransform>();
            rectApri.anchoredPosition = new Vector2(distanza, rectApri.anchoredPosition.y);
            StartCoroutine(Muovi(rectApri, new Vector2(0f, rectApri.anchoredPosition.y), durataAnimazione, false, true));
        }
    }

    public void FaiUscireASinistra(GameObject pagina)
    {
        if (pagina == null || isAnimating) return;

        float distanza = GetLarghezzaCanvas();
        RectTransform rect = pagina.GetComponent<RectTransform>();
        Vector2 target = new Vector2(-distanza, rect.anchoredPosition.y);
        StartCoroutine(Muovi(rect, target, durataAnimazione, true, true));
    }

    private IEnumerator Muovi(RectTransform rect, Vector2 destinazione, float durata, bool disattivaAllaFine, bool gestisceLock)
    {
        if (gestisceLock) isAnimating = true;

        Vector2 start = rect.anchoredPosition;
        float t = 0f;

        while (t < durata)
        {
            t += Time.unscaledDeltaTime; // unscaledDeltaTime permette animazioni anche se Time.timeScale == 0
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