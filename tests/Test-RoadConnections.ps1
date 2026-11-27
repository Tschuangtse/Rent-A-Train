$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot '..\Rent-A-Train\RoadConnectionResolver.cs')
# Use integer IDs as stand-ins for ECS entities; the production code invokes this
# exact graph resolver with the real EntityManager Owner/Attached relationships.
$owners=@{1=2;2=3;4=5;5=4;7=8}
$attachments=@{6=2;7=2}
$lanes=@{3=$true;8=$true}
$has=[Func[int,bool]]{param($id) $lanes.ContainsKey($id)}
$owner=[Func[int,int]]{param($id) if($owners.ContainsKey($id)){$owners[$id]}else{0}}
$attached=[Func[int,int]]{param($id) if($attachments.ContainsKey($id)){$attachments[$id]}else{0}}
function Check($start,$expected,$scenario){
    $actual=[Rent_A_Train.RoadConnectionResolver]::FindLaneSource[int]($start,$has,$owner,$attached)
    if($actual -ne $expected){throw "$scenario : expected $expected, got $actual"}
    Write-Output "PASS: $scenario"
}
Check 1 3 'Road outside marker resolves Owner chain to network lanes'
Check 6 3 'Attached marker resolves onward through Owner'
Check 3 3 'Directly connected outside marker is supported'
Check 4 0 'Cyclic ownership does not hang the simulation'
Check 99 0 'Missing connection returns no source'
Check 7 8 'Multiple relationships preserve a usable ordinary-road source'
