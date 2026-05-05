# Under-Taker 구현 현황 v0.3

**작성**: 2026-04-22
**기준**: 옵션 γ 완료 직후 (시나리오 α 시작 시점)
**이 문서의 역할**: 설계 문서 v0.2 와 **실제 구현 상태**의 갭을 단일 진실 공급원으로 기록. 세션 시작 전 참조용.

---

## 0. 한줄 요약

> 기획 완성 + A(스캐폴딩) + B(시스템 구현 핵심) + 옵션 γ(7종 유물 수직 슬라이스) 완료. 시나리오 α 에서 마이그레이션·각성·툴링을 병렬 확장 중.

---

## 1. 현재 엔진·프로젝트 상태

- **엔진**: Unity 6000.3.10f1, URP 2D
- **언어**: C# (Unity 6 기본)
- **프로젝트 루트**: `C:\Users\wjp22\ProjectB`
- **핵심 Asset 폴더**: `Assets/Scripts/{Core, Loop, Utilities, Skills, Skills/Abilities, Skills/VFX, Enemies, State, Upgrades, UI}`, `Assets/Editor`, `Assets/Resources/Data/Relics`, `Assets/Scenes`

---

## 2. 설계 v0.2 ↔ 실제 구현 매핑

| 설계 v0.2 결정 | 실제 구현 상태 | 비고 |
|--------------|------------|------|
| 선분 교차 판정 CCW Cross Product | ✅ `LoopDetector.TryGetIntersection` | 인접 3개 스킵 포함 |
| Ring Buffer 256 포인트 상한 | 🟡 **256 (코드 기본값)** / 씬 저장값 500 | 사용자 Unity 에디터에서 `SampleScene.unity` 수동 변경 필요 |
| 루프 완성 시 궤적 처리 Option B (교차점 이후 유지) | ❌ **사용자 결정으로 교차점 이전 유지(원래 구현)** | v0.2 문서와 불일치. 이력: `feedback_design_vs_code.md` 참조. v0.2 문서는 갱신하지 않고 보존 |
| ITimeService 인터페이스 | 🟡 **정적 클래스 `GameTime` 유지** + IsPaused / OnPaused / OnResumed 이벤트 추가 | 인터페이스 래핑 기각. 정적이 OK |
| Decorator Pattern 유물 시스템 | 🟡 **대체 구현: AbilityBase 추상 클래스 + IRelicAbility 인터페이스** | Decorator 대신 전략 패턴 + VFX 자동 호출 베이스. 동일 목표(코어 수정 없이 확장) 달성 |
| OnLoopCompleted 페이로드 | ✅ `OnLoopCompleted(Vector2[] loopPoints, IReadOnlyList<EnemyBase> purified)` | 옵션 γ 청크에서 확장 완료 |
| RelicDataImporter (CSV → SO) | ✅ `Assets/Editor/RelicDataCsvTool.cs` | Import/Export 모두 구현 |
| 이벤트 버스 | 🔴 **미구현** (대신 직접 참조 + Instance 싱글톤 사용) | 규모상 지금은 문제 없음. 필요 시 도입 |
| GPU 인스턴싱 + ObjectPool | 🟡 `ObjectPool` 유틸 존재 (`Assets/Scripts/Utilities/ObjectPool.cs`). `LoopColliderPool` 구현됨. BurnZone 은 Instantiate/Destroy (풀 없음) | 최적화 필요 시 전환 |

---

## 3. 구현된 유물 현황 (20종 + 각성 2종 + Obsolete 1종)

### 옵션 γ 7종 (✅ 실구현 완료, 2026-04-22)
| 유물 | 계열 | 상태 | 특이사항 |
|------|------|------|--------|
| MagneticTail | Trail | ✅ 실동작 | 시작점 근접 시 자동 루프 완성. AbilityBase 이관 완료 |
| AfterimageLengthen | Trail | ✅ 실동작 | 원본 time 저장으로 누적 버그 수정. AbilityBase 이관 완료 |
| GhostThread | Trail | ✅ 실동작 | 기존 완성 유지 (AbilityBase 이관 상태는 세션 A 검증 필요) |
| ChainWave | Explosion | ✅ 실동작 | purified 순회 + OverlapCircle + ApplyKnockback |
| SoulBurn | Explosion | ✅ 실동작 | BurnZone.cs 동적 생성 (Assets/Scripts/Skills/) |
| SoulReaper | Explosion | ✅ 실동작 | ExpManager 보너스 EXP |
| DataShield | Core | ✅ 실동작 | PlayerHealth ↔ RelicInventory.GetAbility 연결 |

### 옵션 γ 외 13종 (🔴 스텁/부분)
| 유물 | 계열 | 현재 상태 | 필요 작업 |
|------|------|--------|--------|
| LaserTrail | Trail | 🔴 스텁 | OnCoreMove BoxCast 구현 |
| VenomTrail | Trail | 🔴 스텁 | VenomCloud 오브젝트 + 풀 |
| ThornTrail | Trail | 🔴 스텁 | OverlapCapsule + 쿨다운 |
| GravityWell | Explosion | 🔴 스텁 | OverlapCircle + AddForce (ApplyKnockback 이미 있음) |
| ChainReaction | Explosion | 🔴 스텁 | Random 판정 + 2차 폭발 |
| HolyFlash | Explosion | 🔴 스텁 | EnemyBase.ApplyStun 추가 필요 |
| VampiricLoop | Explosion | 🔴 스텁 | PlayerHealth.Heal 연결 (이미 있음) |
| FrostNova | Explosion | 🔴 스텁 | EnemyBase.ApplyFreeze 추가 필요 |
| OverloadEngine | Core | 🟡 부분 | 속도 적용 OK, 조향 민감도 미적용 |
| NanoRepair | Core | 🟡 부분 | PlayerHealth.Heal 호출만 추가하면 끝 |
| PhaseShift | Core | 🟡 부분 | CoreController.InstantMove 추가 필요 |
| EMPPulse | Core | 🟡 부분 | 투사체 파괴 로직 필요 (투사체 시스템 전제) |
| HeavyEngine | Core | 🟡 부분 | LoopDetector.SetDamageMultiplier 추가 필요 |

### 각성 2종 (🔴 스텁, 세션 C 담당)
- ElectricJudgment (Trail 3+ 시너지) — 타이머만 있고 실제 전격 데미지 미구현
- IcingZone (Explosion 3+ 시너지) — 타이머만 있고 IceZone 생성/슬로우 미구현
- **Core 계열 각성은 SynergyType enum 에 아직 추가 안 됨 (TODO)**

### Obsolete
- ElectricTrailAbility — 하위호환용. [Obsolete] 유지. 건드리지 않음

---

## 4. 블로커 해소 이력 (옵션 γ 과정)

2026-04-22 에 아래 3개 블로커 해소됨:

1. **정화 적 목록 전달 체인**: `LoopColliderPool.SpawnLoop` → `IReadOnlyList<EnemyBase>` 반환. 체인 전체(`LoopColliderInstance.Activate` → `LoopDetector.TriggerLoop` → `CoreController.NotifyLoopCompleted` → `IRelicAbility.OnLoopCompleted`) 시그니처 확장.
2. **`EnemyBase.ApplyKnockback(Vector2 dir, float force)`**: 신규 추가. 폭발 계열 유물이 사용.
3. **`PlayerHealth`**: 신규 생성 (`Assets/Scripts/Core/PlayerHealth.cs`). HP/TakeDamage/Heal + DataShield·PhaseShift·NanoRepair 훅 포함. `RelicInventory.GetAbility<T>()` 로 유물 조회.

---

## 5. 현재 진행 중 (시나리오 α)

| 트랙 | 설명 | 도구 | 상태 |
|------|------|------|------|
| **A** | AbilityBase 마이그레이션 검증 + 미완료 파일 변환 (23개 중) | 🟣 Cowork (system-architect) | ⬜ 대기 |
| **C** | 각성 2종 실동작 구현 (ElectricJudgment, IcingZone) + IceZone.cs 신규 + EnemyBase.AddSlow | 🟣 Cowork (physics-programmer) | ⬜ 대기 |
| **G** | 밸런스 시뮬레이터 에디터 윈도우 (`Assets/Editor/BalanceSimulator.cs`) | 🟣 Cowork (data-tooling) | ⬜ 대기 |
| **K** | 구현 현황 v0.3 문서 (이 문서) + Cowork 브리프 | 🔵 Claude Code | ✅ 이 커밋 |

**브리프 위치**: `under-taker-docs/planning/cowork-session-briefs-v0.1.md`

---

## 6. 남은 주요 작업 (시나리오 α 이후 예상)

- **B**: 옵션 γ 외 13종 유물 구현 (10~15h)
- **D**: 적 탄환 + 원거리 적 시스템 → DataShield 실전 검증 (5~7h)
- **E**: RelicSelectionController 카드 UI 개선 (Feel Target 라운드 3) (4~6h)
- **F**: 유물별 VFX 스펙·프리팹 (8~12h, 사람 작업 병행)
- **H**: 엘리트·보스 적 AI 확장 (6~10h)
- **I**: 메타 진행 (영혼의 제단) 연결 (4~6h)
- **J**: 사운드 통합 (3~5h)

---

## 7. 알려진 리스크 / 미결

- **Core 계열 각성 미정의**: `SynergyType` enum 에 Core 각성 타입 없음 (현재 Trail/Explosion만). 6개 Core 유물 완성 후 각성 기획 재정리 필요.
- **적 탄환 시스템 부재**: DataShield / PhaseShift 의 완전 검증이 막혀있음. 트랙 D 선행 시 해소.
- **파일명-enum 불일치**: `Assets/Resources/Data/Relics/RelicData_ChainWave.asset` 내용이 LaserTrail 등. 런타임 작동에는 영향 없지만 관리 혼란. 사용자가 Unity 에디터에서 수동 rename 권장.
- **메모리**: `.claude/projects/.../memory/feedback_design_vs_code.md` 에 "설계-코드 갭 자동 리팩터링 금지" 규칙 기록됨. 설계 변경 제안 시 두 옵션 제시 필수.

---

## 8. 다음 체크포인트

- Cowork A/C/G 완료 보고 후 → 통합 머지 및 컴파일 검증
- 통합 플레이테스트 (option-gamma-playtest-checklist.md 활용)
- 피드백 기반 트랙 B/D/E 중 우선순위 결정 (시나리오 β 또는 γ 선택)
