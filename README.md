# TricalRevive Server

> 트릭컬 리바이브(Tricaltale) 채용 공고의 기술 스택을 기반으로, 수집형 서브컬처 RPG의 서버 아키텍처를 학습 및 실습하기 위해 만든 개인 포트폴리오 프로젝트입니다.
> Microsoft Orleans의 Virtual Actor 모델을 활용해 플레이어 단위 상태를 관리하고, PostgreSQL로 영속화하는 서버를 처음부터 직접 구축했습니다.

## 왜 이 프로젝트를 만들었나

수집형 게임 서버는 "플레이어 한 명의 상태(재화, 인벤토리, 파티 등)를 어떻게 안전하고 확장 가능하게 관리할 것인가"가 핵심 과제입니다. Orleans의 Virtual Actor 모델은 **"플레이어 1명 = Grain(액터) 1개"**로 자연스럽게 매핑되고, 동일 Grain에 대한 호출을 런타임이 자동으로 순차 처리해주기 때문에 별도의 락(lock) 없이도 동시성 문제를 해결할 수 있습니다. 이 프로젝트는 이 구조를 직접 구현하며 검증하는 것을 목표로 합니다.

## 기술 스택

| 분류 | 기술 |
|---|---|
| 런타임 | .NET 10.0 |
| 액터 프레임워크 | Microsoft Orleans 10.2.1 |
| API 계층 | ASP.NET Core Minimal API, OpenAPI |
| 영속 저장소 | PostgreSQL 16 (Orleans ADO.NET Persistence Provider) |
| 캐시 / 세션 | Redis 7 |
| 인프라(로컬) | Docker / Docker Compose |
| 버전 관리 | Git, GitHub |

> 인프라 자동화(Pulumi, Kubernetes, Helm, GitHub Actions)와 관측성(ELK, Elastic APM)은 로드맵에 포함되어 있으며, 순차적으로 추가할 예정입니다.

## 아키텍처

```
브라우저 / Postman / (추후) Blazor 어드민 / 게임 클라이언트
        │
        │  HTTP
        ▼
┌───────────────────────────┐
│   TricalRevive.Api          │  ← ASP.NET Core Minimal API (Orleans 클라이언트)
│   - UseOrleansClient        │     Silo와는 별도 프로세스
└──────────┬─────────────────┘
           │  Orleans 프로토콜 (TCP, 게이트웨이 포트 30000)
           ▼
┌───────────────────────────┐
│   TricalRevive.Silo        │  ← Orleans 서버 프로세스 (Host)
│   - UseLocalhostClustering │
│   - AdoNetGrainStorage     │
└──────────┬─────────────────┘
           │
           ▼
┌───────────────────────────┐
│   TricalRevive.Grains      │  ← Grain 구현체 (실제 로직)
│   - PlayerGrain             │
│   - GachaGrain               │
└──────────┬─────────────────┘
           │  참조
           ▼
┌───────────────────────────┐
│ TricalRevive.GrainInterfaces│ ← Api/Silo/Grains가 공유하는 계약(인터페이스)
│   - IPlayerGrain             │
│   - IGachaGrain               │
└───────────────────────────┘
           │
           ▼
     PostgreSQL (Docker)
     - OrleansStorage (Grain 상태)
     - OrleansMembershipTable (클러스터 멤버십)
```

프로젝트를 4개 계층(GrainInterfaces / Grains / Silo / Api)으로 나눈 이유는 Orleans의 표준 구조를 따르면서, 실제 서비스 환경과 유사하게 **서버(Silo)와 클라이언트(Api)를 별도 프로세스로 분리**하기 위함입니다. `Api`는 `GrainInterfaces`만 참조하고 `Grains`(구현체)는 참조하지 않습니다 — 이렇게 하면 Grain의 실제 로직이 변경되어도 API 계층은 인터페이스 계약이 바뀌지 않는 한 영향을 받지 않습니다. `Api`와 `Silo`는 Orleans 게이트웨이 포트(기본 30000)를 통해 TCP로 통신합니다.

## 구현 완료 기능

- [x] Orleans Silo 로컬 클러스터링 구동 (`UseLocalhostClustering`)
- [x] `PlayerGrain` — 플레이어 단위 골드/보유 캐릭터 관리
- [x] PostgreSQL 기반 Grain 상태 영속화 (`IPersistentState<T>` + ADO.NET Provider)
  - 서버 재시작 후에도 상태가 유지되는 것을 실제로 검증함
- [x] `GachaGrain` — 등급별 확률 뽑기, 천장(pity) 시스템, 10연차 배치 저장
  - `GachaGrain`이 `PlayerGrain`을 호출(cross-grain call)해 골드 차감/캐릭터 지급을 처리
- [x] REST API 계층 (`TricalRevive.Api`) — Silo와 분리된 프로세스로 Orleans 클러스터에 접속
  - 외부(HTTP)에서 PlayerGrain, GachaGrain 호출 가능
- [x] Docker Compose로 PostgreSQL / Redis 로컬 환경 구성

## 앞으로 할 일 (로드맵)

- [ ] TCP 기반 실시간 통신 — 매칭/길드 채팅
- [ ] Redis 연동 — 세션 캐시, 리더보드
- [ ] Blazor 어드민 도구 — 유저 조회, 재화 지급, GM 기능
- [ ] Pulumi + AKS + Helm — 인프라 코드화 및 클러스터 배포
- [ ] GitHub Actions — 빌드/테스트/배포 CI/CD 파이프라인
- [ ] ELK Stack + Elastic APM — 로그 수집 및 분산 트레이싱

## 프로젝트 구조

```
TricalRevive/
├── docker/
│   └── docker-compose.yml          # PostgreSQL, Redis 로컬 컨테이너 정의
├── src/
│   ├── TricalRevive.GrainInterfaces/  # Grain 인터페이스 (계약)
│   │   ├── IPlayerGrain.cs
│   │   ├── IGachaGrain.cs
│   │   ├── CharacterRarity.cs
│   │   └── GachaModels.cs
│   ├── TricalRevive.Grains/           # Grain 구현체
│   │   ├── PlayerGrain.cs
│   │   ├── PlayerState.cs
│   │   ├── GachaGrain.cs
│   │   ├── GachaState.cs
│   │   └── CharacterCatalog.cs
│   ├── TricalRevive.Silo/             # Orleans 서버 호스트
│   │   └── Program.cs
│   └── TricalRevive.Api/              # Orleans 클라이언트 (REST API)
│       └── Program.cs
├── TricalRevive.sln
└── README.md
```

## 로컬 실행 방법

### 1. 사전 요구사항

- .NET 10 SDK
- Docker Desktop

### 2. 인프라 실행

```bash
cd docker
docker compose up -d
```

### 3. PostgreSQL 스키마 초기화 (최초 1회)

Orleans의 공식 ADO.NET 스크립트를 순서대로 실행합니다. (`PostgreSQL-Main.sql` → `PostgreSQL-Clustering.sql` → `PostgreSQL-Persistence.sql`)

```bash
docker exec -it tricalrevive-postgres psql -U tricaladmin -d tricalrevive -f /PostgreSQL-Main.sql
docker exec -it tricalrevive-postgres psql -U tricaladmin -d tricalrevive -f /PostgreSQL-Clustering.sql
docker exec -it tricalrevive-postgres psql -U tricaladmin -d tricalrevive -f /PostgreSQL-Persistence.sql
```

### 4. 서버 실행 (Silo, Api 각각 별도 프로세스)

Silo(Orleans 서버)를 먼저 실행하고, `Orleans Silo started.` 로그가 뜬 뒤 Api(Orleans 클라이언트)를 실행합니다.

```bash
# 터미널 1 — Silo
dotnet run --project src/TricalRevive.Silo

# 터미널 2 — Api
dotnet run --project src/TricalRevive.Api
```

Api가 정상적으로 Silo에 연결되면, Silo 쪽 콘솔에 아래와 같은 게이트웨이 연결 로그가 찍힙니다.

```
Orleans.Runtime.Messaging.Gateway[101301]
      Recorded opened connection from endpoint 127.0.0.1:13716, client ID sys.client/...
```

Api는 기본적으로 `http://localhost:5000`에서 요청을 받습니다.

## API 엔드포인트

| Method | Path | 설명 |
|---|---|---|
| GET | `/players/{playerId}/gold` | 보유 골드 조회 |
| POST | `/players/{playerId}/gold?amount={n}` | 골드 지급/차감 |
| GET | `/players/{playerId}/characters` | 보유 캐릭터 목록 조회 |
| POST | `/gacha/{playerId}/single` | 단발 뽑기 |
| POST | `/gacha/{playerId}/ten` | 10연차 뽑기 |
| GET | `/gacha/{playerId}/pity` | 현재 천장(pity) 카운트 조회 |

### 호출 예시 (PowerShell)

```powershell
# 골드 조회
Invoke-RestMethod http://localhost:5000/players/player-001/gold

# 골드 지급
Invoke-RestMethod -Method Post "http://localhost:5000/players/player-001/gold?amount=5000"

# 단발 뽑기
Invoke-RestMethod -Method Post http://localhost:5000/gacha/player-001/single

# 10연차
Invoke-RestMethod -Method Post http://localhost:5000/gacha/player-001/ten
```

### 실제 호출 결과 (검증)

```
PS> Invoke-RestMethod http://localhost:5000/players/player-001/gold

playerId    gold
--------    ----
player-001 30500

PS> Invoke-RestMethod -Method Post "http://localhost:5000/players/player-001/gold?amount=5000"

playerId    gold
--------    ----
player-001 35500

PS> Invoke-RestMethod -Method Post http://localhost:5000/gacha/player-001/single

characters                                          goldSpent remainingGold
----------                                          --------- -------------
{@{name=나탈리; rarity=0; isPityTriggered=False}}          150         35350
```

`Api` → `Silo`(TCP) → `PostgreSQL`로 이어지는 전체 요청 흐름이 실제로 동작하는 것을 확인했습니다.

## 뽑기 시스템 동작 검증

`GachaGrain`의 천장(pity) 시스템이 실제로 발동하고, 발동 후 카운터가 정상적으로 리셋되는지 실제 실행 로그로 검증했습니다.

```
=== 뽑기 시스템 테스트 ===
뽑기 전 골드: 32000
단발 뽑기 결과: 셀레스 (SSR)
남은 골드: 31850

10연차 결과:
  - 에스텔 (R)
  - 클로이 (R)
  - 나탈리 (R)
  - 에스텔 (R)
  - 루미엘 (SSR) [천장 발동!]
  - 페넬로프 (SR)
  - 에스텔 (R)
  - 나탈리 (R)
  - 이졸데 (SR)
  - 아리아 (SSR) [천장 발동!]
소모 골드: 1350, 남은 골드: 30500

현재 천장 카운트: 0/60
```

한 번의 10연차 안에서 천장이 두 번 발동했고(테스트를 위해 `PityThreshold`를 임시로 낮춰서 검증), 발동 직후 카운터가 매번 `0`으로 리셋되는 것을 확인했습니다. 최종 카운트가 `0/60`으로 남은 것은 마지막 뽑기(`아리아`)가 천장으로 SSR을 뽑으면서 카운터가 리셋되었기 때문입니다.

## 트러블슈팅 노트

프로젝트를 진행하며 겪었던 문제와 해결 과정을 기록합니다. (신입 개발자로서 문제 해결 과정 자체가 역량을 보여주는 지점이라고 생각해 남겨둡니다.)

- **Orleans ADO.NET Persistence 설정 시 `IPersistentState` 타입을 찾지 못하는 컴파일 에러**
  → `Microsoft.Orleans.Sdk`만으로는 부족했고, `Microsoft.Orleans.Persistence.AdoNet` 패키지를 Grain 구현 프로젝트에도 명시적으로 참조해야 해결됨.
- **PostgreSQL 스키마 미생성으로 인한 Silo 초기화 실패**
  → Orleans는 클러스터 멤버십과 Grain 상태 저장을 위한 전용 테이블 스키마가 필요하며, 공식 SQL 스크립트를 Main → Clustering → Persistence 순서로 실행해야 함.

## 작성자

트릭컬 리바이브 서버 프로그래머 채용에 지원하며, 실제 업무에서 다루게 될 기술 스택을 미리 학습하고자 이 프로젝트를 진행했습니다.