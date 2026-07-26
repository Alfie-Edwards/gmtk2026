using UnityEngine;

public class PlantSpot : MonoBehaviour
{
    public GameObject seedling;
    public GameObject plant;
    public GameObject goldPrefab;

    public bool Growing { get => seedling.activeInHierarchy || plant.activeInHierarchy; }

    void Start()
    {
        seedling.SetActive(false);
        plant.SetActive(false);
    }

   public void Sunrise() {
        if (seedling.activeInHierarchy) {
            Grow();
        }
        if (Growing) {
            for (int i = 0; i != 10; ++i)
            {
                Instantiate(goldPrefab);
            }
        }
    }

    public void PlantSeed()
    {
        seedling.SetActive(true);
        plant.SetActive(false);
    }

    void Grow()
    {
        seedling.SetActive(false);
        plant.SetActive(true);
    }
}
