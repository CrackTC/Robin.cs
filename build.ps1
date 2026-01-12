function Remove-OutDir {
    Remove-Item ./out -Recurse -ErrorAction SilentlyContinue
}

function Restore-Project {
    param (
        [Parameter(Mandatory)]
        [string]$ProjectPath
    )

    dotnet restore $ProjectPath || $(exit 1)
}

function Restore-SubDirProjects {
    param (
        [Parameter(Mandatory)]
        [string]$DirName
    )

    Get-ChildItem ./$DirName |
    ForEach-Object { Get-ChildItem "$($_.FullName)/*.csproj" } |
    ForEach-Object { Restore-Project -ProjectPath $_.FullName }
}

function Build-Project {
    param (
        [Parameter(Mandatory)]
        [string]$ProjectPath
    )

    dotnet build -c Release $ProjectPath || $(exit 1)
}

function Publish-Project {
    param (
        [Parameter(Mandatory)]
        [string]$ProjectPath,
        [string]$Rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,
        [Parameter(Mandatory)]
        [string]$OutputDir
    )

    dotnet publish -c Release -p:DebugSymbols=false -r $Rid -o $OutputDir $ProjectPath || $(exit 1)
}

function Publish-SingleFile {
    param (
        [Parameter(Mandatory)]
        [string]$ProjectPath,
        [string]$Rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,
        [Parameter(Mandatory)]
        [string]$OutputDir
    )

    dotnet publish `
        -c Release `
        -p:'DebugSymbols=false;PublishSingleFile=true;EnableCompressionInSingleFile=true' `
        --self-contained false `
        -r $Rid `
        -o $OutputDir `
        $ProjectPath || $(exit 1)
}

function Publish-SubDirProjects {
    param (
        [Parameter(Mandatory)]
        [string]$DirName,
        [string]$Rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
    )

    Get-ChildItem ./$DirName |
    ForEach-Object {
        $ProjectDir = $_.Name
        Get-ChildItem "$($_.FullName)/*.csproj" |
        ForEach-Object {
            Publish-Project -ProjectPath $_.FullName -Rid $Rid -OutputDir "./out/$DirName/$ProjectDir"
        }
    }
}

function Remove-TransitiveDependencies {
    param (
        [Parameter(Mandatory)]
        [string]$DirName
    )

    $baseDirDlls = Get-ChildItem ./out/Robin.App/*.dll | ForEach-Object { $_.Name }

    Get-ChildItem "./out/$DirName/*/*.dll" |
    ForEach-Object {
        if ($baseDirDlls -contains $_.Name) {
            Write-Host "Removing transitive dependency: $($_.FullName)"
            Remove-Item $_
        }
    }
}

function Build-FinalStructure {
    $tempDir = New-TemporaryFile
    Remove-Item $tempDir
    Move-Item ./out/Robin.App $tempDir
    Move-Item $tempDir/* ./out
    Remove-Item $tempDir -Recurse
}

$subDirs = @("Implementations", "Middlewares", "Extensions")

$subDirs |
ForEach-Object {
    $subDir = $_
    $body = { Restore-SubDirProjects -DirName $subDir }.GetNewClosure()
    New-Item -Path Function: -Name "Restore-$subDir" -Value $body -Force
    $body = { Publish-SubDirProjects -DirName $subDir }.GetNewClosure()
    New-Item -Path Function: -Name "Publish-$subDir" -Value $body -Force
} | Out-Null

function Restore-Abstraction { Restore-Project -ProjectPath ./Robin.Abstractions }
function Build-Abstraction { Build-Project -ProjectPath ./Robin.Abstractions }
function Restore-App { Restore-Project -ProjectPath ./Robin.App }
function Publish-App {
    param (
        [string]$Rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
    )
    Publish-Project -ProjectPath ./Robin.App -Rid $Rid -OutputDir ./out/Robin.App
}

function Build-All {
    param (
        [string]$Rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,
        [bool]$PublishSingleFile = $true
    )

    $ProgressPreference = 'SilentlyContinue'

    Remove-OutDir

    Publish-App -Rid $Rid

    $subDirs |
    ForEach-Object {
        Publish-SubDirProjects -Rid $Rid -DirName $_ 
        Remove-TransitiveDependencies -DirName $_
    }

    if ($PublishSingleFile) {
        Publish-SingleFile -Rid $Rid -ProjectPath ./Robin.App -OutputDir ./out/Robin.App
    }

    Build-FinalStructure
}
