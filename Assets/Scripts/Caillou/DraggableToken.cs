/*  DraggableToken.cs
 *  Attach this script to the Puck prefabs (white / black).
 *  – Ctrl + Left-click  : saisir et déplacer le caillou
 *  – Shift + Left-click : inverser attracteur / répulseur
 *  – Mouse wheel / Inspector slider “strength” : régler la puissance LED (0-1)
 *  – Middle-click (optionnel) : supprimer le caillou
 */

using UnityEngine;
using UnityEngine.EventSystems;      // pour ignorer le clic sur l’UI
using Caillou;                       // namespace où se trouvent TokenData & TokenPolarity

[RequireComponent(typeof(Collider))]
public class DraggableToken : MonoBehaviour
{
    [Header("Paramètres caillou")]
    public TokenPolarity polarity = TokenPolarity.Attractor;  // blanc / noir
    public float         range    = 3f;                       // rayon d’action
    [Range(0f,1f)] public float strength = 1f;                // intensité (LED)
    public float hitRadius = 0.45f; // rayon d’impact (pour les agents)
    // ---------------------------------------------------------
    private TokenData data;
    private Plane     dragPlane;
    private Camera    cam;
    private bool      isDragging = false;

    // ---------------------------------------------------------
    void Start()
    {
        cam       = Camera.main;
        dragPlane = new Plane(Vector3.up, Vector3.zero);

        data = new TokenData(polarity, transform.position, range, strength, hitRadius);
        TokenManager.Instance.AddToken(data);

        UpdateVisual();
    }

    // ---------------------------------------------------------
    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;                                      // ignore UI

        // --- Déplacement quand on maintient CTRL + bouton gauche ---
        if (isDragging)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                hit.y = transform.position.y;            // garde même hauteur
                transform.position = hit;
                data.Position      = hit;
            }

            if (!Input.GetMouseButton(0)) isDragging = false;   // relâchement
        }

        HandleScroll();
        // --- Mise à jour force (LED) ---
        data.Strength01 = strength;
		data.Range = range;
        UpdateEmission();
    }
	// ---------------------------------------------------------
    bool IsPointerOverMe()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, 100f) &&
               hit.collider == GetComponent<Collider>();
    }

	void HandleScroll()
	{
    	if (!IsPointerOverMe()) return;

    	float scroll = Input.mouseScrollDelta.y;
    	if (Mathf.Abs(scroll) < 0.01f) return;

   		if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
    	{   // --------- ALT + molette  : modifie l’intensité LED  ---------
            strength = Mathf.Clamp01(strength + Mathf.Sign(scroll) * 0.10f);
            data.Strength01 = strength;
            UpdateEmission();
        	                                   // synchro immédiate
    	}
    	else
    	{   // --------- molette seule : modifie le rayon d’influence--------------
            range = Mathf.Clamp(range + scroll * 0.5f, 1f, 8f);   // pas de 0,5 m
            data.Range = range;
    	}
    }

	
    // ---------------------------------------------------------
    void OnMouseDown()
    {
        // Début du drag si CTRL + clic gauche
        if (Input.GetMouseButton(0) &&
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            isDragging = true;
        }

        // Inverse polarité si SHIFT + clic gauche (sans Ctrl)
        else if (Input.GetMouseButtonDown(0) &&
                 (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            TogglePolarity();
        }
    }



    // ---------------------------------------------------------
    void TogglePolarity()
    {
        polarity = polarity == TokenPolarity.Attractor ? TokenPolarity.Repulsor
                                                       : TokenPolarity.Attractor;
        data.Polarity = polarity;
        UpdateVisual();
    }

    // ---------------------------------------------------------
    void UpdateVisual()
    {
        var rend = GetComponent<Renderer>();
        if (!rend) return;

        switch (polarity)
        {
            case TokenPolarity.Attractor: rend.material.color = Color.white; break;
            case TokenPolarity.Repulsor:  rend.material.color = Color.black; break;
        }
        UpdateEmission();
    }

    void UpdateEmission()
    {
        var rend = GetComponent<Renderer>();
        if (!rend) return;

        Color baseCol = polarity == TokenPolarity.Attractor ? Color.white : Color.black;
        rend.material.SetColor("_EmissionColor", baseCol * strength * 2f); // boost visible
    }

    // ---------------------------------------------------------
    void OnDestroy()
    {
        if (TokenManager.Instance) TokenManager.Instance.RemoveToken(data);
    }
}
