using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks.Triggers;
using System.Threading;

public class Lesson4 : MonoBehaviour
{
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    async void Start()
    {
        try
        {
            //Destroy(this.gameObject, 5f);
            await LongTask(cancellationTokenSource.Token);
            Debug.Log("ここに来ることはない");
        }
        catch (OperationCanceledException operationCanceledException)
        {
            Debug.Log("キャンセルされました");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cancellationTokenSource.Cancel();
        }
    }

    private async UniTask LongTask(CancellationToken cancellationToken)
    {
        while (true)
        {
            //もしキャンセルされていたら例外を発生させる
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log(gameObject.name + " LongTask");
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            //もしキャンセルされていたら例外を発生させる
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
