using UnityEngine;
using System.Collections;

public class HeavyEnemy : Enemy // 기존 Enemy 스크립트를 상속받음.

{
    [Header("대형 몹(Heavy) 전용 설정")]
    [Tooltip("이 수치 미만의 데미지에는 피격 애니메이션(움찔)이 나오지 않습니다.")]
    public float superArmorThreshold = 30f;

    // [밸런스 설계] 부모 클래스의 HitAnimationRoutine을 오버라이드하여 
    // 경직 시간은 없고, 공격 사거리는 길게(4.5f) 설정하여 일반 적과는 다른 공략법이 필요하도록 유도.
    void Start()
    {
        // 트롤은 공격 애니메이션 파라미터가 attack03이므로 이름을 변경.
        attackAnimName = "attack03";

        // 대형 몹답게 공격 사거리 넓음
        attackRange = 4.5f;
    }
    protected override IEnumerator HitAnimationRoutine()
    {   // 일반 공격으로는 경직 없음.
        anim.SetBool("damage", true);
        yield return new WaitForSeconds(0.1f);
        anim.SetBool("damage", false);
    }

    // 부모 클래스의 TakeDamage를 대형 몹 규칙에 맞게 오버라이드.
    public override void TakeDamage(float amount)
    {
        if (isDead) return;
        hp -= amount;

        AlertPack();

        // [기획 의도] 대형 적(Heavy Enemy)의 묵직함을 표현하기 위해 '슈퍼 아머 임계값(Threshold)' 시스템 도입.
        // 방어력 수치를 통한 단순 데미지 감소 대신, 일정 수치 이상의 강력한 공격에만 경직을 허용하여 전투의 역동성을 부여함.
        // 강력한 공격(임계값 이상)일 때만 경직 애니메이션 실행
        if (amount >= superArmorThreshold)
        {
            if (anim != null && Time.time >= lastHitAnimTime + hitAnimCooldown)
            {
                StartCoroutine(HitAnimationRoutine());
                lastHitAnimTime = Time.time;
            }
        }

        if (hp <= 0) Die();
    }
}