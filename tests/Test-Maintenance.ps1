$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot '..\Rent-A-Train\RentalMaintenancePolicy.cs')
Add-Type -Path (Join-Path $PSScriptRoot '..\Rent-A-Train\RentalAccounting.cs')
function Equal($expected,$actual,$scenario){
    if($expected -ne $actual){throw "$scenario : expected $expected, got $actual"}
    Write-Output "PASS: $scenario"
}
Equal $true ([Rent_A_Train.RentalMaintenancePolicy]::NeedsReplacement($true,$true,$false,$false)) 'Returning maintenance vehicle opens a replacement slot'
Equal $false ([Rent_A_Train.RentalMaintenancePolicy]::NeedsReplacement($false,$true,$false,$false)) 'Vehicle still in service does not open an extra slot'
Equal $false ([Rent_A_Train.RentalMaintenancePolicy]::NeedsReplacement($true,$false,$false,$false)) 'Ordinary retirement does not open a spare slot'
Equal $false ([Rent_A_Train.RentalMaintenancePolicy]::NeedsReplacement($true,$true,$true,$false)) 'Parked vehicle is handled by native maintenance'
Equal $false ([Rent_A_Train.RentalMaintenancePolicy]::NeedsReplacement($true,$true,$false,$true)) 'Dummy outside traffic is ignored'
Equal 11 ([Rent_A_Train.RentalMaintenancePolicy]::DepotCapacity(10,1)) 'Ten rented plus one maintenance return allow ten service vehicles'
Equal 13 ([Rent_A_Train.RentalMaintenancePolicy]::DepotCapacity(10,3)) 'Concurrent maintenance returns each get replacement capacity'
Equal 13 ([Rent_A_Train.RentalMaintenancePolicy]::DepotCapacity(10,3)) 'Repeated updates cannot accumulate replacement slots'
Equal 10 ([Rent_A_Train.RentalMaintenancePolicy]::DepotCapacity(10,0)) 'Reserve disappears when returners leave'
Equal 5 ([Rent_A_Train.RentalMaintenancePolicy]::DepotCapacity(3,2)) 'Reduced contract allows only three service slots plus two returners'
Equal 0 ([Rent_A_Train.RentalMaintenancePolicy]::DepotCapacity(0,10)) 'Ending a rental prevents replacement spawning'
[double]$remainder=0
Equal 50000 ([Rent_A_Train.RentalAccounting]::Accrue(10,5000,262144,[ref]$remainder)) 'Only contracted capacity is charged, regardless of reserve slots'
