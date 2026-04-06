using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ItemController : MonoBehaviour
{
    public ConsumableItem slot1Item;
    public Image slot1IconDisplay;
    private NavMeshAgent agent;
    private bool isUsing = false;

    void Start()    
    {
        agent = GetComponent<NavMeshAgent>();
        if (slot1Item != null && slot1IconDisplay != null)
        {
            slot1IconDisplay.sprite = slot1Item.icon;
        }
    }

    void Update()
    {
        // 숫자 1번을 누르고, 아이템이 있고, 사용 중이 아닐 때
        if (Input.GetKeyDown(KeyCode.Alpha1) && slot1Item != null && !isUsing)
        {
            StartCoroutine(UseItemRoutine());
        }
    }

    IEnumerator UseItemRoutine()
    {
        isUsing = true;
        float originalSpeed = agent.speed;

        // 1. 속도 저하 (이동형 캐스팅)
        agent.speed = originalSpeed * slot1Item.speedMultiplier;
        Debug.Log($"{slot1Item.itemName} 사용 중...");

        // 2. 에셋에 설정된 사용 시간만큼 대기
        yield return new WaitForSeconds(slot1Item.castTime);

        // 3. 효과 적용 (여기선 로그만 찍지만, 실제론 체력을 채움)
        Debug.Log($"{slot1Item.itemName} 효과 발동! {slot1Item.healAmount} 회복");

        // 4. 속도 복구 및 종료
        agent.speed = originalSpeed;
        isUsing = false;
    }
}