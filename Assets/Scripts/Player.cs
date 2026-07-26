using UnityEngine;
using UnityEngine.InputSystem;

using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Keyboard")]
    [SerializeField] public float moveSpeed = 5.0f;
    [SerializeField] public float gravity = -9.81f * 2.0f;
    [SerializeField] public float jumpHeight = 1.5f;

    [Header("World")]
    [SerializeField] public float itemPickupRadius = 2.0f;
    [SerializeField] public DayNightCycle dayNightCycle;
    [SerializeField] public Hotbar hotbar;
    [SerializeField] public Hotbar ammoDisplay;
    [SerializeField] public Transform camera;
    [SerializeField] public Vector3 cameraOffset = new Vector3(1f, 6f, -4f);
    [SerializeField] public GameObject ghostPrefab;

    [Header("Upgrades")]
    [SerializeField] private int startQuiverSize = 0;
    [SerializeField] private int maxQuiverSize = 4;
    [SerializeField] private int startBombBagSize = 0;
    [SerializeField] private int maxBombBagSize = 4;
    [SerializeField] private int startWhipLevel = 1;
    [SerializeField] private int maxWhipLevel = 4;
    [SerializeField] private int maxRoosters = 3;

    [Header("Weapons")]
    [SerializeField] private Whip whip;
    [SerializeField] private Bow bow;
    [SerializeField] private BombBag bombBag;
    [SerializeField] private Pickaxe pickaxe;

    private int quiverSize_;
    private int bombBagSize_;

    public int whipLevel { get; private set; }
    public int quiverSize {
        get => quiverSize_;
        set {
            quiverSize_ = value;
            ammoDisplay.SetLimit(ItemType.Arrow, value);
        }
    }
    public int bombBagSize {
        get => bombBagSize_;
        set {
            bombBagSize_ = value;
            ammoDisplay.SetLimit(ItemType.Dynamite, value);
        }
    }

    private CharacterController controller;
    private Vector3 velocity = Vector3.zero;
    private Quaternion lookTarget;
    private Bag itemsBag;
    private Bag ammoBag;

    void Start()
    {
        // init upgrades
        whipLevel = startWhipLevel;
        quiverSize = startQuiverSize;
        bombBagSize = startBombBagSize;

        // init objects
        itemsBag = new Bag();
        ammoBag = new Bag();
        ammoBag.persistItems = true;
        hotbar.bag = itemsBag;
        ammoDisplay.bag = ammoBag;
        controller = GetComponent<CharacterController>();
        PickupItem(ItemType.Whip);
        camera.transform.position = transform.position + cameraOffset;
        camera.transform.LookAt(transform);

        // init controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ammoBag.Add(ItemType.Gold, 100000);
    }

    void Update()
    {
        Move();
        PickupItems();
        HandleShops();
        UseItems();
        UpdateWeapons();

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            SpawnGhost();
        }
    }

    private void UseItems()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            Use(hotbar.Selected);
        }
    }

    private void Move()
    {
        float moveForwardAmount = 0;
        float moveRightAmount = 0;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveForwardAmount += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveForwardAmount -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveRightAmount += 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveRightAmount -= 1;
        }
        Vector3 move = ((Vector3.forward * moveForwardAmount) + (Vector3.right * moveRightAmount)).normalized * moveSpeed;


        // Jump
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Gravity
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // 4. Move the Controller
        controller.Move((velocity + move) * Time.deltaTime);
        if (move != Vector3.zero)
        {
            lookTarget = Quaternion.LookRotation(move);
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, lookTarget, Mathf.Min(16f * Time.deltaTime, 1f));
        camera.transform.position = Vector3.Lerp(camera.transform.position, transform.position + cameraOffset, Mathf.Min(3f * Time.deltaTime, 1f));
    }

    private void UpdateWeapons()
    {
        whip.gameObject.SetActive(hotbar.Selected == ItemType.Whip);
        bow.gameObject.SetActive(hotbar.Selected == ItemType.Bow);
        bombBag.gameObject.SetActive(hotbar.Selected == ItemType.BombBag);
        pickaxe.gameObject.SetActive(hotbar.Selected == ItemType.Pickaxe);
    }

    private void PickupItems()
    {
        foreach (Item item in FindObjectsByType<Item>()) {
            if (!item.enabled) continue;
            float itemPickupRadiusSq = itemPickupRadius * itemPickupRadius;
            if ((transform.position - item.transform.position).sqrMagnitude < itemPickupRadiusSq)
            {
                if (CanPickupItem(item.type))
                {
                    PickupItem(item.type);
                    Destroy(item.gameObject);
                }
            }
        }
    }

    private void HandleShops() {
        ShopCrate closest = null;
        float closestSqDist = float.MaxValue;
        float sqDistCutoff = 2f * 2f;

        foreach (ShopCrate x in FindObjectsByType<ShopCrate>())
        {
            float sqDist = (x.transform.position - transform.position).sqrMagnitude;

            if (sqDist <= sqDistCutoff && sqDist < closestSqDist)
            {
                closestSqDist = sqDist;
                closest = x;
            }
        }

        if (closest != null)
        {
            switch (closest.itemType) {
                case ItemType.Arrow:
                    closest.description = $"{ammoBag.Amount(ItemType.Arrow)} / {quiverSize}";
                    break;

                case ItemType.Dynamite:
                    closest.description = $"{ammoBag.Amount(ItemType.Dynamite)} / {bombBagSize}";
                    break;
            }

            // Buy from shop
            closest.Show();

            if (Keyboard.current.eKey.wasPressedThisFrame && closest.cost < ammoBag.Amount(ItemType.Gold) && CanPickupItem(closest.itemType)) {
                ammoBag.Remove(ItemType.Gold, closest.cost);
                PickupItem(closest.itemType);
                switch (closest.itemType) {
                    case ItemType.Seed:
                        closest.cost *= 3;
                        if (!CanPickupItem(closest.itemType)) {
                            closest.SetSoldOut();
                        }
                        break;

                    case ItemType.Rooster:
                    case ItemType.QuiverUpgrade:
                    case ItemType.BombBagUpgrade:
                    case ItemType.WhipUpgrade:
                        closest.cost *= 15;
                        if (!CanPickupItem(closest.itemType)) {
                            closest.SetSoldOut();
                        }
                        break;
                }
            }
        }
    }

    private bool CanPickupItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Gold:
            case ItemType.Whisky:
                return true;

            case ItemType.Whip:
            case ItemType.Bow:
            case ItemType.BombBag:
            case ItemType.Pickaxe:
                return itemsBag.Amount(type) == 0;

            case ItemType.Dynamite:
                return itemsBag.Has(ItemType.BombBag) && ammoBag.Amount(ItemType.Dynamite) < bombBagSize;

            case ItemType.Arrow:
                return itemsBag.Has(ItemType.Bow) && ammoBag.Amount(ItemType.Arrow) < quiverSize;

            // Upgrades
            case ItemType.Seed:
                return FindObjectsByType<PlantSpot>().Any(x => !x.Growing);

            case ItemType.QuiverUpgrade:
                return itemsBag.Has(ItemType.Bow) && quiverSize < maxQuiverSize;

            case ItemType.BombBagUpgrade:
                return itemsBag.Has(ItemType.BombBag) && bombBagSize < maxBombBagSize;

            case ItemType.WhipUpgrade:
                return whipLevel < maxWhipLevel;

            case ItemType.Rooster:
                return FindObjectsByType<Rooster>().Count() < maxRoosters;

            default:
                return false;
        }
    }

    private void PickupItem(ItemType type)
    {
        Debug.Log($"Picked up item {type}");
        switch (type)
        {
            case ItemType.Whip:
            case ItemType.Pickaxe:
                itemsBag.Add(type);
                break;

            case ItemType.Bow:
                quiverSize += 1;
                itemsBag.Add(type);
                break;

            case ItemType.BombBag:
                bombBagSize += 1;
                itemsBag.Add(type);
                break;

            case ItemType.Gold:
            case ItemType.Dynamite:
            case ItemType.Arrow:
                ammoBag.Add(type);
                break;

            case ItemType.Whisky:
                dayNightCycle.timeRemainingS += 30;
                break;

            // Upgrades
            case ItemType.Seed:
                List<PlantSpot> ungrown = FindObjectsByType<PlantSpot>()
                    .Where(x => !x.Growing)
                    .ToList();
                if (ungrown.Count > 0)
                {
                    int i = Random.Range(0, ungrown.Count);
                    ungrown[i].PlantSeed();
                    dayNightCycle.DayStart += ungrown[i].Sunrise;
                }
                break;

            case ItemType.QuiverUpgrade:
                quiverSize += 1;
                if (CanPickupItem(ItemType.Arrow)) {
                    PickupItem(ItemType.Arrow);
                }
                break;

            case ItemType.BombBagUpgrade:
                bombBagSize += 1;
                if (CanPickupItem(ItemType.Dynamite)) {
                    PickupItem(ItemType.Dynamite);
                }
                break;

            case ItemType.WhipUpgrade:
                whipLevel += 1;
                break;

            case ItemType.Rooster:
                SpawnRooster();
                break;
        }
    }

    private void SpawnGhost()
    {
        if (ghostPrefab == null) return;

        // Pick a random direction around the player, keeping it flat on the Y axis
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0f;
        randomDirection.Normalize();

        // If it ends up zero (e.g. random vector was straight up/down), fallback to forward
        if (randomDirection == Vector3.zero)
        {
            randomDirection = transform.forward;
        }

        // Calculate spawn position at the fixed distance away
        Vector3 spawnPosition = transform.position + (randomDirection * 15f);

        // Optionally match the player's current Y level or keep the ghost's default
        spawnPosition.y = transform.position.y;

        // Instantiate the ghost
        GameObject spawnedGhost = Instantiate(ghostPrefab, spawnPosition, Quaternion.identity);

        // Automatically trigger its fade-in routine if it has the EnemyGhost script
        EnemyGhost ghostScript = spawnedGhost.GetComponent<EnemyGhost>();
        if (ghostScript != null)
        {
            StartCoroutine(ghostScript.FadeIn());
        }
    }

    private void SpawnRooster() {
        Debug.Log("Spawned rooster!!!");
    }

    private void Use(ItemType type) {
        Debug.Log($"Used {type}");
        switch (type)
        {
            case ItemType.Whip:
                whip.Attack();
                break;
            case ItemType.Bow:
                if (ammoBag.Amount(ItemType.Arrow) > 0)
                {
                    ammoBag.Remove(ItemType.Arrow);
                    bow.Fire(transform.forward);
                }
                break;
            case ItemType.BombBag:
                if (ammoBag.Amount(ItemType.Dynamite) > 0)
                {
                    ammoBag.Remove(ItemType.Dynamite);
                    bombBag.ThrowBomb(transform.forward);
                }
                break;
            case ItemType.Pickaxe:
                pickaxe.Swing();
                break;
        }
    }

    public void GetHit() {
        Debug.Log("Player got hit!");
    }
}