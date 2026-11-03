$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = Join-Path $root 'src/Public/Vixly.Public.Web/Help'
$archive = Join-Path $root 'docs/help/archive/public-v1'
$payload = Get-Content (Join-Path $PSScriptRoot 'article-images.json') -Raw | ConvertFrom-Json -AsHashtable

if (!(Test-Path $archive)) {
    foreach ($file in Get-ChildItem $source -Recurse -Filter *.md) {
        $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
        $target = Join-Path $archive $relative
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        [IO.File]::Copy($file.FullName, $target, $false)
    }
}

foreach ($file in Get-ChildItem $source -Recurse -Filter *.md) {
    $lang = [IO.Path]::GetRelativePath($source, $file.FullName).Split([IO.Path]::DirectorySeparatorChar)[0]
    $i = [array]::IndexOf($payload.languages, $lang)
    $entry = $payload.captions[$file.BaseName]
    if (!$entry) { throw "Missing screenshot mapping: $($file.BaseName)" }
    $text = [IO.File]::ReadAllText($file.FullName)
    $heading = '## ' + $payload.headings[$i]
    $text = ($text -split [regex]::Escape($heading), 2)[0].TrimEnd()
    $text = $text -replace '(?m)^updated: [0-9-]+', 'updated: 2026-10-25'
    if (!$text.Contains('  - build/HelpCapture/capture.cjs')) {
        $text = $text.Replace('updated: 2026-10-25', ('  - build/HelpCapture/capture.cjs' + "`r`n" + '  - src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json' + "`r`n" + 'updated: 2026-10-25'))
    }
    $text = $text -replace '(?i)(complete the human check)(?: if shown)*', '$1 if shown'
    $text = $text -replace '(?i)(effectuez la vérification humaine)(?: si elle est affichée)*', '$1 si elle est affichée'
    $text = $text -replace '(?i)(complete la comprobación humana)(?: si aparece)*', '$1 si aparece'
    $scenes = @($entry.scene)
    if ($entry.ContainsKey('extra')) { $scenes += $entry.extra }
    $images = @()
    foreach ($scene in $scenes) {
        $url = '/help/media/v2/' + $lang + '-' + $scene + '.png'
        $alt = $payload.alts[$i] + ': ' + $payload.sceneNames[$scene][$i]
        $images += '[![' + $alt + '](' + $url + ')](' + $url + ')'
    }
    $text += "`r`n`r`n" + $heading + "`r`n`r`n" + $entry.text[$i] + "`r`n`r`n" + ($images -join "`r`n`r`n") + "`r`n`r`n" + $payload.notes[$i] + "`r`n"
    [IO.File]::WriteAllText($file.FullName, ($text -replace '\r?\n', "`r`n"), [Text.UTF8Encoding]::new($true))
}
Write-Output 'Version 1 archive preserved; all 48 Version 2 articles now have localized screenshot guidance.'
