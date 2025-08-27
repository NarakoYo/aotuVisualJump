$filePath = 'src\ImageRecognitionApp\Localization\ExcelConfig\localization.lua'
$bytes = [System.IO.File]::ReadAllBytes($filePath)
if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
{
    $newBytes = New-Object byte[] ($bytes.Length - 3)
    [System.Array]::Copy($bytes, 3, $newBytes, 0, $newBytes.Length)
    [System.IO.File]::WriteAllBytes($filePath, $newBytes)
    Write-Host '已成功移除UTF-8 BOM'
}
else
{
    Write-Host '文件没有UTF-8 BOM'
}