git status

git add .

$title = Read-Host "Commit title"
$body = Read-Host "Commit body"

git commit -m "$title" -m "$body"

git push
