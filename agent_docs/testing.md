# FMS Testing Strategy

## Backend (.NET)

### Unit Tests
- **Framework:** xUnit + FluentAssertions + NSubstitute
- **Coverage Target:** 80% business logic, 100% domain rules
- **Location:** `tests/Unit/`
- **Patterns:**
  - One test class per handler/command
  - Mock repositories with NSubstitute
  - Test naming: `{MethodName}_{Scenario}_{ExpectedResult}`

### Integration Tests
- **Framework:** WebApplicationFactory + TestContainers
- **Coverage:** API endpoints, database queries, auth flow
- **Location:** `tests/Integration/`
- **Patterns:**
  - Spin up PostgreSQL + MongoDB in Docker for each test suite
  - Use `WebApplicationFactory` with `TestAuthHandler`
  - Clean database between tests

### Architecture Tests
- **Framework:** NetArchTest
- **Rules:**
  - Domain layer does not reference Infrastructure
  - Commands/queries only depend on Domain
  - Controllers only depend on Application

## Frontend (Angular)

### Unit Tests
- **Framework:** Jasmine + Karma
- **Coverage Target:** 70% components, 80% services
- **Location:** `*.spec.ts` alongside source files

### E2E Tests
- **Framework:** Playwright
- **Coverage:** Critical user journeys
- **Location:** `e2e/`
- **Scenarios:**
  - Tenant onboarding flow
  - Login → Dashboard → Vehicle Directory
  - Dynamic table customization
  - White-label theme switching
  - Device provisioning flow

## Load Testing
- **Tool:** k6 (free tier)
- **Scenarios:**
  - 1000 concurrent MQTT connections
  - 500 req/s API load
  - SignalR real-time message burst
