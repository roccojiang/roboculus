#!/usr/bin/env bash
# Above line to select appropriate scripting language.

# regex to validate in commit msg
commit_regex='(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert):.+'

# error message printed if commit_regex is not satisfied
error_msg="Aborting commit.
Your commit should start with a commit tag followed by a colon.
Example of a valid commit: \"fix: bug fixes in main.c\""

# If condition to check if commit_regex is statisfied
if ! grep -iqE "$commit_regex" "$1"; then
    echo "$error_msg" >&2
    exit 1
fi

