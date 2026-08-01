using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Lesson2 : MonoBehaviour
{
    //const string url1 = "https://drive.google.com/file/d/158kMOxQru-8QAUrdp7l1VZcwX6X_-C75/view?usp=sharing";
    const string url1 = "https://drive.google.com/uc?export=view&id=158kMOxQru-8QAUrdp7l1VZcwX6X_-C75";
    //const string url2 = "https://drive.google.com/file/d/1KXPip1y4-qOmxIQrsFl9IIPtu06xaahO/view?usp=sharing";
    const string url2 = "https://drive.google.com/uc?export=view&id=1KXPip1y4-qOmxIQrsFl9IIPtu06xaahO";

    [SerializeField] private RawImage rawImage1;
    [SerializeField] private RawImage rawImage2;

    async void Start()
    {
        // Debug.Log("ダウンロード開始1");
        // UnityWebRequest request1 = UnityWebRequestTexture.GetTexture(url1);
        // await request1.SendWebRequest();
        // rawImage1.texture = DownloadHandlerTexture.GetContent(request1);
        // Debug.Log("ダウンロード完了1");

        // Debug.Log("ダウンロード開始2");
        // UnityWebRequest request2 = UnityWebRequestTexture.GetTexture(url2);
        // await request2.SendWebRequest();
        // rawImage2.texture = DownloadHandlerTexture.GetContent(request2);
        // Debug.Log("ダウンロード完了2");

        // Texture2D texture1 = await DownloadTexture(url1);
        // rawImage1.texture = texture1;

        // Texture2D texture2 = await DownloadTexture(url2);
        // rawImage2.texture = texture2;

        Texture2D texture1;
        Texture2D texture2;

        // 同時にダウンロードする （WhenAllは引数で与えられたタスクがすべて完了するまで待機する）
        (texture1, texture2) = await UniTask.WhenAll(DownloadTexture(url1), DownloadTexture(url2));

        rawImage1.texture = texture1;
        rawImage2.texture = texture2;
    }

    private async UniTask<Texture2D> DownloadTexture(string url)
    {
        Debug.Log("ダウンロード開始");
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        await request.SendWebRequest();
        Debug.Log("ダウンロード完了");

        return DownloadHandlerTexture.GetContent(request);
    }
}
