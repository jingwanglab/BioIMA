# remove-comments.ps1
# 用于批量删除 C# 注释

$files = Get-ChildItem -Path . -Recurse -Include *.cs

foreach ($file in $files) {
    Write-Host "正在处理：$($file.FullName)"

    $content = Get-Content $file.FullName -Raw

    # 删除 /* 多行注释 */
    $content = [regex]::Replace($content, '/\*.*?\*/', '', 'Singleline')

    # 删除 // 单行注释（保留行内代码）
    $content = [regex]::Replace($content, '^\s*//.*$', '', 'Multiline')

    # 删除行末注释（如 int a = 1; // test）
    $content = [regex]::Replace($content, '(.*?)(//.*)', '$1', 'Multiline')

    # 保存修改
    Set-Content -Path $file.FullName -Value $content
}
