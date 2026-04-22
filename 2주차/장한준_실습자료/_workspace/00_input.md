---
status: draft
tags:
  - 내러티브
  - 입력정리
updated: 2026-04-22
---

# 00. 사용자 입력 정리

## 요청 원문
> 우리 게임에서 사용할 NPC의 대사 및 분기를 알려줘. 호감도에 따라서 어떤 대사들과 퀘스트(주로 아이템 찾아주기)를 해줄 지 생각해서 문서화해줘.

## 해석된 요구사항

| 항목 | 값 |
|---|---|
| 장르 | 포스트 아포칼립스 생존/공동체 재건 (Project PA) |
| 세계관 | 기존 `기획 WIKI/세계관 설명.md` 활용 — 대범람 이후 수몰된 지구, 엑소더스 배신 서사 |
| 집중 설계 영역 | **NPC 대사 + 호감도 분기 + 아이템 찾아주기 퀘스트** |
| 대사 스키마 | 기존 `FDialogueRow` 준수 (RowName=`NPC_0000`, TalkerName, DialogueText, CharacterMood, CameraEffect, NextDialogueID, Choices) |
| 호감도 티어 | 기존 선물 시스템 티어(Hate / Dislike / Neutral / Like / Love)와 연동되는 **누적 호감도 구간** 5단계 |
| NPC 수 | 4명 (리오 / 사라 / 한 / 도슨) |

## 실행 모드
**퀘스트 + 대사 + 분기 통합 모드** — 기존 세계관은 확정이므로 worldbuilder 스킵.

투입 에이전트: `quest-designer`, `dialogue-writer`(병렬), `branch-architect`(병렬), `narrative-reviewer`.

## NPC 개요 (요약 — 상세는 01 문서에)

| NPC | 전문 | 초기 감정 | 핵심 상처 | 영입 가치 |
|---|---|---|---|---|
| 리오 (Leo) | 정수 시스템 전문가 | 불신 | 엑소더스 최종 명단 탈락 | 식수·오수 정화, 해수 담수화 |
| 사라 (Sara) | 기상 제어기 수리공 | 경계 | 어린 동생을 폭풍으로 잃음 | 차폐막·기상 예보기 정비 |
| 한 (Han) | 고성능 엔진 설계자 | 분노 | 엑소더스 팀 동료였으나 버림받음 | 발전기·탈것 엔진 개조 |
| 도슨 (Dawson) | 유랑 상인·정보 브로커 | 중립 | 전 공동체가 내부 분열로 붕괴 | 희귀 아이템, 타 거점 정보 |

## 호감도 구간 (누적 점수 기반)

| 단계 | 점수 구간 | 명칭 | 해금 |
|---|---|---|---|
| T0 | 0–24 | 경계 (Wary) | 기본 인사, 거래 차단 |
| T1 | 25–49 | 수용 (Tolerant) | 아이템 퀘스트 1 해금, 기본 거래 |
| T2 | 50–74 | 협력 (Cooperative) | 아이템 퀘스트 2 해금, 전문 지식 단편 공개 |
| T3 | 75–99 | 신뢰 (Trusting) | 아이템 퀘스트 3 해금, 거점 영입 가능 |
| T4 | 100+ | 결속 (Bonded) | 개인 엔딩 퀘스트, 영구 버프 |

호감도는 선물(기존 시스템)과 퀘스트 완료 보상으로 누적된다. 특정 선택지/세계관 관련 발언은 호감도 감소도 유발한다.

## 기존 자원 참조

- `기획 WIKI/세계관 설명.md` — 세계관 원본
- `기획 WIKI/NPC 대화 시스템 가이드 (기획자용).md` — 대화 스키마
- `기획 WIKI/기능 WIKI/NPC 선물 기능.md` — 호감도/티어 수치
- `기획 WIKI/기능 WIKI/거점 에너지 시스템.md` — 아이템 퀘스트 연계 가능 시스템

## 산출물 매핑

| 산출물 | 담당 | 파일 |
|---|---|---|
| 세계관 요약 (참조용) | orchestrator | `01_worldbuilding.md` |
| 퀘스트 설계 | quest-designer | `02_quest_design.md` |
| 대사 스크립트 | dialogue-writer | `03_dialogue_script.md` |
| 분기 구조도 | branch-architect | `04_branch_map.md` |
| 리뷰 보고서 | narrative-reviewer | `05_review_report.md` |
