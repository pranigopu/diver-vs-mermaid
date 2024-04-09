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

- <p style="color:blue">Water</p>: Allows fast travel
- <p style="color:green">Seaweed</p>: Provides cover and allows relatively fast travel (though a bit slower than water)
- <p style="color:yellow">Yellow coral</p>: Provides cover and allows only relatively slow travel
- <p style="color:red">Red coral</p>: Provides cover but hinders travel significantly (almost to a halt)

**NOTE**: _These properties only apply to the diver agent (the player); the mermaid can move freely across the map._

---

Finally 

## STAGE A: Initial 2D grid of filled and empty cells
