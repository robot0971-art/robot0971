# CSV Format Guide

## CSV 파일 구조

```
Id,Name,Level,Hp
1,Warrior,1,100
2,Mage,1,80
```

## 규칙

### 1. 인코딩
- **UTF-8** (BOM 유무 상관없음)
- Windows 한글 환경: UTF-8 BOM 권장

### 2. 구분자
- 기본값: 쉼표 (`,`)
- 탭 구분: `\t` (TSV)

### 3. 헤더
- 첫 행은 반드시 컬럼명
- 대소문자 구분 없음
- 공백은 자동 트림

### 4. 값 처리

#### 큰따옴표
- 값에 쉼표가 포함된 경우 큰따옴표로 감싸기:
  ```csv
  Id,Description
  1,"Hello, World!"
  ```

#### 이스케이프
- 큰따옴표 자체를 포함하려면 두 번:
  ```csv
  Id,Name
  1,"Say ""Hello"""
  ```
  결과: `Say "Hello"`

#### 빈 값
- 빈 셀은 빈 문자열로 처리
- Nullable 타입은 null로 변환

### 5. 여러 줄 값
- 큰따옴표 안에 개행 가능:
  ```csv
  Id,Description
  1,"First line
  Second line"
  ```

## 예제

### 기본 예제

```csv
Id,Name,Level,Hp
1,Warrior,1,100
2,Mage,1,80
3,Archer,1,90
```

### 큰따옴표 포함

```csv
Id,Title,Description
1,Welcome,"Hello, adventurer!"
2,Quest,"Complete the task, then return."
```

### 여러 줄

```csv
Id,Name,Lore
1,Dragon,"Ancient creature
Living in mountains
Breathes fire"
```

### Nullable 값

```csv
Id,Name,OptionalDescription
1,Item1,Description here
2,Item2,
3,Item3,Another description
```

## CSV 생성 팁

### Excel에서 저장
1. 파일 → 다른 이름으로 저장
2. 파일 형식: `CSV UTF-8 (쉼표로 분리)` 선택
3. 저장

### Google Sheets에서 내보기
1. 파일 → 다운로드 → 쉼표로 분리된 값(.csv)
2. UTF-8 자동 적용됨

### 주의사항
- Excel 한글판의 경우 CSV UTF-8 형식을 선택해야 합니다
- 일반 CSV는 ANSI 인코딩으로 저장되어 한글이 깨질 수 있습니다
