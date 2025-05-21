using UnityEngine;
using UnityEngine.EventSystems;   // pour ignorer l’UI

public class TokenSpawner : MonoBehaviour
{
    [Header("Références prefabs")]
    public GameObject whitePuckPrefab;   // PuckWhite
    public GameObject blackPuckPrefab;   // PuckBlack

    [Header("Plan de spawn")]
    public float groundY = 0f;           // hauteur du sol (plan X-Z)

    Plane groundPlane;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        groundPlane = new Plane(Vector3.up, new Vector3(0, groundY, 0));
    }

    void Update()
    {
        // Ignorer si clic sur UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (IsCtrlHeld() || IsShiftHeld()) return;
        
        // clic gauche → blanc
        if (Input.GetMouseButtonDown(0))
            SpawnToken(whitePuckPrefab);

        // clic droit → noir
        if (Input.GetMouseButtonDown(1))
            SpawnToken(blackPuckPrefab);
    }
    
    bool IsCtrlHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }
    
    bool IsShiftHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    void SpawnToken(GameObject prefab)
    {
        // Ray depuis souris vers le plan sol
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!groundPlane.Raycast(ray, out float enter)) return;

        Vector3 spawnPos = ray.GetPoint(enter);
        spawnPos.y = groundY;                    // garantit la bonne hauteur

        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}