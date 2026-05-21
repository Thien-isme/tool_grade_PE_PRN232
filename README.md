# API Runner & Autograder — PE Paper No. 5

Công cụ chấm bài thực hành PRN232 (Paper 5): **Q1 = 5 điểm**, **Q2 = 5 điểm**, tổng **10 điểm**.

## Chạy tool

1. Cài .NET SDK và Node.js
2. Chạy `ChayAPI.bat` hoặc `run_tools.ps1`
3. Mở http://localhost:5173

## Cấu trúc bài nộp (mỗi sinh viên 1 thư mục con)

```
ThưMucChamBai/
  SV001/
    Q1_SV001/     ← Web API (.NET 8, EF Core)
    Q2_SV001/     ← MVC/Razor + HttpClient
```

## Chấm PE Paper 5

1. Chọn thư mục cha → **Chạy Tất Cả Dự Án** (khởi động API Q1)
2. **Chấm tất cả** hoặc chọn từng SV → **Chấm SV đang chọn**
3. **Export CSV** để xuất điểm lớp

### Q1 — 5 điểm (API chạy trên port của SV)

| Mã | Điểm | Nội dung |
|----|------|----------|
| Q1-PRE | 0.5 | `ConnectionStrings:MyCnn` trong appsettings |
| Q1-B1 | 1.0 | GET `/api/students` + schema |
| Q1-C1 | 1.0 | GET `/api/student-performance` hợp lệ |
| Q1-C3 | 0.5 | page=-1 → 400 |
| Q1-D1 | 1.0 | PUT grade hợp lệ |
| Q1-D2 | 0.5 | PUT grade=15 → 400 |
| Q1-E2 | 0.5 | DELETE enrollment 9 → 400 |

### Q2 — 5 điểm (kiểm tra mã nguồn tĩnh)

| Mã | Điểm | Nội dung |
|----|------|----------|
| Q2-PROJ | 0.5 | Có project MVC/Razor |
| Q2-URL | 1.0 | `GivenAPIBaseUrl` = http://localhost:5100 |
| Q2-HTTP | 1.0 | Dùng `HttpClient` |
| Q2-API | 1.5 | Gọi schedules/search + courses |
| Q2-HTML | 1.0 | input/select/textarea có `id` (≥80%) |

## API chấm điểm

- `POST /api/batch/pe5/grade/{studentName}`
- `POST /api/batch/pe5/grade-all`
- `POST /api/batch/pe5/export` → file CSV
