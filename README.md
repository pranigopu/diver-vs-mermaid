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

Finally, we have post-processing (stage C) wherein we (1) place artefacts at random around the map (indicated in the grid by assigning the associated cell the integer value $-1$) and (2) generate the final level's tilemap. The tilemap implements the grid using tiles of the appropriate textures (they are auto-generated solid colour textures by default, but custom textures can also be applied). By default, we have the following color-to-tile mapping:

- Water = Blue
- Seaweed = Green
- Yellow coral = Yellow
- Red coral = Red
- Artefact = Black

_Now, each of the above stages shall be detailed more technically_...

## STAGE A: Initial 2D grid of filled and empty cells

```c#
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
