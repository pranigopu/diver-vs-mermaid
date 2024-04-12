// LEVEL GENERATOR

/*
REFERENCES AND ACKNOWLEDGEMENTS:

Auto-texturing (setting tile colours in code):
https://www.poweredbyjeff.com/2020/11/06/Basic-colored-Tilemaps-in-Unity/

Generating tilemap:
https://youtu.be/W6cBwk0bRWE?si=M2WzPoQn0LUsAAYa
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    // LEVEL GENERATION SETTINGS

    // PRNG seed:
    [SerializeField] string seed;
    // Indicator for random seed:
    [SerializeField] bool useRandomSeed = true;
    // Percentage of the map to be filled:
    [Range(0, 100)] [SerializeField] int randomFillPercent = 60;
    // Seaweed coverage out of total area:
    [Range(0, 100)] [SerializeField] int seaweedPercent = 50;
    // Out of the remaining coverage, we define yellow coral coverage:
    [Range(0, 100)] [SerializeField] int yellowCoralPercent = 80;
    // NOTE: Red coral percent  = 100 - Yellow coral percent
    
    // Total number of artefacts to place:
    public int artefactsInTotal = 5;

    //------------------------------------
    // LEVEL GRID-RELATED VARIABLES

    // Size of the level's grid (in a 16:9 ratio):
    public static int width = 80;
	public static int height = 45;
    // Grid for the level:
    [HideInInspector] public int[,] grid = new int[width, height];
    // Game object to access the actual grid of the rendered map:
    public Grid renderedGrid; // Will be assigned later in the Inspector of Unity Editor
    // Game object to access the tilemap using which the map is rendered:
    public Tilemap tilemap; // Will be assigned later in the Inspector of Unity Editor
    
    //------------------------------------
    // TEXTURE-RELATED VARIABLES

    [SerializeField] bool autoTexturing = true; // Decides whether textures should be automatically generated
    public Texture2D waterTexture;
    public Texture2D seaweedTexture;
    public Texture2D yellowCoralTexture;
    public Texture2D redCoralTexture;
    public Texture2D artefactTexture;
    Vector2Int textureGridDimensions;

    //------------------------------------
    // TESTING/DEBUGGING VARIABLES

    [SerializeField] bool debugMode = false;
    [SerializeField] int testX = 0;
    [SerializeField] int testY = 0;

    //------------------------------------
    // Variable to indicate if level has been generated:
    [HideInInspector] public bool generationComplete;

    //================================================
    // MAIN FUNCTIONS

    //------------------------------------
    // Start is called before the first frame update:
    void Start()
    {
        // Ensuring texture grid dimensions match appropriately-sized square tiles:
        float tileLength = Mathf.Sqrt(width * height) * 1.5f; // Calculates an appropriate length of the square tiles
        textureGridDimensions = new Vector2Int((int) (tileLength * renderedGrid.cellSize.x), (int) (tileLength * renderedGrid.cellSize.y));
        
        // Assigning the necessary textures to avoid computational cost later (if `autoTexturing == true`):
        if(autoTexturing)
        {
            waterTexture = GetTexture(Color.blue);
            seaweedTexture = GetTexture(Color.green);
            yellowCoralTexture = GetTexture(Color.yellow);
            redCoralTexture = GetTexture(Color.red);
            artefactTexture = GetTexture(Color.black);
        }

        generationComplete = false;
        InitialiseGrid();
        ApplyCellularAutomata();
        PlaceArtefacts();
        GenerateTilemap();
        generationComplete = true;
    }

    //------------------------------------
    // Update is called once per frame:
    void Update()
    {
        // Seeing the generation process in steps for debugging and demonstration...
        // NOTE: This is only activated upon certain inputs
        StepwiseGeneration(Input.inputString);
        
        // Generate new playable map:
        if(Input.GetKey(KeyCode.Return))
        {
            generationComplete = false;
            tilemap.ClearAllTiles();
            InitialiseGrid();
            ApplyCellularAutomata();
            PlaceArtefacts();
            GenerateTilemap();
            generationComplete = true;
        }

        // If debug mode is `true`:
        if(debugMode && (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Return)))
        {
            // Updating the data for a test neighbourhood;
            UpdateNeighbourhoodData(testX, testY);

            // Placing the test tile whose neighbourhood we are checking:
            tilemap.SetTile(new Vector3Int(testX, testY, 0), GetTile(GetTexture(Color.black)));
            
            // Showing the values obtained:
            DebugMessage();
        }
    }

    //================================================
    // HELPER FUNCTIONS

    //------------------------------------
    // Initialising random number generator:
    public System.Random InitialisePRNG()
    {
        if(useRandomSeed)
            return new System.Random();
        
        return new System.Random(seed.GetHashCode());
    }

    //------------------------------------
    // Generating the texture automatically (only used if `autoTexturing = false`):
    public Texture2D GetTexture(Color c)
    {
        Texture2D texture = new Texture2D(textureGridDimensions.x, textureGridDimensions.y);
        // Assigning all the pixels with the right colors:
        for(int i = 0; i < textureGridDimensions.x; i++)
            for(int j = 0; j < textureGridDimensions.y; j++)
                texture.SetPixel(i, j, c);

        texture.Apply();
        return texture;
    }

    //------------------------------------
    // Generating a tile according to the given texture:
    public Tile GetTile(Texture2D t)
    {
        Sprite sprite = Sprite.Create(t, new Rect(0, 0, textureGridDimensions.x, textureGridDimensions.y), new Vector2(0.5f, 0.5f));
        // Arguments for the above (in order): Texture, Grid, Pivot (of the sprite w.r.t. the grid)
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        return tile;
    }

    //------------------------------------
    // Output debug message:
    private void DebugMessage()
    {
        string message = "Total 3x3 --> |";
        foreach(int i in moore_3) message += i.ToString() + '|';
        Debug.Log(message);
        message = "Total 5x5 --> |";
        foreach(int i in moore_5) message += i.ToString() + '|';
        Debug.Log(message);
        message = "Total adjacent --> |";
        foreach(int i in vonNeumann_4) message += i.ToString() + '|';
        Debug.Log(message);
    }

    //------------------------------------
    // Step-wise generation for testing and demonstration:
    void StepwiseGeneration(string c)
    {
        generationComplete = false;
        if(String.Equals(c, "0"))
        // Generating a new random grid:
        {
            tilemap.ClearAllTiles();
            InitialiseGrid();
            GenerateTilemap();
        }
        else if(String.Equals(c, "1"))
        // Re-applying only cellular automaton 1 to current grid:
        {
            ApplyCellularAutomata(25, 0, 0);
            GenerateTilemap();
        }
        else if(String.Equals(c, "2"))
        // Re-applying only cellular automaton 2 to current grid:
        {
            ApplyCellularAutomata(0, 10, 0);
            GenerateTilemap();
        }
        else if(String.Equals(c, "3"))
        // Re-applying only cellular automaton 3 to current grid:
        {
            ApplyCellularAutomata(0, 0, 25);
            PlaceArtefacts();
            GenerateTilemap();
            generationComplete = true;
        }
    }

    //------------------------------------
    // Assigning the values of one integer array to another (assuming equal size):
    void AssignArrayValues(int[,] array1, int[,] array2)
    {
        for(int i = 0; i < array1.GetLength(0); i++)
            for(int j = 0; j < array1.GetLength(1); j++)
                array1[i, j] = array2[i, j];
    }

    //================================================
    // STAGE A: Generating initial grid of filled & empty cells

    public void InitialiseGrid()
    {
        
        // Initialising random number generator using seed:
        System.Random prng = InitialisePRNG();

        // Iterate over grid positions
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Fill the grid according to `randomFillPercent`
                if(prng.Next(0, 100) < randomFillPercent)
                {
                    if(prng.Next(0, 100) < seaweedPercent)
                        grid[x, y] = 1;
                    else if(prng.Next(0, 100) < yellowCoralPercent)
                        grid[x, y] = 2;
                    else
                        grid[x, y] = 3;
                }
                else
                    grid[x, y] = 0;
            }
        }
    }

    //================================================
    // STAGE B: Cellular automata

    //------------------------------------
    // STAGE B.1: Getting relevant neighbourhood data
    
    // Data structures that can be conveniently accessed by other functions:
    [HideInInspector] public int[] moore_3 = {0, 0, 0, 0}; // 3 x 3 Moore neighbourhood, i.e. 3 x 3 neighbourhood centred at a given cell
    [HideInInspector] public int[] moore_5 = {0, 0, 0, 0}; // 5 x 5 Moore neighbourhood, i.e. 5 x 5 neighbourhood centred at a given cell
    [HideInInspector] public int[] vonNeumann_4 = {0, 0, 0, 0}; // 4 cell Von Neumann neighbourhood, i.e. neighbourhood consisting of a given cell's adjacent non-diagonal cells
    // NOTE 1: Storing data in global data structures rather than returning them in functions avoids coding complexity when making function calls
    // NOTE 2: The grid value (i.e. cell type) and indices of each of the above coincide

    // Function to update neighbourhood data:
    public void UpdateNeighbourhoodData(int x, int y)
    // Types: 0 = Water, 1 = Seaweed, 2 = Red Coral, 3 = Yellow Coral
    {
        // Clearing previous values:
        for(int i = 0; i < 4; i++)
        {
            moore_3[i] = 0;
            moore_5[i] = 0;
            vonNeumann_4[i] = 0;
        }

        // Iterating through the 5 x 5 Moore neighbourhood positions:
		for(int i = x - 2; i <= x + 2; i++)
        {
			for(int j = y - 2; j <= y + 2; j++)
            {
                // Conditions for skipping to next iteration...

                // Continue to next iteration if any of the following conditions are met:
				if((i == x && j == y) || i < 0 || i >= width || j < 0 || j >= height)
                    continue;

                // Continue to next iteration if the current position has artefact:
                if(grid[i, j] == -1)
                    continue;
                // NOTE:`grid[x, y] == 1` when (x, y) has an artefact

                //________________________
                // Gathering neighbourhood data...

                // Data regarding 5 x 5 Moore neighbourhood:
                moore_5[grid[i, j]]++;
                
                // Conditions for obtaining the other data:
                bool condition1 = i >= x - 1 && i <= x + 1 && j >= y - 1 && j <= y + 1;
                bool condition2 = i == x || j == y;
                
                // Data regarding 3 x 3 Moore neighbourhood:
                if(condition1)
                    moore_3[grid[i, j]]++;
                // Data regarding 4 cell Von Neumann neighbourhood:
                if(condition1 && condition2)
                    vonNeumann_4[grid[i, j]]++;
			}
		}
	}

    //------------------------------------
    // STAGE B.2: Cellular automata
    
    // REMINDER: Types: 0 = Water, 1 = Seaweed, 2 = Red Coral, 3 = Yellow Coral

    // Cellular automaton 1: Coral growth:
    int GrowCoral(int x, int y, System.Random prng)
    {
        UpdateNeighbourhoodData(x, y);
        
        // Relevant values (calculated for both coral types together):
        int a = moore_3[2] + moore_3[3];
        int b = moore_5[2] + moore_5[3];
        
        // If current tile is a water tile...
        if(grid[x, y] == 0)
        {
            if(a >= 4 && b <= 18)
            {
                // Randomly picking between coral types based on predetermined probabilities:
                if(prng.Next(0, 100) < yellowCoralPercent)
                    return 2;
                else
                    return 3;
            }
        }
        // If current tile is not a water or seaweed tile...
        else if(grid[x, y] == 2 || grid[x, y] == 3)
            if(a <= 1 || b > 18)
                return 0;

        return grid[x, y];
    }

    // Cellular automaton 2: Water space growth:
    int GrowWaterSpaces(int x, int y)
    {
        UpdateNeighbourhoodData(x, y);
    
        if(grid[x, y] >= 1 && moore_3[0] >= 5 || moore_5[0] >= 18)
            return 0;
        
        return grid[x, y];
    }

    // Cellular automaton 3: Seaweed growth:
    int GrowSeaweed(int x, int y)
    {
        UpdateNeighbourhoodData(x, y);

        // Relevant values (calculated only for seaweed):
        int a = vonNeumann_4[1];
        int b = moore_3[1];

        // If current tile is a seaweed tile...
        if(grid[x, y] == 1)
            if(a >= 2)
                return 0;
        // If current tile is a water tile...
        else if(grid[x, y] == 0)
            if(b >= 4)
                return 1;
    
        return grid[x, y];
    }

    //------------------------------------
    // STAGE B.3: Running each cellular automaton for a set number of iterations in a set order
    
    void ApplyCellularAutomata(int coralGrowthIterations = 25, int waterSpacesGrowthIterations = 10, int seaweedGrowthIterations = 25)
    {
        // Initialising random number generator using seed:
        System.Random prng = InitialisePRNG();

        // Buffer grid to contain updated cell values until one pass over the grid is done:
        int[,] buffer = new int[width, height];

        // Grow coral (bound by randomly generated seaweed so the coral don't form big masses across the play area):
        for(int i = 0; i < coralGrowthIterations; i++)
        {
            for(int x = 0; x < width; x++)
                for(int y = 0; y < height; y++)
                    buffer[x, y] = GrowCoral(x, y, prng);
            AssignArrayValues(grid, buffer);
        }
        
        // Grow water spaces to some extent (for clearer spaces and passages):
        for(int i = 0; i < waterSpacesGrowthIterations; i++)
        {
            for(int x = 0; x < width; x++)
                for(int y = 0; y < height; y++)
                    buffer[x, y] = GrowWaterSpaces(x, y);
            AssignArrayValues(grid, buffer);
        }
        
        // Grow seaweed to some extent (for aesthetics and easy-to-traverse hiding places):
        for(int i = 0; i < seaweedGrowthIterations; i++)
        {
            for(int x = 0; x < width; x++)
                for(int y = 0; y < height; y++)
                    buffer[x, y] = GrowSeaweed(x, y);
            AssignArrayValues(grid, buffer);
        }
    }

    //================================================
    // STAGE C: POST-PROCESSING

    //------------------------------------
    // STAGE C.1: Placing some artefacts around the map at random
    
    void PlaceArtefacts()
    {
        // Initialising random number generator using seed:
        System.Random prng = InitialisePRNG();

        int x = 0;
        int y = 0;

        for(int i = 0; i < artefactsInTotal; i++)
        {   
            while(true)
            {
                // Generate random coordinates:
                x = prng.Next(0, width);
                y = prng.Next(0, height);
                
                // If artefact already in (x, y):
                if(grid[x, y] == -1)
                    continue;
                
                // Place artefact and leave the loop:
                grid[x, y] = -1;
                break;
            }
        }
    }

    //------------------------------------
    // STAGE C.2: Generating the final level's tilemap

    void GenerateTilemap()
    {        
        // Variable to store the chosen texture per iteration:
        Texture2D chosenTexture = waterTexture;

		for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Choosing the tile to set in this position:
                switch(grid[x, y])
                {
                    case -1:
                        chosenTexture = artefactTexture;
                        break;
                    case 0:
                        chosenTexture = waterTexture;
                        break;
                    case 1:
                        chosenTexture = seaweedTexture;
                        break;
                    case 2:
                        chosenTexture = yellowCoralTexture;
                        break;
                    case 3:
                        chosenTexture = redCoralTexture;
                        break;
                }
                // Setting the chosen tile in the right position:
                tilemap.SetTile(new Vector3Int(x, y, 0), GetTile(chosenTexture));
            }
		}
	}
}