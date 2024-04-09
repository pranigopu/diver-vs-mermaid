# Undersea Explorers
**_An interactive agents & procedural generation project_**

For assignment information, click [here](https://github.com/pranigopu/interactiveAgents-proceduralGeneration/tree/8ef6661915856fa68851a981ee38afe837007ef1/project).

This project is a part of my MSc. AI's "Interactive Agents &amp; Procedural Generation" course and aims to create a game using cellular automata for level generation and behaviour trees for interactive agents.

# Level generator design
The level generator relies on random fill (based on preset proportions) to generate the initial grid (stage A), on which it them applies three cellular automata for a set number of iterations each (stage B). In these stages, the code works only with a 2D integer array (the `grid` variable in `LevelGenerator.cs`), altering the integers so as to represent the following tile types:

- 0 = Water
- 1 = Seaweed
- 2 = Yellow coral
- 3 = Red coral

---

**MORE ABOUT EACH TILE TYPE**:

Each of the aforementioned tile types have unique properties:

- <b style="color:blue;">Water</b>: Allows fast travel
- <b style="color:green;">Seaweed</b>: Provides cover and allows relatively fast travel (though a bit slower than water)
- <b style="color:yellow;">Yellow coral</b>: Provides cover and allows only relatively slow travel
- <b style="color:red;">Red coral</b>: Provides cover but hinders travel significantly (almost to a halt)

**NOTE**: _These properties only apply to the diver agent (the player); the mermaid can move freely across the map._

---

Finally, we have post-processing (stage C) wherein we (1) place artefacts at random around the map (indicated in the grid by assigning the associated cell the integer value $-1$) and (2) generate the final level's tilemap. The tilemap implements the grid using tiles of the appropriate textures (they are auto-generated solid colour textures by default, but custom textures can also be applied). By default, the color-to-tile mapping is as follows:

- Water = Blue
- Seaweed = Green
- Yellow coral = Yellow
- Red coral = Red
- Artefact = Black

_Now, each of the above stages shall be detailed more technically_...

## STAGE A: Initial 2D grid of filled and empty cells
The grid initialisation simply iterates through every possible position of the predefined play area and fills the grid associated with that position according to the above percentages. First, it begins by deciding whether to fill the tile or not based on `randomFillPercent`. If no, it assigns 0 to the grid cell and continues to the next position. If yes, it then decides whether to fill the tile as a seaweed or not based on `seaweedPercent`. If yes, it assigns 1 to the grid cell and continues to the next position. If no, it decides whether to fill the tile as a yellow coral or not based on `yelloCoralPercent`. If yes, it assigs 2 to the grid cell and continues to the next position. If no, it assigns 3 (for red coral) to the grid cell. The whole process goes on until every possible position is covered.

---

For reference, here is the code for the associated function:

```c
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
```

Here, we see three key variables:

- `randomFillPercent`: Specifies how much of the map should be non-water tiles (excluding artefacts)
- `seaweedPercent`: Specifies how much of the map should be seaweed tiles
- `yellowCoralPercent`: Specifies how much of the map should be yellow coral tiles

Note that $CoralPercent = 1 -$ `seaweedPercent`, and it is with respect to $CoralPercentage$ that `yellowCoralPercent` is defined. Hence, note that $RedCoralPercent = 1 -$ `yellowCoralPercent`. Hence, $CoralPercentage$ and $RedCoralPercent$ are not defined as they are directly inferred from `seaweedPercent` and `yellowCoralPercent`.

## STAGE B: Cellular automata
### STAGE B.1: Getting relevant neighbourhood data
Before applying cellular automata, some data about the neighbourhood of a grid cell needs to be obtained (for example, how many cells around it are filled). The data obtained can be used within conditions that determine how the given cell is to be altered. For the cellular automata used later, three kinds of data are obtained:

1. Number of cells of a each tile type around the given cell in a $3 \times 3$ neighbourhood
2. Number of cells of a each tile type around the given cell in a $5 \times 5$ neighbourhood
3. Number of cells of a each tile type adjacent to the given cell (adjacent $implies$ non-diagonal immediate neighbour)

**NOTE**: _Artefacts are not relevant for the cellular automata; they are placed in post-processing._

---

For reference, here is the code for the associated function:

```c
// Data structures that can be conveniently accessed by other functions:
[HideInInspector] public int[] total_3_by_3 = {0, 0, 0, 0};
[HideInInspector] public int[] total_5_by_5 = {0, 0, 0, 0};
[HideInInspector] public int[] total_adjacent = {0, 0, 0, 0};
// NOTE 1: Storing data in global data structures rather than returning them in functions avoids coding and computational complexity
// NOTE 2: The grid value (i.e. cell type) and indices of each of the above coincide

// Function to update neighbourhood data:
public void UpdateNeighbourhoodData(int x, int y)
// Types: 0 = Water, 1 = Seaweed, 2 = Red Coral, 3 = Yellow Coral
{
    // Clearing previous values:
    for(int i = 0; i < 4; i++)
    {
        total_3_by_3[i] = 0;
        total_5_by_5[i] = 0;
        total_adjacent[i] = 0;
    }

    // Iterating through the 5 x 5 neighbourhood positions:
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

            // Data regarding 5 x 5 neighbourhood:
            total_5_by_5[grid[i, j]]++;
            
            // Conditions for obtaining the other data:
            bool condition1 = i >= x - 1 && i <= x + 1 && j >= y - 1 && j <= y + 1;
            bool condition2 = i == x || j == y;
            
            // Data regarding 3 x 3 neighbourhood:
            if(condition1)
                total_3_by_3[grid[i, j]]++;
            // Data regarding adjacent cells:
            if(condition1 && condition2)
                total_adjacent[grid[i, j]]++;
        }
    }
}
```

---

The data (for each of the three kinds) are collected in three integer arrays of size four each:

1. `total_3_by_3` (for the $3 \times 3$ neighbourhood)
2. `total_5_by_5` (for the $5 \times 5$ neighbourhood)
3. `total_adjacent` (for adjacent cells)

**NOTE**: _The indices correspond directly to the tile type, i.e. index 0 holds the number of water tiles, etc._

The function is called each time data about a particular grid cell (corresponding to a map position) is needed. All three are updated at once since it is more computationally economical that way. The update process consists of iterating around a $5 \times 5$ neighbourhood of the given cell and incrementing the count of the tile type encountered under certain conditions (of course, the given cell's own position is skipped). The primary conditions is to ensure that the position being traversed are not the given cell's position and is within the grid. The secondary conditions are to handle the counts of tile types within a $3 \times 3$ neighbourhood (which is a subset of the $5 \times 5$ neighbourhood) and adjacent positions of the given cell.

**NOTE**: _The previous data of each array has to be cleared before update, hence the count-resetting loop at the start._

### STAGE B.2: Cellular automata
