# PI Planning Tool - API Reference

**Version:** 1.0  
**Last Updated:** March 7, 2026  
**Base URL (Docker):** `http://localhost:8080`  
**Base URL (Local Dev):** `http://localhost:5262`

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Error Handling](#error-handling)
4. [Boards API](#boards-api)
5. [Features API](#features-api)
6. [User Stories API](#user-stories-api)
7. [Team API](#team-api)
8. [Azure DevOps Integration API](#azure-devops-integration-api)
9. [Real-Time Events (SignalR)](#real-time-events-signalr)
10. [Status Codes Reference](#status-codes-reference)

---

## Overview

The PI Planning Tool API is a RESTful service built with ASP.NET Core 8.0. It provides endpoints for managing PI planning boards, integrating with Azure DevOps, and supporting real-time collaboration via SignalR.

### Key Features

- **Board Management**: Create, retrieve, search, lock/unlock, finalize/restore boards
- **Azure DevOps Integration**: Fetch Features and User Stories from Azure Boards
- **Team Capacity Planning**: Manage team members and sprint capacities
- **Real-time Collaboration**: SignalR broadcasts for live updates across clients
- **Dual Database Support**: PostgreSQL and SQL Server

### API Conventions

- **Content-Type**: `application/json` for all requests and responses
- **Date Format**: ISO 8601 (`YYYY-MM-DDTHH:mm:ss.fffZ`)
- **ID Fields**: Integer primary keys
- **Null Handling**: Omit optional fields or use `null`

---

## Authentication

### Azure DevOps PAT (Personal Access Token)

**Required for Azure integration endpoints only.**

Pass as a query parameter:

```
GET /api/v1/azure/feature/{org}/{project}/{featureId}?pat=YOUR_PAT_HERE
```

**Scope Required:** `vso.work` (read permissions for Work Items)

**Storage:** PAT is temporarily cached server-side with configurable TTL (default: 10 minutes). Not persisted to database.

### Board Access Control

- No authentication required for board CRUD operations
- **Lock/Unlock**: Password-based protection (PBKDF2 hashing with salt)
- **Preview Endpoint**: Lightweight metadata access (`GET /api/boards/{id}/preview`)

---

## Error Handling

### Standard Error Response Format

```json
{
  "error": {
    "message": "Board is already locked",
    "timestamp": "2026-03-07T14:30:00.000Z"
  }
}
```

### Common Error Scenarios

| Status Code                 | Scenario                                    | Example Message                                   |
| --------------------------- | ------------------------------------------- | ------------------------------------------------- |
| `400 Bad Request`           | Validation failure, business rule violation | "Board is already locked"                         |
| `401 Unauthorized`          | Invalid password for lock/unlock            | "Invalid password"                                |
| `403 Forbidden`             | Board is locked, operation blocked          | "Board is locked and cannot be modified"          |
| `404 Not Found`             | Resource not found                          | "Board not found"                                 |
| `500 Internal Server Error` | Unexpected server error                     | "An error occurred while processing your request" |

### Validation Errors

Handled globally by `ValidateModelStateFilter`. Returns `400 Bad Request` with validation details:

```json
{
  "errors": {
    "Name": ["The Name field is required."],
    "NumSprints": ["NumSprints must be between 1 and 10."]
  },
  "title": "One or more validation errors occurred.",
  "status": 400
}
```

---

## Boards API

### Create Board

**`POST /api/boards`**

Creates a new PI planning board with auto-generated sprints.

**Request Body:**

```json
{
  "name": "PI 2024 Q2",
  "organization": "my-org",
  "project": "MyProject",
  "numSprints": 5,
  "sprintDuration": 2,
  "startDate": "2024-04-01T00:00:00Z",
  "devTestToggle": false,
  "password": "optional-lock-password"
}
```

**Fields:**

- `name` (required, string, 1-200 chars): Board display name
- `organization` (required, string): Azure DevOps organization
- `project` (required, string): Azure DevOps project
- `numSprints` (required, int, 1-10): Number of sprints to generate
- `sprintDuration` (required, int, 1-4 weeks): Duration per sprint
- `startDate` (required, datetime): PI start date
- `devTestToggle` (required, bool): `false` = show total points, `true` = split Dev/Test
- `password` (optional, string, 6-100 chars): Set lock password on creation

**Response:** `201 Created`

```json
{
  "id": 42,
  "name": "PI 2024 Q2",
  "organization": "my-org",
  "project": "MyProject",
  "numSprints": 5,
  "sprintDuration": 2,
  "startDate": "2024-04-01T00:00:00Z",
  "isLocked": false,
  "isFinalized": false,
  "devTestToggle": false,
  "createdAt": "2026-03-07T10:00:00.000Z"
}
```

**Headers:**

```
Location: /api/boards/42
```

---

### Get Board (Full Hierarchy)

**`GET /api/boards/{id}`**

Retrieves complete board data including sprints, features, user stories, and team members.

**Response:** `200 OK`

```json
{
  "id": 42,
  "name": "PI 2024 Q2",
  "organization": "my-org",
  "project": "MyProject",
  "numSprints": 5,
  "sprintDuration": 2,
  "startDate": "2024-04-01T00:00:00Z",
  "isLocked": false,
  "isFinalized": false,
  "devTestToggle": false,
  "createdAt": "2026-03-07T10:00:00.000Z",
  "sprints": [
    {
      "id": 101,
      "name": "Sprint 1",
      "startDate": "2024-04-01T00:00:00Z",
      "endDate": "2024-04-14T23:59:59Z",
      "order": 1
    }
  ],
  "features": [
    {
      "id": 201,
      "azureId": 12345,
      "title": "User Authentication",
      "priority": 1,
      "children": [
        {
          "id": 301,
          "azureId": 12346,
          "title": "Login Page",
          "assignedSprintId": 101,
          "storyPoints": 5,
          "storyPointsDev": 3,
          "storyPointsTest": 2
        }
      ]
    }
  ],
  "teamMembers": [
    {
      "id": 401,
      "name": "Alice Smith",
      "isDev": true,
      "isTest": false,
      "sprints": [
        {
          "sprintId": 101,
          "capacityDev": 40.0,
          "capacityTest": 0.0
        }
      ]
    }
  ]
}
```

**Error Responses:**

- `404 Not Found`: Board does not exist

---

### Search Boards

**`GET /api/boards`**

Search and filter boards by organization, project, and optional criteria.

**Query Parameters:**

- `organization` (required, string): Azure DevOps organization
- `project` (required, string): Azure DevOps project
- `search` (optional, string): Filter by board name (case-insensitive partial match)
- `isLocked` (optional, bool): Filter by lock status
- `isFinalized` (optional, bool): Filter by finalization status

**Example:**

```
GET /api/boards?organization=my-org&project=MyProject&search=Q2&isFinalized=false
```

**Response:** `200 OK`

```json
[
  {
    "id": 42,
    "name": "PI 2024 Q2",
    "organization": "my-org",
    "project": "MyProject",
    "isLocked": false,
    "isFinalized": false,
    "createdAt": "2026-03-07T10:00:00.000Z"
  }
]
```

---

### Get Board Preview

**`GET /api/boards/{id}/preview`**

Retrieve lightweight board metadata without loading full hierarchy (features, stories, team).

**Use Case:** Pre-access validation, board selection UI, checking lock/finalization status.

**Response:** `200 OK`

```json
{
  "id": 42,
  "name": "PI 2024 Q2",
  "organization": "my-org",
  "project": "MyProject",
  "isLocked": true,
  "isFinalized": false,
  "createdAt": "2026-03-07T10:00:00.000Z"
}
```

---

### Validate Board for Finalization

**`GET /api/boards/{id}/validate-finalization`**

Returns warnings if board has unassigned stories or capacity issues (does not block finalization).

**Response:** `200 OK`

```json
[
  "3 user stories are not assigned to any sprint",
  "Sprint 2 has 0 team members assigned"
]
```

**Empty Array:** Board is ready for finalization without warnings.

---

### Finalize Board

**`PATCH /api/boards/{id}/finalize`**

Marks board as finalized. Visual indicators applied to stories moved after finalization. Stories can still be reassigned, but UI shows "moved after finalization" badge.

**Request Body:** Empty `{}`

**Response:** `200 OK`

```json
{
  "success": true,
  "message": "Board finalized with 2 warning(s)",
  "board": {
    "id": 42,
    "name": "PI 2024 Q2",
    "isLocked": false,
    "isFinalized": true,
    "createdAt": "2026-03-07T10:00:00.000Z"
  },
  "warnings": ["3 user stories are not assigned to any sprint"],
  "finalizedAt": "2026-03-07T14:30:00.000Z",
  "timestamp": "2026-03-07T14:30:00.000Z"
}
```

**Error Responses:**

- `400 Bad Request`: Validation failed (cannot finalize)
- `403 Forbidden`: Board is locked
- `404 Not Found`: Board does not exist

**SignalR Broadcast:** `BoardFinalized` event sent to all connected clients on this board.

---

### Restore Board

**`PATCH /api/boards/{id}/restore`**

Reverts finalization status. Clears "moved after finalization" flags on stories.

**Request Body:** Empty `{}`

**Response:** `200 OK`

```json
{
  "success": true,
  "message": "Board restored - editing is now allowed",
  "board": {
    "id": 42,
    "name": "PI 2024 Q2",
    "isLocked": false,
    "isFinalized": false,
    "createdAt": "2026-03-07T10:00:00.000Z"
  },
  "timestamp": "2026-03-07T14:35:00.000Z"
}
```

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Board does not exist

**SignalR Broadcast:** `BoardRestored` event sent to all connected clients.

---

### Lock Board

**`PATCH /api/boards/{id}/lock`**

Locks board with password protection. Prevents all mutation operations (403 Forbidden) until unlocked.

**Request Body:**

```json
{
  "password": "secure-password-123"
}
```

**Response:** `200 OK`

```json
{
  "success": true,
  "message": "Board locked successfully",
  "board": {
    "id": 42,
    "name": "PI 2024 Q2",
    "isLocked": true,
    "isFinalized": false,
    "createdAt": "2026-03-07T10:00:00.000Z"
  },
  "timestamp": "2026-03-07T15:00:00.000Z"
}
```

**Error Responses:**

- `400 Bad Request`: Board is already locked
- `401 Unauthorized`: Invalid password (if board has existing password)
- `404 Not Found`: Board does not exist

**Password Storage:** PBKDF2-HMAC-SHA256 with 10,000 iterations, 32-byte salt, 32-byte hash.

**SignalR Broadcast:** `BoardLockStateChanged` event with `isLocked: true`.

---

### Unlock Board

**`PATCH /api/boards/{id}/unlock`**

Unlocks board with password verification. Restores mutation capabilities.

**Request Body:**

```json
{
  "password": "secure-password-123"
}
```

**Response:** `200 OK`

```json
{
  "success": true,
  "message": "Board unlocked successfully",
  "board": {
    "id": 42,
    "name": "PI 2024 Q2",
    "isLocked": false,
    "isFinalized": false,
    "createdAt": "2026-03-07T10:00:00.000Z"
  },
  "timestamp": "2026-03-07T15:05:00.000Z"
}
```

**Error Responses:**

- `400 Bad Request`: Board is not locked
- `401 Unauthorized`: Invalid password
- `404 Not Found`: Board does not exist

**SignalR Broadcast:** `BoardLockStateChanged` event with `isLocked: false`.

---

## Features API

### Import Feature from Azure

**`POST /api/v1/boards/{boardId}/features/import`**

Imports a Feature (with child User Stories) from Azure DevOps into the board.

**Request Body:**

```json
{
  "azureId": 12345,
  "title": "User Authentication",
  "priority": 1,
  "children": [
    {
      "azureId": 12346,
      "title": "Login Page",
      "storyPoints": 5,
      "storyPointsDev": 3,
      "storyPointsTest": 2
    }
  ]
}
```

**Response:** `201 Created`

```json
{
  "id": 201,
  "azureId": 12345,
  "title": "User Authentication",
  "priority": 1,
  "children": [
    {
      "id": 301,
      "azureId": 12346,
      "title": "Login Page",
      "assignedSprintId": null,
      "storyPoints": 5,
      "storyPointsDev": 3,
      "storyPointsTest": 2
    }
  ]
}
```

**Behavior:**

- If Feature with same `azureId` exists, updates existing and merges children
- User Stories assigned to "Placeholder" sprint (`assignedSprintId: null`)
- Sets `priority` to `maxPriority + 1` for new features

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Board does not exist

**Header:**

```
Location: /api/v1/boards/{boardId}/features/201
```

**SignalR Broadcast:** `FeatureImported` event.

---

### Refresh Feature from Azure

**`PATCH /api/v1/boards/{boardId}/features/{id}/refresh`**

Re-fetches Feature and children from Azure DevOps, updating local data.

**Query Parameters:**

- `organization` (required, string)
- `project` (required, string)
- `pat` (required, string): Azure DevOps PAT

**Example:**

```
PATCH /api/v1/boards/42/features/201/refresh?organization=my-org&project=MyProject&pat=YOUR_PAT
```

**Response:** `200 OK` (same structure as Import Feature response)

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Feature not found or not in Azure

**SignalR Broadcast:** `FeatureRefreshed` event.

---

### Reorder Features

**`PATCH /api/v1/boards/{boardId}/features/reorder`**

Updates priority order for multiple features atomically.

**Request Body:**

```json
{
  "features": [
    { "featureId": 201, "newPriority": 1 },
    { "featureId": 202, "newPriority": 2 },
    { "featureId": 203, "newPriority": 3 }
  ]
}
```

**Response:** `204 No Content`

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Board does not exist

**SignalR Broadcast:** `FeaturesReordered` event.

---

### Delete Feature

**`DELETE /api/v1/boards/{boardId}/features/{id}`**

Deletes a feature and all its child User Stories from the board.

**Response:** `204 No Content`

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Feature does not exist

**SignalR Broadcast:** `FeatureDeleted` event.

---

## User Stories API

### Move Story to Sprint

**`PATCH /api/boards/{boardId}/stories/{storyId}/move`**

Assigns a User Story to a specific sprint (or Placeholder with `null`).

**Request Body:**

```json
{
  "targetSprintId": 101
}
```

**Use `null` for Placeholder:**

```json
{
  "targetSprintId": null
}
```

**Response:** `204 No Content`

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Story or sprint not found

**SignalR Broadcast:** `StoryMoved` event.

---

### Refresh Story from Azure

**`PATCH /api/boards/{boardId}/stories/{storyId}/refresh`**

Re-fetches User Story details from Azure DevOps.

**Query Parameters:**

- `organization` (required, string)
- `project` (required, string)
- `pat` (required, string)

**Response:** `200 OK`

```json
{
  "id": 301,
  "azureId": 12346,
  "title": "Login Page (Updated)",
  "assignedSprintId": 101,
  "storyPoints": 8,
  "storyPointsDev": 5,
  "storyPointsTest": 3
}
```

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Story not found

**SignalR Broadcast:** `StoryRefreshed` event.

---

## Team API

### Get Team Members

**`GET /api/boards/{boardId}/team`**

Retrieves all team members with sprint capacity allocations.

**Response:** `200 OK`

```json
[
  {
    "id": 401,
    "name": "Alice Smith",
    "isDev": true,
    "isTest": false,
    "sprints": [
      {
        "sprintId": 101,
        "capacityDev": 40.0,
        "capacityTest": 0.0
      },
      {
        "sprintId": 102,
        "capacityDev": 40.0,
        "capacityTest": 0.0
      }
    ]
  }
]
```

---

### Add Team Member

**`POST /api/boards/{boardId}/team`**

Adds a new team member to the board with default capacities for all sprints.

**Request Body:**

```json
{
  "name": "Bob Johnson",
  "isDev": true,
  "isTest": true
}
```

**Fields:**

- `name` (required, string, 1-100 chars)
- `isDev` (required, bool): Developer role
- `isTest` (required, bool): Tester role

**Auto-Capacity Calculation:**

- Dev capacity: `sprintDuration * 40 hours * devWeightFactor` if `isDev = true`
- Test capacity: `sprintDuration * 40 hours * testWeightFactor` if `isTest = true`

**Response:** `200 OK`

```json
{
  "id": 402,
  "name": "Bob Johnson",
  "isDev": true,
  "isTest": true,
  "sprints": [
    {
      "sprintId": 101,
      "capacityDev": 80.0,
      "capacityTest": 80.0
    }
  ]
}
```

**Error Responses:**

- `403 Forbidden`: Board is locked

**SignalR Broadcast:** `TeamMemberAdded` event.

---

### Update Team Member

**`PUT /api/boards/{boardId}/team/{teamMemberId}`**

Updates team member details (name, isDev, isTest). Re-calculates default capacities.

**Request Body:**

```json
{
  "name": "Bob J. Johnson",
  "isDev": true,
  "isTest": false
}
```

**Response:** `200 OK` (same structure as Add Team Member)

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Team member not found

**SignalR Broadcast:** `TeamMemberUpdated` event.

---

### Delete Team Member

**`DELETE /api/boards/{boardId}/team/{teamMemberId}`**

Removes team member and all associated sprint capacity records.

**Response:** `204 No Content`

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Team member not found

**SignalR Broadcast:** `TeamMemberDeleted` event.

---

### Update Sprint Capacity

**`PATCH /api/boards/{boardId}/team/{teamMemberId}/sprints/{sprintId}`**

Adjusts a team member's capacity for a specific sprint.

**Request Body:**

```json
{
  "capacityDev": 32.0,
  "capacityTest": 16.0
}
```

**Response:** `200 OK`

```json
{
  "sprintId": 101,
  "capacityDev": 32.0,
  "capacityTest": 16.0
}
```

**Error Responses:**

- `403 Forbidden`: Board is locked
- `404 Not Found`: Team member or sprint not found

**SignalR Broadcast:** `CapacityUpdated` event.

---

## Azure DevOps Integration API

### Fetch Feature from Azure Boards

**`GET /api/v1/azure/feature/{organization}/{project}/{featureId}`**

Retrieves a Feature and its child User Stories directly from Azure DevOps.

**Query Parameters:**

- `pat` (required, string): Azure DevOps Personal Access Token

**Example:**

```
GET /api/v1/azure/feature/my-org/MyProject/12345?pat=YOUR_PAT_HERE
```

**Response:** `200 OK`

```json
{
  "azureId": 12345,
  "title": "User Authentication",
  "priority": 0,
  "children": [
    {
      "azureId": 12346,
      "title": "Login Page",
      "storyPoints": 5,
      "storyPointsDev": 3,
      "storyPointsTest": 2
    }
  ]
}
```

**Error Responses:**

- `400 Bad Request`: Invalid PAT or feature not found in Azure
- `401 Unauthorized`: PAT lacks required permissions
- `500 Internal Server Error`: Azure API error

**Note:** This endpoint does NOT save data to the database. Use `/api/v1/boards/{boardId}/features/import` to persist.

---

## Real-Time Events (SignalR)

**WebSocket URL:** `ws://localhost:8080/planningHub`

### Connection

**Client Library:** `@microsoft/signalr` (JavaScript/TypeScript)

**Example Connection:**

```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:8080/planningHub")
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Join Board Room

**Method:** `JoinBoard`

**Invoke:**

```typescript
await connection.invoke("JoinBoard", boardId, userName);
```

**Parameters:**

- `boardId` (int): Board ID to join
- `userName` (string): Display name for presence

**Effect:** Client joins SignalR group `Board_{boardId}` and receives real-time updates.

---

### Leave Board Room

**Method:** `LeaveBoard`

**Invoke:**

```typescript
await connection.invoke("LeaveBoard", boardId);
```

---

### Send Cursor Update

**Method:** `UpdateCursor`

**Invoke:**

```typescript
await connection.invoke("UpdateCursor", boardId, cursorX, cursorY);
```

**Parameters:**

- `boardId` (int)
- `cursorX` (double): X coordinate (percentage or pixels)
- `cursorY` (double): Y coordinate

**Throttle:** Client should throttle to ~15Hz (66ms interval) to avoid flooding.

---

### Received Events

All mutation operations broadcast events to other clients. Subscribe with `.on()`:

```typescript
connection.on("FeatureImported", (data) => {
  console.log("New feature:", data.Feature);
  // Update UI
});

connection.on("StoryMoved", (data) => {
  console.log(`Story ${data.StoryId} moved to Sprint ${data.TargetSprintId}`);
  // Update board state
});

connection.on("BoardLockStateChanged", (data) => {
  if (data.IsLocked) {
    // Disable all editing UI
  } else {
    // Re-enable editing
  }
});

connection.on("CursorUpdate", (data) => {
  // Render remote cursor at (data.X, data.Y)
  renderCursor(data.UserName, data.X, data.Y);
});
```

**Event List:**

- `FeatureImported`
- `FeatureRefreshed`
- `FeaturesReordered`
- `FeatureDeleted`
- `StoryMoved`
- `StoryRefreshed`
- `TeamMemberAdded`
- `TeamMemberUpdated`
- `TeamMemberDeleted`
- `CapacityUpdated`
- `BoardFinalized`
- `BoardRestored`
- `BoardLockStateChanged`
- `CursorUpdate`

**Payload Structure:** Each event includes `BoardId`, `TimestampUtc`, and resource-specific fields.

**Initiator Exclusion:** Events are NOT sent back to the originating client (based on `X-SignalR-ConnectionId` header).

---

## Status Codes Reference

| Code                        | Meaning                                   | Common Scenario                              |
| --------------------------- | ----------------------------------------- | -------------------------------------------- |
| `200 OK`                    | Request successful                        | GET, PATCH with response body                |
| `201 Created`               | Resource created                          | POST board, POST feature                     |
| `204 No Content`            | Success, no response body                 | PATCH, DELETE operations                     |
| `400 Bad Request`           | Validation error, business rule violation | Missing required field, board already locked |
| `401 Unauthorized`          | Authentication failed                     | Invalid lock/unlock password                 |
| `403 Forbidden`             | Operation not allowed                     | Board is locked, mutation blocked            |
| `404 Not Found`             | Resource does not exist                   | Board, Feature, Story, Team Member not found |
| `500 Internal Server Error` | Unexpected error                          | Database failure, Azure API error            |

---

## Request Headers

### Standard Headers

```
Content-Type: application/json
Accept: application/json
```

### SignalR Integration Header

**Optional:** `X-SignalR-ConnectionId`

**Purpose:** Prevents echoing broadcast events back to the initiating client.

**Usage:**

```typescript
const connectionId = connection.connectionId;

fetch("/api/boards/42/stories/301/move", {
  method: "PATCH",
  headers: {
    "Content-Type": "application/json",
    "X-SignalR-ConnectionId": connectionId,
  },
  body: JSON.stringify({ targetSprintId: 101 }),
});
```

---

## Rate Limiting

**Current Status:** No rate limiting implemented.

**Recommendation:** For production deployment, consider:

- API Gateway rate limiting (e.g., Azure API Management, AWS API Gateway)
- ASP.NET Core middleware: `AspNetCoreRateLimit` NuGet package
- Suggested limits: 100 req/min per IP for mutations, 1000 req/min for reads

---

## Support & Troubleshooting

### Common Issues

**CORS Errors:**

- Ensure frontend origin is in `appsettings.json` → `Cors:AllowedOrigins`
- Default: `["*"]` (allow all, for development only)

**SignalR Connection Fails:**

- Check WebSocket support in reverse proxy (Nginx, IIS)
- Verify `app.MapHub<PlanningHub>("/planningHub")` in `Program.cs`

**Azure PAT Invalid:**

- Verify PAT has `vso.work` (read) scope
- Check PAT expiration in Azure DevOps user settings

**Board Mutations Return 403:**

- Board is locked - check `GET /api/boards/{id}/preview` → `isLocked`
- Unlock with `PATCH /api/boards/{id}/unlock`

### Swagger UI

**Local Development:** `http://localhost:5262/swagger`  
**Docker:** `http://localhost:8080/swagger` (if Swagger enabled in Production)

**Note:** Swagger is disabled by default in Production. Enable via `appsettings.json`:

```json
{
  "Swagger": {
    "Enabled": true
  }
}
```

---

## Change Log

### Version 1.0 (March 2026)

- Initial API documentation
- Covers all endpoints from Phase 7 completion (Board Lock/Unlock feature)
- Real-time collaboration via SignalR documented
- Dual-provider database support (PostgreSQL + SQL Server)

---

**For architecture details, see:** [ARCHITECTURE.md](ARCHITECTURE.md)  
**For deployment guides:**

- Docker: [DOCKER_DEPLOYMENT_GUIDE.md](DOCKER_DEPLOYMENT_GUIDE.md)
- Windows IIS: [IIS_DEPLOYMENT_GUIDE.md](IIS_DEPLOYMENT_GUIDE.md)

**For user guide:** [USER_GUIDE.md](USER_GUIDE.md)
