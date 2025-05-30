using System.Collections.Generic;
using UnityEngine;

public class DroneOrbitController : MonoBehaviour
{
    [Header("Налаштування")]
    public GameObject dronePrefab;
    public int totalDrones = 10;
    public float orbitWidth = 22f;
    public float orbitHeight = 0.5f;
    public float orbitSpeed = 0.5f;
    public int activeDronesCount = 5;
    public float maxOrbitAngleDegrees = 10f; 

    private class DroneData
    {
        public GameObject obj;
        public float t;
        public int direction;
        public float orbitAngleRad; 
    }

    private List<DroneData> drones = new();

    private void Start()
    {
        for (int i = 0; i < totalDrones; i++)
        {
            GameObject drone = Instantiate(dronePrefab, transform);
            drone.SetActive(false);

            float randomAngleDeg = Random.Range(-maxOrbitAngleDegrees, maxOrbitAngleDegrees);
            float orbitAngleRad = randomAngleDeg * Mathf.Deg2Rad;

            drones.Add(new DroneData
            {
                obj = drone,
                t = Random.Range(0f, 1f),
                direction = Random.Range(0, 2) == 0 ? 1 : -1,
                orbitAngleRad = orbitAngleRad
            });
        }

        ActivateDrones(activeDronesCount);
    }

    public void ActivateDrones(int count)
    {
        foreach (var drone in drones)
        {
            drone.obj.SetActive(false);
        }

        for (int i = 0; i < Mathf.Min(count, drones.Count); i++)
        {
            drones[i].obj.SetActive(true);
        }
    }

    public void DeactivateAllDrones()
    {
        foreach (var drone in drones)
        {
            drone.obj.SetActive(false);
        }
    }

    private void Update()
    {
        foreach (var drone in drones)
        {
            if (!drone.obj.activeSelf) continue;

            float sharpness = 1f;
            float speedFactor = Mathf.Pow(Mathf.Sin(drone.t * Mathf.PI), sharpness);
            speedFactor = Mathf.Max(speedFactor, 0.25f);
            drone.t += Time.deltaTime * orbitSpeed * speedFactor * drone.direction;

            if (drone.t > 1f)
            {
                drone.t = 1f;
                drone.direction = -1;
            }
            else if (drone.t < 0f)
            {
                drone.t = 0f;
                drone.direction = 1;
            }

            float x = Mathf.Lerp(-orbitWidth, orbitWidth, drone.t);
            float y = Mathf.Sin(drone.t * Mathf.PI * 2f) * orbitHeight * 0.5f;
            Vector3 offset = new Vector3(x, y, 0);

            offset = Quaternion.Euler(0, 0, drone.orbitAngleRad * Mathf.Rad2Deg) * offset;
            drone.obj.transform.localPosition = offset;

            SpriteRenderer sr = drone.obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = drone.direction > 0 ? -1 : 1;

                float shadowT = Mathf.InverseLerp(0.5f, 1f, drone.t); 
                float brightness = Mathf.Lerp(1f, 0f, shadowT);
                sr.color = new Color(brightness, brightness, brightness);
            }
        }
    }
}
