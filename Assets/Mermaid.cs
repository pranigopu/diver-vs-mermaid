// NPC SPRITE (MERMAID)

/*
Behaviour summary:

- If player is within sight, turn toward player and approach
- If player is out of sight, move at random such that the whole map is eventually covered
- If player is in a 5 x 5 radius, stop and attack
- Once in sight, the mermaid will pursue the player for at least 2 seconds (no matter if the player hides again)

What does "in sight" mean here?
"In sight" here means there are enough water blocks around the player.
Hence, to hide, the player has to use corals or seaweed.

The mermaid can detect you if either of the following hold:
(1) The 5 x 5 square around you (the player) has at least 12 water blocks and the 3 x 3 radius around you has at least 5 water blocks.
(2) The mermaid is within a 5 x 5 square of you.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using UnityEngine.Tilemaps;

public class Mermaid : MonoBehaviour
{
    // GENERAL PURPOSE VARIABLES

    System.Random prng;

    //------------------------------------
    // SPRITE/AGENT MOVEMENT-RELATED VARIABLES
    float horizontalInput;
    float verticalInput;
    Vector2Int position; // Position of the agent as per the grid in `LevelGenerator`
    /*
    NOTE ON LEVEL GENERATOR GRID VS. ACTUAL GRID:
    Position of agent in level generator grid is based on some scaling of the actual tilemap grid.
    In particular, we adjust for the cell size used in the `Grid` game object and thus get the level generator grid position.
    */
    [SerializeField] float movementSpeed = 10f;
    // To manipulate the physical aspects of the sprite:
    Rigidbody2D rb;

    //------------------------------------
    // AGENT-RELATED VARIABLES

    [SerializeField] int meleeDamage = 3;

    //------------------------------------
    // LEVEL-RELATED VARIABLES

    // Game object to access the level grid information:
    public LevelGenerator levelGenerator; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the actual grid of the rendered map:
    public Grid renderedGrid; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the tilemap using which the map is rendered:
    public Tilemap tilemap;

    //------------------------------------
    // ENEMY-RELATED VARIABLES

    // Game object to access the diver agent's information:
    public Diver diver; // Will be assigned later in the Inspector of Unity Editor
    
    //------------------------------------
    // PROJECTILE-RELATED VARIABLES

    // Point of lauching the projectile:
	[SerializeField] GameObject launchPoint; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the projectile sprite:
	[SerializeField] GameObject projectilePrefab; // Will be assigned later in the Inspector of Unity Editor
    
    //------------------------------------
    // BEHAVIOUR TREE-RELATED VARIABLES

    // The mermaid's behaviour tree:
    Root tree;
    // The mermaid's behaviour blackboard:
    Blackboard blackboard;

    //================================================
    // MAIN FUNCTIONS

    //------------------------------------
    // Start is called before the first frame update...
    void Start()
    {
        // Initialising the PRNG:
        prng = levelGenerator.InitialisePRNG();

        // Initialising the mermaid's rigid body component:
        rb = GetComponent<Rigidbody2D>();

        // Initialising and starting the behaviour tree (with the blackboard):
        tree = CreateBehaviourTree();
        blackboard = tree.Blackboard;
        blackboard["visible"] = false;
        tree.Start();
    }

    //================================================
    // BEHAVIOURS

    //------------------------------------
    // BEHAVIOUR 1: Patrolling (random walk)

    // Helper function for patrolling behaviour:
    void ContainAgentWithinMap()
    {
        float max_x = LevelGenerator.width * renderedGrid.cellSize.x;
        float max_y = LevelGenerator.height * renderedGrid.cellSize.y;
        /*
        NOTE ON ACCESSING STATIC VARIABLES FROM A CLASS:
        You can't call a static method from an instance of a class; instead, use the class name itself.
        Hence, instead of doing `levelGenerator.width` (`levelGenerator` being an instance of `LevelGenerator`) we did `LevelGenerator.width`.
        */

        // If hitting edge or beyond edge, move toward the map's centre:
        if(rb.position.x <= 0 || rb.position.y <= 0 || rb.position.x >=  max_x - 1 || rb.position.y >= max_y)
        {
            Vector2 mapCentre = new Vector2(max_x / 2, max_y / 2);
            rb.velocity = (mapCentre - rb.position).normalized * movementSpeed;
            // NOTE: The `normalized` property gets vector with the same direction but magnitude 1; this helps scale the velocity by the required speed
        }
    }

    // Variable to keep track of time between random direction changes:
    private float t_randomDirectionChange = 0f;
    // Variable to keep track of the maximum waiting time between random direction changes:
    private float max_t_randomDirectionChange = 1f;
    // Patrolling behaviour function:
    void Patrol()
    {
        ContainAgentWithinMap();

        // If last random change happened less than `max_t_randomDirectionChange` seconds ago:
        if(Time.time - t_randomDirectionChange < max_t_randomDirectionChange)
            return;

        // Resetting the time variables:
        t_randomDirectionChange = Time.time;
        max_t_randomDirectionChange = (float) prng.NextDouble() * 3 + 1; // Returns a value between 1 and 3

        float x = (float) (prng.NextDouble() * 2 - 1);
        float y = (float) (prng.NextDouble() * 2 - 1);
        // Each the above two lines returns a value between -1 and 1
        
        Vector2 movementDirection = new Vector2(x, y).normalized;
        // NOTE: The `normalized` property gets vector with the same direction but magnitude 1; this helps scale the velocity by the required speed

        rb.velocity = movementDirection * movementSpeed;
    }

    Node PatrolBehaviour()
    {
        return new Action(() => Patrol());
    }

    //------------------------------------
    // BEHAVIOUR 2: Seeking/chasing the diver

    void Seek()
    {
        Vector2 targetPosition = diver.transform.position;
        targetPosition = new Vector2(targetPosition.x, targetPosition.y);
        rb.velocity = (targetPosition - rb.position).normalized * movementSpeed;
    }

    Node SeekBehaviour()
    {
        return new Action(() => Seek());
    }

    //------------------------------------
    // BEHAVIOUR 3: Melee/close quarters attack

    // Precursor to the melee behaviour:
    // NOTE: I have used a trigger instead of collision since a trigger can detect a wider area without halting movement; this helps better simulate melee combat
    void OnTriggerEnter2D(Collider2D colliderObject)
    {
        Diver diver = colliderObject.GetComponent<Diver>();
        // Cause damage to the diver (if applicable):
        if(diver != null)
        {
            // Resetting the time variable:
            t_melee = Time.time;

            // Causing damage to the diver:
            diver.TakeDamage(meleeDamage);
        }
    }

    // Variable to keep track of time between two melee attacks:
    float t_melee = 0f;
    void Melee()
    {
        rb.velocity = Vector2.zero;

        // If last melee attack happened less than or equal to 1 second ago, do not attack:
        if(Time.time - t_melee <= 2)
            return;

        // Resetting the time variable:
        t_melee = Time.time;

        // Causing damage to the diver:
        diver.TakeDamage(meleeDamage);
    }

    Node MeleeBehaviour()
    {
        return new Action(() => Melee());
    }

    //------------------------------------
    // BEHAVIOUR 4: Shoot/long-range attack

    // Variable to keep track of time between two shots:
    float t_shoot;
    void Shoot()
	{
        // If last shot happened less than or equal to 2 seconds ago, do not shoot:
        if(Time.time - t_shoot <= 2)
            return;

        // Resetting the time variable:
        t_shoot = Time.time;

        // Making the shot:
		Instantiate(projectilePrefab, launchPoint.transform.position, launchPoint.transform.rotation);
	}

    Node ShootBehaviour()
    {
        return new Action(() => Shoot());
    }

    //================================================
    // MAKING THE BEHAVIOUR TREE

    // Variable to keep track of time between two perception updates:
    float t_updatePerception = 0f;
    // Mermaid perception update function:
    void UpdatePerception()
    {
        // If last sighting happened less than or equal to 3 seconds ago, do not update:
        if((bool) blackboard["visible"] == true && Time.time - t_updatePerception <= 3)
            return;
        
        // Resetting the time variables:
        t_updatePerception = Time.time;

        // Updating perception:
        Vector2Int targetPosition = new Vector2Int((int) (diver.transform.position.x / renderedGrid.cellSize.x), (int) (diver.transform.position.y / renderedGrid.cellSize.y));
        Vector2Int sourcePosition = new Vector2Int((int) (rb.position.x / renderedGrid.cellSize.x), (int) (rb.position.y / renderedGrid.cellSize.y));
        levelGenerator.UpdateNeighbourhoodData(targetPosition.x, targetPosition.y);
        blackboard["distanceFromTarget"] = (targetPosition - sourcePosition).magnitude;
        blackboard["visible"] = (levelGenerator.total_5_by_5[0] >= 12 && levelGenerator.total_3_by_3[0] >= 5)|| (Mathf.Abs(targetPosition.x - sourcePosition.x) < 5 && Mathf.Abs(targetPosition.y - sourcePosition.y) < 5);
    }

    // Behaviour tree:
    Root CreateBehaviourTree()
    {
        return new Root(new Service(
                0.1f,
                () => UpdatePerception(),
                new Selector(
                    new BlackboardCondition(
                        "distanceFromTarget", // Defines the key in the blackboard; the condition is w.r.t its value
                        Operator.IS_SMALLER, // Defines the conditional operator to be used
                        3f, // Checks for condition w.r.t. this value and the specified blackboard value (checks if player is in melee distance)
                        Stops.SELF, // Stops if condition is not met and allows the parent composite node to move to its next node
                        MeleeBehaviour()), // If the condition is true, executes this action node (stop moving)
                    new Sequence(
                        new BlackboardCondition(
                            "visible", // Defines the key in the blackboard; the condition is w.r.t its value
                            Operator.IS_EQUAL, // Defines the conditional operator to be used
                            true, // Checks for condition w.r.t. this value and the specified blackboard value (checks if player is visible)
                            Stops.SELF, // Stops if condition is not met and allows the parent composite node to move to its next node
                            SeekBehaviour()), // If the condition is true, executes this action node (seeks diver)
                        ShootBehaviour()),
                    PatrolBehaviour())));
    }
}