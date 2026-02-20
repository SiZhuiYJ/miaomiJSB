# 从 Git 中移除应该被忽略的文件，但保留本地副本

Write-Host '正在从 Git 暂存区移除应该被忽略的文件...'

git rm -r --cached web/.vite 2>$null

git rm -r --cached api/bin 2>$null

git rm -r --cached api/obj 2>$null

git rm -r --cached api/obj_build 2>$null

git rm -r --cached api/.vs 2>$null

git rm -r --cached api/errorLog 2>$null

Write-Host '完成。请提交更改。'
