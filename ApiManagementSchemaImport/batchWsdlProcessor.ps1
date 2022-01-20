param ([Parameter(Mandatory)]$configFile, [Parameter(Mandatory)]$apimresource, [Parameter(Mandatory)]$wsdlProcessor)
#$apimResource = "https://management.apim.net/subscriptions/a200340d-6b82-494d-9dbf-687ba6e33f9e/resourceGroups/devenv/providers/Microsoft.ApiManagement/service/devenv"
#$configFile = ""
Write-Output "Starting Azure login"
az login
Write-Output "Starting the process"
$ErrorActionPreference = "Stop"
$token = az account get-access-token | ConvertFrom-Json
$bearer =$token.accessToken
$headers = @{Authorization = "Bearer $bearer"}

Write-Output "Reading file $configFile"
$json = (Get-Content -Path $configFile -Raw) | ConvertFrom-Json
$counter = 1
$json.psobject.properties | ForEach-Object {
    $directory = $_.Value
    $wsdlFile = $_.Name
    Set-Location $directory
    $fileOutput = "new-" + $wsdlFile
    & $wsdlProcessor $wsdlFile $fileOutput
    $content = Get-Content -Path $fileOutput -Raw -Encoding utf8
    $apiName = "test" + $counter
    Write-Output "Api name: $apiName"
    $uri = "$apimResource/apis/" + $apiName + "?import=true&api-version=2021-01-01-preview"
    Write-Output "Calling $uri"
    $res = iwr -UseBasicParsing -Method Put -Headers $headers -Uri $uri -ContentType "application/json; charset=utf-8" -Body (@{ "id"="/apis/$apiName"; "name"="$apiName" ; "properties" = @{ "format" = "wsdl"; "value" = "$content"; "displayName"="$apiName"; "protocols"=@("https"); "path"="$apiName"}; } | ConvertTo-Json)
    if (($res.StatusCode -ne 200) -and ($res.StatusCode -ne 201) -and ($res.StatusCode -ne 202))
    {
        Throw "Request returned with HTTP code $req.StatusCode"
    }
    $counter = $counter + 1
}

Write-Output "Process successfully ended"
#Now we upload the file
#$res = iwr -UseBasicParsing -Method Put -Headers $headers -Uri $uri -ContentType "application/json; charset=utf-8" -Body (@{ "id"="/apis/test2"; "name"="test2" ; "properties" = @{ "format" = "wsdl"; "value" = "$content"; "displayName"="test1"; "protocols"=@("https"); "path"="test2"}; } | ConvertTo-Json)