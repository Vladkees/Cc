using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HoverLightController : MonoBehaviour
{
    public Light2D hoverLight;
    public float fadeSpeed = 8f;
    public float targetIntensity = 1.5f;

    private Camera mainCamera;
    private bool hovering = false;
    private Vector3 targetPosition;

    private const float threshold = 0.01f; 

    void Start()
    {
        mainCamera = Camera.main;
        hoverLight.intensity = 0f;
        hoverLight.enabled = true;
        targetPosition = hoverLight.transform.position;
    }

    void Update()
    {
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        bool isOverPlanet = hit.collider != null && hit.collider.CompareTag("Highlightable");

        if (isOverPlanet)
        {
            hovering = true;
            targetPosition = mouseWorldPos;
        }
        else
        {
            hovering = false;
        }

        float desiredIntensity = hovering ? targetIntensity : 0f;

        // Перевірка, чи потрібно оновлювати
        bool intensityStable = Mathf.Abs(hoverLight.intensity - desiredIntensity) < threshold;

        hoverLight.transform.position = targetPosition;
        
        if (intensityStable)
            return;

        // Плавна зміна яскравості, позиція — миттєва
        hoverLight.intensity = Mathf.Lerp(hoverLight.intensity, desiredIntensity, Time.deltaTime * fadeSpeed);
    }

}
