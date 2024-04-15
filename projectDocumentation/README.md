# PROJECT DOCUMENTATION
The images used for the explanations in the main README of the project as well as potentially useful implementation and conceptual notes taken during the project (the notes are given below).

---

# NOTES

## Orthographic vs. perspective projection
Orthographic projection (also orthogonal projection and analemma) is a means of representing three-dimensional objects in two dimensions. Orthographic projection is a form of parallel projection in which all the projection lines are orthogonal to the projection plane, resulting in every plane of the scene appearing in affine transformation on the viewing surface.

> REFERENCE: https://en.wikipedia.org/wiki/Orthographic_projection

Our eyes are used to perspective viewing where distant objects appear smaller. Orthographic projection often seems a bit odd at first, because objects stay the same size regardless of their distance. It is like viewing the scene from an infinitely distant point. Nevertheless, orthographic viewing can be very useful, because it provides a more “technical” insight into the scene, making it easier to model and judge proportions.

> REFERENCE: https://docs.blender.org/manual/en/latest/editors/3dview/navigate/projections.html

## Kinematic colliders
To enable colliders such that the sprites can move but not be moved, use the "Kinematic" option instead of "Dynamic" option.

## Use of "Is Trigger" property for Collider2D of projectile
A trigger doesn't register a collision with an incoming Rigidbody. Instead, it sends `OnTriggerEnter`, `OnTriggerExit` and `OnTriggerStay` message when a rigidbody enters or exits the trigger volume. This is perfect for a projectile that is not supposed to physically move the target.

## Setting the "Gravity Scale" property to 0 in RigidBody2D component
To avoid gravity effects on the sprite for which you add a RigidBody2D component, I have make sure to set the "Gravity Scale" object to 0.

## `Awake` vs. `Start`
The callback methods `Start` and `Awake` work in similar ways except that `Awake` is called first and, unlike `Start`, will be called even if the script component is disabled. Using `Start` and `Awake` together is useful for separating initialisation tasks into two steps. For example, a script’s self-initialisation. For example, creating component references and initialising variables can be done in `Awake` before another script attempts to access and use that data in `Start`, avoiding errors.

> REFERENCE: https://gamedevbeginner.com/start-vs-awake-in-unity/

## Callback method `OnDrawGizmos`
Implement the callback method `OnDrawGizmos` if you want to draw gizmos that are also pickable and always drawn. This allows you to quickly pick important objects in your Scene. Note that `OnDrawGizmos` will use a mouse position that is relative to the Scene View.

In our case, each Gizmo is a cell on the grid that needs to be positioned individually. Note that Gizmos do not render in runtime, so they are for testing and debugging only.

``` 
void OnDrawGizmos()
{
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            switch(grid[x, y])
            {
                case 1:
                    Gizmos.color = Color.green;
                    break;
                case 2:
                    Gizmos.color = Color.yellow;
                    break;
                case 3:
                    Gizmos.color = Color.red;
                    break;
                default:
                    Gizmos.color = Color.blue;
                    break;
            }

            // Positioning the Gizmo:
            Vector2 position = new Vector2(x - width - 1, y + height + 1);
            Gizmos.DrawCube(position, Vector2.one);
        }
    }

    // If `debugMode = true`, draw the test cell:
    if(debugMode)
    {
        Gizmos.color = Color.black;
        Vector2 pos = new Vector2(testX - width - 1, testY + height + 1);
        Gizmos.DrawCube(pos, Vector2.one);
    }
}
```
