using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlaneController : MonoBehaviour
{
    //当前向前、左右移动速度和初始向前、左右移动速度.
    public float forwordMoveSpeed;
    public float moveSpeed;
    public float initialFMSpeed;
    public float initialMSpeed;
    //旋转机身时候速度
    public float rotateSpeed;
    public float returenRotateSpeed;
    private float cameraSpeed=3.5f;
    private float changeFWSpeed = 2f;
    private float changeMSpeed = 1f;
    //改变油量.
    private float changBrustSlider = 0.3f;

    private bool canObstacle = true;

    public GameObject planePrefab;
    public Slider brustSlider;
    public GameObject mainCamera;

    //加速拖尾特效/碰撞特效.
    public GameObject paticlePrefab;
    public GameObject boom;

    public static PlaneController instance;

    

    private void Start()
    {
        brustSlider.value = 1;
        initialFMSpeed = forwordMoveSpeed;
        initialMSpeed = moveSpeed;
    }

    void Update()
    {
        instance = this;
        //自动向前行驶.
        transform.Translate(forwordMoveSpeed * Time.deltaTime, 0,0);
        //move.
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(0, moveSpeed * Time.deltaTime*0.5f, 0);
            if (planePrefab.transform.localEulerAngles.z<30|| planePrefab.transform.localEulerAngles.z > 329)
            {
                if (planePrefab.transform.rotation.z > 0)
                {
                    planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime, Space.World);
                }
                else planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime * returenRotateSpeed, Space.World);
            }  
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(0, -moveSpeed * Time.deltaTime * 0.5f, 0);
            if (planePrefab.transform.localEulerAngles.z < 31 || planePrefab.transform.localEulerAngles.z > 330)
            {
                if (planePrefab.transform.rotation.z < 0)
                {
                    planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime, Space.World);
                }
                else planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime * returenRotateSpeed, Space.World);
            }
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate( 0, 0, -moveSpeed * Time.deltaTime);
            if (planePrefab.transform.localEulerAngles.x < 31 || planePrefab.transform.localEulerAngles.x > 330)
            {
                if (planePrefab.transform.rotation.x < 0)
                {
                    planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime, 0, 0, Space.World);
                }
                else planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0, Space.World);
            }
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(0, 0, moveSpeed * Time.deltaTime);
            if (planePrefab.transform.localEulerAngles.x < 30 || planePrefab.transform.localEulerAngles.x > 329) 
            {
                if (planePrefab.transform.rotation.x > 0)
                {
                    planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime, 0, 0, Space.World);
                }
                else planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0, Space.World);
            }  
        }

  
        if (planePrefab.transform.rotation.y > 0)
        {
            planePrefab.transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0 * returenRotateSpeed);
        }
        if (planePrefab.transform.rotation.y < 0)
        {
            planePrefab.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0 * returenRotateSpeed);
        }

        //rotate.
        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
        {

            if (planePrefab.transform.rotation.z > 0)
            {
                planePrefab.transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime * returenRotateSpeed);
            }
            if (planePrefab.transform.rotation.z < 0)
            {
                planePrefab.transform.Rotate(0, 0, rotateSpeed * Time.deltaTime * returenRotateSpeed);
            }

        }
        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            if (planePrefab.transform.rotation.x > 0)
            {
                planePrefab.transform.Rotate(-rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0);
            }
            if (planePrefab.transform.rotation.x < 0)
            {
                planePrefab.transform.Rotate(rotateSpeed * Time.deltaTime * returenRotateSpeed, 0, 0);
            }
        }

        //加速.
        if (canObstacle == true)
        {
            if (Input.GetKey(KeyCode.Space) && brustSlider.value > 0)
            {
                paticlePrefab.SetActive(true);
                ChangeFMSpeed(changeFWSpeed);
                ChangeMSpeed(changeMSpeed);
                ChangeBrustSlider(-changBrustSlider * Time.deltaTime);
                ChangeXOfCamera(-cameraSpeed * Time.deltaTime);
            }
            else
            {
                paticlePrefab.SetActive(false);
                ChangeFMSpeed(-.5f * changeFWSpeed);
                ChangeMSpeed(-changeMSpeed);
                ChangeXOfCamera(cameraSpeed * Time.deltaTime * 2);
            }
        }     
    }

    //改变前进的速度.
    public void ChangeFMSpeed(float value)
    {
        forwordMoveSpeed += value;
        if (forwordMoveSpeed >= initialFMSpeed * 5)
        {
            forwordMoveSpeed = initialFMSpeed * 5;
        }
        if (forwordMoveSpeed <= initialFMSpeed)
        {
            forwordMoveSpeed = initialFMSpeed;
        }
    }

    //改变左右的速度.
    public void ChangeMSpeed(float value)
    {
        moveSpeed += value;
        if (moveSpeed >= initialMSpeed * 2)
        {
            moveSpeed = initialMSpeed * 2;
        }
        if (moveSpeed <= initialMSpeed)
        {
            moveSpeed = initialMSpeed;
        }
    }

    //加速时，改变相机和飞机的距离.
    public void ChangeXOfCamera(float value)
    {
        mainCamera.transform.Translate(value, 0, 0, Space.World);
        if (mainCamera.transform.localPosition.x >= -2.5f)
        {
            mainCamera.transform.localPosition = new Vector3(-2.5f, 0.5f, 0);
        }
        if (mainCamera.transform.localPosition.x <= -7f)
        {
            mainCamera.transform.localPosition = new Vector3(-7f, 0.5f, 0);
        }
    }

    //改变加速燃料值.
    public void ChangeBrustSlider(float value)
    {
        brustSlider.value += value;
        if (brustSlider.value > 1)
        {
            brustSlider.value = 1;
        }
        if (brustSlider.value < 0)
        {
            brustSlider.value = 0;
        }
    }

    //撞到建筑物.
    public void Obstacle()
    {
        canObstacle = false;
        forwordMoveSpeed = initialFMSpeed*5;
        forwordMoveSpeed = -forwordMoveSpeed;
        Invoke("ObstacleOver", 0.5f);
        ChangeXOfCamera(-cameraSpeed * Time.deltaTime);
        Instantiate(boom,transform.position,Quaternion.identity);
    }
   
    public void ObstacleOver()
    {
        forwordMoveSpeed = initialFMSpeed;
        forwordMoveSpeed = -forwordMoveSpeed;
        canObstacle = true;
        ChangeXOfCamera(cameraSpeed * Time.deltaTime*2);
    }
}
