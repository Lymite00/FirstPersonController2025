using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;

    private float xRotation;
    private float yRotation;

    public float leanAngle = 15f;
    public float leanSpeed = 5f;
    private float currentLeanX = 0f;
    private float currentLeanY = 0f;

    void Start()
    {
        SetMouse(true);
    }

    void Update()
    {
        CalculateCameraRotation();
        ApplyLean();
    }

    private void SetMouse(bool hide)
    {
        if (hide)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void CalculateCameraRotation()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation + currentLeanY, yRotation, currentLeanX);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void ApplyLean()
    {
        float horizontal = Input.GetAxis("Horizontal");

        float targetLeanX = -horizontal * leanAngle;
        currentLeanX = Mathf.Lerp(currentLeanX, targetLeanX, Time.deltaTime * leanSpeed);

        currentLeanY = Mathf.Lerp(currentLeanY, 0f, Time.deltaTime * leanSpeed);
    }
}