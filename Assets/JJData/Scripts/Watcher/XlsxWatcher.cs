using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class XlsxFileWatcher
{
    private static FileSystemWatcher fileWatcher;

    static XlsxFileWatcher()
    {
        // 에디터 종료 및 도메인 언로드 시 Dispose 호출 등록
        EditorApplication.quitting += DisposeWatcher;
        AppDomain.CurrentDomain.DomainUnload += (sender, e) => DisposeWatcher();

        // 디폴트값은 미실행
        //StartWatching();
    }

    /// <summary>
    /// 설정한 경로의 xlsx 파일 감시 시작
    /// </summary>
    private static void StartWatching()
    {
        string folderPath = $"{Application.dataPath}/JJData/Xlsx"; // 감시할 폴더 경로
        string fileExtension = "*.xlsx"; // 감시할 파일 확장자

        try
        {
            // 경로 확인 및 생성
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // 기존 감시기가 있으면 정리
            fileWatcher?.Dispose();

            // 새로운 감시기 설정
            fileWatcher = new(folderPath, fileExtension)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName, // 수정 및 파일명 변경 감지
                IncludeSubdirectories = true, // 하위 디렉토리 포함 여부
                EnableRaisingEvents = true // 이벤트 활성화
            };

            // 이벤트 핸들러 등록
            fileWatcher.Changed += OnFileChanged;
            fileWatcher.Created += OnFileAdded;
            fileWatcher.Renamed += OnFileRenamed;
            fileWatcher.Deleted += OnFileDeleted;

            Debug.Log($"XLSX 파일 변경 감시 시작: {folderPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"FileSystemWatcher 초기화 중 오류 발생: {ex.Message}");
        }
    }

    /// <summary>
    /// 파일 변경 이벤트 핸들러
    /// </summary>
    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (FilterTempFile(e.FullPath))
            return;
        Debug.Log($"파일 변경됨: {e.FullPath}");

        XlsxDataManager.RequestFileProcess(e.FullPath);
    }

    /// <summary>
    /// 파일 추가 이벤트 핸들러
    /// </summary>
    private static void OnFileAdded(object sender, FileSystemEventArgs e)
    {
        if (FilterTempFile(e.FullPath))
            return;
        Debug.Log($"파일 추가됨: {e.FullPath}");

        XlsxDataManager.RequestFileProcess(e.FullPath);
    }

    /// <summary>
    /// 파일 이름 변경 이벤트 핸들러
    /// </summary>
    private static void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (FilterTempFile(e.FullPath))
            return;
        Debug.Log($"파일 이름 변경: {e.OldFullPath} -> {e.FullPath}");

        XlsxDataManager.RequestFileProcess(e.FullPath);
    }

    /// <summary>
    /// 파일 삭제 이벤트 핸들러
    /// </summary>
    private static void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (FilterTempFile(e.FullPath))
            return;
        Debug.Log($"파일 삭제됨: {e.FullPath}");
        XlsxDataManager.RequestDeleteProcess(Path.GetFileNameWithoutExtension(e.FullPath));
    }


    private static bool FilterTempFile(string path) => Path.GetFileName(path).StartsWith("~$");

    /// <summary>
    /// 감시기 Dispose 호출
    /// </summary>
    private static void DisposeWatcher()
    {
        try
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false; // 이벤트 비활성화
                fileWatcher.Dispose(); // 리소스 정리
                fileWatcher = null;
                Debug.Log("FileSystemWatcher가 안전하게 종료되었습니다.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Dispose 중 오류 발생: {ex.Message}");
        }
    }

    /// <summary>
    /// 메뉴를 통해 감시기 재시작
    /// </summary>
    [MenuItem("JJData/Watcher/Restart XLSX Watcher")]
    private static void RestartWatcher()
    {
        Debug.Log("XLSX 감시기를 재시작합니다.");
        StartWatching();
    }

    [MenuItem("JJData/Watcher/Stop XLSX Watcher")]
    private static void StopWatcher()
    {
        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false; // 이벤트 비활성화
            fileWatcher.Dispose(); // 리소스 정리
            fileWatcher = null;
            Debug.Log("XLSX 감시가 중지되었습니다.");
        }
    }
}