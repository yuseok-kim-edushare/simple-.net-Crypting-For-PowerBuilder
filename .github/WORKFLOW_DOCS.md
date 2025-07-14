# GitHub Actions Workflow Documentation

This document explains the automated workflow chain for dependency updates and releases.

## Workflow Chain

### 1. Dependabot (`dependabot.yaml`)
- **Trigger**: Daily at 12:00 Asia/Seoul for NuGet packages, 09:00 UTC for GitHub Actions
- **Purpose**: Creates PRs for dependency updates
- **Output**: Creates PRs with `dependencies` label

### 2. Auto-Merge (`auto-merge.yaml`)
- **Trigger**: When PRs are opened, reopened, or synchronized
- **Purpose**: Enables auto-merge for dependabot PRs after CI tests pass
- **Conditions**: 
  - Only runs for PRs created by `dependabot[bot]`
  - Waits for PR to become mergeable (all checks pass)
  - Uses SQUASH merge method
- **Output**: Enables auto-merge on the PR

### 3. CI Tests (`ci.yaml`)
- **Trigger**: PR to main branch or push to main branch
- **Purpose**: Runs automated tests to ensure code quality
- **Platform**: Windows 2022 (required for .NET Framework 4.8.1 projects)
- **Process**:
  - Restores NuGet packages with caching
  - Builds solution
  - Runs tests

### 4. Continuous Deployment (`cd.yaml`)
- **Trigger**: 
  - Push to main branch (direct merges)
  - CI workflow completion on main branch (for auto-merged PRs)
- **Purpose**: Creates releases with built artifacts
- **Platform**: Windows 2022 (required for ILMerge and Windows-specific features)
- **Process**:
  - Builds release version of all projects
  - Merges dependencies using ILMerge and ILRepack
  - Creates GitHub release with version increment
  - Uploads build artifacts

## Expected Flow

```
Dependabot creates PR → Auto-merge enabled → CI tests run → PR auto-merged → CD workflow triggered → Release created
```

## Key Features

- **Automatic dependency updates**: Dependabot handles NuGet and GitHub Actions updates
- **Quality gates**: Auto-merge only happens after CI tests pass
- **Automatic releases**: Every successful merge to main creates a new release
- **Build artifacts**: Releases include merged DLLs for PowerBuilder integration

## Troubleshooting

### Auto-merge not working
- Check if PR is from dependabot[bot]
- Verify CI tests are passing
- Check auto-merge workflow logs for timeout or errors

### CD workflow not triggering
- Check if push event occurred to main branch
- Verify CI workflow completed successfully
- Review CD workflow conditions and triggers

### Build failures
- Ensure solution file references are correct
- Check that all dependencies are restored
- Verify Windows-specific features are properly conditioned