using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks.Triggers;

public class Lesson3 : MonoBehaviour
{
    async void Start()
    {
        AsyncCollisionEnterTrigger collisionEnterTrigger = this.GetAsyncCollisionEnterTrigger();
        AsyncCollisionExitTrigger collisionExitTrigger = this.GetAsyncCollisionExitTrigger();

        Debug.Log("接触するまで待つよ");
        await collisionEnterTrigger.OnCollisionEnterAsync();
        Debug.Log("接触したよ");

        Debug.Log("離れるまで待つよ");
        await collisionExitTrigger.OnCollisionExitAsync();
        Debug.Log("離れたよ");
    }
}
