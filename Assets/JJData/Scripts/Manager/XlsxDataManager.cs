using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class XlsxDataManager
{
    private static readonly ConcurrentQueue<string> _fileQueue = new(); // 처리 대기열
    private static readonly ConcurrentDictionary<string, Task> _fileProcessingTasks = new(); // 파일별 처리 상태
    private static readonly ConcurrentQueue<string> _deleteQueue = new(); // 삭제 작업 큐
    private static readonly ConcurrentDictionary<string, Task> _deleteProcessingTasks = new();
    public static string DataClassFilePath = $"{Application.dataPath}/JJData/Data/";
    public static string DataFilePath = $"{Application.dataPath}/Resources/";
    public static string ResourcesFilePath = "JJData/";
    public static string XlsxFilePath = $"{Application.dataPath}/JJData/Xlsx";

    [MenuItem("JJData/Xlsx Data/Parse All Files #J")]
    public static void RequestProcessAllFiles()
    {
        if (Application.isPlaying) return;

        if (!Directory.Exists(XlsxFilePath))
        {
            Debug.LogError($"폴더를 찾을 수 없습니다: {XlsxFilePath}");
            return;
        }

        string[] xlsxFiles = Directory.GetFiles(XlsxFilePath, "*.xlsx", SearchOption.AllDirectories);

        if (xlsxFiles.Length == 0)
        {
            Debug.LogWarning("처리할 XLSX 파일이 없습니다.");
            return;
        }

        foreach (string filePath in xlsxFiles)
        {
            if (FilterTempFile(filePath))
                continue;
            _fileQueue.Enqueue(filePath);
            Debug.Log($"파일 처리 대기열 : 대기열에 추가됨 - {filePath}");
        }

        // 대기열 처리 시작
        ProcessFileQueue();
    }

    private static bool FilterTempFile(string path) => Path.GetFileName(path).StartsWith("~$");

    /// <summary>
    /// XLSX 파일 처리 요청
    /// </summary>
    public static void RequestFileProcess(string filePath)
    {
        // 큐에 파일 추가
        _fileQueue.Enqueue(filePath);
        Debug.Log($"파일 처리 대기열 : 대기열에 추가됨 - {filePath}"); ;

        // 대기열 처리 시작
        ProcessFileQueue();
    }



    private static void ProcessFileQueue()
    {
        if (_fileQueue.IsEmpty)
        {
            EditorApplication.delayCall += () =>
            {
                Debug.Log("파일 처리 대기열 : Queue 처리 끝");
                if (_deleteQueue.IsEmpty)
                    RefreshAssets();
            };
            return;
        }

        if (_fileQueue.TryDequeue(out string filePath))
        {
            // 동일 파일이 처리 중이면 대기
            if (_fileProcessingTasks.TryGetValue(filePath, out Task existingTask))
            { 
                Debug.Log($"파일 처리 대기열 - {Path.GetFileName(filePath)} : 동일 파일 처리 중, 대기...");
                existingTask.ContinueWith(t => ProcessFileQueue()); // 기존 작업 완료 후 대기열 재처리
                return;
            }

            // 처리 시작
            Debug.Log($"파일 처리 대기열 - {Path.GetFileName(filePath)} : 파일 처리 시작");
            Task task = ProcessFile(filePath);

            // 파일별 작업 상태 저장
            _fileProcessingTasks[filePath] = task;

            // 작업 완료 후 상태 제거 및 다음 작업 호출
            task.ContinueWith(t =>
            {
                _fileProcessingTasks.TryRemove(filePath, out _);
                Debug.Log($"파일 처리 대기열 - {Path.GetFileName(filePath)} : 파일 처리 완료");

                // 다음 파일 처리
                ProcessFileQueue();
            });
        }
    }

    /// <summary>
    /// 파일 처리
    /// </summary>
    private static async Task ProcessFile(string filePath)
    {
        string xlsxName = Path.GetFileNameWithoutExtension(filePath);
        int retryCount = 0; // 재시도 횟수
        const int maxRetries = 3; // 최대 재시도 횟수 (0이면 무제한)
        const int retryDelayMs = 500; // 재시도 간격 (밀리초)

        while (true)
        {
            await DeleteOldData(xlsxName);
            Debug.Log($"{xlsxName} : [재시도 {retryCount + 1}] 기존 데이터 전부 삭제 완료...");
            Debug.Log($"{xlsxName} : [재시도 {retryCount + 1}] 새로운 데이터 처리 중...");

            // Processor Part들 생성
            XlsxDataProcessor xlsxDataProcessor = new();
            BinSheetDataGenerator binSheetDataGenerator = new();
            ClassGenerator classGenerator = new();
            ParserGenerator parserGenerator = new();

            try
            {
                await xlsxDataProcessor.ProcessData(
                    filePath,
                    DataClassFilePath,
                    DataFilePath,
                    ResourcesFilePath,
                    classGenerator,
                    binSheetDataGenerator,
                    parserGenerator
                );
                Debug.Log($"{xlsxName} : 데이터 생성 성공");
                break; // 성공 시 반복 종료
            }
            catch (Exception ex)
            {
                retryCount++;
                Debug.LogError($"{xlsxName} : 데이터 생성 실패 - {ex.Message} {ex.StackTrace} {ex.InnerException}");

                if (maxRetries > 0 && retryCount >= maxRetries)
                {
                    Debug.LogError($"{xlsxName} : 데이터 생성 실패 - 최대 재시도 횟수 초과");
                    RequestDeleteProcess(xlsxName);
                    break; // 최대 재시도 횟수 초과 시 종료
                }

                Debug.Log($"{xlsxName} : {retryDelayMs / 1000}초 후 재시도...");
                await Task.Delay(retryDelayMs); // 재시도 대기
            }
        }
    }

    [MenuItem("JJData/Xlsx Data/Refresh Classes #R")]
    public static void RefreshFile()
    {
        if (Application.isPlaying) return;

        Debug.Log("파일 Refresh : 시작");

        string[] xlsxFileNames = Directory.GetFiles(XlsxFilePath, "*.xlsx", SearchOption.AllDirectories)
                                       .Select(Path.GetFileNameWithoutExtension) // 파일 이름만 추출
                                       .ToArray();
        string[] dataClassFileNames = Directory.GetFiles(DataClassFilePath, "*", SearchOption.TopDirectoryOnly)
                                           .Select(Path.GetFileNameWithoutExtension)
                                           .ToArray();

        string[] unmatchedFileNames = dataClassFileNames.Where(fileName => !xlsxFileNames.Contains(fileName)).ToArray();
        List<Task> deleteTask = new();

        foreach (string unmatchedFileName in unmatchedFileNames )
        {
            _deleteQueue.Enqueue(unmatchedFileName);
            Debug.Log($"파일 삭제 대기열 : 대기열에 추가됨 - {unmatchedFileName}");
        }
    }

    /// <summary>
    /// XLSX 파일 처리 요청
    /// </summary>
    public static void RequestDeleteProcess(string xlsxName)
    {
        // 큐에 파일 추가
        _deleteQueue.Enqueue(xlsxName);
        Debug.Log($"파일 삭제 대기열 : 대기열에 추가됨 - {xlsxName}"); ;

        // 대기열 처리 시작
        ProcessDeleteQueue();
    }



    private static void ProcessDeleteQueue()
    {
        if (_deleteQueue.IsEmpty)
        {
            EditorApplication.delayCall += () =>
            {
                Debug.Log("파일 삭제 대기열 : Queue 처리 끝");
                if(_fileQueue.IsEmpty)
                    RefreshAssets();
            };
            return;
        }

        if (_deleteQueue.TryDequeue(out string xlsxName))
        {
            // 동일 파일이 처리 중이면 스킵
            if (_deleteProcessingTasks.TryGetValue(xlsxName, out Task existingTask))
            {
                Debug.Log($"파일 삭제 대기열 - {Path.GetFileName(xlsxName)} : 동일 파일 삭제 중, 대기열 삭제");
                return;
            }

            // 처리 시작
            Debug.Log($"파일 삭제 대기열 - {Path.GetFileName(xlsxName)} : 파일 삭제 시작");
            Task task = DeleteOldData(xlsxName);

            // 파일별 작업 상태 저장
            _deleteProcessingTasks[xlsxName] = task;

            // 작업 완료 후 상태 제거 및 다음 작업 호출
            task.ContinueWith(t =>
            {
                _deleteProcessingTasks.TryRemove(xlsxName, out _);
                Debug.Log($"파일 삭제 대기열 - {Path.GetFileName(xlsxName)} : 파일 삭제 완료");

                // 다음 파일 처리
                ProcessDeleteQueue();
            });
        }
    }


    [MenuItem("JJData/Xlsx Data/Delete All Classes #A")]
    public static async void DeleteAllData()
    {
        if (Application.isPlaying) return;

        Debug.Log("파일 초기화 : 시작");

        await RetryDeleteDirectoryAsync(DataClassFilePath, "DataClassFile");
        await RetryDeleteDirectoryAsync(DataFilePath + ResourcesFilePath, "DataFile");

        Debug.Log("파일 초기화 : 전부 삭제 완료");

        RefreshAssets();
    }

    private static async Task DeleteOldData(string xlsxName)
    {
        string dataClassFilePath = $"{DataClassFilePath}/{xlsxName}";
        string dataFilePath = $"{DataFilePath}{ResourcesFilePath}/{xlsxName}";

        // 비동기로 삭제 작업 실행
        await RetryDeleteDirectoryAsync(dataClassFilePath, "DataClassFile");
        await RetryDeleteDirectoryAsync(dataFilePath, "DataFile");
    }

    private static async Task RetryDeleteDirectoryAsync(string path, string type)
    {
        Debug.Log($"{Path.GetFileName(path)} {type} : 삭제 시작");
        const int retryDelayMs = 500; // 재시도 간격 (밀리초)
        const int maxRetryAttempts = 1; // 0이면 무제한 시도
        int retryCount = 0;

        while (Directory.Exists(path))
        {
            Debug.Log($"{Path.GetFileName(path)} {type} : [삭제 재시도 {retryCount + 1}] 기존 데이터 삭제 중...");

            try
            {
                // 디렉토리 삭제
                Directory.Delete(path, true);

                // 메타 파일 삭제
                string metaFilePath = $"{path}.meta";
                if (File.Exists(metaFilePath))
                    File.Delete(metaFilePath);

                // 삭제 성공 확인
                if (!Directory.Exists(path))
                {
                    Debug.Log($"{Path.GetFileName(path)} {type} :삭제 성공");
                    return; // 성공 시 함수 종료
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Path.GetFileName(path)} {type} : 삭제 중 오류 발생 {ex.Message}");
            }

            // 최대 재시도 횟수 확인
            retryCount++;
            if (maxRetryAttempts > 0 && retryCount >= maxRetryAttempts)
            {
                Debug.LogError($"{Path.GetFileName(path)} {type} : 삭제 실패 - 최대 시도 횟수 초과");
                return;
            }

            // 재시도 대기
            await Task.Delay(retryDelayMs);
        }
    }


    /// <summary>
    /// 에셋 갱신
    /// </summary>
    private static void RefreshAssets()
    {
        Debug.Log("RefreshAssets : AssetDatabase 갱신 시작");
        AssetDatabase.Refresh();
        Debug.Log("RefreshAssets : AssetDatabase 갱신 완료");
    }
}