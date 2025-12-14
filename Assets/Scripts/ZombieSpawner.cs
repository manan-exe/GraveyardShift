using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject zombieAPrefab;
    public GameObject zombieBPrefab;
    //word

    [Header("Spawn Timing")]
    [Tooltip("Seconds between spawns.")]
    public float spawnInterval = 2.0f;

    [Tooltip("Random +/- seconds added to spawnInterval.")]
    public float spawnJitter = 0.4f;

    [Header("Spawn Ratios (weights)")]
    [Tooltip("Relative chance to spawn Zombie A.")]
    public int weightA = 70;

    [Tooltip("Relative chance to spawn Zombie B.")]
    public int weightB = 30;

    [Header("Placement")]
    [Tooltip("How high above the spawner we raycast down to find the ground.")]
    public float raycastHeight = 20f;

    [Tooltip("Extra height added above ground so the zombie 'drops' onto the map.")]
    public float spawnAboveGround = 0.5f;

    [Tooltip("What counts as ground for the drop raycast.")]
    public LayerMask groundMask;

    [Header("Optional Limits")]
    [Tooltip("0 = unlimited.")]
    public int maxAliveFromThisSpawner = 0;

    [Tooltip("How far from the spawner to randomize spawn position.")]
    public float spawnRadius = 0f; // 0 = exact point

    float nextSpawnTime;
    int aliveCount;

    void Start() {
        ScheduleNext();
    }

    void Update() {
        // Only spawn during active gameplay
        if (!GameFlowManager.GameplayActive) return;

        if (maxAliveFromThisSpawner > 0 && aliveCount >= maxAliveFromThisSpawner)
            return;

        if (Time.time >= nextSpawnTime)
        {
            TrySpawn();
            ScheduleNext();
        }
    }

    void ScheduleNext() {
        float jitter = Random.Range(-spawnJitter, spawnJitter);
        nextSpawnTime = Time.time + Mathf.Max(0.05f, spawnInterval + jitter);
    }

    void TrySpawn() {
        GameObject prefab = ChoosePrefab();
        if (prefab == null) return;

        Vector3 basePos = transform.position;

        // randomize inside radius (XZ only)
        if (spawnRadius > 0f)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadius;
            basePos += new Vector3(r.x, 0f, r.y);
        }

        // raycast down to find ground
        Vector3 rayStart = basePos + Vector3.up * raycastHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 spawnPos = hit.point + Vector3.up * spawnAboveGround;
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject z = Instantiate(prefab, spawnPos, rot);

            // Track alive count if you want a per-spawner cap
            aliveCount++;

            // When it gets destroyed, decrement aliveCount safely
            var tracker = z.AddComponent<SpawnedZombieTracker>();
            tracker.Init(this);
        }
        else
        {
            // If no ground found, do nothing (or spawn at basePos)
            // Debug.LogWarning($"Spawner {name}: no ground hit. Check groundMask / raycastHeight.");
        }
    }

    GameObject ChoosePrefab() {
        int total = Mathf.Max(0, weightA) + Mathf.Max(0, weightB);
        if (total <= 0) return zombieAPrefab != null ? zombieAPrefab : zombieBPrefab;

        int roll = Random.Range(0, total);
        if (roll < weightA) return zombieAPrefab;
        return zombieBPrefab;
    }

    // Called by tracker when zombie is destroyed
    public void NotifySpawnedZombieDestroyed() {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    // Helper component added at runtime
    private class SpawnedZombieTracker : MonoBehaviour
    {
        ZombieSpawner spawner;

        public void Init(ZombieSpawner s) => spawner = s;

        void OnDestroy() {
            if (spawner != null)
                spawner.NotifySpawnedZombieDestroyed();
        }
    }

    // Nice: show radius in editor
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        if (spawnRadius > 0f)
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        else
            Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
