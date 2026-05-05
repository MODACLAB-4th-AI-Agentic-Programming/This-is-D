# 옵션 γ 통합 플레이 테스트 체크리스트 v0.1

**작성**: 2026-04-22
**범위**: 7종 유물 시너지 테스트 구성 (Trail 3개 + Explosion 3개 + Core 1개)

---

## 0. 선행 작업 (Unity 에디터)

### 0-1. 유물 데이터 Import
1. Unity 에디터 열기
2. 메뉴 `Tools > Undertaker > Relic CSV Tool`
3. **📥 Import from CSV** 클릭
4. `Assets/Resources/Data/Relics/RelicData_OptionGamma.csv` 선택
5. "신규 생성: N개, 갱신: M개" 팝업 확인

### 0-2. 기존 .asset 파일명 불일치 점검 (선택)
이전 진단에서 다음 파일명 불일치가 있었음:
- `RelicData_ChainWave.asset` → 내용은 LaserTrail
- `RelicData_Soulburn.asset` → 내용은 VenomTrail
- `RelicData_OverloadEngine.asset` → 내용은 GhostThread
- `RelicData_Datashield.asset` → 내용은 ThornTrail

Import 후 `relicType` 기준으로 데이터는 맞게 덮어쓰이지만 파일명은 그대로. 원한다면 수동 rename. 기능에는 영향 없음.

### 0-3. 씬 값 점검
- `Assets/Scenes/SampleScene.unity`의 TrailGenerator `_maxPoints`: 500 → 256 수동 변경 권장
- RelicSelectionController에 7종이 후보로 뜨는지 확인 (또는 모든 relicType 후보로)

---

## 1. 기본 동작 테스트

| 항목 | 기대 | 실패 시 의심 |
|------|------|-----------|
| Play 진입 시 컴파일 에러 없음 | Console 청결 | IRelicAbility 시그니처 누락 파일 |
| SampleScene Play | 플레이어 이동 정상 | PlayerHealth 누락? |
| 원을 그려 루프 완성 | 적 처치 + 흡수 이펙트 | LoopColliderPool 배치 누락 |
| Console 로그: `[SoulReaperAbility] Lv.1 — ExpMultiplier=1.2` 등 | 유물 획득 시 레벨 로그 | RelicAbilityFactory switch 누락 |

## 2. 유물별 체감 검증

### 2-1. GhostThread (Trail/Epic) — 수정 없음, 기존 완성
- **기대**: 궤적이 눈에 띄게 오래 유지됨 (TrailRenderer.time × 0.4 Lv1)
- **체크**: 레벨업 시 원본 × 0.2 = 더 오래. 누적 아님.

### 2-2. AfterimageLengthen (Trail/Common) — 누적 버그 수정
- **기대**: 궤적 time 배율 적용. 레벨업 후 누적 없음.
- **회귀 테스트**: Lv1 → Lv2 2회 발동해도 time 비정상적으로 짧아지지 않음.

### 2-3. MagneticTail (Trail/Common) — 신규 구현
- **기대**: 궤적이 4포인트 이상일 때 시작점 근처로 돌아오면 자동 루프 완성
- **체크**:
  - SnapRadius 2.0(Lv1) 이내 접근 → 즉시 루프
  - 판정 1회 후 `_snapLatch` 로 재트리거 방지
  - 루프 완성 후 재무장
- **의심 버그**: `TrailGenerator._minPointDistance` 필터 우회 시 눈에 보이는 점프 가능

### 2-4. ChainWave (Explosion/Rare) — 신규 구현
- **기대**: 정화 적 각각의 위치에서 BlastRadius(3m Lv1) 내 적에게 BlastForce(8 Lv1) 넉백
- **체크**:
  - 적 군집에서 루프 완성 → 정화 외 적들이 방사형으로 튕김
  - 완전 겹침 시 랜덤 방향으로 튕김 (NaN 방지)
- **의심 버그**: Rigidbody2D Dynamic 아니면 AddForce 무반응

### 2-5. SoulBurn (Explosion/Rare) — BurnZone 신규 생성
- **기대**: 루프 중심에 불꽃 존 생성, BurnDuration(3초 Lv1) 동안 DPS(5 Lv1) 지속 적용
- **체크**:
  - 씬 하이어라키에 "BurnZone" 임시 오브젝트 생성됨
  - 3초 후 자동 Destroy
  - 범위는 `max(루프 꼭짓점~중심 거리, 0.5)` 로 추정
  - `GameTime.IsPaused` 시 타이머/데미지 정지

### 2-6. SoulReaper (Explosion/Common) — 신규 구현
- **기대**: 정화된 적 수 × ExpValue × (1.2-1) = 20% 보너스 EXP
- **체크**:
  - 정화 적 0명이면 보너스 0
  - ExpManager 게이지가 정상 EXP 외에 추가로 차오름 (중복 지급 아님)
- **의심 버그**: ExpManager.AddExp 이벤트가 UI와 동기화되는지

### 2-7. DataShield (Core/Epic) — PlayerHealth 연결 완료
- **기대**: 궤적 긋는 중 정면(각도 60도 이내) 탄환 1발 차단 (Lv1)
- **체크**:
  - `PlayerHealth.TakeDamage(damage, hitDir)` 호출 시 DataShield.TryBlock 먼저 시도
  - 차단 성공 시 HP 감소 없음
  - 루프 완성 시 실드 카운트 리셋
- **전제**: 적 탄환 시스템 존재해야 테스트 가능. 없으면 `PlayerHealth.TakeDamage(10, Vector2.up)` 같은 수동 호출로 검증.

## 3. 시너지 (Synergy) 발동 테스트

### 3-1. 궤적 계열 3개 → ElectricJudgment 각성
- MagneticTail + AfterimageLengthen + GhostThread 수집
- **기대**: Console 로그 `[SynergyManager] 각성 발동: ElectricJudgment`
- **주의**: ElectricJudgmentAbility는 현재 스텁 (효과 미구현). 발동 로그만 확인.

### 3-2. 폭발 계열 3개 → IcingZone 각성
- ChainWave + SoulBurn + SoulReaper 수집
- **기대**: `[SynergyManager] 각성 발동: IcingZone`
- **주의**: IcingZoneAbility는 현재 스텁.

### 3-3. 혼합 (시너지 미달)
- Trail 2개 + Core 1개: 각성 발동 X (임계치 미달)

## 4. 알려진 미완성 (이번 청크 범위 밖)

- **각성 능력 실제 로직**: ElectricJudgment / IcingZone 모두 타이머만 있고 데미지·슬로우 적용 없음
- **유물 13종**: LaserTrail, VenomTrail, ThornTrail, GravityWell, ChainReaction, HolyFlash, VampiricLoop, FrostNova, OverloadEngine(부분), NanoRepair(부분), PhaseShift(부분), EMPPulse(부분), HeavyEngine(부분)
- **적 탄환 시스템**: DataShield 완전 검증 불가
- **VFX**: 자기장 폭발, 충격파, 불꽃 존 시각 이펙트 모두 미구현
- **사운드**: 전부 미구현

## 5. 피드백 수집 포맷

플레이테스트 후 다음 형식으로 피드백 정리:

```
### Feel Target 정합도

**좋은 것 (의도대로)**
- (예) 루프 크기 비례 보상 — 크게 그릴수록 정화 범위 큼
- ...

**나쁜 것 (의도 미달)**
- (예) MagneticTail 자동 완성 시 궤적 점프가 부자연스러움
- ...

**어색한 것 (버그 의심)**
- (예) SoulBurn BurnZone이 벽 너머까지 데미지
- ...

### 수치 조정 요구
- MagneticTail SnapRadius 2→3 (너무 타이트함)
- ...
```
