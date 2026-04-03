using UnityEngine;
using System.Collections;

public class HeavyEnemy : Enemy // 기존 Enemy 스크립트를 상속받습니다.
{
    [Header("대형 몹(Heavy) 전용 설정")]
    [Tooltip("이 수치 미만의 데미지에는 피격 애니메이션(움찔)이 나오지 않습니다.")]
    public float superArmorThreshold = 30f;


    void Start()
    {
        // 트롤은 공격 애니메이션 파라미터가 attack03이므로 이름을 변경.
        attackAnimName = "attack03";

        // 대형 몹답게 체력이나 공격 사거리를 코드에서 한 번 더 잡아줘도 좋습니다.
        attackRange = 4.5f;
    }

    // 부모 클래스의 TakeDamage를 대형 몹 규칙에 맞게 재정의(Override).
    public override void TakeDamage(float amount)
    {
        if (isDead) return;
        hp -= amount;

        // 무리 알림 (무리 몬스터일 경우 주석처리 해제)
        // AlertPack();

        // 2. 슈퍼 아머 로직: 강력한 공격(임계값 이상)일 때만 경직 애니메이션 실행
        if (amount >= superArmorThreshold)
        {
            // 부모(Enemy)에 정의된 피격 애니메이션 쿨타임과 로직을 체크하여 실행
            if (anim != null && Time.time >= lastHitAnimTime + hitAnimCooldown)
            {
                StartCoroutine(HitAnimationRoutine());
                lastHitAnimTime = Time.time;
            }
        }

        if (hp <= 0) Die();
    }
}