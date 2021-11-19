param ([Parameter(Mandatory)]$configFile, [Parameter(Mandatory)]$apimresource, [Parameter(Mandatory)]$xmlProcessor)
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
$json.psobject.properties | ForEach-Object {
    $inputDirectory = $_.Name
    $outputDirectory = $_.Value
    Write-Output "Input directory: $inputDirectory"
    Write-Output "Output directory: $outputDirectory"
    Set-Location $inputDirectory
    & $xmlProcessor $inputDirectory $outputDirectory
    $file = Join-Path -Path $outputDirectory -ChildPath "upload-plan.json"
    $jsonXml = (Get-Content -Path $file -Raw) | ConvertFrom-Json

    $jsonXml.psobject.properties | ForEach-Object {
        $childPath = $_.Value + ".xsd"
        $schema = Join-Path -Path $outputDirectory -ChildPath $childPath
        $uri = $apimresource + "/schemas/" + $_.Value + "?api-version=2021-04-01-preview"
        Write-Output "Reading $schema"
        $content = Get-Content -Path $schema -Raw -Encoding utf8
        Write-Output "Updating schema to $uri"
        $res = iwr -UseBasicParsing -Method Put -Headers $headers -Uri $uri -ContentType "application/json; charset=utf-8" `
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
}

Write-Output "Process successfully ended"
#Now we upload the file
#$res = iwr -UseBasicParsing -Method Put -Headers $headers -Uri $uri -ContentType "application/json; charset=utf-8" -Body (@{ "id"="/apis/test2"; "name"="test2" ; "properties" = @{ "format" = "wsdl"; "value" = "$content"; "displayName"="test1"; "protocols"=@("https"); "path"="test2"}; } | ConvertTo-Json)