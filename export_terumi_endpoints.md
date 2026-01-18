# Export All Terumi Endpoints

Run this command to export all 4 Terumi endpoints to JSON files:

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiFactorData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiFactorData.json" -Encoding UTF8; Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiSimpleSkillData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiSimpleSkillData.json" -Encoding UTF8; Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiCharacterData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiCharacterData.json" -Encoding UTF8; Invoke-RestMethod -Uri "http://localhost:5000/api/TerumiSupportCardData" -Method GET | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\Users\cminh\Desktop\TerumiSupportCardData.json" -Encoding UTF8; Write-Host "All 4 Terumi endpoints exported successfully!"
```

## Exported Files:
- `TerumiFactorData.json`
- `TerumiSimpleSkillData.json`
- `TerumiCharacterData.json`
- `TerumiSupportCardData.json`