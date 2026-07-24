using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCoolUI_Inquisitor : MonoBehaviour
{
    // [기획 의도] 스킬 가용 상태의 직관적 피드백 제공.
    // 쿨타임 오버레이(Fill Amount)와 남은 시간(Text)을 동시에 표시하여 
    // 유저가 전투 중 다음 스킬 사용 시점을 명확히 인지하게 함.

    [Header("설정")]
    public string skillType; // "Q", "W", "E", "R", "F" (대소문자 무관)
    public Image coolOverlay;
    public TextMeshProUGUI coolText;

    // 이단심문관(PlayerSkills_Inquisitor)으로 변경!
    private PlayerSkills_Inquisitor player;

    void Start() => player = PlayerSkills_Inquisitor.Instance;

    void Update()
    {
        // 씬 시작 시 순서 차이로 Instance를 뒤늦게 얻을 경우 보정
        if (player == null)
        {
            player = PlayerSkills_Inquisitor.Instance;
            if (player == null) return;
        }

        if (coolOverlay == null) return;

        float currentTimer = 0f;
        float maxCooldown = 1f; // 0으로 나누기 방지용

        // [데이터 동기화] PlayerSkills_Inquisitor와 연동
        switch (skillType.ToUpper())
        {
            case "Q":
                currentTimer = player.timerQ;
                maxCooldown = player.cdQ;
                break;
            case "W":
                currentTimer = player.timerW;
                maxCooldown = player.cdW;
                break;
            case "E":
                currentTimer = player.timerE;
                maxCooldown = player.cdE;
                break;
            case "R":
                currentTimer = player.timerR;
                maxCooldown = player.cdR;
                break;
            case "F":
                currentTimer = player.timerF;
                maxCooldown = player.cdF;
                break;
        }

        // 쿨타임 UI 업데이트
        if (currentTimer > 0.05f) // 쿨타임 진행 중
        {
            if (!coolOverlay.gameObject.activeSelf) coolOverlay.gameObject.SetActive(true);
            if (coolText != null && !coolText.gameObject.activeSelf) coolText.gameObject.SetActive(true);

            coolOverlay.fillAmount = currentTimer / maxCooldown;

            if (coolText != null)
                coolText.text = currentTimer.ToString("F1");
        }
        else // 쿨타임 종료
        {
            if (coolOverlay.gameObject.activeSelf) coolOverlay.gameObject.SetActive(false);
            if (coolText != null && coolText.gameObject.activeSelf) coolText.gameObject.SetActive(false);
        }
    }
}