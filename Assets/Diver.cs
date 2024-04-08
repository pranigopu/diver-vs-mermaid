// PLAYABLE SPRITE (DIVER)

/*
REFERENCES AND ACKNOWLEDGEMENTS:

Sprite programming basics:
https://youtu.be/pYu36PLmdq0?si=EVTe3E5qznEFdpWh
*/

/*
Objective summary:

- Avoid the mermaid as you collect artefacts
- If you collect all the artefacts, you win
- You cannot attack the mermaid not regain lost health
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using UnityEngine.Tilemaps;

public class Diver : MonoBehaviour
{
    // SPRITE/AGENT MOVEMENT-RELATED VARIABLES
    float horizontalInput;
    float verticalInput;
    Vector2Int position; // Position of the agent as per the grid in `LevelGenerator`
    /*
    NOTE ON LEVEL GENERATOR GRID VS. ACTUAL GRID:
    Position of agent in level generator grid is based on some scaling of the actual tilemap grid.
    In particular, we adjust for the cell size used in the `Grid` game object and thus get the level generator grid position.
    */
    [SerializeField] float movementSpeed = 20f;
    // To manipulate the physical aspects of the sprite:
    [HideInInspector] Rigidbody2D rb;

    //------------------------------------
    // AGENT-RELATED VARIABLES

    // Variable to keep track of the artefacts collected:
    public int artefactsInHand = 0;
    // Variable for storing maximum health:
    [HideInInspector] public int maxHealth;
    // Variable to keep track of health points:
    [SerializeField] int health = 6;
    // Constants to make the game's status easier to read:
    public const int WIN = 1;
    public const int LOSE = 0;
    public const int ONGOING = -1;
    // Variable to store the game's status:
    [HideInInspector] public int gameStatus = ONGOING;

    //------------------------------------
    // LEVEL-RELATED VARIABLES

    // Game object to access the level grid information:
    public LevelGenerator levelGenerator; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the actual grid of the rendered map:
    public Grid renderedGrid; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the tilemap using which the map is rendered:
    public Tilemap tilemap;

    //------------------------------------
    // BEHAVIOUR TREE-RELATED VARIABLES

    // The mermaid's behaviour tree:
    Root tree;
    // The mermaid's behaviour blackboard:
    Blackboard blackboard;

    //------------------------------------
    // VARIABLES FOR TIMING THE GAME (FOR PERFORMANCE MEASUREMENT)

    float t_start;

    //================================================
    // MAIN FUNCTIONS

    //------------------------------------
    // Start is called before the first frame update:
    void Start()
    {
        // Initialising maximum health as current health:
        maxHealth = health;

        // Initialising the diver's rigid body component:
        rb = GetComponent<Rigidbody2D>();

        //________________________
        // Setting the diver sprite's color to a fixed color (black)
        GetComponent<SpriteRenderer>().color = Color.black;

        //________________________
        // Placing agent in open water after level generation is completed:
        StartCoroutine(PlaceAgentInOpenWaterAfterLevelGeneration());

        //________________________
        // Initialising and starting the behaviour tree (with the blackboard):
        tree = CreateBehaviourTree();
        blackboard = tree.Blackboard;
        blackboard["artefactsInHand"] = 0;
        t_start = Time.time; // Starting the timer!
        tree.Start();
    }

    //================================================
    // HELPER FUNCTIONS

    //------------------------------------
    // Function to reset the game for the diver:
    void ResetGame()
    {
        // Resetting statistics:
        health = maxHealth;
        artefactsInHand = 0;

        // Restoring previous settings (before victory or defeat):
        GetComponent<SpriteRenderer>().color = Color.black;
        GetComponent<PolygonCollider2D>().enabled = true;
        movementSpeed = 30f;

        // Placing agent in open water after level generation is completed:
        StartCoroutine(PlaceAgentInOpenWaterAfterLevelGeneration());

        // Setting the game status to ongoing:
        gameStatus = ONGOING;
    }

    //------------------------------------
    // Function to bring game-over mode:
    void GameOver(int k)
    {
        gameStatus = k;

        if(gameStatus == LOSE)
        {
            Debug.Log("Game Over; YOU DIED! Enjoy being a ghost! | Press 'Return' to replay...");
            
            // EXTRA: Ghostly effect:
            GetComponent<SpriteRenderer>().color = Color.white;
            GetComponent<PolygonCollider2D>().enabled = false; // Disabling collider for "ghostly" effect
            movementSpeed = 10f;
        }
        else if(gameStatus == WIN)
        {
            Debug.Log("Game Over; YOU WON! Enjoy being a mermaid!\n Your time: " + (Time.time - t_start).ToString() + " seconds | Press 'Return' to replay...");
            
            // EXTRA: Become a mermaid:
            GetComponent<SpriteRenderer>().color = new Color(1, 0.5f, 0, 1);
            movementSpeed = 30f;
        }
    }

    //------------------------------------    
    // Function to contain the agent's position within certain bounds:
    void ContainAgentWithinMap()
    {
        float max_x = LevelGenerator.width * renderedGrid.cellSize.x;
        float max_y = LevelGenerator.height * renderedGrid.cellSize.y;
        /*
        NOTE ON ACCESSING STATIC VARIABLES FROM A CLASS:
        You can't call a static method from an instance of a class; instead, use the class name itself.
        Hence, instead of doing `levelGenerator.width` (`levelGenerator` being an instance of `LevelGenerator`) we did `LevelGenerator.width`.
        */

        if(rb.position.x <= 0)
            rb.position = new Vector2(0, rb.position.y);
        else if(rb.position.x >=  max_x - 1)
            rb.position = new Vector2(max_x - 1, rb.position.y);
        if(rb.position.y <= 0)
            rb.position = new Vector2(rb.position.x, 0);
        else if(rb.position.y >= max_y)
            rb.position = new Vector2(rb.position.x, max_y - 1);
    }

    //------------------------------------
    // Function to place agent in open water:
    void PlaceAgentInOpenWater()
    {
        // Boolean to indicate if ideal position has been found:
        bool foundPosition = false;

        // Backup coordinates in case the most desired condition is not met:
        Vector2 backup = Vector2.zero;

        for(int x = 0; x < LevelGenerator.width; x++)
        {
            for(int y = 0; y < LevelGenerator.height; y++)
            {   
                // If current tile is water tile...
                if(levelGenerator.grid[x, y] == 0)
                {
                    // Updating the current tile's neighbourhood information:
                    levelGenerator.UpdateNeighbourhoodData(x, y);

                    //________________________
                    // Making sure the surrounding tiles have ample water...
                    
                    // Most desired condition:
                    if(levelGenerator.total_5_by_5[0] >= 24)
                    {
                        // Setting the agent's position accordingly:
                        rb.position = new Vector2(x * renderedGrid.cellSize.x, y * renderedGrid.cellSize.y);
                        foundPosition = true;
                        break;
                    }
                    // Second-most desired condition:
                    else if(levelGenerator.total_3_by_3[0] >= 8)
                        backup = new Vector2(x * renderedGrid.cellSize.x, y * renderedGrid.cellSize.y);
                }
            }
            if(foundPosition)
                break;
        }

        // Applying backup position:
        rb.position = backup;
    }

    // Function to wait until level generation is complete before placing the agent:
    IEnumerator PlaceAgentInOpenWaterAfterLevelGeneration()
    {
        yield return new WaitUntil(() => levelGenerator.generationComplete);
        PlaceAgentInOpenWater();
    }

    //------------------------------------
    // Function to record damage taken or (if appliable) death:
    public void TakeDamage(int damage)
    {
        health -= damage;
        if(gameStatus == ONGOING && health <= 0)
            GameOver(LOSE);
        else if(gameStatus == ONGOING)
            Debug.Log("Health = " + health.ToString());
    }

    //================================================
    // AGENT BEHAVIOUR FUNCTIONS

    //------------------------------------
    // BEHAVIOUR 1: Handling artefacts (picking or placing)

    // Variable to keep track of time between spacebar presses:
    private float t_spacebar = 0f;
    // Function to pick up or place artefact(s):
    void HandleArtefacts()
    {
        // We ensure a 0.5 seconds cooldown between artefact handling calls...
        if(Time.time - t_spacebar <= 0.5)
            return;

        // Search 3 x 3 neighbourhood for artefact:
        bool foundArtefact = false;
        int i = 0;
        int j = 0;

        // Try to pick up artefact(s):
        for(i = position.x - 1; i <= position.x + 1; i++)
        {
            for(j = position.y - 1; j <= position.y + 1; j++)
            {
                // Continue to next iteration if the following condition is met:
                if(i < 0 || i >= LevelGenerator.width || j < 0 || j >= LevelGenerator.height)
                    continue;

                // Checking if artefact is present in (i, j):
                if(levelGenerator.grid[i, j] == -1)
                {
                    // Replace artefact tile with water tile:
                    tilemap.SetTile(new Vector3Int(i, j, 0), levelGenerator.GetTile(levelGenerator.waterTexture));
                    levelGenerator.grid[i, j] = 0;
                    artefactsInHand++;
                    foundArtefact = true;
                    break;
                }
            }
        }

        // Try to place an artefact if none were found during the loop above:
        if(foundArtefact == false && artefactsInHand > 0)
        {
            tilemap.SetTile(new Vector3Int(position.x, position.y, 0), levelGenerator.GetTile(levelGenerator.artefactTexture));
            levelGenerator.grid[position.x, position.y] = -1;
            artefactsInHand--;
        }

        // Resetting the timer for the spacebar cooldown:
        t_spacebar = Time.time;
    }

    Node HandleArtefactsBehaviour()
    {
        return new Action(() => HandleArtefacts());
    }

    //------------------------------------
    // Updating the motion of the sprite:
    private void Move()
    {
        // Updating agent velocity (with artefact-in-hand-based speed penalty):
        int k = levelGenerator.grid[position.x, position.y];
        if(gameStatus == ONGOING)
        {
            switch(k)
            {
                case -1: case 0: // NOTE: Case -1 is when an artefact is present
                    movementSpeed = 30f - artefactsInHand * 3;
                    break;
                case 1:
                    movementSpeed = 20f - artefactsInHand * 2;
                    break;
                case 2:
                    movementSpeed = 10f - artefactsInHand * 1;
                    break;
                case 3:
                    movementSpeed = 1f - artefactsInHand * 0;
                    break;

            }
        }
        rb.velocity = new Vector2(horizontalInput * movementSpeed, verticalInput * movementSpeed);
    }

    Node MoveBehaviour()
    {
        return new Action(() => Move());
    }

    //================================================
    // MAKING THE BEHAVIOUR TREE
    
    // Diver status update function:
    // NOTE: We shall store in the blackboard only what is needed for the behaviour tree conditions
    void UpdateStatus()
    {
        // Reset game if applicable:
        if(Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Alpha2))
            ResetGame();

        //________________________
        // If agent position is out-of-bounds (with respect to the level map), snap it back to within bounds:
        ContainAgentWithinMap();

        // Update inputs for movement:
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        // Updating the agent's position as per the grid in `LevelGenerator`:
        position = new Vector2Int((int) (rb.position.x / renderedGrid.cellSize.x), (int) (rb.position.y / renderedGrid.cellSize.y));

        // Storing other user input:
        blackboard["input"] = Input.inputString;

        //________________________
        // Checking for game-over conditions:
        
        // Checking for winning condition:
        if(gameStatus == ONGOING && artefactsInHand >= levelGenerator.artefactsInTotal && health > 0)
            GameOver(WIN);
        
        // Checking for losing condition (we should not need this, just fool-proofing):
        else if(gameStatus == ONGOING && health <= 0)
            GameOver(LOSE);

        blackboard["gameStatus"] = gameStatus;
    }

    // Behaviour tree:
    Root CreateBehaviourTree()
    {
        return new Root(new Service(
                () => UpdateStatus(),
                new Selector(
                    new BlackboardCondition(
                    "gameStatus", // Defines the key in the blackboard; the condition is w.r.t its value
                    Operator.IS_GREATER, // Defines the conditional operator to be used
                    ONGOING, // Checks for condition w.r.t. this value and the specified blackboard value (checks if game is ongoing)
                    Stops.SELF, // Stops if condition is not met and allows the parent composite node to move to its next node
                    MoveBehaviour()), // If the condition is true, executes this action node (move freely)
                    new Sequence(
                        MoveBehaviour(),
                        new Selector(
                            new BlackboardCondition(
                            "input", // Defines the key in the blackboard; the condition is w.r.t its value
                            Operator.IS_EQUAL, // Defines the conditional operator to be used
                            " ", // Checks for condition w.r.t. this value and the specified blackboard value (checks if ' ' (space) was pressed)
                            Stops.SELF, // Stops if condition is not met and allows the parent composite node to move to its next node
                            HandleArtefactsBehaviour())))))); // If the condition is true, executes this action node (handle artefacts)
    }
}
