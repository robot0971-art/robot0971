# Addressable 패키지 설치 가이드

## 패키지 설치 방법

### 1. Package Manager에서 설치

1. Unity Editor에서 **Window > Package Manager** 선택
2. **Packages: Unity Registry** 선택
3. 검색창에 **"Addressables"** 입력
4. **Addressables** 패키지 선택
5. **Install** 버튼 클릭

### 2. 설치 확인

설치가 완료되면 다음 네임스페이스를 사용할 수 있습니다:
- `UnityEngine.AddressableAssets`
- `UnityEngine.ResourceManagement.AsyncOperations`

## 생성된 파일 구조

```
Assets/
├── Scripts/
│   └── Addressables/
│       ├── AddressableResourceManager.cs    # 싱글톤 리소스 매니저
│       ├── AddressableInstanceTracker.cs    # 인스턴스 트래커
│       ├── GameDataLoader.cs                # 게임 데이터 로더
│       └── SceneLoader.cs                   # 씬 로더
│
├── Scripts/
│   └── GameData/
│       └── Updated/
│           ├── ItemDataAddressable.cs       # 아이템 데이터 (Addressable)
│           ├── BuildingDataAddressable.cs   # 건설물 데이터 (Addressable)
│           ├── MonsterDataAddressable.cs    # 몬스터 데이터 (Addressable)
│           └── WeaponDataAddressable.cs     # 무기 데이터 (Addressable)
│
└── Editor/
    └── AddressableSetupUtility.cs           # Addressable 설정 자동화 도구
```

## 사용 방법

### 1. Addressable 초기 설정

Unity Editor 메뉴에서 실행:
```
Tools > Addressables > Setup Addressable Groups
```

자동으로 생성되는 그룹:
- Items
- Buildings
- Characters
- UI
- Effects
- Audio
- Scenes

### 2. 에셋 자동 등록

```
Tools > Addressables > Auto Assign Addressables
```

다음 폴더의 에셋이 자동으로 등록됩니다:
- `Assets/Prefabs/Items` → Items 그룹
- `Assets/Prefabs/Buildings` → Buildings 그룹
- `Assets/Prefabs/Characters` → Characters 그룹
- `Assets/Prefabs/Enemies` → Characters 그룹
- `Assets/Prefabs/UI` → UI 그룹
- `Assets/Scenes` → Scenes 그룹

### 3. Addressable 빌드

```
Tools > Addressables > Build Addressables
```

또는 Window > Asset Management > Addressables > Groups에서 Build 클릭

## 코드 사용 예시

### 에셋 로드
```csharp
// AddressableResourceManager 사용
var manager = AddressableResourceManager.Instance;

// 아이템 아이콘 로드
var itemData = gameData.GetItem("item_001");
Sprite icon = await itemData.LoadIconAsync();

// 건설물 인스턴스화
var buildingData = gameData.GetBuilding("house_001");
GameObject building = await buildingData.InstantiateAsync(position, rotation);

// 몬스터 스폰
var monsterData = gameData.GetMonster("goblin_001");
GameObject monster = await monsterData.InstantiateAsync(spawnPosition, Quaternion.identity);
```

### 씬 로드
```csharp
// SceneLoader 사용
await SceneLoader.Instance.LoadScene("GameScene");

// 또는 Addressable 씬 로드
await SceneLoader.Instance.LoadAddressableScene(sceneReference);
```

### 데이터 로드
```csharp
// GameDataLoader 사용
var item = await GameDataLoader.Instance.LoadItemData("item_001");
var building = await GameDataLoader.Instance.CreateBuilding("house_001", position);
var monster = await GameDataLoader.Instance.SpawnMonster("goblin_001", spawnPosition);
```

## 주요 특징

### 1. 하위 호환성
- 기존 `string iconPath`와 새로운 `AssetReference` 동시 지원
- Addressable 패키지가 없어도 Resources.Load로 폴백

### 2. 비동기 로드
- 모든 로드 작업은 `async/await` 지원
- 로딩 중 중복 호출 방지

### 3. 캐싱
- 로드된 에셋은 자동 캐싱
- `ReleaseAssets()`로 메모리 해제

### 4. 인스턴스 트래킹
- Addressable로 생성된 오브젝트는 자동 트래킹
- 파괴 시 자동으로 Reference Count 감소

## 문제 해결

### Addressable 네임스페이스를 찾을 수 없음
```
'UnityEngine.AddressableAssets' 네임스페이스를 찾을 수 없습니다.
```
**해결**: Package Manager에서 Addressables 패키지 설치

### AssetReference를 찾을 수 없음
```
'AssetReference' 형식을 찾을 수 없습니다.
```
**해결**: `using UnityEngine.AddressableAssets;` 추가 확인

### 에셋 로드 실패
```
KeyNotFoundException: Key not found in AssetDatabase
```
**해결**: 
1. Addressable Groups에서 에셋 등록 확인
2. Addressables Build 실행
3. Addressable 경로 확인

## 참고 문서

- [Unity Addressables Documentation](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html)
- [Addressable Asset System](https://docs.unity3d.com/Manual/com.unity.addressables.html)
