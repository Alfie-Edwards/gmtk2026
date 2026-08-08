using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;

using System.Collections.Generic;
using System.Linq;

public enum MapArea {
    Town,
    Wilderness,
    Wilderness2,
    Boss,
    Escape,
    None,
}

[RequireComponent(typeof(CharacterController), typeof(DamageFlash))]
public class Player : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference useAction;

    [Header("Keyboard")]
    [SerializeField] public float moveSpeed = 5.0f;
    [SerializeField] public float moveAcceleration = 10f;
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
    [SerializeField] public float cameraFollowSpeed = 3f;
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
    private int startBombBagSize = 0;
    private int startWhipLevel = 1;
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
    private int lastCheckedSecond = -1;
    public bool disableControls = false;

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
    public Vector3 velocity { get; private set; }
    private Quaternion lookTarget;
    private Bag itemsBag;
    private Bag ammoBag;
    private Bag treasureBag;
    private Vector3 knockbackVelocity;
    private Vector3 move;
    public Vector3 cameraOffset {get; set; }
    private bool actionUsed;
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
        hotbar.SelectedChanged += OnItemChanged;
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

        // init controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        velocity = Vector3.zero;
        actionUsed = false;

        ammoBag.Add(ItemType.Gold, 10);

        WildernessMusic = RuntimeManager.CreateInstance(wildernessMusicSource);
        TownMusic = RuntimeManager.CreateInstance(townMusicSource);
        TenSecondMusic =RuntimeManager.CreateInstance(tenSecondMusicSource);

        mapArea = MapArea.Town;
        TownMusic.start();

        Spawn();
        treasureBag.Add(ItemType.Gold, 400);
        // ammoBag.Add(ItemType.Gold, 99990);
        // PickupItem(ItemType.Bow);
        // PickupItem(ItemType.BombBag);
        // PickupItem(ItemType.Pickaxe);
    }

    void Spawn()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        transform.position = new Vector3(13.89f, 0.5f, -8.35f);
        transform.rotation = Quaternion.Euler(0f, -180f, 0f);
        lookTarget = Quaternion.Euler(0f, -180f, 0f);

        treasureBag.Empty();
        dayNightCycle.SetDay();

        // 3. Re-enable the CharacterController
        if (controller != null) controller.enabled = true;
        whip.Reset();
        move = Vector3.zero;
    }

    void Update()
    {
        if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            Spawn();
        }
        if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Overworld");
        }
        actionUsed = false;
        Move();
        PickupItems();
        HandleSaloon();
        HandleShops();
        UseItems();
        MapAreaStuff();
        SyncMusic();
    }

    private void SyncMusic()
    {
        if (TenSecondMusic.isValid() && mapArea != MapArea.Town)
        {
            int secondsRemainingDay = Mathf.CeilToInt(dayNightCycle.timeRemainingS);
            int targetTimeSongMs = Mathf.Clamp(Mathf.FloorToInt((12f - dayNightCycle.timeRemainingS) * 1000), 0, 12000);
            TenSecondMusic.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);

            switch (state)
            {
                case FMOD.Studio.PLAYBACK_STATE.PLAYING:
                    TenSecondMusic.getTimelinePosition(out int currentPositionMs);
                    int currentSecondSong = currentPositionMs / 1000;
                    int currentSecondDay = 12 - secondsRemainingDay;
                    if ((lastCheckedSecond != -1) && (currentSecondSong < 12) && (currentSecondSong < currentSecondDay) && (currentSecondSong != lastCheckedSecond))
                    {
                        TenSecondMusic.setTimelinePosition(targetTimeSongMs);
                    }
                    lastCheckedSecond = currentSecondSong;
                    break;

                case FMOD.Studio.PLAYBACK_STATE.STOPPED:
                case FMOD.Studio.PLAYBACK_STATE.STOPPING:
                    if (secondsRemainingDay <= 12 && mapArea != MapArea.Escape)
                    {
                        WildernessMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                        TownMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                        TenSecondMusic.start();
                        TenSecondMusic.setTimelinePosition(targetTimeSongMs);
                    }
                    break;
            }
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
            if (transform.position.x < -26)
            {
                return MapArea.Escape;
            }
            return MapArea.Town;
        }
        else if (transform.position.z < 34)
        {
            if (transform.position.x < -42 || (transform.position.z < 10 && transform.position.x < -34))
            {
                return MapArea.Escape;
            }
            return MapArea.Wilderness;
        }
        else if (transform.position.z < 68)
        {
            if (transform.position.x < -52)
            {
                return MapArea.Escape;
            }
            return MapArea.Wilderness2;
        }
        else if (transform.position.x < -40 || transform.position.z > 83)
        {
            return MapArea.Escape;
        }
        else
        {
            return MapArea.Boss;
        }
    }

    private void DoMapAreaStuff(MapArea area) {
        if (area != MapArea.Town && area != MapArea.Escape && dayNightCycle.isNight)
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
                if (newArea != MapArea.Escape)
                {
                    if (!dayNightCycle.isNight) dayNightCycle.UnpauseTime();
                    TownMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    if (dayNightCycle.timeRemainingS <= 12)
                    {
                        TenSecondMusic.start();
                    }
                    else
                    {
                        WildernessMusic.start();   
                    }
                }
                break;
            case MapArea.Escape:
                if (newArea == MapArea.Town)
                {
                    dayNightCycle.SetDay();
                }
                break;
        }
        switch (newArea)
        {
            case MapArea.Town:
                TownMusic.start();
                StartCoroutine(treasureBag.EmptyInto(ammoBag, 40f));
                WildernessMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                TenSecondMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                break;
            case MapArea.Wilderness:
                WildernessMusic.setParameterByNameWithLabel("Variation", "Desert");
                break;
            case MapArea.Wilderness2:
                WildernessMusic.setParameterByNameWithLabel("Variation", "Frog Oasis");
                break;
            case MapArea.Boss:
                WildernessMusic.setParameterByNameWithLabel("Variation", "Lava Mountains");
                FindAnyObjectByType<HealthBar>()?.ShowIfEverShown();
                break;
            case MapArea.Escape:
                win = true;
                dayNightCycle.SetNight();
                FindAnyObjectByType<HealthBar>()?.Hide();
                WildernessMusic.setParameterByNameWithLabel("Variation", "Lava Mountains");
                TenSecondMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                WildernessMusic.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);
                if (state != FMOD.Studio.PLAYBACK_STATE.PLAYING)
                {
                    WildernessMusic.start();
                }
                break;
        }
    }

    private void UseItems()
    {
        if (!actionUsed && useAction.action.WasPressedThisFrame())
        {
            actionUsed = true;
            Use(hotbar.Selected);
        }
    }

    private void Move()
    {
        Vector3 delta = Vector3.zero;
        Vector3 moveTarget = Vector3.zero;
        if (!disableControls)
        {
            Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
            moveTarget = ((Vector3.forward * moveInput.y) + (Vector3.right * moveInput.x)) * moveSpeed;
            move = Vector3.MoveTowards(move, moveTarget, moveAcceleration * Time.deltaTime);

            if (jumpAction.action.WasPressedThisFrame() && controller.isGrounded)
            {
                velocity = Vector3.up * Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }

            if (!controller.isGrounded)
            {
                velocity += Vector3.up * gravity * Time.deltaTime;
            }

            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);

            Vector3 prevPos = transform.position;
            controller.Move((velocity + move + knockbackVelocity) * Time.deltaTime);
            delta = transform.position - prevPos;
            float xMove = delta.x / Time.deltaTime;
            float zMove = delta.z / Time.deltaTime;
            if (move.x > 0 && xMove < move.x) move.x = xMove < 0 ? 0 : xMove;
            if (move.x < 0 && xMove > move.x) move.x = xMove > 0 ? 0 : xMove;
            if (move.z > 0 && zMove < move.z) move.z = zMove < 0 ? 0 : zMove;
            if (move.z < 0 && zMove > move.z) move.z = zMove > 0 ? 0 : zMove;
            if (move != Vector3.zero)
            {
                lookTarget = Quaternion.LookRotation(move);
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, lookTarget, Mathf.Min(16f * Time.deltaTime, 1f));
        }
        // Camera targets ahead of the player.
        Vector3 cameraTarget = transform.position + cameraOffset + 0.25f * moveTarget;
        camera.transform.position = Vector3.Lerp(camera.transform.position + delta, cameraTarget, cameraFollowSpeed * Time.deltaTime);
    }

    private void OnItemChanged(ItemType itemType)
    {
        whip.gameObject.SetActive(hotbar.Selected == ItemType.Whip);
        bow.gameObject.SetActive(hotbar.Selected == ItemType.Bow);
        bombBag.gameObject.SetActive(hotbar.Selected == ItemType.BombBag);
        pickaxe.gameObject.SetActive(hotbar.Selected == ItemType.Pickaxe);
        switch(itemType)
        {
            case ItemType.Whip:
                whip.Reset();
                break;
            case ItemType.Pickaxe:
                pickaxe.Reset();
                break;
        }
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
                    PickupItem(item.type, item.amount);
                    Destroy(item.gameObject);
                }
            }
        }
    }

    private void HandleSaloon() {
        float sqDistCutoff = 2f * 2f;
        foreach (Saloon saloon in FindObjectsByType<Saloon>())
        {
            float sqDist = (saloon.transform.position - transform.position).sqrMagnitude;

            if (sqDist <= sqDistCutoff)
            {
                if (dayNightCycle.isNight)
                {
                    saloon.message = "go to sleep?";
                    if (!actionUsed && useAction.action.WasPressedThisFrame())
                    {
                        actionUsed = true;
                        dayNightCycle.SetDay();
                    }
                }
                else
                {
                    saloon.message = "Come back at\n~ sundown ~";
                }
                saloon.Show();
            }
        }
    }

    private void HandleShops() {
        ShopCrate closest = null;
        float closestSqDist = float.MaxValue;
        float sqDistCutoff = 1.5f * 1.5f;

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
            if (!actionUsed && useAction.action.WasPressedThisFrame())
            {
                if (closest.remainingPurchases != 0)
                {
                    actionUsed = true;
                }
                if (closest.cost <= ammoBag.Amount(ItemType.Gold) && CanPickupItem(closest.itemType))
                {
                    ammoBag.Remove(ItemType.Gold, closest.cost);
                    PickupItem(closest.itemType);
                    RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_Purchase");
                    switch (closest.itemType) {
                        case ItemType.Seed:
                            numSeeds += 1;
                            closest.cost += 10 * numSeeds;
                            break;

                        case ItemType.Rooster:
                        case ItemType.QuiverUpgrade:
                        case ItemType.BombBagUpgrade:
                        case ItemType.WhipUpgrade:
                            closest.cost *= 3;
                            break;
                    }
                    closest.Buy();
                }
            }
        }
    }

    private bool CanPickupItem(ItemType type, int amount = 1)
    {
        switch (type)
        {
            case ItemType.Rooster:
            case ItemType.WhipUpgrade:
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
                return itemsBag.Has(ItemType.Bow);

            case ItemType.BombBagUpgrade:
                return itemsBag.Has(ItemType.BombBag);

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
                if (!dayNightCycle.isNight)
                {
                    TenSecondMusic.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    dayNightCycle.timeRemainingS += 30;
                    WildernessMusic.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);
                    if (state != FMOD.Studio.PLAYBACK_STATE.PLAYING)
                    {
                        WildernessMusic.start();
                    }
                }
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
            randomDirection = -transform.forward;
        }

        // Calculate spawn position at the fixed distance away
        Vector3 spawnPosition = transform.position - (7f * transform.forward) + (randomDirection * 15f);

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
                whip.Attack(move == Vector3.zero ? transform.forward : move);
                break;
            case ItemType.Bow:
                if (ammoBag.Amount(ItemType.Arrow) > 0)
                {
                    
                    ammoBag.Remove(ItemType.Arrow);
                    bow.Fire(move == Vector3.zero ? transform.forward : move);
                }
                break;
            case ItemType.BombBag:
                if (ammoBag.Amount(ItemType.Dynamite) > 0)
                {
                    ammoBag.Remove(ItemType.Dynamite);
                    bombBag.ThrowBomb(move == Vector3.zero ? transform.forward : move);
                }
                break;
            case ItemType.Pickaxe:
                pickaxe.Swing();
                break;
        }
    }

    public void GetHit(Vector3 impulse) {
        RuntimeManager.PlayOneShot("event:/SFX/Player/SFX_PlayerGetsHit");
        if (mapArea == MapArea.Escape || Time.time < lastHitTime + hitCooldown) return;
        lastHitTime = Time.time;
        knockbackVelocity += impulse;
        GetComponent<DamageFlash>()?.TriggerFlash();
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