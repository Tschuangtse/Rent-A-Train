$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '..\Rent-A-Train\RentalAccounting.cs')
function Assert-Equal($expected, $actual, $scenario) {
    if ($expected -ne $actual) { throw "$scenario : expected $expected, got $actual" }
    Write-Output "PASS: $scenario"
}
Assert-Equal 20 ([Rent_A_Train.RentalAccounting]::ClampCapacity(30,20)) 'Contract cannot exceed configured maximum'
Assert-Equal 0 ([Rent_A_Train.RentalAccounting]::ClampCapacity(-1,20)) 'Negative request becomes zero'
Assert-Equal 100 ([Rent_A_Train.RentalAccounting]::ClampCapacity(1000,1000)) 'Prototype hard limit'
[double]$carry = 0
Assert-Equal 15000 ([Rent_A_Train.RentalAccounting]::Accrue(3,5000,262144,[ref]$carry)) 'Three train sets for a full day'
[double]$carry = 0
[long]$charged = 0
for ($i = 0; $i -lt 1024; $i++) { $charged += [Rent_A_Train.RentalAccounting]::Accrue(3,5000,256,[ref]$carry) }
Assert-Equal 15000 $charged 'Small simulation updates equal one daily charge'
Assert-Equal 0 $carry 'No accumulated rounding loss after one day'
[double]$carry = 0
$charged = [Rent_A_Train.RentalAccounting]::Accrue(3,1000,131072,[ref]$carry)
$charged += [Rent_A_Train.RentalAccounting]::Accrue(1,2000,131072,[ref]$carry)
Assert-Equal 2500 $charged 'Capacity and price change halfway through a day'
[double]$carry = 0
$charged = [Rent_A_Train.RentalAccounting]::Accrue(1,1,131072,[ref]$carry)
Assert-Equal 0 $charged 'Fractional currency is carried'
# The saved contract serializes this exact value. Simulate its restoration.
[double]$restoredCarry = [double]::Parse($carry.ToString('R',[cultureinfo]::InvariantCulture),[cultureinfo]::InvariantCulture)
$charged += [Rent_A_Train.RentalAccounting]::Accrue(1,1,131072,[ref]$restoredCarry)
Assert-Equal 1 $charged 'Save/load retains fractional daily charges'
[double]$carry = 0
Assert-Equal 0 ([Rent_A_Train.RentalAccounting]::Accrue(0,5000,262144,[ref]$carry)) 'Ended rental costs nothing'
Assert-Equal 0 ([Rent_A_Train.RentalAccounting]::Accrue(20,5000,0,[ref]$carry)) 'Paused simulation costs nothing'
Assert-Equal 0 ([Rent_A_Train.RentalAccounting]::Accrue(20,5000,[uint32]::MaxValue,[ref]$carry)) 'Backward timeline cannot cause an enormous bill'
Write-Output 'All accounting checks passed.'

