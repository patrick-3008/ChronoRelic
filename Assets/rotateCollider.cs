using UnityEngine;

public class rotateCollider : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();

        // Swap width (x) and depth (z) to simulate rotation
        Vector3 newSize = new Vector3(boxCollider.size.z, boxCollider.size.y, boxCollider.size.x);
        boxCollider.size = newSize;

        // Adjust center if needed (optional)
        Vector3 newCenter = new Vector3(boxCollider.center.z, boxCollider.center.y, boxCollider.center.x);
        boxCollider.center = newCenter;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
