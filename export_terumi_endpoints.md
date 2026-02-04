# Export All Terumi Endpoints

Run these commands individually to export each Terumi endpoint:

## TerumiFactorData
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiFactorData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiFactorData.json" -Encoding UTF8
```

## TerumiSimpleSkillData
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiSimpleSkillData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiSimpleSkillData.json" -Encoding UTF8
```

## TerumiCharacterData
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiCharacterData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiCharacterData.json" -Encoding UTF8
```

## TerumiSupportCardData
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiSupportCardData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiSupportCardData.json" -Encoding UTF8
```

## TerumiRaceData
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiRaceData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiRaceData.json" -Encoding UTF8
```


## For accessing DB:

docker exec umamusume-db mariadb -uroot -pyourpassword umamusume -e