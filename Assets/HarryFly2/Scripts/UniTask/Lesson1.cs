using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class Lesson1 : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("Start");
        Debug.Log("5秒後に次のログをだします");
        await UniTask.Delay(TimeSpan.FromSeconds(5));
        Debug.Log("5秒経過しました");

        Debug.Log("5秒まつよー1");
        await WaitFiveSecondsMethod();
        Debug.Log("5秒たったよー2");

        Debug.Log("5秒まつよー3");
        WaitFiveSecondsMethod();
        Debug.Log("5秒たったよー3");
    }

    private async UniTask WaitFiveSecondsMethod()
    {
        Debug.Log("5秒まつよー2");
        await UniTask.Delay(TimeSpan.FromSeconds(5));
        Debug.Log("5秒たったよー1");
    }

}
