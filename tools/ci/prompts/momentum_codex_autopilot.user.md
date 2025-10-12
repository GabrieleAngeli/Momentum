Pull Request metadata:
- Number: {{PR_NUMBER}}
- Title: {{PR_TITLE}}
- Author: {{PR_AUTHOR}}
- Base branch: {{BASE_BRANCH}}
- Head branch: {{HEAD_BRANCH}}
- URL: {{PR_HTML_URL}}

Pull Request body:
{{PR_BODY}}

Latest commit subjects:
{{COMMIT_SUBJECTS}}

Unified diff (`origin/{{BASE_BRANCH}}...HEAD`):
```
{{UNIFIED_DIFF}}
```

Repository context excerpts:
{{REPOSITORY_CONTEXT}}

Instructions:
- Infer the intent and scope of the change using the diff and supporting context.
- Document impacts across architecture, security, testing, observability, and module ownership.
- Suggest concrete documentation updates. Provide unified diffs for each update.
- Only return JSON matching the system prompt schema. No prose outside the JSON body.
