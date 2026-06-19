# Guardrails Review — 2026-06-18 (User Aggregate)

**Commit:** `0b6158d18250176a8a1a4e67c41c4771c2128ebf`
**Trigger:** Automatic post-commit
**Changed files reviewed:** 18

---

## Summary

| Dimension      | Status    | Open Issues                              |
|----------------|-----------|------------------------------------------|
| DDD Compliance | VIOLATION | 2 new violations (V009, V010)            |
| Security       | CRITICAL  | 12 total (5C / 3H / 4M / 0L) — 3 new    |
| **Overall**    | **FAIL**  |                                          |

---

## Critical Blockers (must fix before merge/deploy)

1. **[S010]** All User mutation endpoints lack `.RequireAuthorization()` — `UserEndpoints.cs:20-51`
2. **[V009]** `CreateUserCommandHandler` is a stub — returns `Guid.NewGuid()` without calling `User.Create()` or persisting — `CreateUserCommandHandler.cs:12`
3. **[S011]** Exception message leaked in HTTP 404 on UpdateAsync — `UserEndpoints.cs:93`
4. **[S012]** Exception message leaked in HTTP 404 on DeleteAsync — `UserEndpoints.cs:109`

---

## DDD Compliance Findings

### Aggregate Layer Results

| Aggregate | Domain | Application | Infrastructure.Data | Infrastructure.Integration | Presentation |
|-----------|--------|-------------|---------------------|----------------------------|--------------|
| User      | PASS   | VIOLATION   | PASS                | PASS                       | VIOLATION    |

### New Violations

**V009 — Rules A3 + A4 — `src/Application/Commands/CreateUser/CreateUserCommandHandler.cs:12`**
Handler returns `Guid.NewGuid()` as a stub. No aggregate constructed, no domain event raised, nothing persisted.

Fix:
```csharp
public async Task<Guid> Handle(CreateUserCommand command, CancellationToken cancellationToken)
{
    var user = User.Create(command.Name, command.Email /*, other props */);
    await _userRepository.AddAsync(user, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    return user.Id.Value;
}
```

**V010 — Rule P1 — `src/Presentation/Endpoints/UserEndpoints.cs:115`**
`UpdateUserRequest` declared inside endpoint file. Move to `src/Application/Commands/UpdateUser/UpdateUserRequest.cs`.

---

## Security Findings — New This Commit

**S010 — Critical — `UserEndpoints.cs:20-51`**
`MapGroup` for `/api/v1/users` has no `.RequireAuthorization()`. POST, PUT, DELETE are publicly accessible.

Fix:
```csharp
var group = app.MapGroup("/api/v1/users")
               .WithTags("Users")
               .RequireAuthorization();
```

**S011 — High — `UserEndpoints.cs:93-95`**
`UpdateAsync` returns `TypedResults.NotFound(new { ex.Message })` — domain exception text leaks.

Fix:
```csharp
catch (UserNotFoundException)
    return TypedResults.Problem(statusCode: 404, title: "User not found.");
```

**S012 — High — `UserEndpoints.cs:109-111`**
`DeleteAsync` returns `TypedResults.NotFound(new { ex.Message })` — same issue.

Fix: same pattern as S011.

---

## Carry-over Findings (unchanged)

S001–S009 all remain OPEN. See `security-reviewer-memory.md` for details and cached fix suggestions.

---

## Recommended Fix Order

1. [S010] Add `.RequireAuthorization()` to User route group
2. [S001] Remove hardcoded DB password from appsettings.json
3. [S002] Fix placeholder JWT Authority
4. [S003] Add auth to CatalogProduct endpoints
5. [S004] Add auth to Entity endpoints
6. [V009] Implement `CreateUserCommandHandler` properly
7. [S011] Fix exception leak in `UpdateAsync`
8. [S012] Fix exception leak in `DeleteAsync`
9. [S005] Fix exception leak in `CatalogProductEndpoints.cs`
10. [V010] Move `UpdateUserRequest` to Application layer
11. [S006–S008] Rate limiter, UseExceptionHandler, AllowedHosts
12. [V001–V003] Remove `Class1.cs` placeholder files
13. [V006] Move `UpdateCatalogProductRequest` out of `CatalogProductEndpoints.cs`
