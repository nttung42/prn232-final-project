#!/bin/bash
# Gắn ảnh minh họa riêng cho từng cơ sở demo: tạo PNG -> upload lên MinIO qua API -> cập nhật imageUrl.
# Dùng: bash scripts/set-facility-images.sh <mật_khẩu_admin>
set -u
API=http://localhost:6789
ADMIN_EMAIL='admin@badminton.com'
ADMIN_PWD="${1:?cần mật khẩu admin}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TMP=$(cygpath -m "$(mktemp -d)" 2>/dev/null || mktemp -d)
IMGDIR="$TMP/img"
mkdir -p "$IMGDIR"

# Danh sách cơ sở demo (lấy từ seed-hanoi-demo.sh để khớp tên và thông tin)
source <(sed -n '/^VENUES=(/,/^)/p' "$ROOT/scripts/seed-hanoi-demo.sh")

JAR="$TMP/admin.jar"; : > "$JAR"
printf '{"email":"%s","password":"%s"}' "$ADMIN_EMAIL" "$ADMIN_PWD" > "$TMP/login.json"
curl -s -c "$JAR" -H 'X-Requested-With: CourtSideWeb' -H 'Content-Type: application/json; charset=utf-8' \
     -X POST "$API/api/auth/login" --data-binary @"$TMP/login.json" -o /dev/null -w "admin login %{http_code}\n"

# 1. Lấy id cơ sở theo tên (trang danh sách admin)
curl -s -b "$JAR" -H 'X-Requested-With: CourtSideWeb' "$API/api/facilities" > "$TMP/list.json"
LIST="$TMP/list.tsv"; : > "$LIST"
i=0
for v in "${VENUES[@]}"; do
  IFS='|' read -r NAME ADDR DIST PHONE NCOURT OPEN CLOSE PIDX PEND <<< "$v"
  FID=$(grep -o "\"id\":[0-9]*,[^}]*\"name\":\"$NAME\"" "$TMP/list.json" | head -1 | grep -o '"id":[0-9]*' | grep -o '[0-9]*')
  if [ -z "$FID" ]; then echo "không tìm thấy: $NAME"; i=$((i+1)); continue; fi
  echo "$FID|$NAME|$DIST|$ADDR|$PHONE|$OPEN|$CLOSE" >> "$LIST"
  i=$((i+1))
done

# 2. Tạo ảnh PNG riêng (id|tên|quận)
cut -d'|' -f1-3 "$LIST" > "$TMP/list-img.tsv"
powershell -NoProfile -ExecutionPolicy Bypass -File "$ROOT/scripts/make-court-images.ps1" -ListFile "$(cygpath -w "$TMP/list-img.tsv")" -OutDir "$(cygpath -w "$IMGDIR")"

# 3. Upload từng ảnh và cập nhật cơ sở
OK=0
while IFS='|' read -r FID NAME DIST ADDR PHONE OPEN CLOSE; do
  F="$IMGDIR/$FID.png"
  [ -f "$F" ] || { echo "thiếu ảnh cho $FID"; continue; }
  URL=$(curl -s -b "$JAR" -H 'X-Requested-With: CourtSideWeb' -F "file=@$F;type=image/png" "$API/api/upload/image" \
        | grep -o '"url":"[^"]*"' | sed 's/"url":"//; s/"$//')
  [ -z "$URL" ] && { echo "upload lỗi $FID"; continue; }
  BODY=$(printf '{"name":"%s","address":"%s","district":"%s","phone":"%s","description":"Cơ sở demo ở %s. Dữ liệu bịa phục vụ dự án local.","imageUrl":"%s","openTime":"%02d:00:00","closeTime":"%02d:00:00"}' \
    "$NAME" "$ADDR" "$DIST" "$PHONE" "$DIST" "$URL" "$OPEN" "$CLOSE")
  printf '%s' "$BODY" > "$TMP/upd.json"
  CODE=$(curl -s -b "$JAR" -c "$JAR" -H 'X-Requested-With: CourtSideWeb' -H 'Content-Type: application/json; charset=utf-8' \
         -X PUT "$API/api/facilities/$FID" --data-binary @"$TMP/upd.json" -o /dev/null -w '%{http_code}')
  [ "$CODE" = "200" ] && OK=$((OK+1)) || echo "cập nhật lỗi $FID: $CODE"
done < "$LIST"
echo "đã gắn ảnh cho $OK cơ sở"
rm -rf "$TMP"
