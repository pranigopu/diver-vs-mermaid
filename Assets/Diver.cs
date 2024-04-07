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
    [HideInInspector] public Rigidbody2D rb;

    //------------------------------------
    // AGENT-RELATED VARIABLES

    // Variable to keep track of the artefacts collected:
    public int artefactsInHand = 0;
    // Variable to keep track of health points:
    [SerializeField] int health = 10;

    //------------------------------------
    // LEVEL-RELATED VARIABLES

    // Game object to access the level grid information:
    public LevelGenerator levelGenerator; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the actual grid of the rendered map:
    public Grid renderedGrid; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the tilemap using which the map is rendered:
    public Tilemap tilemap;

    //------------------------------------
    // TIME-RELATED VARIABLES

    // Variable to keep track of time between spacebar presses:
    private float t_spacebar = 0f;

    //================================================
    // MAIN FUNCTIONS

    //------------------------------------
    // Start is called before the first frame update:
    void Start()
    {
        // Initialising the diver's rigid body component:
        rb = GetComponent<Rigidbody2D>();

        // Placing agent in open water after level generation is completed:
        StartCoroutine(PlaceAgentInOpenWaterAfterLevelGeneration());
    }

    //------------------------------------
    // Updating inputs and position (this function is called once per frame):
    void Update()
    {
        // Update inputs for movement:
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        // If agent position is out-of-bounds (with respect to the level map), snap it back to within bounds:
        ContainAgentWithinMap();

        // Updating the agent's position as per the grid in `LevelGenerator`:
        position = new Vector2Int((int) (rb.position.x / renderedGrid.cellSize.x), (int) (rb.position.y / renderedGrid.cellSize.y));

        //________________________
        // Pick up artefact if possible, else place artefacat if possible:
        if(Input.GetKey(KeyCode.Space))
        {   
            // We ensure a 0.5 seconds cooldown between artefact handling calls...
            if(Time.time - t_spacebar > 0.5)
            {
                HandleArtefacts();
                t_spacebar = Time.time;
            }
        }
        //________________________
        // Reset game:
        if(Input.GetKey(KeyCode.Return))
            artefactsInHand = 0;

        //________________________
        // In case the map was regenerated in some way...
        if(Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Return))
            StartCoroutine(PlaceAgentInOpenWaterAfterLevelGeneration());
    }

    //------------------------------------
    // Updating the physical aspects of the sprite:
    /*
    NOTE:
    `FixedUpdate` is a method in `MonoBehaviour` with a certain function.
    In this way, it is similar to `Start`, `Update` and `Awake`; these methods have certain broad purposes whose specific processes are implemented by us.
    
    Use `FixedUpdate` when using `Rigidbody`. Set a force to a Rigidbody and it applies each fixed frame.
    `FixedUpdate` occurs at a measured time step that typically does not coincide with `MonoBehaviour.Update`.

    REFERENCE:
    https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html
    */
    private void FixedUpdate()
    {
        // Updating agent velocity (with artefact-in-hand-based speed penalty):
        int k = levelGenerator.grid[position.x, position.y];
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
        rb.velocity = new Vector2(horizontalInput * movementSpeed, verticalInput * movementSpeed);
    }

    //================================================
    // HELPER FUNCTIONS

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

    //================================================
    // AGENT ACTION FUNCTIONS

    //------------------------------------
    // Function to pick up or place artefact(s):
    void HandleArtefacts()
    {
        // Search 3 x 3 neighbourhood for artefact:
        bool foundArtefact = false;
        int i = 0;
        int j = 0;

        // Try to pick up artefact(s):
        for(i = position.x - 1; i <= position.x + 1; i++)
        {
            for(j = position.y - 1; j <= position.y + 1; j++)
            {
                // Continue if the following condition is met:
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
    }

    //------------------------------------
    // Function to take damage or (if appliable) die:
    public void TakeDamage(int damage)
    {
        health -= damage;
        if(health <= 0)
            Debug.Log("You're dead");
    }
}