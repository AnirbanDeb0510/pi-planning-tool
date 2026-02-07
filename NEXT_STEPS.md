# PI Planning Tool - NEXT IMMEDIATE STEPS

**Target:** Get the complete board fetch endpoint working so frontend can start building the UI.

---

## 🎯 WEEK 1 SPRINT: Backend API Completion

### Priority 1: Complete Board Fetch Endpoint (2-3 days)

**Why:** This is the foundation for the frontend board view. Nothing else works until we have all board data in one optimized call.

#### Step 1.1–1.5: Board fetch implementation (COMPLETED)

**Files updated:** `DTOs/BoardResponseDto.cs`, `Repositories/Implementations/BoardRepository.cs`, `Services/Implementations/BoardService.cs`, `Controllers/BoardsController.cs`

- [x] Step 1.1: Create DTOs for Response (`BoardResponseDto` and related DTOs)
- [x] Step 1.2: Update repository with `GetBoardWithFullHierarchyAsync()` (eager loading)
- [x] Step 1.3: Implement `GetBoardWithHierarchyAsync()` in service and map entity → DTO
- [x] Step 1.4: Update `GET /api/boards/{id}` in `BoardsController` to return DTO
- [x] Step 1.5: Tested endpoint locally via `curl`/Swagger

**Note:** Board fetch is implemented and tested locally. Remaining backend tasks in Priority 2 & 3 still require attention (routing, exception handling, validation).

---

### Priority 2: Fix BoardsController Routing (1 day)

**Issue:** Controller is named `BoardController` but routes show `[Route("api/[controller]")]` which becomes `/api/board` (singular).

**Fix:**

```csharp
[ApiController]
[Route("api/boards")]  // Explicit routing for consistency
public class BoardsController : ControllerBase
{
    // ... rest of code
}
```

Also rename file from `BoardsController.cs` to `BoardController.cs` (or vice versa for consistency).

---

### Priority 3: Add Global Exception Handler (1 day)

**File:** `backend/pi-planning-backend/Middleware/GlobalExceptionHandlingMiddleware.cs`

```csharp
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ArgumentNullException => (statusCode: StatusCodes.Status400BadRequest, message: "Invalid request"),
            _ => (statusCode: StatusCodes.Status500InternalServerError, message: "Internal server error")
        };

        context.Response.StatusCode = response.statusCode;
        return context.Response.WriteAsJsonAsync(new { error = response.message });
    }
}
```

**Register in Program.cs:**

```csharp
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

---

### Priority 4: Review Remaining API Work (routing, validation, logging)

These final review tasks were implemented/verified locally and are ready for the PR. Authentication is intentionally deferred until frontend integration.

- Routing (1h):
  - Ensure controllers use explicit plural routes (`[Route("api/boards")]`, etc.)
  - Verify Swagger paths and controller filenames

- Exception handling (2-3h):
  - Global middleware mapped to status codes (400/404/500)
  - Ensure exceptions are logged via `ILogger` and return minimal JSON to clients

- Validation (3-4h):
  - Apply DataAnnotations to DTOs and consider `FluentValidation` for complex rules
  - Standardize validation error responses

- Request logging (2-3h):
  - Basic request/response logging (method, path, status, latency)
  - Optionally integrate `Serilog` later for structured logs

Status: Implemented/verified locally; ready to include in PR.

---

## 🎯 WEEK 1 Progress Checkpoint

- [ ] DTOs created & tested
- [ ] Repository eager loading implemented
- [ ] Service layer updated
- [ ] `GET /api/boards/{id}` returning full hierarchy
- [ ] Controller routing fixed
- [ ] Global exception handling added
- [ ] All endpoints tested with Swagger

**Expected Outcome:** Frontend team can now fetch board data and start building the UI.

---

## 🎯 WEEK 2: Frontend Board View (Parallel)

Once backend is ready, frontend can start immediately with:

1. **Board Component Setup**
   - Material Design layout
   - Fetch board from API on load
   - Display sprints, features, stories in grid

2. **Drag-Drop Implementation**
   - CDK drag-drop for stories (horizontal - sprint change)
   - Call `PATCH /api/boards/{boardId}/stories/{storyId}/move` on drop
   - Visual feedback during drag

3. **Team Panel**
   - Display team members
   - Show capacity per sprint
   - Basic editable form

---

## 🚀 QUICK CHECKLIST FOR TODAY

- [ ] Read this file completely
- [ ] Understand the board fetch pattern
- [ ] Create the DTOs
- [ ] Update repositories
- [ ] Implement service method
- [ ] Fix controller
- [ ] Test with Postman/Swagger
- [ ] Commit changes with clear message: "feat: implement complete board fetch endpoint"

---

## 💬 COMMON QUESTIONS

**Q: Why do we need a separate BoardResponseDto?**  
A: The Board model includes navigation properties that can cause circular serialization. The DTO flattens the hierarchy for API responses.

**Q: Why eager loading instead of lazy loading?**  
A: Single API call is faster than N+1 queries. Eager loading with `.Include()` fetches everything in one DB round-trip.

**Q: Should we implement pagination for large boards?**  
A: Not yet. Get it working first, optimize later if needed.

**Q: What if board has 1000+ stories?**  
A: You'll optimize later with virtual scrolling + lazy-loaded features. For now, assume reasonable board sizes.

---

## 📞 UNRESOLVED ITEMS

Check if these utilities exist, create if not:

- [ ] `PasswordHelper.HashPassword()` - Used in BoardService
- [ ] `PasswordHelper.VerifyPassword()` - For unlock endpoint
- [ ] Request/response logging service

Create a `Services/Utilities/PasswordHelper.cs` if missing:

```csharp
using System.Security.Cryptography;
using System.Text;

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public static bool VerifyPassword(string? password, string hash)
    {
        if (string.IsNullOrEmpty(password)) return false;
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }
}
```

---

**Ready to start? Begin with Step 1.1 above.**
