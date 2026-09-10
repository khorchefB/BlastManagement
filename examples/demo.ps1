$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5080"

$blast = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/blasts" `
    -ContentType "application/json" `
    -Body (@{ name = "B-042" } | ConvertTo-Json)

$holeBody = @{
    name = "H-01"
    position = @{ x = 10.5; y = 20.25; z = -3.0 }
    direction = 180
    inclination = 15
} | ConvertTo-Json -Depth 3

$hole = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/blasts/$($blast.id)/holes" `
    -ContentType "application/json" `
    -Body $holeBody

Invoke-RestMethod `
    -Method Put `
    -Uri "$baseUrl/blasts/$($blast.id)/holes/$($hole.id)/charge"

# Uncomment when BlastRules:RequireReadyToFire is true.
# Invoke-RestMethod `
#     -Method Put `
#     -Uri "$baseUrl/blasts/$($blast.id)/holes/$($hole.id)/ready"

Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/blasts/$($blast.id)/fire"

Write-Host "Projected blast state:"
Invoke-RestMethod -Method Get -Uri "$baseUrl/blasts/$($blast.id)" |
    ConvertTo-Json -Depth 8

Write-Host "Event history:"
Invoke-RestMethod -Method Get -Uri "$baseUrl/blasts/$($blast.id)/history" |
    ConvertTo-Json -Depth 8
