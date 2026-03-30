using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// ============================================
// 유니티에서 서버로 점수를 보내는 스크립트
// ============================================
// 이 스크립트는 UI 버튼에 연결되어 사용됩니다.
// 버튼을 누르면 서버로 플레이어 이름과 점수를 JSON 형식으로 전송합니다.

public class UnityScoreSender : MonoBehaviour
{
    // ============================================
    // 1. Inspector에서 설정할 수 있는 변수들
    // ============================================
    
    [Header("=== 서버 설정 ===")]
    [Tooltip("서버 주소와 포트")]
    // 서버 주소 - Node.js 서버가 실행 중인 주소입니다.
    // localhost는 "내 컴퓨터"를 의미하고, 3000은 포트 번호입니다.
    [SerializeField] private string serverUrl = "http://localhost:3000";
    
    [Header("=== 플레이어 정보 ===")]
    [Tooltip("플레이어 이름")]
    // 플레이어의 이름을 저장하는 변수입니다.
    // Inspector에서 미리 설정하거나, 게임 중 입력받을 수 있습니다.
    [SerializeField] private string playerName = "홍길동";
    
    [Tooltip("플레이어 점수")]
    // 플레이어의 점수를 저장하는 변수입니다.
    // 게임 로직에 따라 동적으로 변경될 수 있습니다.
    [SerializeField] private int playerScore = 100;
    
    [Header("=== UI 요소 ===")]
    [Tooltip("점수 전송 버튼")]
    // Unity 에디터에서 버튼을 연결해주세요.
    // 버튼을 누르면 SendScoreToServer() 함수가 실행됩니다.
    [SerializeField] private Button sendButton;
    
    [Tooltip("결과를 표시할 텍스트 (선택사항)")]
    // 서버 응답 결과를 화면에 표시할 텍스트 컴포넌트입니다.
    // 없어도 동작합니다.
    [SerializeField] private Text resultText;

    // ============================================
    // 2. 게임 시작 시 실행되는 함수
    // ============================================
    // Start는 스크립트가 활성화되면 자동으로 한 번 실행됩니다.
    void Start()
    {
        // 버튼이 연결되어 있다면, 버튼 클릭 이벤트에 함수를 등록합니다.
        if (sendButton != null)
        {
            // 버튼이 클릭되면 SendScoreToServer 함수가 실행되도록 설정
            sendButton.onClick.AddListener(SendScoreToServer);
            Debug.Log("버튼이 성공적으로 연결되었습니다.");
        }
        else
        {
            // 버튼이 연결되지 않았다면 경고 메시지 출력
            Debug.LogWarning("경고: sendButton이 설정되지 않았습니다. Inspector에서 버튼을 연결해주세요.");
        }
    }

    // ============================================
    // 3. 서버로 점수를 보내는 함수
    // ============================================
    // 이 함수는 버튼이 클릭되었을 때 호출됩니다.
    public void SendScoreToServer()
    {
        // 코루틴을 시작하여 비동기로 서버 통신을 수행합니다.
        // 코루틴은 Unity에서 시간이 걸리는 작업(네트워크 통신 등)을 할 때 사용합니다.
        StartCoroutine(SendScoreCoroutine());
    }

    // ============================================
    // 4. 실제 서버 통신을 수행하는 코루틴
    // ============================================
    // IEnumerator는 코루틴 함수의 반환 타입입니다.
    // yield return을 사용하여 작업을 잠시 중단하고, 다음 프레임에 계속할 수 있습니다.
    private IEnumerator SendScoreCoroutine()
    {
        // ----------------------------------------
        // 4-1. JSON 데이터 준비
        // ----------------------------------------
        // C# 객체를 JSON 문자열로 변환합니다.
        // $"..." 문자열 보간을 사용하여 변수를 문자열에 삽입합니다.
        string jsonData = $"{{\"name\": \"{playerName}\", \"score\": {playerScore}}}";
        
        // 디버그 로그로 보낼 데이터를 확인합니다.
        Debug.Log($"서버로 보낼 데이터: {jsonData}");
        
        // ----------------------------------------
        // 4-2. UnityWebRequest 생성 및 설정
        // ----------------------------------------
        // UnityWebRequest.Post를 사용하여 POST 요청을 생성합니다.
        // 첫 번째 인자: 서버 URL
        // 두 번째 인자: 보낼 데이터 (문자열)
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(serverUrl, jsonData))
        {
            // ----------------------------------------
            // 4-3. 요청 헤더 설정
            // ----------------------------------------
            // Content-Type 헤더를 "application/json"으로 설정합니다.
            // 이것은 "내가 보내는 데이터는 JSON 형식입니다"라고 서버에 알려주는 것입니다.
            request.SetRequestHeader("Content-Type", "application/json");
            
            // 업로드 핸들러의 Content-Type도 JSON으로 설정
            // 이 설정이 없으면 서버가 데이터를 제대로 인식하지 못할 수 있습니다.
            request.uploadHandler.contentType = "application/json";
            
            // ----------------------------------------
            // 4-4. 서버에 요청 보내기
            // ----------------------------------------
            // SendWebRequest는 네트워크 요청을 시작합니다.
            // yield return을 사용하여 요청이 완료될 때까지 기다립니다.
            Debug.Log("서버에 데이터를 전송 중...");
            yield return request.SendWebRequest();
            
            // ----------------------------------------
            // 4-5. 응답 처리
            // ----------------------------------------
            // 요청이 성공했는지 확인합니다.
            // result가 Success면 성공, 그 외에는 실패입니다.
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 성공한 경우
                Debug.Log("서버 응답 성공!");
                Debug.Log($"서버에서 받은 메시지: {request.downloadHandler.text}");
                
                // 결과 텍스트 UI가 있다면 업데이트
                if (resultText != null)
                {
                    resultText.text = $"전송 성공!\n{request.downloadHandler.text}";
                    resultText.color = Color.green;
                }
            }
            else
            {
                // 실패한 경우
                Debug.LogError($"서버 통신 실패: {request.error}");
                Debug.LogError($"응답 코드: {request.responseCode}");
                
                // 결과 텍스트 UI가 있다면 업데이트
                if (resultText != null)
                {
                    resultText.text = $"전송 실패: {request.error}";
                    resultText.color = Color.red;
                }
            }
        }
        
        // using 문이 끝나면 request 객체가 자동으로 해제됩니다.
    }

    // ============================================
    // 5. Inspector에서 값이 변경될 때 호출되는 함수 (선택사항)
    // ============================================
    // OnValidate는 Unity 에디터에서 Inspector 값이 변경되었을 때 호출됩니다.
    // 게임 실행 중에는 호출되지 않습니다.
    void OnValidate()
    {
        // playerScore가 음수가 되지 않도록 확인
        if (playerScore < 0)
        {
            playerScore = 0;
        }
    }
}
