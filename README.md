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
[HideInInspector] public int[] total_3_by_3 = {0, 0, 0, 0};
[HideInInspector] public int[] total_5_by_5 = {0, 0, 0, 0};
[HideInInspector] public int[] total_adjacent = {0, 0, 0, 0};

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

The function is called each time data about a particular grid cell (corresponding to a map position) is needed. All three are updated at once since it is more computationally economical that way. Furthermore, Storing data in global data structures rather than returning them in functions avoids the hassle of handling the return values when calling the function. The update process consists of iterating around a $5 \times 5$ neighbourhood of the given cell and incrementing the count of the tile type encountered under certain conditions (of course, the given cell's own position is skipped). The primary conditions is to ensure that the position being traversed is not either the given cell's position or an artefact and is within the grid. The secondary conditions are to handle the counts of tile types within a $3 \times 3$ neighbourhood (which is a subset of the $5 \times 5$ neighbourhood) and adjacent positions of the given cell.

**NOTE**: _The previous data of each array has to be cleared before update, hence the count-resetting loop at the start._

### STAGE B.2: Cellular automata
Three cellular automata are used for the following functions:

1. Growing corals (red and yellow)
2. Growing seaweed
3. Growing water spaces

They are applied sequentially, and each is applied for a set number of iterations. The order is key, because each cellular automaton relies on certain conditions to be effective, and these conditions are only achieved in a certain order. The coral growth is made to happen in clusters that are not too large. The randomly placed seaweed from stage A helps contain and shape the growth of coral clusters. The water spaces are made to clear excess scattered non-water tiles ("scattered" here means surrounded by too many water tiles so as to not be a part any cluster). Finally, the seaweed is made to grow in scattered, non-clustered patterns so as to ensure the map has not just clean passageways amid clusters of hiding spaces that hinder movement but also some hiding spaces that do not hinder movement. The seaweed growth also introduces some aesthetic messinenss that may help make the map more closely resemble a natural underwater terrain.

---

For reference, here are the functions for the three cellular automata:

**CORAL GROWTH**:

```c
int GrowCoral(int x, int y, System.Random prng)
{
    UpdateNeighbourhoodData(x, y);
    
    // Relevant values (calculated for both coral types together):
    int a = total_3_by_3[2] + total_3_by_3[3];
    int b = total_5_by_5[2] + total_5_by_5[3];
    
    // If current tile is a water tile...
    if(grid[x, y] == 0)
    {
        if(a >= 4 && b <= 12)
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
```

The first condition ensures that a water tile surrounded by enough coral tiles in a smaller ($3 \times 3$) neighbourhood is turned to coral, provided there are not too many corals in a wider ($5 \times 5$)  neighbourhood. Furthermore, note that the coral tiles are picked in random based on the preset `yellowCoralPercent`; this is an easy and effective way of adding variety to the map both aesthetically and functionally. This ensures clustering and little to no scattered growth of coral. The second condition ensures that a coral tile surrounded by too few coral tiles in a smaller neighbourhood or by too many water tiles in a wider water tiles is overcome by water. This ensures that any scattered coral tiles are removed, further ensuring clustering of coral.

**WATER GROWTH**:

```c
int GrowWaterSpaces(int x, int y)
{
    UpdateNeighbourhoodData(x, y);

    if(grid[x, y] >= 1 && total_3_by_3[0] >= 5 || total_5_by_5[0] >= 18)
        return 0;
    
    return grid[x, y];
}
```

The sole condition ensures that if a non-water tile is surrounded by too much water ("too much" is decided differently based on the neighbourhood size, as seen in the condition), the tile is overcome by water. This helps limit scattered non-water tiles and helps clear out passageways between coral clusters, which is useful in-game when traversing the map.

**SEAWEED GROWTH**:

```c
int GrowSeaweed(int x, int y)
{
    UpdateNeighbourhoodData(x, y);

    // Relevant values (calculated only for seaweed):
    int a = total_adjacent[1];
    int b = total_3_by_3[1];

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
```

The first condition ensures that a seaweed tile surrounded by too many adjacent seaweed dies out (i.e. is replaced by water). Note that `a >= n` (present as `a >= 2` in the above function) ensures greater scattering the lower `n` is (provided that `n` is any integer from 1 to 4); hence, if you want more clustered seaweed, choose a higher `n`. The second condition ensures that a water tile surrounded by enough seaweed becomes a seaweed tile; this enourages some seaweed growth beyond the clustered non-water spaces. Note that `b >= 4` (present as `b >= 4` in the above function) ensures greater seaweed growth the lower `n` is (provided that `n` is any integer from 1 to 8); hence, if you want most seaweed growth beyond the clustered non-water spaces, choose a lower `n`.

### STAGE B.3: Running each cellular automaton for a set number of iterations in a set order
As mentioned before, the order of running the cellular automata shapes the final result. Furthermore, the number of iterations for which each automaton is run can also be significant upto a certain point (beyond which the map may stabilise and no updates may occur). The number of iterations may need to be changed for a better result if other parameters (such as `randomFillPercent` or `seaweedPercent`) are changed. Based on trial-and-error, the following number of iterations are decided in the following order (given the parameters `randomFillPercent` = 60 and `seaweedPercent` = 80):

- Coral growth for 25 iterations
- Water space growth for 10 iterations
- Seaweed growth for 25 iterations

**EXAMPLES**:

Example 1 | Example 2
---|---
![](https://github.com/pranigopu/underseaExplorers/blob/86b9346fb077326c9cb442140143e49120d8ab2d/Media/levelGeneration_1.png) | ![](https://github.com/pranigopu/underseaExplorers/blob/86b9346fb077326c9cb442140143e49120d8ab2d/Media/levelGeneration_2.png)


