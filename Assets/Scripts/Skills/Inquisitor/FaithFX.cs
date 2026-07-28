using System.Collections;
using UnityEngine;

public class FaithFX : MonoBehaviour
{
    private Material mat;
    private Color initialColor;

    public void PlayEffect(float maxRadius, float duration)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material; // 인스턴스 재질 복사
            initialColor = mat.color;
        }

        StartCoroutine(ExpandAndFade(maxRadius, duration));
    }

    private IEnumerator ExpandAndFade(float maxRadius, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero; // 중심점에서 시작
        Vector3 targetScale = Vector3.one * (maxRadius * 2f); // 지름 = 반지름 * 2

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // 1. 구체 크기 확대 (Fast Out 모션)
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easeT);

            // 2. 점점 투명하게 페이드 아웃
            if (mat != null)
            {
                Color c = initialColor;
                c.a = Mathf.Lerp(initialColor.a, 0f, t);
                mat.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 연출 완료 후 자동 파괴
        Destroy(gameObject);
    }
}