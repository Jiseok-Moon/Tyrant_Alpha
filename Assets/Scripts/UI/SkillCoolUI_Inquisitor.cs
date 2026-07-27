using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCoolUI_Inquisitor : MonoBehaviour
{
    [Header("설정")]
    public string skillType; // "Q", "W", "E", "R", "F"
    public Image coolOverlay;
    public TextMeshProUGUI coolText;

    private PlayerSkills_Inquisitor player;

    private void Start()
    {
        player = PlayerSkills_Inquisitor.Instance;
    }

    private void Update()
    {
        if (player == null)
        {
            player = PlayerSkills_Inquisitor.Instance;
            if (player == null) return;
        }

        if (coolOverlay == null) return;

        float currentTimer = 0f;
        float maxCooldown = 1f;

        switch (skillType.ToUpper())
        {
            case "Q":
                currentTimer = player.qTimer;
                maxCooldown = player.qMaxCD;
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

        // 쿨타임이 0 이하이거나 maxCooldown이 설정되지 않은 스킬 예외 처리
        if (currentTimer > 0.05f && maxCooldown > 0f)
        {
            if (!coolOverlay.gameObject.activeSelf) coolOverlay.gameObject.SetActive(true);
            if (coolText != null && !coolText.gameObject.activeSelf) coolText.gameObject.SetActive(true);

            coolOverlay.fillAmount = Mathf.Clamp01(currentTimer / maxCooldown);

            if (coolText != null)
                coolText.text = currentTimer.ToString("F1");
        }
        else
        {
            if (coolOverlay.gameObject.activeSelf) coolOverlay.gameObject.SetActive(false);
            if (coolText != null && coolText.gameObject.activeSelf) coolText.gameObject.SetActive(false);
        }
    }
}