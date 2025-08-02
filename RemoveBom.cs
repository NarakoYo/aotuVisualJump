using System;
using System.IO;
using System.Text;

class RemoveBom
{
    static void Main(string[] args)
    {
        string filePath = "src\\ImageRecognitionApp\\Localization\\ExcelConfig\\localization.lua";
        
        // 读取文件内容
        byte[] bytes = File.ReadAllBytes(filePath);
        
        // 检查是否有BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            // 移除BOM
            byte[] newBytes = new byte[bytes.Length - 3];
            Array.Copy(bytes, 3, newBytes, 0, newBytes.Length);
            
            // 写入不带BOM的文件
            File.WriteAllBytes(filePath, newBytes);
            Console.WriteLine("已成功移除UTF-8 BOM");
        }
        else
        {
            Console.WriteLine("文件没有UTF-8 BOM");
        }
    }
}