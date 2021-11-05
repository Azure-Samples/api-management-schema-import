param ([Parameter(Mandatory)]$file, [Parameter(Mandatory)]$apimresource)
az login
Write-Output "Starting the process"
$ErrorActionPreference = "Stop"
$token = az account get-access-token | ConvertFrom-Json
$bearer =$token.accessToken
$headers = @{Authorization = "Bearer $bearer"}

$directoryPath = Split-Path -Path $file
Write-Output "Reading file $file"
$json = (Get-Content -Path $file -Raw) | ConvertFrom-Json

$json.psobject.properties | ForEach-Object {
    $childPath = $_.Value + ".xsd"
    $schema = Join-Path -Path $directoryPath -ChildPath $childPath
    $uri = $apimresource + "/schemas/" + $_.Value + "?api-version=2021-04-01-preview"
    Write-Output "Reading $schema"
    $content = Get-Content -Path $schema -Raw
    Write-Output "Updating schema to $uri"
    $res = iwr -UseBasicParsing -Method Put -Headers $headers -Uri $uri -ContentType "application/json" `
 -Body (@{ `
             "properties" = @{ `
                 "schemaType" = "xml"; `
                 "value" = "$content"; `
             }; `
         } | ConvertTo-Json)
    if (($res.StatusCode -ne 200) -and ($res.StatusCode -ne 201) -and ($res.StatusCode -ne 202))
    {
        Throw "Request returned with HTTP code $req.StatusCode"
    }
}

Write-Output "Process successfully ended"