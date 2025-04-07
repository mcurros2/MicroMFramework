# Config
$pfxPath = ".\localhost.pfx"
$certPath = ".\localhost.crt"
$keyPath = ".\localhost.key"
$tempPemPath = ".\temp.pem"
$pfxPassword = "test_password"

# Delete existing files
if (Test-Path $certPath) {
    Remove-Item $certPath
}
if (Test-Path $keyPath) {
    Remove-Item $keyPath
}
if (Test-Path $pfxPath) {
    Remove-Item $pfxPath
}
if (Test-Path $tempPemPath) {
    Remove-Item $tempPemPath
}

# Create new dev certificates and trust them
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# Await a few seconds for certificate creation and trust
Start-Sleep -Seconds 5

# Find new certificate thumbprint
$certDetails = dotnet dev-certs https --check --verbose | Select-String -Pattern "(\b[0-9A-F]{40}\b)"
if ($certDetails -and $certDetails.Matches.Count -gt 0) {
    $thumbprint = $certDetails.Matches[0].Value
} else {
    Write-Error "Can't find the thumbprint for the new dev certificate."
    exit 1
}

# Export certificate and key to a .pfx file with password "test_password"
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $thumbprint }
if ($cert) {
    $password = ConvertTo-SecureString -String $pfxPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password
} else {
    Write-Error "Can't find the certificate in the certificate store."
    exit 1
}

# Verify that OpenSSL is installed
if (-not (Get-Command "openssl" -ErrorAction SilentlyContinue)) {
    Write-Error "OpenSSL is not installed. Please install OpenSSL y make sure it's in the PATH."
    exit 1
}

# Convert .pfx a .crt y .key using OpenSSL
& openssl pkcs12 -in $pfxPath -out $tempPemPath -nodes -passin pass:test_password
& openssl rsa -in $tempPemPath -out $keyPath
& openssl x509 -in $tempPemPath -out $certPath

# Delete temp PEM and PFX
if (Test-Path $tempPemPath) {
    Remove-Item $tempPemPath
}
if (Test-Path $pfxPath) {
    Remove-Item $pfxPath
}

Write-Output "Certificate exported and files localhost.crt and localhost.key generated correctly."
