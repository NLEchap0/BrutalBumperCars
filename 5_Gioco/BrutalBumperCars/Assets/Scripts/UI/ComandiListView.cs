using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Parte puramente grafica della schermata "Comandi".
/// Costruisce a runtime lo ScrollRect (lista + viewport + contenuto) e
/// istanzia le righe a partire dal template. Non conosce Input System.
/// </summary>
public class ComandiListView
{
    // Distanza dall'alto del pannello (lascia spazio al titolo "COMANDI").
    private const float TopOffset = 160f;

    // Velocita' della rotellina: piu' alto = scroll piu' rapido.
    private const float ScrollSpeed = 120f;

    // Spazio verticale tra una riga e la successiva.
    private const float RowSpacing = 20f;

    // Dimensioni della lista: coincide con la reference resolution (2560x1440).
    private static readonly Vector2 ListSize = new Vector2(2560f, 1100f);

    // Margini interni: 150px a sinistra/destra per centrare le righe (2260 di larghezza).
    private static readonly RectOffset ListPadding = new RectOffset(150, 150, 10, 10);

    private readonly RectTransform parent;      // il pannello "Comandi"
    private readonly ComandoRowUI rowTemplate;  // riga di esempio da clonare
    private readonly float rowHeight;           // altezza copiata dal template

    private RectTransform content;              // contenitore dentro cui finiscono le righe

    public ComandiListView(RectTransform parent, ComandoRowUI rowTemplate)
    {
        this.parent = parent;
        this.rowTemplate = rowTemplate;

        // Leggiamo l'altezza dal template: ogni riga clonata avra' la stessa.
        rowHeight = ((RectTransform)rowTemplate.transform).sizeDelta.y;
    }

    /// <summary>Costruisce l'intera gerarchia dello scroll. Va chiamato una sola volta.</summary>
    public void Build()
    {
        // Il template non deve essere visibile: serve solo come stampo.
        rowTemplate.gameObject.SetActive(false);

        RectTransform scrollArea = CreateScrollArea();
        RectTransform viewport = CreateViewport(scrollArea);
        content = CreateContent(viewport);

        ConfigureScrollRect(scrollArea.GetComponent<ScrollRect>(), viewport);
    }

    /// <summary>Clona il template, aggiorna i testi e restituisce la riga creata.</summary>
    public ComandoRowUI AddRow(string action, string binding)
    {
        ComandoRowUI row = Object.Instantiate(rowTemplate, content);
        row.gameObject.SetActive(true);
        row.Show(action, binding);

        // Il VerticalLayoutGroup usa questo valore per posizionare le righe.
        // Senza LayoutElement l'altezza verrebbe calcolata a 0.
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = rowHeight;

        return row;
    }

    /// <summary>Radice scrollabile: immagine trasparente (per i click) + ScrollRect.</summary>
    private RectTransform CreateScrollArea()
    {
        GameObject list = CreateUIObject("ListaComandi", parent);
        RectTransform rect = (RectTransform)list.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -TopOffset);
        rect.sizeDelta = ListSize;

        // Immagine completamente trasparente: serve solo a ricevere i click/drag
        // perche' lo ScrollRect ha bisogno di un graphic "raycastable".
        Image background = list.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0f);
        background.raycastTarget = true;

        list.AddComponent<ScrollRect>();
        return rect;
    }

    /// <summary>Area visibile che ritaglia il contenuto (maschera).</summary>
    private RectTransform CreateViewport(RectTransform scrollArea)
    {
        GameObject viewport = CreateUIObject("Viewport", scrollArea);
        RectTransform rect = (RectTransform)viewport.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 1f);

        // RectMask2D ritaglia la grafica fuori dal viewport.
        viewport.AddComponent<RectMask2D>();
        return rect;
    }

    /// <summary>Contenitore delle righe, con impilamento verticale automatico.</summary>
    private RectTransform CreateContent(RectTransform viewport)
    {
        GameObject contentObject = CreateUIObject("Contenuto", viewport);
        RectTransform rect = (RectTransform)contentObject.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = Vector2.zero;

        // Dispone le righe una sotto l'altra e le allarga a tutta la larghezza.
        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.padding = ListPadding;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        // Il contenuto cresce in altezza in base al numero di righe, cosi' lo scroll funziona.
        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    /// <summary>Collega viewport/contenuto allo ScrollRect e imposta la velocita'.</summary>
    private void ConfigureScrollRect(ScrollRect scroll, RectTransform viewport)
    {
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = ScrollSpeed;
    }

    /// <summary>Crea un GameObject UI vuoto, sullo stesso layer del pannello.</summary>
    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
    }
}
