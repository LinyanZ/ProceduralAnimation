using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float walkSpeed = 10f;
    [SerializeField] float rotateSpeed = 10f;

    float angle = 0f;

    void Update()
    {
        float forward = Input.GetAxis("Vertical");
        float right = Input.GetAxis("Horizontal");
        float multiplier = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;

        transform.position += right * Time.deltaTime * walkSpeed * multiplier * transform.right;
        transform.position += forward * Time.deltaTime * walkSpeed * multiplier * transform.forward;

        if (Input.GetKey(KeyCode.Q))
            angle -= Time.deltaTime * rotateSpeed;
        if (Input.GetKey(KeyCode.E))
            angle += Time.deltaTime * rotateSpeed;

        transform.rotation = Quaternion.AngleAxis(angle, transform.up);
    }
}
