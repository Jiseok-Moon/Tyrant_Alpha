## 🛠️ 사용 엔진 및 기술 스택
* Engine: Unity 2022.3 (URP)
* Language: C#
* Core Systems: UI Framework (Inventory, Pause & Game Over Panel), NavMesh Agent, Animator Layer System

---

## 💡 핵심 구현 정보 (Core Features)

### 1. 플레이어블 캐릭터 구현 ('혈마법사' & '이단심판관')
> Project Tyrant 기획서 기반, 독립적인 메커니즘을 가진 두 클래스의 완성도 높은 스킬 루프 구현

* 혈마법사 (Blood Mage):
  체력(HP) 소모 기반 리스크-리턴 메커니즘:** 스킬 시전 시 자원으로 체력을 소모하며 화력을 극대화.
  유기적 회복 전투 루프:** 적 피격 시 발생시키는 '출혈' 상태를 활용하여, 지속적인 중거리 전투 속 체력을 흡수하는 사이클 설계.

 * 이단심판관 (Inquisitor):
   신앙심(Faith) 축적 & 방출 메커니즘: 유효 타격을 통해 신앙심을 축적하고, [F: 폭발하는 신념]으로 일시에 방출하여 **자원 비례 보호막 및 광역 기절(Stasis)** 획득.
   근거리 제어 & 공수전환 딜탱: [E: 선고] 및 [W: 방패 돌진]으로 거리를 강제로 좁힌 후, 진형 파괴와 생존력을 동시에 확보하는 선순환 구조 완성.

---

## ⚔️ 전투 및 시스템 구조 (Combat & Technical Details)

** 정교한 판정 및 상호작용 (Physics & Overlap):**
   OverlapSphere 및 Dynamic Raycast 기반의 직관적이고 정확한 타격/피격 범위를 구현하여 액션 타격감 부여.
** 애니메이션 레이어 제어 (UpperBody Layer Weight):**
   이동(LowerBody)과 상체 공격 모션(UpperBody)이 끊김 없이 연계되도록 `Animator Layer Weight`를 상황별로 실시간 동기화.
** 시각적 피드백 & 연출 (VFX & UI):**
   셰이더와 Alpha Fade 연산을 활용하여 이펙트의 범위와 지속 시간을 직관적으로 전달하도록 수치화된 이펙트 연출 적용.
   캐릭터 컨셉과 게임의 톤앤매너에 맞춘 맞춤형 스킬 아이콘 및 비주얼 리소스 배치.

---

## 🖥️ 게임 루프 및 UI (Game Loop)
* **UI Framework 연동:** 인벤토리(아이템 사용 및 관리), Pause 메인 메뉴, Game Over 패널 등 전체 게임 흐름 제어.
