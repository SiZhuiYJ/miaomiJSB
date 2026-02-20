@echo off

echo 正在从 Git 暂存区移除应该被忽略的文件...

git rm -r --cached web/.vite >NUL 2>NUL

git rm -r --cached api/bin >NUL 2>NUL

git rm -r --cached api/obj >NUL 2>NUL

git rm -r --cached api/obj_build >NUL 2>NUL

git rm -r --cached api/.vs >NUL 2>NUL

git rm -r --cached api/errorLog >NUL 2>NUL

echo 完成。请提交更改。

