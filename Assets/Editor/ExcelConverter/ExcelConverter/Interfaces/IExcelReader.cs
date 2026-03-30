using System.Collections.Generic;

namespace ExcelConverter.Interfaces
{
    /// <summary>
    /// Excel/CSV 파일을 읽기 위한 인터페이스입니다.
    /// </summary>
    public interface IExcelReader
    {
        /// <summary>
        /// 파일에서 모든 시트 이름을 가져옵니다.
        /// </summary>
        IEnumerable<string> GetSheetNames();

        /// <summary>
        /// 특정 시트의 데이터를 읽어옵니다.
        /// </summary>
        /// <param name="sheetName">시트 이름 (null이면 첫 번째 시트)</param>
        /// <returns>각 행의 데이터 (첫 행은 헤더)</returns>
        IEnumerable<Dictionary<string, string>> ReadSheet(string sheetName = null);

        /// <summary>
        /// 파일을 닫습니다.
        /// </summary>
        void Close();
    }
}
