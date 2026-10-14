# =========================================================
# FILE: check-authenticator-code.ps1
#
# PURPOSE:
# Developer check: prints the authenticator codes this PC
# expects for a setup key, to compare with the phone app.
#
# USAGE:
#   powershell -ExecutionPolicy Bypass -File build\scripts\dev\check-authenticator-code.ps1 -Key "ABCD EFGH ..."
#
# IMPORTANT NOTES:
# - Development only. Runs locally, sends nothing anywhere
# - Never use it with a production account's key
# =========================================================
param([Parameter(Mandatory = $true)][string]$Key)

$alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
$b32 = $Key.ToUpper().Replace(' ', '').Replace('-', '').TrimEnd('=')
$bits = ($b32.ToCharArray() | ForEach-Object { [Convert]::ToString($alphabet.IndexOf($_), 2).PadLeft(5, '0') }) -join ''
[byte[]]$secret = for ($i = 0; $i + 8 -le $bits.Length; $i += 8) { [Convert]::ToByte($bits.Substring($i, 8), 2) }
$hmac = New-Object System.Security.Cryptography.HMACSHA1 -ArgumentList (,$secret)
$now = [DateTimeOffset]::UtcNow
$step = [long][Math]::Floor($now.ToUnixTimeSeconds() / 30)

foreach ($offset in -1, 0, 1) {
    $counter = [BitConverter]::GetBytes([long]($step + $offset))
    [Array]::Reverse($counter)
    $h = $hmac.ComputeHash($counter)
    $o = $h[19] -band 15
    $code = ((([int]$h[$o] -band 0x7f) -shl 24) -bor ([int]$h[$o + 1] -shl 16) -bor ([int]$h[$o + 2] -shl 8) -bor [int]$h[$o + 3]) % 1000000
    $label = @{ -1 = 'previous'; 0 = 'NOW     '; 1 = 'next    ' }[$offset]
    '{0}  {1:D6}' -f $label, $code
}

'PC time (UTC): {0:yyyy-MM-dd HH:mm:ss}' -f $now.UtcDateTime
