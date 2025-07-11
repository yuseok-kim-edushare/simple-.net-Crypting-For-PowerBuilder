[auto-merge](.github/workflows/auto-merge.yaml)

is automatically merging the pull request when the build is successful.

but, not triggered CD

and github Copilot suggest to me,

4. Check Repository Settings
Additionally, check these settings in your repository:

Go to Settings → Actions → General
Ensure "Allow GitHub Actions to create and approve pull requests" is enabled
Make sure "Workflow permissions" is set to "Read and write permissions"
In Settings → Branches → Branch protection rules for main branch:
Ensure "Require status checks to pass before merging" doesn't block your workflow
If you have "Restrict who can push to matching branches", make sure GitHub Actions is allowed








