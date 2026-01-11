$ProgressPreference = 'SilentlyContinue'
function Build-SubDirProjects {
    param ($DirName)
    Get-ChildItem ./$DirName | Where-Object { Get-ChildItem "$($_.FullName)/*.csproj" } | ForEach-Object {
        dotnet publish -c Release -p:DebugSymbols=false -r win-x64 -o "./out/$DirName/$($_.Name)" $_.FullName || $(exit 1)
    }
}

Remove-Item ./out -Recurse -ErrorAction SilentlyContinue

$dynamicDirs = @("Implementations", "Middlewares", "Extensions")
$dynamicDirs | ForEach-Object { Build-SubDirProjects $_ }
dotnet publish -c Release '-p:DebugSymbols=false' -r win-x64 -o ./out/Robin.App ./Robin.App/Robin.App.csproj
Get-ChildItem ./out/Robin.App/*.dll | ForEach-Object {
    $dllName = $_.Name
    $dynamicDirs | ForEach-Object {
        Get-ChildItem "./out/$_/*/$dllName" | ForEach-Object {
            Write-Host "Removing transitive dependency: $($_.FullName)"
            Remove-Item $_
        }
    }
}

dotnet publish -c Release '-p:DebugSymbols=false;PublishSingleFile=true;EnableCompressionInSingleFile=true' --self-contained false -r win-x64 -o ./out/Robin.App ./Robin.App/Robin.App.csproj
Move-Item ./out/Robin.App/* ./out
Remove-Item ./out/Robin.App -Recurse