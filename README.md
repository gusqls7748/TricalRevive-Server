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
| 영속 저장소 | PostgreSQL 16 (Orleans ADO.NET Persistence Provider) |
| 캐시 / 세션 | Redis 7 |
| 인프라(로컬) | Docker / Docker Compose |
| 버전 관리 | Git, GitHub |

> 인프라 자동화(Pulumi, Kubernetes, Helm, GitHub Actions)와 관측성(ELK, Elastic APM)은 로드맵에 포함되어 있으며, 순차적으로 추가할 예정입니다.

## 아키텍처

```
Client (콘솔 테스트 / 추후 확장)
        │
        │  IClusterClient
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
└──────────┬─────────────────┘
           │  참조
           ▼
┌───────────────────────────┐
│ TricalRevive.GrainInterfaces│ ← 클라이언트/서버 공유 계약(인터페이스)
│   - IPlayerGrain             │
└───────────────────────────┘
           │
           ▼
     PostgreSQL (Docker)
     - OrleansStorage (Grain 상태)
     - OrleansMembershipTable (클러스터 멤버십)
```

프로젝트를 3개 계층(GrainInterfaces / Grains / Silo)으로 나눈 이유는 Orleans의 표준 구조를 따르기 위함입니다. 인터페이스와 구현을 분리해두면, 추후 별도의 클라이언트 애플리케이션(예: Blazor 어드민, 게임 클라이언트)이 `GrainInterfaces`만 참조해서 서버 내부 구현에 의존하지 않고 통신할 수 있습니다.

## 구현 완료 기능

- [x] Orleans Silo 로컬 클러스터링 구동 (`UseLocalhostClustering`)
- [x] `PlayerGrain` — 플레이어 단위 골드/보유 캐릭터 관리
- [x] PostgreSQL 기반 Grain 상태 영속화 (`IPersistentState<T>` + ADO.NET Provider)
  - 서버 재시작 후에도 상태가 유지되는 것을 실제로 검증함
- [x] `GachaGrain` — 등급별 확률 뽑기, 천장(pity) 시스템, 10연차 배치 저장
  - `GachaGrain`이 `PlayerGrain`을 호출(cross-grain call)해 골드 차감/캐릭터 지급을 처리
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
│   │   └── IPlayerGrain.cs
│   ├── TricalRevive.Grains/           # Grain 구현체
│   │   ├── PlayerGrain.cs
│   │   └── PlayerState.cs
│   └── TricalRevive.Silo/             # Orleans 서버 호스트
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

### 4. 서버 실행

```bash
dotnet run --project src/TricalRevive.Silo
```

정상적으로 실행되면 다음과 같은 로그와 함께 테스트용 Grain 호출 결과가 출력됩니다.

```
초기 골드: 0
1000골드 지급 후: 1000
보유 캐릭터: 셀렌, 스텔라
```

서버를 껐다가 다시 실행하면 `초기 골드`가 이전에 저장된 값으로 유지되는 것을 확인할 수 있습니다 — 이것이 PostgreSQL 영속화가 정상 동작하고 있다는 증거입니다.

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
[결과](./image.png)


한 번의 10연차 안에서 천장이 두 번 발동했고(테스트를 위해 `PityThreshold`를 임시로 낮춰서 검증), 발동 직후 카운터가 매번 `0`으로 리셋되는 것을 확인했습니다. 최종 카운트가 `0/60`으로 남은 것은 마지막 뽑기(`아리아`)가 천장으로 SSR을 뽑으면서 카운터가 리셋되었기 때문입니다.

## 트러블슈팅 노트

프로젝트를 진행하며 겪었던 문제와 해결 과정을 기록합니다. (신입 개발자로서 문제 해결 과정 자체가 역량을 보여주는 지점이라고 생각해 남겨둡니다.)

- **Orleans ADO.NET Persistence 설정 시 `IPersistentState` 타입을 찾지 못하는 컴파일 에러**
  → `Microsoft.Orleans.Sdk`만으로는 부족했고, `Microsoft.Orleans.Persistence.AdoNet` 패키지를 Grain 구현 프로젝트에도 명시적으로 참조해야 해결됨.
- **PostgreSQL 스키마 미생성으로 인한 Silo 초기화 실패**
  → Orleans는 클러스터 멤버십과 Grain 상태 저장을 위한 전용 테이블 스키마가 필요하며, 공식 SQL 스크립트를 Main → Clustering → Persistence 순서로 실행해야 함.

## 작성자

트릭컬 리바이브 서버 프로그래머 채용에 지원하며, 실제 업무에서 다루게 될 기술 스택을 미리 학습하고자 이 프로젝트를 진행했습니다.