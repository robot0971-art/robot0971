# Unity Excel Converter

Excel(xlsx, xls) 및 CSV 파일을 Unity ScriptableObject로 변환하는 모듈입니다.

## 특징

- **자동 매핑**: 필드명과 시트/컬럼명이 자동으로 매핑됩니다 (대소문자 무관)
- **ScriptableObject**: GameData는 ScriptableObject로 저장됩니다
- **Editor 통합**: Unity Editor에서 GUI로 쉽게 변환 가능
- **확장 가능**: 커스텀 타입 파서를 등록하여 새로운 타입 지원 가능
- **다중 포맷**: xlsx, xls, csv 지원

## 설치

### 1. 의존성 설치

ExcelDataReader 패키지를 설치해야 합니다:

**방법 A: NuGet (권장)**
- Visual Studio에서 `ExcelDataReader` 및 `ExcelDataReader.DataSet` 설치
- 또는 NuGet Package Manager 사용

**방법 B: DLL 직접 추가**
1. [NuGet Gallery](https://www.nuget.org/packages/ExcelDataReader/)에서 DLL 다운로드
2. `Assets/Plugins` 폴더에 추가

**방법 C: Unity Package Manager (git URL)**
```json
// Packages/manifest.json
{
  "dependencies": {
    "com.github.excelfdatareader": "https://github.com/ExcelDataReader/ExcelDataReader.git"
  }
}
```

### 2. 스크립트 임포트

이 폴더 전체를 프로젝트의 `Assets/Scripts/ExcelConverter`에 복사하세요.

## 사용 방법

### 1. GameData 정의

```csharp
using UnityEngine;
using ExcelConverter.Attributes;

[CreateAssetMenu(fileName = "GameData", menuName = "Game/GameData")]
public class GameData : ScriptableObject
{
    // 필드명이 시트명과 자동 매핑됨
    public List<CharacterData> Characters;
    public List<ItemData> Items;
    
    // 강제 시트명 지정
    [Sheet("EquipmentTable")]
    public List<EquipmentData> Equipments;
    
    // 변환에서 제외
    [Ignore]
    public int Version;
}
```

### 2. 데이터 클래스 정의

```csharp
using System;
using ExcelConverter.Attributes;

[Serializable]
public class CharacterData
{
    // 필드명이 컬럼명과 자동 매핑됨
    public int Id;
    public string Name;
    public int Level;
    public float Hp;
    
    // 강제 컬럼명 지정
    [Column("CharacterType")]
    public string Type;
    
    // 변환에서 제외
    [Ignore]
    public float CalculatedPower;
}
```

### 3. Excel 파일 구성

**Characters 시트:**
| Id | Name | Level | Hp | CharacterType |
|---|---|---|---|---|
| 1 | Warrior | 1 | 100 | Melee |
| 2 | Mage | 1 | 80 | Ranged |

**Items 시트:**
| Id | Name | Price |
|---|---|---|
| 1 | Sword | 100 |
| 2 | Potion | 50 |

### 4. 변환

**Editor 사용:**
1. Unity 메뉴: `Tools → Excel Converter → Open Window`
2. Excel 파일 선택
3. GameData 스크립트 선택
4. 출력 경로 설정
5. `CONVERT` 클릭

**또는 코드에서:**
```csharp
using ExcelConverter.Core;

var converter = new ExcelConverter<GameData>();
var gameData = converter.Convert("Assets/Data/GameData.xlsx");
```

## 지원 타입

### 기본 타입
- `string`
- `int`, `int?`
- `float`, `float?`
- `double`, `double?`
- `long`, `long?`
- `bool`, `bool?`
- `enum` (문자열 또는 숫자)

### 커스텀 타입

`ICustomTypeParser` 인터페이스를 구현하여 커스텀 타입을 지원할 수 있습니다:

```csharp
public class RangeParser : ICustomTypeParser
{
    public bool CanParse(Type targetType) => targetType == typeof(Range);
    
    public object Parse(string value, Type targetType)
    {
        var parts = value.Split('-');
        return new Range { Min = int.Parse(parts[0]), Max = int.Parse(parts[1]) };
    }
}

// 등록
var converter = new ExcelConverter<GameData>();
converter.RegisterCustomParser(new RangeParser());
```

## 어트리뷰트

### `[Ignore]`
변환에서 해당 필드를 제외합니다.

### `[Column("name")]`
필드를 특정 컬럼명과 강제로 매핑합니다.

### `[Sheet("name")]`
GameData의 List 필드를 특정 시트명과 강제로 매핑합니다.

## CSV 포맷

자세한 내용은 [CSV_FORMAT.md](./CSV_FORMAT.md)를 참조하세요.

## 예외 처리

- `SheetNotFoundException`: 시트를 찾을 수 없음
- `ColumnNotFoundException`: 컬럼을 찾을 수 없음
- `ParseException`: 값 파싱 실패
- `TypeNotSupportedException`: 지원하지 않는 타입

## 라이선스

MIT License
