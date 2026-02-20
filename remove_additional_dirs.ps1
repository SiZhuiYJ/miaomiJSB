# 从 Git 中移除应该被忽略的额外目录，但保留本地副本

Write-Host '正在从 Git 暂存区移除应该被忽略的目录...'

git rm -r --cached api/.config 2>$null

git rm -r --cached web/.vscode 2>$null

Write-Host '完成。请提交更改。'
