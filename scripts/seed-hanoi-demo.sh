#!/bin/bash
# Seed 18 cơ sở demo ở Hà Nội qua API (không xóa dữ liệu). Tên và số điện thoại là dữ liệu bịa cho dự án local.
# Dùng: bash scripts/seed-hanoi-demo.sh <mật_khẩu_admin>
set -u
API=http://localhost:6789
PROV_PWD='Provider@123'
ADMIN_EMAIL='admin@badminton.com'
ADMIN_PWD="${1:?cần mật khẩu admin}"
IMG='https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?q=80&w=1200&auto=format&fit=crop'
# curl trên Windows cần đường dẫn kiểu Windows (C:/...), không dùng /tmp của Git Bash
TMP=$(cygpath -m "$(mktemp -d)" 2>/dev/null || mktemp -d)

# POST/PATCH JSON gửi từ file UTF-8 (tránh lỗi mã hóa tiếng Việt khi truyền qua tham số)
# Dùng: req METHOD URL COOKIE_JAR BODY  -> in ra phản hồi
req() {
  local f="$TMP/body.$RANDOM.json"
  [ -f "$3" ] || : > "$3"   # curl -b lỗi nếu file cookie chưa tồn tại
  printf '%s' "$4" > "$f"
  curl -s -b "$3" -c "$3" -H 'X-Requested-With: CourtSideWeb' -H 'Content-Type: application/json; charset=utf-8' \
       -X "$1" "$2" --data-binary @"$f"
  rm -f "$f"
}

login() { # login EMAIL PASSWORD JAR
  local f="$TMP/login.$RANDOM.json"
  printf '{"email":"%s","password":"%s"}' "$1" "$2" > "$f"
  curl -s -c "$3" -H 'X-Requested-With: CourtSideWeb' -H 'Content-Type: application/json; charset=utf-8' \
       -X POST "$API/api/auth/login" --data-binary @"$f" -o /dev/null -w '%{http_code}'
  rm -f "$f"
}

extract_id() { grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*'; }

# 6 provider (tên bịa)
PROV_NAMES=("Nguyễn Hoàng Nam" "Trần Thu Trang" "Lê Anh Dũng" "Phạm Thị Lan" "Hoàng Văn Thắng" "Vũ Minh Châu")
PROV_MAIL=("hn.provider1@demo.local" "hn.provider2@demo.local" "hn.provider3@demo.local" "hn.provider4@demo.local" "hn.provider5@demo.local" "hn.provider6@demo.local")

# name|address|district|phone|courts|open|close|provider_index|pending
VENUES=(
"Cầu Lông Hồng Hà Sport|12 Trần Quốc Hoàn|Cầu Giấy|0912100101|3|6|22|0|0"
"Sân Cầu Lông Thăng Long Arena|88 Lạc Long Quân|Tây Hồ|0912100102|4|6|22|1|0"
"CLB Cầu Lông Đống Đa Star|25 Tôn Thất Tùng|Đống Đa|0912100103|3|6|23|2|0"
"Cầu Lông Kim Ngưu Center|140 Minh Khai|Hai Bà Trưng|0912100104|2|6|22|3|0"
"Sân Cầu Lông Thanh Xuân Premier|210 Khuất Duy Tiến|Thanh Xuân|0912100105|4|5|22|4|0"
"Cầu Lông Long Biên Sunrise|45 Nguyễn Văn Cừ|Long Biên|0912100106|3|6|22|5|0"
"Cầu Lông Hoàng Mai Eagle|300 Giải Phóng|Hoàng Mai|0912100107|3|6|22|0|0"
"Sân Cầu Lông Mỹ Đình Arena|15 Lê Đức Thọ|Nam Từ Liêm|0912100108|4|6|22|1|0"
"Cầu Lông Hà Đông Smash|52 Quang Trung|Hà Đông|0912100109|2|6|21|2|0"
"Cầu Lông Bắc Từ Liêm Victory|18 Phạm Văn Đồng|Bắc Từ Liêm|0912100110|3|6|22|3|0"
"Cầu Lông Ba Đình Pro|60 Kim Mã|Ba Đình|0912100111|2|6|22|4|0"
"Cầu Lông Hoàn Kiếm Riverside|120 Bà Triệu|Hoàn Kiếm|0912100112|2|6|22|5|0"
"Cầu Lông Thanh Trì Lotus|9 Ngọc Hồi|Thanh Trì|0912100113|3|6|22|0|0"
"Cầu Lông Gia Lâm Phoenix|33 Ngô Gia Tự|Gia Lâm|0912100114|2|6|22|1|0"
"Cầu Lông Đông Anh Champion|5 Cao Lỗ|Đông Anh|0912100115|2|6|22|2|0"
"Cầu Lông Ngọc Hà Đức Thắng|77 Ngọc Hà|Ba Đình|0912100116|3|6|22|3|0"
"Cầu Lông Cầu Diễn Smash Club|28 Phạm Hùng|Nam Từ Liêm|0912100117|2|6|22|4|0"
"Cầu Lông Thanh Xuân Mini Court|80 Nguyễn Trãi|Thanh Xuân|0912100118|2|6|22|5|1"
)

COURT_TYPES=("PVC" "PVC" "Wooden" "Acrylic")
SLOTS=("Ca sáng sớm|6|8|70000" "Ca sáng|8|10|90000" "Ca chiều|15|17|100000" "Ca tối giờ vàng 1|18|20|150000" "Ca tối giờ vàng 2|20|22|140000")

# 1. Đăng ký provider (bỏ qua nếu đã có)
for i in "${!PROV_NAMES[@]}"; do
  out=$(req POST "$API/api/auth/register" "$TMP/reg.jar" \
    "{\"fullName\":\"${PROV_NAMES[$i]}\",\"email\":\"${PROV_MAIL[$i]}\",\"phoneNumber\":\"09900000$((10+i))\",\"password\":\"$PROV_PWD\",\"role\":\"Provider\"}")
  echo "register ${PROV_MAIL[$i]}: $(echo "$out" | grep -o '"success":[a-z]*')"
done
echo "providers registered"

# 2. Đăng nhập từng provider và tạo cơ sở, sân, khung giờ
declare -a JARS
for i in "${!PROV_MAIL[@]}"; do
  JARS[$i]="$TMP/prov$i.jar"
  code=$(login "${PROV_MAIL[$i]}" "$PROV_PWD" "${JARS[$i]}")
  echo "login ${PROV_MAIL[$i]}: $code"
  sleep 8   # giới hạn 10 lần đăng nhập/phút
done

FAC_IDS=()
PENDING_IDS=()
idx=0
for v in "${VENUES[@]}"; do
  IFS='|' read -r NAME ADDR DIST PHONE NCOURT OPEN CLOSE PIDX PEND <<< "$v"
  JAR=${JARS[$PIDX]}
  FBODY=$(printf '{"name":"%s","address":"%s","district":"%s","phone":"%s","description":"Cơ sở demo ở %s. Dữ liệu bịa phục vụ dự án local.","imageUrl":"%s","openTime":"%02d:00:00","closeTime":"%02d:00:00"}' \
    "$NAME" "$ADDR" "$DIST" "$PHONE" "$DIST" "$IMG" "$OPEN" "$CLOSE")
  FID=$(req POST "$API/api/facilities" "$JAR" "$FBODY" | extract_id)
  if [ -z "$FID" ]; then echo "FAIL facility $NAME"; idx=$((idx+1)); continue; fi
  FAC_IDS+=("$FID")
  [ "$PEND" = "1" ] && PENDING_IDS+=("$FID")

  for c in $(seq 1 "$NCOURT"); do
    CTYPE=${COURT_TYPES[$(( (c-1) % 4 ))]}
    CID=$(req POST "$API/api/courts" "$JAR" "{\"facilityId\":$FID,\"courtName\":\"Sân $c\",\"courtType\":\"$CTYPE\",\"status\":\"Active\"}" | extract_id)
    [ -z "$CID" ] && { echo "FAIL court $NAME #$c"; continue; }
    ADJ=$(( (idx % 3) * 10000 ))
    for s in "${SLOTS[@]}"; do
      IFS='|' read -r SN SS SE SP <<< "$s"
      req POST "$API/api/timeslots" "$JAR" \
        "{\"courtId\":$CID,\"slotName\":\"$SN ($(printf '%02d' $SS):00 - $(printf '%02d' $SE):00)\",\"startTime\":\"$(printf '%02d' $SS):00:00\",\"endTime\":\"$(printf '%02d' $SE):00:00\",\"price\":$((SP+ADJ)),\"isActive\":true}" >/dev/null
    done
  done
  echo "ok: $NAME (facility $FID)"
  idx=$((idx+1))
done

# 3. Admin duyệt các cơ sở (trừ cơ sở đang chờ)
ADMIN_JAR="$TMP/admin.jar"
sleep 8
echo "admin login: $(login "$ADMIN_EMAIL" "$ADMIN_PWD" "$ADMIN_JAR")"
APPROVED=0
for fid in "${FAC_IDS[@]}"; do
  skip=0; for p in "${PENDING_IDS[@]}"; do [ "$p" = "$fid" ] && skip=1; done
  [ $skip -eq 1 ] && continue
  req PATCH "$API/api/facilities/$fid/status" "$ADMIN_JAR" '{"status":"Approved"}' >/dev/null && APPROVED=$((APPROVED+1))
done
echo "approved $APPROVED facilities; pending ${#PENDING_IDS[@]}; created ${#FAC_IDS[@]}"
rm -rf "$TMP"
