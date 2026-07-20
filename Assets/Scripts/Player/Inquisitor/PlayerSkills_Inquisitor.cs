using System.Collections;
using UnityEngine;

public class PlayerSkills_Inquisitor : MonoBehaviour
{
    [Header("기본 성향 및 자원")]
    public float maxFaith = 50f;
    public float currentFaith = 0f;

    [Header("스킬 쿨타임 (초)")]
    public float cdQ = 3f;
    public float cdW = 6f;
    public float cdE = 8f;
    public float cdR = 15f;
    public float cdF = 20f;

    private float timerQ, timerW, timerE, timerR, timerF;

    [Header("컴포넌트 참조")]
    private Animator anim;
    private CharacterController controller; // 이동 및 돌진용 (없으면 자동 생략 가능)

    private void Awake()
    {
        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 쿨타임 타이머 감소
        if (timerQ > 0) timerQ -= Time.deltaTime;
        if (timerW > 0) timerW -= Time.deltaTime;
        if (timerE > 0) timerE -= Time.deltaTime;
        if (timerR > 0) timerR -= Time.deltaTime;
        if (timerF > 0) timerF -= Time.deltaTime;

        // 키 입력 테스트 (필요시 인풋 매니저와 연동)
        HandleSkillInputs();
    }

    private void HandleSkillInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q) && timerQ <= 0) UseQ();
        if (Input.GetKeyDown(KeyCode.W) && timerW <= 0) UseW();
        if (Input.GetKeyDown(KeyCode.E) && timerE <= 0) UseE();
        if (Input.GetKeyDown(KeyCode.R) && timerR <= 0) UseR();
        if (Input.GetKeyDown(KeyCode.F) && timerF <= 0) UseF();
    }

    #region Q 스킬: [징벌] (2연타 콤보)
    public void UseQ()
    {
        timerQ = cdQ;
        anim.SetTrigger("Skill_Q");

        // 타격 성공 시 신앙심 수급 (예시: +15)
        AddFaith(15f);
        Debug.Log("Q 스킬 [징벌] 시전!");
    }
    #endregion

    #region W 스킬: [방패 돌파] (하체 무빙 + 상체 방패)
    public void UseW()
    {
        timerW = cdW;
        anim.SetTrigger("Skill_W");

        StartCoroutine(DashRoutine(0.5f, 12f)); // 0.5초 동안 속도 12로 돌진
        Debug.Log("W 스킬 [방패 돌파] 시전!");
    }

    private IEnumerator DashRoutine(float duration, float speed)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (controller != null && controller.enabled)
            {
                controller.Move(transform.forward * speed * Time.deltaTime);
            }
            else
            {
                transform.position += transform.forward * speed * Time.deltaTime;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    #region E 스킬: [이단 구속] (제자리 사슬 투척)
    public void UseE()
    {
        timerE = cdE;
        anim.SetTrigger("Skill_E");

        StartCoroutine(ExecuteHook());
        Debug.Log("E 스킬 [이단 구속] 시전!");
    }

    private IEnumerator ExecuteHook()
    {
        // 왼손을 뻗는 모션 타이밍에 맞춰 사슬 발사 (0.2초 딜레이)
        yield return new WaitForSeconds(0.2f);

        // TODO: 사슬 이펙트 생성 및 타겟 당기기 / 날아가기 판정 실행
    }
    #endregion

    #region R 스킬: [심판] (회전 난무 / 광역 대미지)
    public void UseR()
    {
        timerR = cdR;
        anim.SetTrigger("Skill_R");

        StartCoroutine(ExecuteSmash());
        Debug.Log("R 스킬 [심판] 시전!");
    }

    private IEnumerator ExecuteSmash()
    {
        // 한 바퀴 크~게 휘두르는 타이밍에 맞춰 묵직한 대미지 (예: 0.4초 후)
        yield return new WaitForSeconds(0.4f);

        // 신앙심 대량 수급
        AddFaith(30f);
        // TODO: 주변 360도 범위 대미지(80) 및 카메라 흔들림 처리
    }
    #endregion

    #region F 스킬: [폭발하는 신념] (신앙심 소모 광역 기절)
    public void UseF()
    {
        // 신앙심이 일정량 이상일 때만 발동 가능 (예: 최소 20 이상)
        if (currentFaith < 20f)
        {
            Debug.Log("신앙심이 부족합니다!");
            return;
        }

        timerF = cdF;
        anim.SetTrigger("Skill_F");

        // 쌓인 신앙심을 모두 소모하여 방어막 변환 및 광역 기절
        float consumedFaith = currentFaith;
        currentFaith = 0f;

        Debug.Log($"F 스킬 [폭발하는 신념] 시전! (소모된 신앙심: {consumedFaith})");
        // TODO: consumedFaith 비례 광역 기절 및 보호막 생성 로직
    }
    #endregion

    #region 자원(신앙심) 관리
    public void AddFaith(float amount)
    {
        currentFaith = Mathf.Clamp(currentFaith + amount, 0f, maxFaith);
        Debug.Log($"현재 신앙심: {currentFaith} / {maxFaith}");
    }
    #endregion
}