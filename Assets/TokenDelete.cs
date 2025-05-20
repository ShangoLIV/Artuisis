using UnityEngine;
using UnityEngine.EventSystems;

public class TokenDelete : MonoBehaviour
{
    Camera cam;

    void Awake() => cam = Camera.main;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Delete)) return;

        // Ne supprime rien si on clique sur l’UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Si le collider porte un DraggableToken => on détruit
            if (hit.collider.GetComponent<DraggableToken>() != null)
            {
                Destroy(hit.collider.gameObject);
            }
        }
    }
}