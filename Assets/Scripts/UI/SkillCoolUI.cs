using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCoolUI : MonoBehaviour
{
    // [기획 의도] 스킬 가용 상태의 직관적 피드백 제공.
    // 쿨타임 오버레이(Fill Amount)와 남은 시간(Text)을 동시에 표시하여 
    // 유저가 전투 중 다음 스킬 사용 시점을 명확히 인지하게 함.

    [Header("설정")]
    public string skillType; // "Q", "W", "E", "R", "F" (대소문자 무관)
    public Image coolOverlay;
    public TextMeshProUGUI coolText;

    private PlayerSkills player;

    void Start() => player = PlayerSkills.Instance;

    void Update()
    {
        if (player == null || coolOverlay == null) return;

        float currentTimer = 0;
        float maxCooldown = 1f; // 0으로 나누기 방지용


        // [데이터 동기화] PlayerSkills 클래스와의 직접 연동.
        // UI 스크립트에서 수치를 별도로 관리하지 않고, 실제 논리 데이터(PlayerSkills.qTimer 등)를 
        // 실시간 참조함으로써 데이터 불일치 문제를 원천 차단함.

        // 1. 각 스킬에 맞는 '진짜' 최대 쿨타임을 정확히 입력.
        // (기존 PlayerSkills에 설정된 값과 일치해야 함)
        switch (skillType.ToUpper())
        {
            case "Q":
                currentTimer = player.qTimer;
                maxCooldown = player.qMaxCD; // 직접 입력하지 말고 player의 변수를 가져옴.
                break;
            case "W":
                currentTimer = player.wTimer;
                maxCooldown = player.wMaxCD;
                break;
            case "E":
                currentTimer = player.eTimer;
                maxCooldown = player.eMaxCD;
                break;
            case "R":
                currentTimer = player.rTimer;
                maxCooldown = player.rMaxCD;
                break;
            case "F":
                currentTimer = player.fTimer;
                maxCooldown = player.fMaxCD;
                break;
        }

        // 2. UI 업데이트 로직
        if (currentTimer > 0.05f) // 0에 가까우면 종료 처리
        {
            if (!coolOverlay.gameObject.activeSelf) coolOverlay.gameObject.SetActive(true);
            if (coolText != null && !coolText.gameObject.activeSelf) coolText.gameObject.SetActive(true);

            // (현재 남은 시간 / 해당 스킬의 최대 시간)
            coolOverlay.fillAmount = currentTimer / maxCooldown;

            if (coolText != null)
                coolText.text = currentTimer.ToString("F1");
        }
        else
        {
            // 쿨타임 종료 시 모두 숨김
            if (coolOverlay.gameObject.activeSelf) coolOverlay.gameObject.SetActive(false);
            if (coolText != null && coolText.gameObject.activeSelf) coolText.gameObject.SetActive(false);
        }
    }
}