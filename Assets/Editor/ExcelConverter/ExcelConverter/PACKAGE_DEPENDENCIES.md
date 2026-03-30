# Unity Excel Converter - Package Dependencies

## 필수 패키지

### ExcelDataReader (for xlsx support) - 선택사항

CSV 파일만 사용할 경우 ExcelDataReader가 필요 없습니다.
xlsx/xls 파일을 사용하려면 아래 단계를 따르세요.

#### 설치 방법

**방법 A: NuGet (권장)**
1. Visual Studio에서 솔루션 열기
2. NuGet Package Manager에서 설치:
   - `ExcelDataReader`
   - `ExcelDataReader.DataSet` (권장)

**방법 B: DLL 직접 추가**
- [NuGet Gallery - ExcelDataReader](https://www.nuget.org/packages/ExcelDataReader/)에서 DLL 다운로드
- `Assets/Plugins` 폴더에 추가

#### 심볼 정의 (중요!)

ExcelDataReader 설치 후 **EXCELDATAREADER** 심볼을 정의해야 xlsx 지원이 활성화됩니다.

**Unity Editor 설정:**
1. Edit → Project Settings → Player
2. Scripting Define Symbols에 추가:
   ```
   EXCELDATAREADER
   ```
3. Apply 클릭

**또는 asmdef 파일 사용 시:**
- Assembly Definition 파일의 `Define Constraints`에 추가

## 참고
- Unity 6.0+ 권장
- .NET Standard 2.0 이상 필요
- CSV 파일은 별도 패키지 없이 사용 가능
