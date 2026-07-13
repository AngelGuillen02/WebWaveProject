---
name: webwave
description: Enterprise development skill for the WebWave platform. Use for implementing, modifying, reviewing and optimizing .NET Web APIs, Angular components, DevExtreme interfaces, SQL Server scripts, Entity Framework repositories, DTOs, services and enterprise business logic. Keywords: WebWave, Angular, .NET, API, DevExtreme, SQL Server, Entity Framework, Repository, UnitOfWork, DTO, CRUD, REST, optimization, code review.
---

# PURPOSE

Provide enterprise-grade implementations for the WebWave platform following existing project conventions.

This skill assumes the project already contains an established architecture and its objective is to integrate new functionality without changing the existing design.

---

# TECHNOLOGY STACK

Always assume:

- ASP.NET Core
- C#
- Angular
- TypeScript
- DevExtreme
- SQL Server
- Entity Framework
- Repository Pattern
- UnitOfWork
- Dependency Injection

Do not introduce technologies outside this stack unless explicitly requested.

---

# IMPLEMENTATION RULES

Always:

- Reuse existing services.
- Reuse DTOs whenever possible.
- Follow the current folder structure.
- Follow existing naming conventions.
- Preserve project architecture.
- Produce production-ready code.

Never:

- Rewrite unrelated code.
- Change architecture.
- Rename classes unnecessarily.
- Create duplicate logic.
- Add unnecessary abstractions.

---

# API DEVELOPMENT

When implementing APIs:

- Validate all inputs.
- Return consistent responses.
- Reuse existing repositories.
- Use transactions when modifying multiple entities.
- Keep controllers thin.
- Place business logic inside services.
- Preserve REST conventions.

---

# ANGULAR DEVELOPMENT

When implementing Angular:

- Reuse existing services.
- Reuse existing models.
- Follow current component structure.
- Use DevExtreme controls already used in the project.
- Keep HTML consistent with other modules.

---

# DATABASE

When generating SQL:

- Preserve existing data.
- Avoid destructive operations.
- Prefer idempotent scripts.
- Validate foreign keys before inserts.
- Preserve referential integrity.

---

# PERFORMANCE

Prefer:

- Efficient LINQ queries.
- Single SaveChanges when possible.
- Batch operations.
- Reuse loaded entities.

Avoid:

- N+1 queries.
- Duplicate database calls.
- Unnecessary allocations.

---

# CODE REVIEW

When reviewing code:

1. Identify bugs.
2. Detect performance issues.
3. Detect architecture violations.
4. Suggest the smallest possible correction.
5. Preserve existing functionality.

---

# RESPONSE FORMAT

Default behavior:

- Return only the required implementation.
- Do not explain obvious code.
- Keep responses concise.
- Include explanations only when requested.

---

# EXAMPLES

## API

Input:

Implement an endpoint to assign branches to a user.

Output:

- DTOs
- Service implementation
- Controller endpoint
- Validation
- Repository usage

---

## Angular

Input:

Create a DevExtreme grid for branch assignments.

Output:

- HTML
- TypeScript
- Required bindings
- Minimal styling

---

## SQL

Input:

Generate a migration script.

Output:

- Safe SQL script
- Preserves data
- Validates existing objects

---

## Code Review

Input:

Review this service.

Output:

- Identified issues
- Minimal fixes
- Optimized implementation