// MERMAID PROJECTILE SPRITE

/*
REFERENCES AND ACKNOWLEDGEMENTS:

Implementing 2D shooting:
https://www.youtube.com/watch?v=wkKsl1Mfp5M
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MermaidProjectile : MonoBehaviour
{

	[SerializeField] float movementSpeed = 100f;
	[SerializeField] int projectileDamage = 1;
	Rigidbody2D rb;
    public GameObject diver;

	// Initialization...
	void Awake()
	{
        // Finding the target, which is the game object named "Diver" from the hierarchy:
        diver = GameObject.Find("Diver");
        // NOTE: We cannot assign it in the inspector since the projectile object is made to instantiate at runtime
        
        // Initialising the rigid body component:
        rb = GetComponent<Rigidbody2D>();

        // Setting its trajectory and speed:
		rb.velocity = GetRequiredVelocity();
	}

    Vector2 GetRequiredVelocity()
    {
        // Obtaining the 2D position of the diver:
        Vector2 targetPosition = new Vector2(diver.transform.position.x, diver.transform.position.y);
        // NOTE: We obtain the 2D position for convenience of operation for setting the 2D rigid body's velocity
    
        return (targetPosition - rb.position).normalized * movementSpeed;
        // NOTE: The `normalized` property gets vector with the same direction but magnitude 1; this helps scale the velocity by the required speed
    }

    // NOTE: Callback method `OnBecameInvisible` is called when the renderer is no longer visible by any camera
    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    /*
    NOTE: Use of "Is Trigger" property for Collider2D of projectile:
    A trigger doesn't register a collision with an incoming Rigidbody.
    Instead, it sends `OnTriggerEnter`, `OnTriggerExit` and `OnTriggerStay` message when a rigidbody enters or exits the trigger volume.
    This is perfect for a projectile that is not supposed to physically move the target.
    */
    void OnTriggerEnter2D(Collider2D colliderObject)
    {
        Diver diver = colliderObject.GetComponent<Diver>();
        if(diver != null)
        {
            // Cause damage to the diver:
            diver.TakeDamage(projectileDamage);
            // Destroy the projectile game object:
            Destroy(gameObject);
        }
    }
}