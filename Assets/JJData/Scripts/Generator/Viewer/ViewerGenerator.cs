using System.IO;
using System.Text;

public class ViewerGenerator
{
    /// <summary>
    /// Parser Class 제작을 시작한다.
    /// </summary>
    /// <param name="xlsxName">Xlsx 이름</param>
    public void StartViewerGenerate(string xlsxName, string[] sheetNames, string dataClassPath)
    {
        StringBuilder sb = new();
        string className = $"{xlsxName}Viewer";
        BuildClassDefault(className, sb, false); // Sheet 기본 틀 제작
        foreach(string sheetName in sheetNames)
        {
            sb.AppendLine($"    public {xlsxName}_{sheetName}[] {sheetName}DataViewer;");
        }

        sb.AppendLine("    void Start()");
        sb.AppendLine("    {");
        foreach (string sheetName in sheetNames)
        {
            sb.AppendLine($"        {sheetName}DataViewer = {xlsxName}.{sheetName};");
        }
        sb.AppendLine("    }");

        sb.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
        sb.AppendLine("    private static void CreateViewer()");
        sb.AppendLine("    {");
        sb.AppendLine($"        GameObject viewer = new GameObject($\"{xlsxName}Viewer\");");
        sb.AppendLine($"        viewer.AddComponent<{xlsxName}Viewer>();");
        sb.AppendLine("        GameObject.DontDestroyOnLoad(viewer);");
        sb.AppendLine("    }");

        string dataViewerPath = Path.Combine(dataClassPath, xlsxName, "Viewer");
        FinishClass(className, dataViewerPath, sb);
    }

    public void BuildClassDefault(string className, StringBuilder sb, bool isStatic = false)
    {
        sb.AppendLine("using UnityEngine;");
        sb.Append("\n\n\n\n");
        sb.AppendLine($"public {(isStatic ? "static" : "")} class {className} : MonoBehaviour");
        sb.AppendLine("{");
    }

    /// <summary>
    /// Class를 닫고 cs 파일을 생성한다.
    /// </summary>
    public void FinishClass(string className, string path, StringBuilder sb)
    {
        sb.AppendLine("}");

        string filePath = $"{path}/{className}.cs";

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (File.Exists(filePath))
            File.Delete(filePath);

        File.WriteAllText(filePath, sb.ToString());
    }
}