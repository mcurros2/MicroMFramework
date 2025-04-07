New-SelfSignedCertificate -DnsName "dev.local" -CertStoreLocation "cert:\CurrentUser\My" -TextExtension @("2.5.29.19={text}CA=0")
#New-SelfSignedCertificate -DnsName "localhost", "192.168.3.235"  -CertStoreLocation "cert:\CurrentUser\My" -TextExtension @("2.5.29.19={text}CA=0")

###### add to trusted root certificate
# Set the certificate's subject name
$subjectName = "dev.local"

# Find the certificate in the LocalMachine\My store
$cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object { $_.Subject -eq "CN=$subjectName" }

if ($cert -eq $null) {
    Write-Error "Certificate not found"
    exit
}

# Export the certificate as a .cer file
$exportPath = Join-Path $env:TEMP "$subjectName.cer"
Export-Certificate -Cert $cert -FilePath $exportPath

# Import the .cer file into the LocalMachine\Root store
Import-Certificate -FilePath $exportPath -CertStoreLocation "Cert:\CurrentUser\Root"

# Remove the temporary .cer file
Remove-Item $exportPath

#################

##### export certificate

# Set the certificate's subject name
$subjectName = "dev.local"

# Find the certificate in the CurrentUser\My store
$cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object { $_.Subject -eq "CN=$subjectName" }

if ($cert -eq $null) {
    Write-Error "Certificate not found"
    exit
}

# Export the certificate as a PFX file
$pfxExportPath = Join-Path $env:TEMP "$subjectName.pfx"
$password = ConvertTo-SecureString -String "local_password" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxExportPath -Password $password


####### convert to x509 base 64 for react
openssl pkcs12 -in "$subjectName.pfx" -nokeys -out "$subjectName.crt" -passin pass:local_password
openssl pkcs12 -in "$subjectName.pfx" -nocerts -nodes -out "$subjectName.key" -passin pass:local_password
