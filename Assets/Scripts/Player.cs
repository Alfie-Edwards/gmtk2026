using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;

using System.Collections.Generic;
using System.Linq;

public enum MapArea {
    Town,
    Wilderness,
    Wilderness2,
    Boss,
    None,
}

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
    [SerializeField] public Hotbar treasureDisplay;
    [SerializeField] public Transform camera;
    [SerializeField] public float cameraDist = 8f;
    [SerializeField] public float cameraAngle = 45f;
    [SerializeField] public float cameraUpTilt = 10f;
    [SerializeField] public float cameraZOffset = 10f;
    [SerializeField] public GameObject ghostPrefab;
    [SerializeField] public int ghostSpawnsPerSecond = 2;
    [SerializeField] public GameObject arrowShop;
    [SerializeField] public GameObject bombShop;
    [SerializeField] public GameObject quiverShop;
    [SerializeField] public GameObject bombBagShop;
    [SerializeField] public GameObject droppedGoldPrefab;

    [SerializeField] public Transform roosterSpawnPoint;
    [SerializeField] public GameObject roosterPrefab;

    private int startQuiverSize = 0;
    private int maxQuiverSize = 4;
    private int startBombBagSize = 0;
    private int maxBombBagSize = 4;
    private int startWhipLevel = 1;
    private int maxWhipLevel = 4;
    private int maxRoosters = 3;
    private int numSeeds = 0;

    [Header("Weapons")]
    [SerializeField] private Whip whip;
    [SerializeField] private Bow bow;
    [SerializeField] private BombBag bombBag;
    [SerializeField] private Pickaxe pickaxe;

    [Header("Combat")]
    [SerializeField] private float knockbackDamping = 10f;
    [SerializeField] private float hitCooldown = 0.5f;
    public bool win = false;

    private float lastHitTime = -999f;
    private int quiverSize_;
    private int bombBagSize_;
    private MapArea mapArea;

    public EventReference wildernessMusicSource;
    public EventReference townMusicSource;
    public EventReference tenSecondMusicSource;

    private EventInstance WildernessMusic;
    private EventInstance TownMusic;
    private EventInstance TenSecondMusic;

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
    private Bag treasureBag;
    private Vector3 knockbackVelocity;
    private Vector3 cameraOffset;
    void Start()
    {
        // init upgrades
        whipLevel = startWhipLevel;
        quiverSize = startQuiverSize;
        bombBagSize = startBombBagSize;

        // bags
        itemsBag = new Bag();
        ammoBag = new Bag
        {
            persistItems = true
        };
        treasureBag = new Bag();
        itemsBag.OnItemAdded += OnItemPickedup;
        ammoBag.OnItemAdded += OnItemPickedup;
        treasureBag.OnItemAdded += OnItemPickedup;

        // Hookup to and init stuff in the world.
        hotbar.bag = itemsBag;
        ammoDisplay.bag = ammoBag;
        treasureDisplay.bag = treasureBag;
        controller = GetComponent<CharacterController>();
        PickupItem(ItemType.Whip);
        cameraOffset = new Vector3(0f, cameraDist * Mathf.Sin(cameraAngle * Mathf.Deg2Rad), cameraZOffset + cameraDist * -Mathf.Cos(cameraAngle * Mathf.Deg2Rad));
        camera.transform.position = transform.position + cameraOffset;
        camera.transform.rotation = Quaternion.Euler(cameraAngle - cameraUpTilt, 0f, 0f);
        mapArea = MapArea.Town;
        arrowShop.SetActive(false);
        quiverShop.SetActive(false);
        bombShop.SetActive(false);
        bombBagShop.SetActive(false);
        Spawn();

        // init controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ammoBag.Add(ItemType.Gold, 10);

        WildernessMusic = RuntimeManager.CreateInstance(wildernessMusicSource);
        TownMusic = RuntimeManager.CreateInstance(townMusicSource);
        TenSecondMusic =RuntimeManager.CreateInstance(tenSecondMusicSource);

        TownMusic.start();

        ammoBag.Add(ItemType.Gold, 99990);
        PickupItem(ItemType.Bow);
        PickupItem(ItemType.BombBag);
        PickupItem(ItemType.Pickaxe);
    }

    void Spawn()
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        // 1. Temporarily disable the CharacterController so it releases control of the transform
        if (controller != null) controller.enabled = false;

        // 2. Set position and rotation
        transform.position = new Vector3(13.89f, 0.5f, -8.35f);
        transform.rotation = Quaternion.Euler(0f, -180f, 0f);
        lookTarget = Quaternion.Euler(0f, -180f, 0f); // Keep if used in your script
        camera.position = transform.position + cameraOffset;

        dayNightCycle.SetDay();

        // 3. Re-enable the CharacterController
        if (controller != null) controller.enabled = true;
        whip.Reset();
    }

    void Update()
    {
        Move();
        PickupItems();
        HandleShops();
        HandleSaloon();
        UseItems();
        UpdateWeapons();
        MapAreaStuff();

        if (dayNightCycle.timeRemainingS < 12f && (dayNightCycle.timeRemainingS + Time.deltaTime) >= 12f && mapArea != MapArea.Town)
        {
            WildernessMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            TownMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            TenSecondMusic.start();
        }
    }

    private void MapAreaStuff() {
        MapArea current = GetCurrentMapArea();
        if (current != mapArea) {
            OnChangeMapArea(mapArea, current);
            mapArea = current;
        }
        DoMapAreaStuff(mapArea);
    }

    private MapArea GetCurrentMapArea() {
        if (transform.position.z < 0)
        {
            return MapArea.Town;
        }
        if (transform.position.z < 34)
        {
            return MapArea.Wilderness;
        }
        if (transform.position.z < 68)
        {
            return MapArea.Wilderness2;
        }
        return MapArea.Boss;
    }

    private void DoMapAreaStuff(MapArea area) {
        if (area != MapArea.Town && dayNightCycle.isNight)
        {
            if (Random.value < ghostSpawnsPerSecond * Time.deltaTime)
            {
                SpawnGhost();
            }
        }
    }

    private void OnChangeMapArea(MapArea prevArea, MapArea newArea) {
        Debug.Log($"Entered {newArea}");
        switch (prevArea) {
            case MapArea.Town:
                if (!dayNightCycle.isNight) dayNightCycle.UnpauseTime();
                TownMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                WildernessMusic.start();
                break;
            case MapArea.Boss:
                Object.FindAnyObjectByType<HealthBar>()?.Hide();
                break;
        }
        switch (newArea)
        {
            case MapArea.Town:
                TownMusic.start();
                StartCoroutine(treasureBag.EmptyInto(ammoBag, 10f));
                WildernessMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                TenSecondMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                if (win)
                {
                    SceneManager.LoadScene("WIN");
                }
                break;
            case MapArea.Wilderness:
                WildernessMusic.setParameterByNameWithLabel("Variation", "Desert");
                break;
            case MapArea.Wilderness2:
                WildernessMusic.setParameterByNameWithLabel("Variation", "Frog Oasis");
                break;
            case MapArea.Boss:
                WildernessMusic.setParameterByNameWithLabel("Variation", "Lava Mountains");
                Object.FindAnyObjectByType<HealthBar>()?.ShowIfEverShown();
                break;
        }
    }

    private void UseItems()
    {
        if (Keyboard.current.xKey.wasPressedThisFrame)
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
            if (Keyboard.current.upArrowKey.isPressed) moveForwardAmount += 1;
            if (Keyboard.current.downArrowKey.isPressed) moveForwardAmount -= 1;
            if (Keyboard.current.rightArrowKey.isPressed) moveRightAmount += 1;
            if (Keyboard.current.leftArrowKey.isPressed) moveRightAmount -= 1;
        }
        Vector3 move = ((Vector3.forward * moveForwardAmount) + (Vector3.right * moveRightAmount)).normalized * moveSpeed;


        // Jump
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Gravity
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);

        // 4. Move the Controller
        controller.Move((velocity + move + knockbackVelocity) * Time.deltaTime);

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
            if (item.Age > 0.2f && (transform.position - item.transform.position).sqrMagnitude < itemPickupRadiusSq)
            {
                if (CanPickupItem(item.type, item.amount))
                {
                    Debug.Log(item.Age);
                    PickupItem(item.type, item.amount);
                    Destroy(item.gameObject);
                }
            }
        }
    }

    private void HandleSaloon() {
        float sqDistCutoff = 3f * 3f;
        foreach (Saloon saloon in FindObjectsByType<Saloon>())
        {
            float sqDist = (saloon.transform.position - transform.position).sqrMagnitude;

            if (sqDist <= sqDistCutoff)
            {
                if (dayNightCycle.isNight)
                {
                    saloon.message = "go to sleep?";
                    if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame) dayNightCycle.SetDay();
                }
                else
                {
                    saloon.message = "Come back at the end of the day.";
                }
                saloon.Show();
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
            if ((Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame) && closest.cost <= ammoBag.Amount(ItemType.Gold) && CanPickupItem(closest.itemType)) {
                ammoBag.Remove(ItemType.Gold, closest.cost);
                PickupItem(closest.itemType);
                RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_Purchase");
                switch (closest.itemType) {
                    case ItemType.Seed:
                        numSeeds += 1;
                        closest.cost += 10 * numSeeds;
                        if (!CanPickupItem(closest.itemType)) {
                            closest.SetSoldOut();
                        }
                        break;

                    case ItemType.Rooster:
                    case ItemType.QuiverUpgrade:
                    case ItemType.BombBagUpgrade:
                    case ItemType.WhipUpgrade:
                        closest.cost *= 3;
                        if (!CanPickupItem(closest.itemType)) {
                            closest.SetSoldOut();
                        }
                        break;
                }
            }
        }
    }

    private bool CanPickupItem(ItemType type, int amount = 1)
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

    private void PickupItem(ItemType type, int amount = 1)
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
                if (CanPickupItem(ItemType.Arrow)) {
                    PickupItem(ItemType.Arrow);
                }
                arrowShop.SetActive(true);
                quiverShop.SetActive(true);
                break;

            case ItemType.BombBag:
                bombBagSize += 1;
                itemsBag.Add(type);
                if (CanPickupItem(ItemType.Dynamite)) {
                    PickupItem(ItemType.Dynamite);
                }
                bombShop.SetActive(true);
                bombBagShop.SetActive(true);
                break;

            case ItemType.Gold:
                if (mapArea == MapArea.Town)
                {
                    ammoBag.Add(type, amount);
                }
                else
                {
                    treasureBag.Add(type, amount);
                }
                break;

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
                whip.damage += 10;
                break;

            case ItemType.Rooster:
                SpawnRooster();
                break;
        }
    }

    private void OnItemPickedup(ItemType type)
    {
    
        switch (type)
        {
            case ItemType.Gold:
                RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_PickupCoin");
                break;

            case ItemType.Whisky:
                RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_SlurpCoffee");
                TenSecondMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                break;

            case ItemType.Whip:
            case ItemType.Bow:
            case ItemType.BombBag:
            case ItemType.Pickaxe:
                RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_PickupMajorItem");
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
        dayNightCycle.dayStartAngle -= 5f;
        dayNightCycle.IncreaseDayLength(30f);
        Instantiate(roosterPrefab, roosterSpawnPoint.position, Quaternion.identity);
    }

    private void Use(ItemType type) {
        switch (type)
        {
            case ItemType.Whip:
                whip.Attack();
                RuntimeManager.PlayOneShot("event:/SFX/Weapons/SFX_WhipCrack");
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

    public void GetHit(Vector3 impulse) {
        RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_PlayerGetsHit");
        if (Time.time < lastHitTime + hitCooldown) return;
        lastHitTime = Time.time;
        knockbackVelocity += impulse;
        if (dayNightCycle.isNight)
        {
            if (treasureBag.Amount(ItemType.Gold) > 0)
            {
                DropGold(10);
            }
            else
            {
                Spawn();
            }
        }
        else
        {
            dayNightCycle.timeRemainingS -= 5f;
        }
    }

    private void DropGold(int amount) {
        int totalGold = treasureBag.Amount(ItemType.Gold);
        if (amount > totalGold) {
            amount = totalGold;
        }
        treasureBag.Remove(ItemType.Gold, amount);

        if (FindObjectsByType<RandomFlingOnSpawn>().Length < 25)
        {
            if (amount > 25) amount = 25;
            for (int i = 0; i != amount; ++i)
            {
                Instantiate(droppedGoldPrefab, transform.position, Random.rotation);
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Enemy enemy = hit.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            Debug.Log("HIT ENEMY");
            Vector3 knockbackDir = hit.normal;
            knockbackDir.y *= 0.01f;
            knockbackDir.Normalize();
            GetHit(knockbackDir * enemy.contactForce);
        }
    }
}