# Death Cruise

경일게임아카데미 프로그래밍 3기 · 네트워크 팀 프로젝트 (5인)

> Among Us, Lockdown Protocol을 레퍼런스로 한 **실시간 1인칭 3D 마피아 게임**  
> Photon PUN2 / Voice2 기반 멀티플레이 · 음성채팅 · Firebase 회원 시스템

[![YouTube](https://img.shields.io/badge/YouTube-시연영상-red?logo=youtube)](https://youtu.be/7Ba0twLhmE0)

---

<details>
<summary><b>👤 Ellug (팀장) 핵심 구현 파트</b></summary>

<br>

### 전체 아키텍처 설계 및 GitHub 형상 관리 총괄

---

### 1. DevConsole — 빌드 환경 실시간 디버그 콘솔

> 네트워크 프로젝트 특성상 빌드 환경에서의 실시간 로그 추적 필요성으로 도입

**구현 포인트**

- `Application.logMessageReceivedThreaded` 를 통해 **멀티스레드 로그 전체 수집**
- 멀티 → 싱글스레드 자료구조 접근 시 **`lock` 키워드로 데드락 방지**
- `Dictionary<string, int>` 기반 **중복 로그 Stacking** → UI 갱신 비용 O(1) 유지
- 개발 단계별 **치트 커맨드** 지원 (역할 강제 지정, 스킵 등)
- 앱 종료 시점에 **txt 파일 자동 저장** (`DevConsoleLog_yyyyMMdd_HHmmss.txt`)

**해결한 문제**

| 문제 | 원인 | 해결 |
|------|------|------|
| 데드락 위험 | 백그라운드 스레드 → Unity 단일 스레드 자료구조 접근 | `lock` 키워드로 임계 구역 보호 |
| 대량 로그 병목 | 동일 로그 반복 시 문자열 할당·UI 갱신 급증 | Dictionary Stacking으로 렌더링 통합 |

---

### 2. Firebase Auth + Firestore 회원 시스템

> Realtime DB 대비 GUI 가독성·확장성 우선으로 Firestore 채택

**플로우**

```
Client → Firebase Auth (Email/PW 검증) → FirebaseUser 반환
      → Firestore users/{uuid} 조회/생성 → UserData 로컬 캐싱 → 전역 사용
```

**구현 포인트**

- Auth UUID → Firestore 문서 키로 사용, **닉네임은 DB에서 직접 관리**
- 로그인 성공 시 `UserDataStore`에 캐싱 → 반복 쿼리 없이 전역 참조
- Coroutine + Callback 구조의 **비동기 통신**
- **닉네임 중복 검사** + 유효성 검증 파이프라인 (`IdChecker`, `NicknameChecker` 등)
- win/lose 기록 등 **메타데이터 확장 구조** 설계

---

### 3. Photon PUN2 멀티플레이 환경 설계

**씬 구조**

```
Title → Lobby → Room → InGameLoading → InGame
```

**동기화 전략**

| 데이터 유형 | 방식 |
|-------------|------|
| 위치 / 회전 | PhotonTransformView (Transform 동기화) |
| 애니메이션 | PhotonAnimatorView |
| 단발 액션 (피격·텔레포트·처형) | RPC |
| 상태 플래그 (투표·역할·사망·로딩) | Room / Player Custom Properties |

- **마스터 클라이언트 기반** 역할 배정·승패 판정·투표 중앙 제어
- 마스터 전환 시 복구 로직 + RPC 버퍼 정리로 안정성 확보
- `OnJoinedRoom` 이후 네트워크 오브젝트 초기화 완료를 보장하기 위한 **3초 유예 딜레이** 적용

---

### 4. 미니맵 빌더 (Editor Tool)

> 씬에 배치된 오브젝트 정보만으로 미니맵을 자동 생성하는 에디터 확장 툴

- `Confiner BoxCollider` 기준으로 Wall·Room 프리팹의 **상대 위치·크기 계산**
- 지정한 UI RectTransform 영역 내에 Image 오브젝트 **자동 생성 (Bake)**
- 맵 확장 시 재Bake만으로 즉시 반영되는 **확장성 있는 구조**
- 런타임에서 Confiner ↔ UI 미니맵 좌표 변환으로 **Player Mark 실시간 렌더링**

---

### 5. 동적 조명 최적화 (Lighting System)

> 다수 실시간 라이트로 인한 Shadow Atlas 초과 경고 및 저사양 프레임 드랍 해결

- `LightRegistry` → 씬 내 모든 라이트 중앙 등록 관리
- `LightCullingController` → 플레이어 위치 + 카메라 범위 기준 라이트 활성/비활성
- `LightUpdateScheduler` → 카메라 거리 순 정렬, **최대 N개 그림자만 활성화**
- `PowerSystem` / `BlackoutController` → 전력 차단·정전 연출과 연동

---

### 6. wantsToQuit 기반 종료 처리

> 멀티 클라이언트 테스트 중 발생한 백그라운드 프로세스 잔류·메모리 누적 문제 해결

**디버깅 과정**
1. 작업 관리자에서 Death Cruise.exe 백그라운드 프로세스 다수 잔류 확인
2. Unity Memory Profiler로 Managed 메모리 정상 정리 확인 → Unity 내부 문제 아님
3. Photon 연결 미해제 가설 → Photon 제거 후에도 동일 현상 → 직접 원인 아님
4. URP 관련 가설 → 리소스 제거 후에도 동일 현상 → 직접 원인 아님
5. **`Application.wantsToQuit` 이벤트에서 씬·리소스 명시적 정리 처리 → 해소**

---

### 7. Git 이력 기반 추가 확인 파트 (Ellug 직접 작업)

#### 7-1. 시체 신고·센터콜·투표 파이프라인

- 시체 신고/센터콜 트리거부터 토론·투표·처형까지 전체 흐름 구현
- 엔진 사보타지 진행 중 센터콜 차단, 투표 단계 전환/스킵 예외 처리 보완
- 근거 커밋: [805ab99](https://github.com/Ellug/Project_Pirates/commit/805ab99), [bbf4080](https://github.com/Ellug/Project_Pirates/commit/bbf4080), [dbf1500](https://github.com/Ellug/Project_Pirates/commit/dbf1500), [1b41e97](https://github.com/Ellug/Project_Pirates/commit/1b41e97)

**관련 소스코드**

- `InGame`: [`VoteManager.cs`](Assets/_Scripts/InGame/VoteManager.cs), [`VoteRoomProperties.cs`](Assets/_Scripts/InGame/VoteRoomProperties.cs), [`VoteUI.cs`](Assets/_Scripts/InGame/VoteUI.cs)
- `InteractableObjects`: [`CenterCall.cs`](Assets/_Scripts/InteractableObjects/CenterCall.cs), [`DeadBody.cs`](Assets/_Scripts/InteractableObjects/DeadBody.cs)
- `System`: [`PlayerManager.cs`](Assets/_Scripts/System/PlayerManager.cs)

#### 7-2. 사보타지 시스템 (엔진/정전/텔레포터)

- 엔진 사보타지 동시 상호작용(2인 홀드) 해제 로직 설계 및 동기화
- 정전(Blackout)·엔진·도어락·텔레포터를 하나의 사보타지 흐름으로 연결
- 근거 커밋: [f6436f4](https://github.com/Ellug/Project_Pirates/commit/f6436f4), [cc48b4f](https://github.com/Ellug/Project_Pirates/commit/cc48b4f), [35f564e](https://github.com/Ellug/Project_Pirates/commit/35f564e)

**관련 소스코드**

- `Sabotage`: [`SabotageManager.cs`](Assets/_Scripts/InGame/Sabotage/SabotageManager.cs), [`EngineSabotageManager.cs`](Assets/_Scripts/InGame/Sabotage/EngineSabotageManager.cs), [`EngineSabotageConsole.cs`](Assets/_Scripts/InGame/Sabotage/EngineSabotageConsole.cs), [`MafiaTeleporter.cs`](Assets/_Scripts/InGame/Sabotage/MafiaTeleporter.cs), [`GlobalDoorLockController.cs`](Assets/_Scripts/InGame/Sabotage/GlobalDoorLockController.cs)
- `Light`: [`BlackoutController.cs`](Assets/_Scripts/Light/BlackoutController.cs), [`BlackoutSwitch.cs`](Assets/_Scripts/Light/BlackoutSwitch.cs), [`BlackoutPropertyBinder.cs`](Assets/_Scripts/Light/BlackoutPropertyBinder.cs)

#### 7-3. 옵션 메뉴/입력 연동 및 이탈 처리 보강

- 옵션 패널에서 씬 상태별 이탈 처리(타이틀 종료, 로비 디스커넥트, 룸/인게임 LeaveRoom) 정리
- 디스플레이/오디오 옵션 UI와 저장값 적용 흐름 보강
- 근거 커밋: [ad1a6f3](https://github.com/Ellug/Project_Pirates/commit/ad1a6f3), [e5c0cdb](https://github.com/Ellug/Project_Pirates/commit/e5c0cdb), [b701762](https://github.com/Ellug/Project_Pirates/commit/b701762)

**관련 소스코드**

- `UI`: [`OptionMenuView.cs`](Assets/_Scripts/UI/OptionMenuView.cs)
- `Display/Audio`: [`DisplayOptionsView.cs`](Assets/_Scripts/Display/DisplayOptionsView.cs), [`AudioOptionsView.cs`](Assets/_Scripts/Audio/AudioOptionsView.cs)
- `System`: [`InputManager.cs`](Assets/_Scripts/System/InputManager.cs)

</details>

---

## 프로젝트 소개

**Death Cruise**는 거대한 화물선을 배경으로 한 실시간 1인칭 멀티플레이 심리·추리 게임입니다.  
플레이어는 **시민**과 **마피아** 세력으로 나뉘어 소통, 추리, 기습, 투표를 통해 승리를 쟁취합니다.

| 항목 | 내용 |
|------|------|
| 장르 | 스릴러 / 심리 / 추리 |
| 플랫폼 | PC (Windows) |
| 개발 기간 | 2026.01 ~ 2026.02 |
| 개발 인원 | 5인 팀 프로젝트 |
| Unity 버전 | 6000.2.10f1 (Universal 3D / URP) |

---

## 기술 스택

| 분류 | 기술 |
|------|------|
| 엔진 | Unity 6 (URP) |
| 언어 | C# |
| 네트워크 | Photon PUN2 |
| 음성 채팅 | Photon Voice 2 |
| 인증 / DB | Firebase Auth, Firebase Firestore |
| 스레딩 | C# `lock` 키워드 (Thread-safe 구조) |

---

## 핵심 시스템

- **상태 패턴** 기반 플레이어 행동 로직 (PlayerStateMachine)
- **RPC + Custom Properties** 실시간 동기화 전략
- 세력·직업 분배 및 마스터 클라이언트 기반 중앙 제어
- 레이캐스트 + RPC 기반 상호작용 (공격, 넉백, 처형)
- **호출(CenterCall) 및 투표 시스템** (VoteManager)
- 사보타지 시스템 (엔진 고장, 문 잠금, 텔레포터)
- 8종 미니게임 미션 (수학, 테트리스, 야구, 가챠 등)
- RPC 기반 대기실 채팅 + Photon Voice 음성 채팅

---

## 씬 구조

```
Title → Lobby → Room → InGameLoading → InGame
```

---

## 조작키

| 입력 | 기능 |
|------|------|
| W / A / S / D | 이동 |
| Shift + WASD | 달리기 |
| Ctrl | 앉기 |
| F | 상호작용 |
| 마우스 좌클릭 | 공격 |
| 마우스 우클릭 | 밀치기 |
| Tab | 미니맵 |
| V | PTT 음성 채팅 |
| ` (~) | 보이스 옵션 |
| ESC | 시스템 옵션 |
| F1 | 도움말 |
| 1 / 2 | 아이템 사용 |

---

## 역할 설명

### 시민 (Citizen)
- 맵 곳곳의 **미션을 수행**해 진행도 증가
- **진행도 100% 달성** 시 시민 팀 승리

### 마피아 (Mafia)
- 시민의 미션 수행 **방해**, 사보타지·공격 등 활용
- **모든 시민 처치** 또는 **사보타지 타이머 정지 방해** 시 승리

---

## 시연 영상

https://youtu.be/7Ba0twLhmE0

---

> 실시간 멀티플레이 환경에서의 안정성, 네트워크 구조 이해, 협업을 고려한 시스템 설계에 집중한 프로젝트입니다.
