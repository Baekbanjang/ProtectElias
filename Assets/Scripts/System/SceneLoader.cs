using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>씬 전환 - 버튼 OnClick 연결용</summary>
public class SceneLoader : MonoBehaviour
{
     /// <summary>전투 시작, 다시하기</summary>
     public void LoadBattle()
     {
         Time.timeScale = 1f; // 게임오버 정지 해제
         SceneManager.LoadScene("Battle");
     }
    
     /// <summary>타이틀 복귀</summary>
     public void LoadTitle()
     {
         Time.timeScale = 1f;
         SceneManager.LoadScene("Title");
     }
}
