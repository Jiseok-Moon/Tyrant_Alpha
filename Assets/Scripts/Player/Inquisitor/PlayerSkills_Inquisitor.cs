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

    [Header("시전 중 이동 제어")]
    public bool isCasting = false;

    #region Q 스킬: [징벌] (2연타 콤보)
    public void UseQ()
    {
        if (isCasting) return; // 이미 다른 스킬이나 Q를 쓰는 중이면 무시

        timerQ = cdQ;
        anim.SetTrigger("Skill_Q");

        // 0.8초 동안 이동을 멈추고 제자리에 고정 (애니메이션 길이에 맞춰 조절)
        StartCoroutine(LockMovementForDuration(1.55f));

        AddFaith(15f);
        Debug.Log("Q 스킬 [징벌] 시전!");
    }

    private IEnumerator LockMovementForDuration(float duration)
    {
        isCasting = true;

        // 플레이어 이동 스크립트가 있다면 여기서 이동을 끕니다.
        // 예: GetComponent<PlayerMovement>().enabled = false;

        yield return new WaitForSeconds(duration);

        isCasting = false;
        // 예: GetComponent<PlayerMovement>().enabled = true;
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
        if (isCasting) return;

        timerE = cdE;
        anim.SetTrigger("Skill_E");

        StartCoroutine(LockMovementForDuration(1.01f)); // E 스킬 시전 동안 제자리 고정
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
        if (isCasting) return;

        timerR = cdR;
        anim.SetTrigger("Skill_R");

        StartCoroutine(LockMovementForDuration(2.1f)); // R 스킬 시전 동안 제자리 고정 (애니메이션 길이에 맞춰 수치 조절)
        StartCoroutine(ExecuteSmash());
        Debug.Log("R 스킬 [심판] 시전!");
    }

    private IEnumerator ExecuteSmash()
    {
        // 한 바퀴 크~게 휘두르는 타이밍에 맞춰 묵직한 대미지 (예: 0.4초 후)
        yield return new WaitForSeconds(1.07f);

        // 신앙심 대량 수급
        AddFaith(30f);
        // TODO: 주변 360도 범위 대미지(80) 및 카메라 흔들림 처리
    }
    #endregion

    #region F 스킬: [폭발하는 신념] (신앙심 소모 광역 기절)
    public void UseF()
    {
        if (currentFaith < 20f || isCasting)
        {
            Debug.Log("신앙심이 부족하거나 시전 중입니다!");
            return;
        }

        timerF = cdF;
        anim.SetTrigger("Skill_F");

        StartCoroutine(LockMovementForDuration(2.11f)); // F 스킬 시전 동안 제자리 고정

        float consumedFaith = currentFaith;
        currentFaith = 0f;
        Debug.Log($"F 스킬 [폭발하는 신념] 시전! (소모된 신앙심: {consumedFaith})");
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