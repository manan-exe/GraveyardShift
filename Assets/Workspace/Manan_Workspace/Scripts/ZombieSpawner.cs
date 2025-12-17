using UnityEngine;


//zombie spawn system
//spawns zombie prefabs 
//attached to an invisible game object to place around the map
public class ZombieSpawner : MonoBehaviour
{
    //fields to wire in the zombie prefabs
    [Header("Prefabs")]
    public GameObject zombieAPrefab;
    public GameObject zombieBPrefab;

    //set how fast zombies spawn one after another
    [Header("Spawn Timing")]
    [Tooltip("Seconds between spawns.")]
    public float spawnInterval = 2.0f;

    //i think this randomizes how often a zombie spawns slightly
    //why did we include this guys?
    [Tooltip("Random +/- seconds added to spawnInterval.")]
    public float spawnJitter = 0.4f;

    //controls the ratio of zombieA versus zombieB spawns
    //we want zombieB to spawn less often because it is harder to hit and faster
    [Header("Spawn Ratios (weights)")]
    [Tooltip("Relative chance to spawn Zombie A.")]
    public int weightA = 70;
    [Tooltip("Relative chance to spawn Zombie B.")]
    public int weightB = 30;

    //use raycast to locate ground so we spawn zombie in right y position
    [Header("Placement")]
    [Tooltip("How high above the spawner we raycast down to find the ground.")]
    public float raycastHeight = 20f;

    //makes sure zombie does not spawn inside the ground because of bad spawner object placement
    [Tooltip("Extra height added above ground so the zombie 'drops' onto the map.")]
    public float spawnAboveGround = 0.5f;

    //raycast searches for specifically objects tagged ground
    [Tooltip("What counts as ground for the drop raycast.")]
    public LayerMask groundMask;

    //maximum zombies from a spawner setting
    //we adjusted spawn rate so we do not need to mess with this
    [Header("Optional Limits")]
    [Tooltip("0 = unlimited.")]
    public int maxAliveFromThisSpawner = 0;

    //randomize within a radius where zombie spawns
    //this is so all zombies don't just spawn inside eachother
    //not sure how that would mess with colliders
    //but also so spawns don't look obvious
    //but we ended up just putting the spawners behind the pyramids so the player would not really notice
    [Tooltip("How far from the spawner to randomize spawn position.")]
    public float spawnRadius = 0f; // 0 = exact point

    //tracks time until next spawn
    float nextSpawnTime;
    //tracks zombies tied to specific spawner
    int aliveCount;

    void Start() {
        //at start of game set time until next spawn
        ScheduleNext();
    }


    void Update() {
        //only spawn while game is running not during pause or anything
        if (!GameFlowManager.GameplayActive) return;

        //do not spawn if over limit of zombies alive at once
        if (maxAliveFromThisSpawner > 0 && aliveCount >= maxAliveFromThisSpawner)
            return;

        //update time until next spawn
        if (Time.time >= nextSpawnTime)
        {
            TrySpawn();
            ScheduleNext();
        }
    }

    //calculate time until next spawn
    //add a slight random offset to spawn time
    //don't really need this
    void ScheduleNext() {
        //set gaurds so that the random spawan time offset isn't insanely high or low
        float jitter = Random.Range(-spawnJitter, spawnJitter);
        nextSpawnTime = Time.time + Mathf.Max(0.05f, spawnInterval + jitter);
    }

    //runs checks for conditions before spawning a prefab
    void TrySpawn() {
        //error gaurd in case prefab not assigned
        GameObject prefab = ChoosePrefab();
        if (prefab == null) return;

        //get the position of spawner
        Vector3 basePos = transform.position;

        //randomly spawn zombie inside a certain radius. don't want zombies to spawn way out of the map or something
        if (spawnRadius > 0f)
        {
            //use random unit circle volume and scale it up
            Vector2 r = Random.insideUnitCircle * spawnRadius;
            //update x and z. NOT Y
            basePos += new Vector3(r.x, 0f, r.y);
        }

        //raycast that helpss to find ground via groundMask
        Vector3 rayStart = basePos + Vector3.up * raycastHeight;

        //raycast straight down
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            //spawn point set to slightly above position that ground mask was found
            Vector3 spawnPos = hit.point + Vector3.up * spawnAboveGround;
            //randomize direction zombie is facing
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            //create zombie using prefab
            GameObject z = Instantiate(prefab, spawnPos, rot);

            //track how many zombies alive
            aliveCount++;

            //update alive count if zombie dies
            var tracker = z.AddComponent<SpawnedZombieTracker>();
            tracker.Init(this);
        }
        else
        {
            //do nothing if spawner can't find ground
            //error gaurd
        }
    }

    //does spawning based on ratios set
    GameObject ChoosePrefab() {
        //calculate total spawn weights
        int total = Mathf.Max(0, weightA) + Mathf.Max(0, weightB);
        //fallback in case a zombie prefab is not assigned
        if (total <= 0) return zombieAPrefab != null ? zombieAPrefab : zombieBPrefab;

        //random gen
        int roll = Random.Range(0, total);
        //if value lower then threshold spawn zombieA
        if (roll < weightA) return zombieAPrefab;
        //if above spawn zombieB
        return zombieBPrefab;
    }

    //track when zombie destroyed
    public void NotifySpawnedZombieDestroyed() {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

   //helps track when zombies are not alive anymore
    private class SpawnedZombieTracker : MonoBehaviour
    {
        //tracks which spawner spanwed the zombie.
        //have multiple spawners so this would be necessary to prevent things breaking later.
        ZombieSpawner spawner;

        //reference to specific spawner
        public void Init(ZombieSpawner s) => spawner = s;

        //if a zombie spanwer exists let it know that the specific zombie is destroyed
        void OnDestroy() {
            if (spawner != null)
                spawner.NotifySpawnedZombieDestroyed();
        }
    }

  //show spawn radius using gizmo tool so you can see it in editor but not during gameplay
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        if (spawnRadius > 0f)
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        else
            Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
