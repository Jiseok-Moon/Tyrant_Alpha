using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCoolUI : MonoBehaviour
{
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

        // 1. 각 스킬에 맞는 '진짜' 최대 쿨타임을 정확히 입력합니다.
        // (기존 PlayerSkills에 설정된 값과 일치해야 합니다)
        switch (skillType.ToUpper())
        {
            case "Q":
                currentTimer = player.qTimer;
                maxCooldown = player.qMaxCD; // 직접 입력하지 말고 player의 변수를 가져옵니다.
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

            // [핵심 계산] (현재 남은 시간 / 해당 스킬의 최대 시간)
            // 이렇게 해야 각자 100%에서 시작해서 0%로 끝납니다.
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