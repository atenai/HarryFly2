using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks.Triggers;

public class Lesson3_2 : MonoBehaviour
{
    [SerializeField] private Button button;

    async void Start()
    {
        Debug.Log("ボタンが押されるまで待つよ");
        var eventHandler = button.GetAsyncClickEventHandler();
        await eventHandler.OnClickAsync();
        Debug.Log("ボタンが押されたよ");
    }
}
