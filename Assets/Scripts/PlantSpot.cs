using UnityEngine;

public class PlantSpot : MonoBehaviour
{
    public GameObject seedling;
    public GameObject plant;
    public GameObject goldPrefab;
    public GameObject bigGoldPrefab;
    public GameObject arrowPrefab;
    public GameObject dynamitePrefab;
    public GameObject whiskyPrefab;

    public bool Growing { get => seedling.activeInHierarchy || plant.activeInHierarchy; }

    public int dropGold { get; set; } = 0;
    public int dropBigGold { get; set; } = 0;
    public int dropArrows { get; set; } = 0;
    public int dropDynamite { get; set; } = 0;
    public int dropWhisky { get; set; } = 0;

    void Start()
    {
        seedling.SetActive(false);
        plant.SetActive(false);
    }

   public void Sunrise() {
        if (seedling.activeInHierarchy) {
            Grow();
        }
        Vector3 RandomPosition() => transform.position + Vector3.up * 0.25f + Random.insideUnitSphere * 0.25f;
        if (Growing) {
            for (int i = 0; i != dropGold; ++i)
            {
                Instantiate(goldPrefab, RandomPosition(), Random.rotation);
            }
            for (int i = 0; i != dropBigGold; ++i)
            {
                Instantiate(bigGoldPrefab, RandomPosition(), Random.rotation);
            }
            for (int i = 0; i != dropArrows; ++i)
            {
                Instantiate(arrowPrefab, RandomPosition(), Random.rotation);
            }
            for (int i = 0; i != dropDynamite; ++i)
            {
                Instantiate(dynamitePrefab, RandomPosition(), Random.rotation);
            }
            for (int i = 0; i != dropWhisky; ++i)
            {
                Instantiate(whiskyPrefab, RandomPosition(), Random.rotation);
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
