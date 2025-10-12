const ignoreLeadingOrTrailingWhitespace = (message) => message.trim() !== message;

export default {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      ['feat', 'fix', 'docs', 'style', 'refactor', 'perf', 'test', 'build', 'ci', 'chore', 'revert']
    ],
    'scope-empty': [0, 'never'],
    'subject-case': [0, 'never'],
    'header-max-length': [2, 'always', 120]
  },
  ignores: [
    ignoreLeadingOrTrailingWhitespace,
    (commit) => commit.startsWith('Merge '),
    (commit) => commit.startsWith('Revert "')
  ]
};
